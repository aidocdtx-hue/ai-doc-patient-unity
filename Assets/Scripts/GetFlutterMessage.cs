using System.Collections;
using System.Collections.Generic;
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
                }
            }
            else if (splitedMessage[0].Contains("continue"))
            {
                //재시작
                if (TryParseTimeAndLevel(splitedMessage, out float destTime, out int level))
                {
                    StaticData.destTime = destTime;
                    StaticData.level = level;
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

                SendToFlutter.Send("{\"command\":\"result\", \"reason\":\"background\"}");
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
