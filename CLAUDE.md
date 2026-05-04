# Aidoc2025 — Unity 프로젝트 Claude 컨텍스트

이 파일은 Claude(또는 다른 AI 코딩 도구)가 이 Unity 프로젝트에서 작업할 때 참고하는 컨텍스트입니다. **한국어로 응답**하고, Unity 게임 개발 컨벤션을 우선합니다.

## 프로젝트 개요

**Aidoc2025** — `solidosis_patient` Flutter 앱에 임베드되는 Unity 게임/시각화 모듈. 카메라 기반 자세/동작 인식(MediaPipe)으로 환자의 운동을 평가.

- Unity 버전: **2022.3.61f1** (Editor: 6c53ebaf375d)
- 임베딩: `flutter_embed_unity` 패키지의 표준 위치(`<flutter_root>/unity/Aidoc2025`)
- 부모 Flutter 앱: `../../` = `solidosis_patient/`
- 사용자: 강찬형 (rkdcksgud1@gmail.com)

## 기술/패키지

- **MediaPipeUnity** (`com.github.homuler.mediapipe`) — 자세 추적 코어
- **CartoonVFX9X** — 게임 이펙트
- **TextMesh Pro**, **XR**, **URP** 일부
- **flutter_embed_unity** 패키지 자산:
  - `Assets/FlutterEmbed/` — Editor 스크립트, SendToFlutter 브리지
  - `Assets/FlutterUnityIntegration/` — 위젯 통합

## 디렉터리 구조

```
Aidoc2025/
├── Assets/
│   ├── Scripts/                      **게임 로직 (C#)**
│   │   ├── GetFlutterMessage.cs       Flutter→Unity 수신 (Solution GameObject)
│   │   ├── BlockGame.cs / Block.cs / BlockGameTrigger.cs
│   │   ├── PairGame.cs / PairObject.cs / PairGrid.cs / PairGameTrigger.cs
│   │   ├── Calibration.cs / CalibrationPoint.cs
│   │   ├── PointsController.cs / ShowScoreImages.cs
│   │   ├── PauseTimeChecker.cs / Timer.cs
│   │   ├── Result.cs / SaveLandmarkDatas.cs
│   │   ├── StartScreen.cs / StaticData.cs
│   │   └── ...
│   ├── FlutterEmbed/
│   │   ├── SendToFlutter/SendToFlutter.cs   **Unity→Flutter 송신**
│   │   └── Editor/ProjectExporterIos.cs
│   ├── FlutterUnityIntegration/
│   ├── MediaPipeUnity/                MediaPipe 통합 자산
│   ├── Scene/Main.unity               **유일한 씬**
│   ├── Prefebs/                       (오타지만 그대로 유지: Prefebs)
│   ├── Resources/, Sounds/, Images/, Fonts/, Plugins/
│   ├── StreamingAssets/, XR/
│   ├── CartoonVFX9X/                  VFX 에셋
│   └── Example/                       샘플
├── Packages/
│   └── com.github.homuler.mediapipe/  MediaPipe 패키지 (LFS 추적: .aar, .dylib)
├── ProjectSettings/                   Unity 프로젝트 세팅 (Force Text 직렬화 권장)
├── mediapipe_api/, third_party/, docker/  네이티브/빌드 부속
├── Library/, Logs/, Temp/, UserSettings/   (gitignore — 추적 안 함)
└── .gitignore, .gitattributes
```

## Flutter ↔ Unity 통신 구조

### Unity → Flutter (`Assets/FlutterEmbed/SendToFlutter/SendToFlutter.cs`)

```csharp
SendToFlutter.Send("scene_loaded");          // 씬 로드 완료 알림
SendToFlutter.Send("{\"command\":\"ack\", \"reason\":\"start\"}");  // 메시지 수신 응답
SendToFlutter.Send("{\"command\":\"result\", \"reason\":\"finish|background\"}"); // 운동 종료
```

내부적으로:
- Android: `AndroidJavaClass("com.learntoflutter.flutter_embed_unity_android.messaging.SendToFlutter")`의 static 메서드 호출
- iOS: `[DllImport("__Internal")] FlutterEmbedUnityIos_sendToFlutter(string)`

### Flutter → Unity (`Assets/Scripts/GetFlutterMessage.cs`)

Flutter 측에서:
```dart
sendToUnity('Solution', 'RecieveMessage', message);
```
- 대상 **GameObject 이름**: `Solution` (씬 `Assets/Scene/Main.unity`에 존재 — fileID 1064799459)
- **메서드 이름**: `RecieveMessage(string)` (오타지만 양쪽 일치하므로 동작)
- 메시지 포맷: `"start,time:60.0,level:3"`, `"continue,time:..."`, `"pause"`, `"background"`

`Solution` GameObject에 붙어 있는 컴포넌트 (Main.unity 라인 35660~35790 영역):
- BaseRunner (MediaPipe)
- PoseLandmarkerResultAnnotationController
- 기타 시뮬레이션 컴포넌트
- **GetFlutterMessage** (GUID `e67e858f7430b4541a7bf8cee94cfee3`) — Flutter 메시지 수신

### 메시지 처리 흐름 (현재 구현)

```csharp
// GetFlutterMessage.cs
void OnEnable()
{
    SceneManager.sceneLoaded += OnSceneLoaded;
}
void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    SendToFlutter.Send("scene_loaded");
}

public void RecieveMessage(string message)
{
    SendToFlutter.Send("{\"command\":\"ack\", ...}");
    if (message.Contains("start"))    StaticData.destTime = ...; StaticData.level = ...;
    if (message.Contains("continue")) startScreen.CheckLastPlayed();
    if (message.Contains("pause"))    pointsController.PauseApp();
    if (message.Contains("background")) SendToFlutter.Send("{\"command\":\"result\", \"reason\":\"background\"}");
}
```

## ✅ 해결된 통신 버그 (2026-05-04 수정 완료)

### `OnSceneLoaded` 미발화 가능성 (FIXED)
- 증상: `OnEnable`에서 `SceneManager.sceneLoaded` 이벤트를 구독하지만, **이미 로드된 씬에서 컴포넌트가 활성화될 때는 이벤트가 다시 발화하지 않음**. 결과적으로 `SendToFlutter.Send("scene_loaded")`가 한 번도 호출되지 않을 수 있고, Flutter 측은 `_isSceneLoaded=false` 상태로 남아 메시지가 큐에 갇힘.
- 적용된 수정: `Assets/Scripts/GetFlutterMessage.cs:11-16` — `Start()`에 `SendToFlutter.Send("scene_loaded")` 명시적 호출 추가. `OnEnable`의 이벤트 구독은 그대로 유지하여 향후 추가 씬 로드 시에도 동작.

```csharp
private void Start()
{
    startScreen = FindAnyObjectByType<StartScreen>();
    pointsController = FindAnyObjectByType<PointsController>();
    SendToFlutter.Send("scene_loaded");   // 명시적 송신 (현재 단일 씬 구조 가정)
}
```

> 새로운 통신 결함 발견 시 이 섹션 위에 "## ⚠️ 알려진 문제" 섹션을 신설해 기록.

자세한 분석 및 Flutter 측 대응 수정은 부모 Flutter 프로젝트의 `CLAUDE.md` 참고.

## 빌드 / 내보내기

이 Unity 프로젝트는 **단독으로 실행하지 않고**, Flutter 앱에 임베드되어 빌드됩니다.

```text
1. Unity Hub에서 Aidoc2025 프로젝트 열기 (2022.3.61f1)
2. flutter_embed_unity의 Export 절차에 따라 Android/iOS 라이브러리 export
   - Android: Project Settings → Player → Android, IL2CPP 빌드
   - iOS: ProjectExporterIos.cs 참고
3. Export 결과:
   - Android: ../android/unityLibrary/  (Gradle 모듈)
   - iOS: Flutter 빌드 시점에 통합
4. Flutter 측에서 `flutter run --release` (debug는 Unity 정상 동작 안 함)
```

## 코딩 컨벤션 / 워크플로우

- **씬은 1개만 사용** (`Main.unity`). 새 화면이 필요하면 GameObject 활성/비활성으로 처리하거나 `LoadScene` 추가 시 통신 영향 검토
- Flutter로 보낼 메시지는 항상 JSON 또는 콤마 구분 포맷 (`unity_service.dart`의 파서와 호환)
- 새 메시지 추가 시 양쪽 동기 업데이트 필요 (Flutter `unity_service.dart`, Unity `GetFlutterMessage.cs`)
- MediaPipe 자산은 LFS로 관리 (`*.aar`, `*.dylib`, `*.so`, `*.tflite`)
- C# 직렬화 모드: **Force Text** 권장 (`ProjectSettings/EditorSettings.asset`의 `m_SerializationMode: 2`) — diff/merge 가능
- 씬/프리팹 머지 충돌 시: UnityYAMLMerge (`Editor/Data/Tools/UnityYAMLMerge.exe`) 사용 — `.gitattributes`에 `merge=unityyamlmerge` 매핑됨

## Git / 협업

- 이 Unity 프로젝트는 Flutter `solidosis_patient` repo와는 **별도 git 저장소**로 관리 (사용자 결정)
- LFS 필수: MediaPipe AAR/dylib (~226MB)
- `.gitignore`: 표준 Unity (Library, Temp, Logs, Builds, UserSettings 등 제외)
- `.gitattributes`: 텍스트 정규화 + LFS 패턴 + UnityYAMLMerge 매핑

## UnityYAMLMerge 설정 (선택, 권장)

전역 `~/.gitconfig` 또는 프로젝트 `.git/config`에:
```ini
[merge "unityyamlmerge"]
    name = Unity SmartMerge
    driver = "C:/Program Files/Unity/Hub/Editor/2022.3.61f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p --force --fallback none %O %B %A %A
    recursive = binary
```
