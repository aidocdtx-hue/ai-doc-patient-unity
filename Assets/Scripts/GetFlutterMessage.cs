using System.Collections;
using System.Collections.Generic;
using Mediapipe.Unity.Sample;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GetFlutterMessage : MonoBehaviour
{
    private StartScreen startScreen;
    private PointsController pointsController;

    //ADR-0011 §2.4: Start() 코루틴과 OnSceneLoaded(SceneManager.sceneLoaded 콜백)가 첫 진입에서
    //둘 다 fire하면 scene_loaded가 376ms 간격으로 2회 송신됨(logcat 095159 PID 8488 검증).
    //멱등 가드로 첫 송신만 유효화. 추가 씬 로드가 발생해도 이 인스턴스 lifetime 내 1회만.
    private static bool _sceneLoadedSent = false;

    private void Start()
    {
        startScreen = FindAnyObjectByType<StartScreen>();
        pointsController = FindAnyObjectByType<PointsController>();
        StartCoroutine(NotifySceneLoadedWhenReady());
    }

    //옵션 3 적용 후: 시작 화면(게임 선택)에서 카메라/추론 OFF 유지 → 카메라 ready 대기 무의미.
    //Unity 부트 직후 시작 화면 패널이 풀스크린으로 RawImage를 가리므로 회색 placeholder 노출 없음.
    //한 프레임만 대기 후 즉시 scene_loaded 송신 → Flutter 검은 오버레이가 시작 화면 표시 시점에 빠르게 걷힘.
    //카메라는 사용자가 게임 선택(StartScreen.ClickBtn) 시 명시 Play로 켜짐.
    private IEnumerator NotifySceneLoadedWhenReady()
    {
        yield return null;
        SendSceneLoadedOnce();
    }

    //ADR-0011 §2.4 멱등 가드. Start()와 OnSceneLoaded 양쪽에서 호출 가능.
    private static void SendSceneLoadedOnce()
    {
        if (_sceneLoadedSent) return;
        _sceneLoadedSent = true;
        SendToFlutter.Send("scene_loaded");
    }

    public void RecieveMessage(string message)
    {
        try
        {
            Debug.LogWarning("GetFlutterMessage: " + message);
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogWarning("Received empty message from Flutter");
                return;
            }

            string[] splitedMessage = message.Split(',');

            SendToFlutter.Send("{\"command\":\"ack\", \"reason\":\"" + splitedMessage[0] + "\"}");

            if (splitedMessage[0].Contains("start"))
            {
                //시작
                if (TryParseTimeAndLevel(splitedMessage, out float destTime, out int level))
                {
                    StaticData.destTime = destTime;
                    StaticData.level = level;
                    PlayerPrefs.SetInt("NormalEnd", 0);
                    //신규 운동 진입 — 직전 게임/일시정지/캘리브레이션 패널이 남아있을 수 있으므로
                    //시작 화면으로 강제 복귀. ResetToInitial 안에서 AudioListener.pause=false도 처리되어
                    //이전 백그라운드 진입으로 mute된 사운드가 자동 복원됨.
                    if (pointsController != null) pointsController.ResetToInitial();
                }
            }
            else if (splitedMessage[0].Contains("continue"))
            {
                //재시작 — ADR-0011 §2.2: 운동 record(destTime/level)는 유지, 튜토리얼은 처음부터.
                if (TryParseTimeAndLevel(splitedMessage, out float destTime, out int level))
                {
                    StaticData.destTime = destTime;
                    StaticData.level = level;

                    //이전 백그라운드 진입에서 mute된 경우 복원.
                    AudioListener.pause = false;

                    //ADR-0011 §2.2: continue는 운동 record를 이어가지만 Unity 시나리오는 처음부터.
                    //직전 stage/calibration 진행도가 메모리에 남아 "이어하기" 시 가이드 영상이
                    //중간부터 재생되거나 calibration이 중간 상태에서 시작되던 결함 해소.
                    //흐름: ResetToInitial(패널 모두 닫고 시작 화면 초기 상태) → CheckLastPlayed(직전
                    //LastPlayed PlayerPref로 isPairGame 복원 + 시작 화면 비활성 + gameGuidePanel 활성
                    //+ Mediapipe Start). 한 프레임에 처리되어 사용자에겐 시작 화면 깜빡임 없음.
                    //ResetToInitial이 AudioListener.pause=false도 set하므로 위 unmute는 idempotent.
                    if (pointsController != null) pointsController.ResetToInitial();
                    startScreen.CheckLastPlayed();
                }
            }
            else if (splitedMessage[0].Contains("pause_release"))
            {
                //Flutter SeniorDialog "재개" 선택 시 송신 (옵션 A — 일시정지 UI 통합).
                //pointsController.Resume() → ClosePauseScreen 코루틴 시작 → MediaPipe Play +
                //2초 대기 후 calibrationPanel 활성. 게임 그 자리부터가 아닌 캘리브레이션부터 다시.
                if (pointsController != null) pointsController.Resume();
            }
            else if (splitedMessage[0].Contains("pause"))
            {
                //일시정지 호출
                if (pointsController != null)
                {
                    pointsController.PauseApp();
                }
            }
            else if (splitedMessage[0].Contains("background"))
            {
                //일시정지 호출
                // SendToFlutter.Send("background"); //250811 수정사항

                //홈 버튼/화면 OFF로 Flutter가 background 메시지를 송신한 시점에 사운드 명시 mute.
                //flutter_embed_unity 환경에서 OnApplicationPause/Focus가 신뢰성 있게 발화하지 않아
                //BGM/효과음이 계속 재생되던 결함 차단. resume 메시지 수신 시 false로 복원.
                AudioListener.pause = true;
                //MediaPipe 추론 + 카메라(WebCamTexture) 정지 — 앱 밖 머무는 동안 자원/배터리 누수 차단.
                //카메라 LED도 함께 꺼짐. 복귀 시 resume 메시지에서 ReturnToGameGuide로 재개.
                if (pointsController != null) pointsController.PauseMediapipe();

                //모든 단계에서 background reason 통일 송신. Flutter가 단계별 confirm 다이얼로그 표시.
                //이전엔 단계별 reason 분기(Playing → background, 그 외 → cancelled_pre_game)였으나
                //사용자 결정: OS 홈 버튼은 의도 모호(전화/알림/실수)라 모든 단계에서 사용자 동의 받는 팝업 필요.
                SendToFlutter.Send("{\"command\":\"result\", \"reason\":\"background\"}");
            }
            else if (splitedMessage[0].Contains("resume"))
            {
                //앱 포그라운드 복귀 시 사용자가 confirm 다이얼로그에서 "계속하기" 선택 후 송신.
                //
                //사용자 결정 (2026-05-11): "앱 외부로 전환(홈버튼)시 게임 중에 스켈레톤 화면으로 넘어가는
                //경우가 있는데 이거 그냥 기능 빼줘" — 자동 화면 전환(ReturnToGameGuide → gameGuidePanel)
                //제거. 모든 단계(Selection/Guide/Calibration/Playing)에서 sound unmute만 처리하고
                //패널 전환은 안 함. 사용자가 background 진입 직전 보던 화면 그대로 유지.
                //ResumeMediapipe는 호출 안 함 — playing 상태였으면 PauseMediapipe로 paused 상태인데
                //사용자가 명시 행동(다음 버튼 또는 운동 재개 액션) 시 게임 흐름이 자연스럽게 재개.
                AudioListener.pause = false;
            }
            else if (splitedMessage[0].Contains("mute"))
            {
                //Flutter가 운동 화면을 dispose하고 홈 등으로 navigate할 때 송신.
                //Unity Player는 같은 Activity 안에 계속 살아있어 OnApplicationPause가 발화 안 함 →
                //명시적으로 audio + MediaPipe 추론을 정지해 자원/소리 누수 방지.
                //
                //ADR-0011 §2.3: dispose 시 카메라 close 순서 정리.
                //- ResetToInitial: 시작 화면 패널 복귀(AudioListener.pause=false도 포함).
                //- PauseMediapipe: MediaPipe runner + WebCamTexture 정지(카메라 LED OFF + race 차단).
                //- AudioListener.pause = true: ResetToInitial이 false로 되돌린 걸 다시 mute.
                //옵션 B(SceneManager.LoadScene) 폐기 사유: 1.5–3초 비용 + 튜토리얼 reset은
                //§2.2의 continue 분기에서 별도 처리하는 게 더 명확.
                if (pointsController != null) pointsController.ResetToInitial();
                if (pointsController != null) pointsController.PauseMediapipe();
                AudioListener.pause = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing Flutter message: {message}. Error: {e.Message}");
        }
    }

    private bool TryParseTimeAndLevel(string[] messageParts, out float destTime, out int level)
    {
        destTime = 0f;
        level = 1;

        try
        {
            if (messageParts.Length >= 2)
            {
                string timePart = messageParts[1];
                if (timePart.Contains(":"))
                {
                    string[] timeSplit = timePart.Split(':');
                    if (timeSplit.Length >= 2 && float.TryParse(timeSplit[1], out float parsedTime))
                    {
                        destTime = parsedTime;
                    }
                }
            }

            if (messageParts.Length >= 3)
            {
                string levelPart = messageParts[2];
                if (levelPart.Contains(":"))
                {
                    string[] levelSplit = levelPart.Split(':');
                    if (levelSplit.Length >= 2 && int.TryParse(levelSplit[1], out int parsedLevel))
                    {
                        level = parsedLevel;
                    }
                }
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing time and level: {e.Message}");
            return false;
        }
    }

    void OnEnable()
    {
        Debug.LogWarning("GetFlutterMessage OnEnable");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.LogWarning("GetFlutterMessage OnSceneLoaded");
        //ADR-0011 §2.4: Start()의 NotifySceneLoadedWhenReady와 멱등 가드 공유.
        //씬 첫 진입에선 둘 다 fire되지만 SendSceneLoadedOnce가 1회만 송신.
        SendSceneLoadedOnce();
    }

    void OnDisable()
    {
        Debug.LogWarning("GetFlutterMessage OnDisable");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
