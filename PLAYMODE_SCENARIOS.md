# Unity Editor PlayMode 시나리오 가이드

ADR-0011 Flutter↔Unity 라이프사이클 fix 검증용. 디바이스/adb 없이 Unity Editor 안에서 직접 시뮬레이션.

## 준비

1. Unity Hub로 `unity/Aidoc2025` 프로젝트 열기 (Unity 2022.3.61f1)
2. `Assets/Scene/Main.unity` 더블클릭 → 씬 로드
3. `Window → General → Console` 활성 — Debug.Log 추적용
4. Hierarchy에서 다음 GameObject 위치 확인:
   - `Solution` — `GetFlutterMessage` 컴포넌트 (메시지 시뮬)
   - `Solution` 자식의 `BlockGamePanel` 또는 `PairGamePanel` (게임 상태)
   - `StartScreen` (시작 화면)
5. ▶ **Play** 버튼 클릭

**ContextMenu 사용법**: 우측 Inspector의 컴포넌트 헤더에서 **⋮ (점 세 개)** 클릭 → 메뉴에 `Sim/...`, `Log/...`, `Force ...` 항목 노출.

---

## Scenario 1 — First Tutorial Completion (정상 흐름)

**목적**: 첫 진입 시 Tutorial이 정상 시작 → 끝 → 게임 진행 확인.

**단계**:
1. `Solution.GetFlutterMessage` → `Sim/Start (time=60, level=1)`
2. 가이드 화면 활성 확인 → 임의로 진행 (NextPageBtn 또는 가이드 SKIP)
3. 캘리브레이션 화면 활성 (CameraDevice가 PC 웹캠 사용 시도 — 없으면 SKIP)
4. BlockGame 또는 PairGame 활성 → **Tutorial 코루틴 시작**
5. Console에서 `[BlockGame.OnEnable]` Log 확인
6. 게임 진행 → `result(finish)` 메시지 송신 확인

**PASS 신호**:
- `[BlockGame.State] isFirst=true _restoredFromIncomplete=false`
- `dim.activeSelf=true` (Tutorial 검은 배경)
- `tutorialText.text` 변경 흐름 ("지금부터 연습을..." → "3 Set로 진행..." → ...)

**FAIL 신호**:
- Tutorial 코루틴 시작 안 함 → `dim.activeSelf=false`
- `else if (isFirst)` 분기 매칭 실패

---

## Scenario 2 — Tutorial Mid-Dispose + Continue (사용자 핵심 보고)

**목적**: 튜토리얼 진행 도중 이탈 → 재진입 시 튜토리얼 처음부터 + timer 복원.

**단계**:
1. `Solution.GetFlutterMessage` → `Sim/Start (time=60, level=1)`
2. BlockGame 활성, Tutorial 진행 중인 상태로 진행 (페이지 2~3)
3. `BlockGame` 컴포넌트 → `Log/Block State` 클릭 → 현재 state dump
   - 예상: `isFirst=false _restoredFromIncomplete=false`
4. `Solution.GetFlutterMessage` → `Sim/Mute (dispose 진입)`
   - Console: `[BlockGame.OnDisable] ...`
   - dim/tutorialGuideLine 비활성 + StopAllCoroutines 흔적
5. `Solution.GetFlutterMessage` → `Sim/Continue (time=30, level=1)`
6. 가이드 → 캘리브레이션 → BlockGame 재활성

**PASS 신호** (ADR-0011 Timer race fix):
- 재진입 후 `[BlockGame.OnEnable]` Log
- `[BlockGame.State] isFirst=false _restoredFromIncomplete=true`
- `dim.activeSelf=true` (Tutorial 다시 시작)
- timer Inspector — 카운트다운 **정지 상태** (Tutorial 진행 중)
- Tutorial 끝나면 timer가 **30초 그대로** 시작 (gameTime 60으로 덮어쓰기 안 됨)

**FAIL 신호** (Timer race 재발):
- timer가 Tutorial 중에도 카운트다운 → 만료 시 게임 강제 종료
- `_restoredFromIncomplete=false`로 잘못 set → gameTime(60)으로 덮어쓰기

---

## Scenario 3 — Playing → Background → Resume (자동 화면 전환 제거 검증)

**목적**: 사용자 결정 "background 복귀 시 스켈레톤 화면 자동 전환 X" — `ReturnToGameGuide` 호출 제거 확인.

**단계**:
1. `Sim/Start` → Scenario 1처럼 BlockGame `playing` 단계까지 진행
2. `BlockGame` → `Log/Block State` (stage=Playing 확인)
3. `Solution.GetFlutterMessage` → `Sim/Background`
   - Console: `AudioListener.pause=true` + `PauseMediapipe` + `result(background)` 송신
4. `Solution.GetFlutterMessage` → `Sim/Resume`

**PASS 신호** (resume 자동 전환 제거):
- `AudioListener.pause=false`만 호출 (Console에서 확인)
- `ReturnToGameGuide` 호출 **0건** — gameGuidePanel 활성 변경 없음
- 현재 활성 패널(BlockGamePanel) 그대로 유지 — 스켈레톤 화면 자동 전환 안 됨

**FAIL 신호** (회귀):
- `ReturnToGameGuide` 호출 → gameGuidePanel.SetActive(true) → calibration/스켈레톤 화면 전환

---

## Scenario 4 — Playing → Background → Mute → Continue (운동 복원)

**목적**: 사용자가 background 후 앱 완전히 나갔다(mute) 다시 진입(continue) 시 운동 record 복원 + Tutorial 처음부터.

**단계**:
1. Scenario 1처럼 Playing 단계 진입 + 게임 일부 진행 (남은 시간 ~30초 가정)
2. `Sim/Background` → background 메시지
3. `Sim/Mute` → 앱 dispose
4. `Sim/Continue` → 재진입

**PASS 신호** (ADR-0011 §2.2 + Timer race):
- 재진입 후 BlockGame `[OnEnable]` Log
- `_restoredFromIncomplete=true` (line 130 분기 진입)
- Tutorial 코루틴 다시 시작 (`dim.activeSelf=true`)
- Tutorial 끝나면 timer 복원된 30초로 카운트다운 시작 (gameTime 60 덮어쓰기 SKIP)

**FAIL 신호**:
- Tutorial 시작 안 함 (line 130 분기 SKIP 또는 timer race 발생)
- timer가 새 60초로 시작 → "이어하기" 의도 깨짐

---

## Scenario 5 — Tutorial → Pause Request → Pause Release

**목적**: Tutorial 도중 어깨 1초 이탈로 pause_request 송신 시 Flutter SeniorDialog 표시 + pause_release로 재개.

**단계**:
1. `Sim/Start` → BlockGame 활성, Tutorial 진행
2. PointsController.PauseApp 호출 (수동 — Inspector 메서드 호출 가능)
3. `pause_request` Flutter 송신 → SeniorDialog 시뮬
4. `Solution.GetFlutterMessage` → `Sim/Pause` (사용자 "그만하기" 시뮬) 또는 별도 pause_release helper로 재개

**PASS 신호**:
- `pause_request` Flutter 메시지 송신 1회
- "재개" 선택 시 `pointsController.Resume()` 호출 → ClosePauseScreen → calibrationPanel 활성
- "그만하기" 선택 시 _abortExercise → 홈 navigate

**FAIL 신호**:
- pause_request 중복 송신 (pauseFlag 가드 깨짐)

---

## Scenario 6 — Continuous Background (중복 가드 검증)

**목적**: paused/hidden 연속 발화 시 background 메시지 1회만 전송 (`_isInBackground` 가드).

**단계**:
1. Scenario 1처럼 Playing 단계
2. `Sim/Background` 클릭 (1회) → background 메시지 1회 송신 확인
3. **연속 3-5회** `Sim/Background` 클릭 → 추가 송신 0건 확인 (가드 동작)
4. `Sim/Resume` → `_isInBackground=false` reset
5. 다시 `Sim/Background` → 새 사이클에서 송신 1회

**PASS 신호**:
- background 메시지가 사이클당 1회만 송신
- Flutter 측 `result(background) 무시` Log (CLAUDE.md 명시)

**FAIL 신호**:
- background 연속 클릭으로 메시지 중복 송신 → Flutter dispose 호출 → 운동 강제 종료

---

## Scenario 7 — scene_loaded 중복 송신 가드 (ADR §2.4)

**목적**: 첫 진입에서 `scene_loaded`가 1회만 송신 (이전 376ms 간격 2회 중복 fix 확인).

**단계**:
1. Unity Editor PlayMode 정지 후 다시 ▶ Play (새 세션)
2. Console에서 PlayMode 시작 직후 `SendToFlutter.Send("scene_loaded")` 흔적 찾기
3. GetFlutterMessage.cs의 `_sceneLoadedSent` static 변수 추적 — Inspector 또는 Debug.LogWarning 시점

**PASS 신호**:
- `scene_loaded` 메시지 1회만 송신 (Start 코루틴 또는 OnSceneLoaded 둘 중 하나만)
- 두 번째 호출은 `_sceneLoadedSent=true` 가드로 SKIP

**FAIL 신호**:
- `scene_loaded` 2회 송신 (376ms 간격) → ADR §2.4 회귀

---

## Scenario 8 — camera_ready timing (ADR §2.1)

**목적**: `PointsController.StartMediapipe()` 호출 후 7초 wait 후 `camera_ready` 송신.

**단계**:
1. `Sim/Start` → 가이드 진입 → 가이드 SKIP → 캘리브레이션 진입 (StartMediapipe 호출 시점)
2. Console에서 StartMediapipe 호출 시각 기록 (또는 Debug.Log 추가)
3. 7초 후 `SendToFlutter.Send("camera_ready")` 송신 확인

**PASS 신호**:
- StartMediapipe 호출 후 정확히 ~7초에 camera_ready 송신 1회
- `_cameraReadyPending=false` reset (다음 진입에 또 송신 가능)

**FAIL 신호**:
- camera_ready 송신 안 함 (코루틴 실패) 또는 중복 송신

---

## Scenario 9 — Pair Game 동일 흐름

**목적**: PairGame도 BlockGame과 동일하게 Tutorial reset + Timer race fix 동작 확인.

**단계**:
- Scenario 2와 동일 흐름 단 게임 선택을 Pair로:
  - `StartScreen.ClickPairBtn()` 호출 (Inspector Method 또는 게임 화면에서 Pair 버튼)
- `Sim/Continue`는 LastPlayed PlayerPref 기준으로 분기 — Pair로 set 후 검증
- `PairGame` 컴포넌트의 `Log/Pair State` ContextMenu로 검증

**PASS 신호** (Scenario 2와 동일):
- 재진입 시 Tutorial 다시 시작
- `_restoredFromIncomplete=true` 동작
- timer 복원

---

## Scenario 10 — Force Disable/Enable (격리 검증)

**목적**: dispose/재진입의 가장 직접 시뮬 — Flutter 메시지 없이 GameObject 토글만으로.

**단계**:
1. BlockGame 또는 PairGame 활성 상태에서 컴포넌트 ContextMenu → `Force Disable`
2. `Log/Block State` 또는 `Log/Pair State` (OnDisable 직후 state)
3. `Force Enable`
4. `Log/...` 다시 (OnEnable 후 state)

**PASS 신호**:
- OnDisable → StopAllCoroutines 호출 + dim/tutorialGuideLine 비활성 흔적
- OnEnable → isFirst=true, _restoredFromIncomplete=false, dim 명시 비활성, triggeredTutorial=true reset

**FAIL 신호**:
- OnDisable에서 코루틴 stop 안 됨 → 다음 OnEnable에서 잔존 코루틴이 race
- OnEnable에서 state reset 누락

---

## Scenario 11 — 정상 종료 후 새 운동 (Tutorial 재생 확인)

**목적**: 운동 정상 완료(finish) 후 새로 운동 시작 시 Tutorial 매번 재생되는지 확인.

**단계**:
1. Scenario 1 정상 흐름 → 끝까지 → `result(finish)` 송신 + `NormalEnd=1` set
2. 운동 화면 dispose
3. `Sim/Start` (또는 `Sim/Continue`) → 새 운동 시작
4. BlockGame 재활성 시 Tutorial 시작 확인

**PASS 신호**:
- 매 진입마다 Tutorial 시작 (사용자 결정 "매 진입마다 튜토리얼 재생")
- `[BlockGame.State] isFirst=true` (정상 종료 후엔 _restoredFromIncomplete=false)

**FAIL 신호**:
- 정상 종료 후 Tutorial SKIP (사용자 의도와 불일치)

---

## Scenario 12 — 게임 모드 전환 (Block ↔ Pair)

**목적**: 한 번 Block 진행 후 다음에 Pair 선택 시 PairGame Tutorial 처음부터.

**단계**:
1. Scenario 1로 Block 시작 → 진행 → finish
2. `StartScreen` 활성 → Pair 버튼 클릭 (`ClickPairBtn`)
3. 가이드 → 캘리브레이션 → PairGame 활성 → Tutorial 시작

**PASS 신호**:
- PairGame `[OnEnable] isFirst=true` (Pair는 첫 진입)
- PairGame Tutorial 정상 시작 (Block Tutorial 잔존 없음)
- `LastPlayed` PlayerPref가 "Pair"로 갱신

**FAIL 신호**:
- Block 상태 잔존 (dim 활성 등)
- Tutorial 분기 매칭 실패

---

## Scenario 13 — Sequential Background 사이클

**목적**: Playing 도중 background → resume → background → resume 반복 시 안정성.

**단계**:
1. Playing 단계
2. `Sim/Background` → `Sim/Resume` → `Sim/Background` → `Sim/Resume` (5회 반복)
3. 매 사이클마다 `[Log/Block State]` 확인

**PASS 신호**:
- 각 background → resume 사이클이 독립 동작
- `_isInBackground` flag가 정확히 toggle (background에서 true, resume에서 false)
- 운동 화면 패널 변경 없음 (ReturnToGameGuide 호출 0)
- timer 카운트다운 정지/재개 정확

**FAIL 신호**:
- 사이클 누적으로 state 손상 (예: _isInBackground 항상 true)
- 패널 의도치 않은 활성/비활성

---

## Scenario 14 — Selection 단계 Background (cancelled_pre_game)

**목적**: 운동 시작 전 단계(Selection/Guide/Calibration)에서 background 진입 시 `result(cancelled_pre_game)` 송신 → Flutter 자동 종료.

**단계**:
1. `Sim/Start` → 시작 화면(Selection) 상태에서 정지 (게임 선택 안 함)
2. `Sim/Background`

**PASS 신호**:
- Unity가 `result(cancelled_pre_game)` 송신 확인
- Flutter 측 `_abortExercise` 호출 흔적
- (디바이스 테스트 시) 자동 홈 navigate

**FAIL 신호**:
- `result(background)` 송신 (잘못된 reason) → Flutter가 일시 중단으로 잘못 해석

---

## Scenario 15 — Force Quit 시뮬레이션 (5회 빠른 동작)

**목적**: `FastQuit` PlayerPref가 1로 set된 상태에서 재진입 시 팝업 메시지 변경 확인.

**단계**:
1. Editor에서 PlayerPrefs 수동 set:
   ```csharp
   PlayerPrefs.SetInt("FastQuit", 1);
   PlayerPrefs.SetInt("NormalEnd", 0);
   ```
2. `Sim/Continue` → 재진입
3. Initialize line 137 분기 진입 — popupText에 "빠른 동작 누적으로 강제종료 되었습니다." 표시 확인

**PASS 신호**:
- popupText.text = "빠른 동작 누적으로 강제종료 되었습니다."
- 2초 후 OffPopup 호출
- 그 외 정상 복원 흐름

**FAIL 신호**:
- FastQuit 분기 매칭 실패 → "이전 게임 상태를 불러옵니다." 일반 메시지

---

## 시나리오 우선순위 (사용자 보고 매칭)

| 우선순위 | 시나리오 | 사용자 보고 |
|---|---|---|
| **P0** | Scenario 2 | "튜토리얼 도중에 이탈한 뒤에 튜토리얼 다시 진입하면 튜토리얼 실행이 안되네" |
| **P0** | Scenario 4 | "운동 시작하기가 안됨" (배경: Timer race) |
| **P1** | Scenario 3 | "스켈레톤 화면으로 넘어가는 경우 그냥 기능 빼줘" |
| **P1** | Scenario 6 | "백그라운드 나갔다가 복귀하는 과정" |
| **P1** | Scenario 8 | "카메라 인식 후 로딩화면 꺼져야하는데..." |
| **P2** | Scenario 7 | (직전 logcat 분석 검증 — scene_loaded 중복 가드) |
| **P2** | Scenario 11 | 정상 종료 후 새 운동 (회귀 차단) |
| **P3** | Scenario 5, 9, 10, 12, 13, 14, 15 | 기타 예외 케이스 |

## 회귀 검증 매트릭스

| Scenario | ADR §2.1 camera_ready | §2.2 Tutorial reset | §2.3 dispose race | §2.4 scene_loaded 가드 | resume 자동 전환 제거 | Timer race |
|---|:-:|:-:|:-:|:-:|:-:|:-:|
| 1 | ✅ | ✅ | - | ✅ | - | - |
| 2 | - | ✅ | ✅ | - | - | ✅ |
| 3 | - | - | - | - | ✅ | - |
| 4 | - | ✅ | ✅ | - | - | ✅ |
| 5 | - | - | - | - | - | - |
| 6 | - | - | - | - | - | - |
| 7 | - | - | - | ✅ | - | - |
| 8 | ✅ | - | - | - | - | - |
| 9 | - | ✅ | ✅ | - | - | ✅ |
| 10 | - | - | ✅ | - | - | - |
| 11 | - | ✅ | - | - | - | - |
| 12 | - | ✅ | ✅ | - | - | - |
| 13 | - | - | - | - | ✅ | - |
| 14 | - | - | - | - | - | - |
| 15 | - | - | - | - | - | ✅ |

## 추가 ContextMenu helpers 권고 (선택)

현재 helpers로 Scenarios 1~15 모두 진행 가능. 자주 쓰는 시퀀스는 묶음 ContextMenu로 자동화 가능:

```csharp
// 예: GetFlutterMessage.cs에 추가
[ContextMenu("Scenario/Mid-Dispose Reentry (자동)")]
private void ScenarioMidDisposeReentry()
{
    StartCoroutine(_AutoMidDisposeReentry());
}

private IEnumerator _AutoMidDisposeReentry()
{
    SimulateStart();
    yield return new WaitForSeconds(2f);
    Debug.Log("--- dispose 시뮬 ---");
    SimulateMute();
    yield return new WaitForSeconds(2f);
    Debug.Log("--- 재진입 시뮬 ---");
    SimulateContinue();
}
```

필요 시 별도 commit으로 추가.

## 참고 자료

- 부모 Flutter 레포: `doc/adr/0011-unity-camera-ready-and-tutorial-reset.md`
- 사용자 보고 logcat: `scripts/e2e/artifacts/logcat-*.log` (Flutter 레포)
- 관련 Unity 파일:
  - `Assets/Scripts/GetFlutterMessage.cs` — 메시지 라우터 + Sim helpers
  - `Assets/Scripts/BlockGame.cs` — BlockGame Tutorial + state helpers
  - `Assets/Scripts/PairGame.cs` — PairGame Tutorial + state helpers
  - `Assets/Scripts/PointsController.cs` — StartMediapipe + camera_ready coroutine
  - `Assets/Scripts/GameGuide.cs` — 가이드 페이지 reset (OnEnable)
