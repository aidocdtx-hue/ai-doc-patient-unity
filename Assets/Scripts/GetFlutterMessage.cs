using System.Collections;
using System.Collections.Generic;
using Mediapipe.Unity.Sample;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GetFlutterMessage : MonoBehaviour
{
    private StartScreen startScreen;
    private PointsController pointsController;

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
                //재시작
                if (TryParseTimeAndLevel(splitedMessage, out float destTime, out int level))
                {
                    StaticData.destTime = destTime;
                    StaticData.level = level;
                    //이전 백그라운드 진입에서 mute된 경우 복원. ResetToInitial은 호출하지 않음 —
                    //continue는 직전 게임 상태를 그대로 이어가야 하므로 패널/StaticData 리셋 금지.
                    AudioListener.pause = false;
                    startScreen.CheckLastPlayed();
                }
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

                //단계별 reason 분기:
                //- Playing: 운동 진행 중 → background reason → Flutter는 confirm 다이얼로그 → 이어하기 시 resume 송신
                //- Selection/Guide/Calibration: 운동 시작 전 → cancelled_pre_game reason → Flutter는 record 미완료 처리 + 즉시 navigate
                if (StaticData.stage == ExerciseStage.Playing)
                {
                    SendToFlutter.Send("{\"command\":\"result\", \"reason\":\"background\"}");
                }
                else
                {
                    SendToFlutter.Send("{\"command\":\"result\", \"reason\":\"cancelled_pre_game\"}");
                }
            }
            else if (splitedMessage[0].Contains("resume"))
            {
                //앱 포그라운드 복귀 시 사용자가 confirm 다이얼로그에서 "이어하기" 선택 후 송신.
                //사용자 결정: 이어하기는 게임 그 자리부터가 아닌 가이드 → 캘리브레이션 → 게임 흐름으로 다시 시작.
                //ReturnToGameGuide가 게임/일시정지/캘리브레이션 패널을 닫고 gameGuidePanel 활성화 +
                //AudioListener.pause=false + ResumeMediapipe(IsPaused 가드로 idempotent) 처리.
                if (pointsController != null) pointsController.ReturnToGameGuide();
            }
            else if (splitedMessage[0].Contains("mute"))
            {
                //Flutter가 운동 화면을 dispose하고 홈 등으로 navigate할 때 송신.
                //Unity Player는 같은 Activity 안에서 계속 살아있어 OnApplicationPause가 발화 안 함 →
                //명시적으로 audio + MediaPipe 추론을 정지해 자원/소리 누수 방지.
                AudioListener.pause = true;
                if (pointsController != null) pointsController.PauseApp();
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
        SendToFlutter.Send("scene_loaded");
    }

    void OnDisable()
    {
        Debug.LogWarning("GetFlutterMessage OnDisable");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
