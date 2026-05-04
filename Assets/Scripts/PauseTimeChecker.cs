using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// pause상태에서 시간을 체크하는 스크립트
/// </summary>
public class PauseTimeChecker : MonoBehaviour
{
    public SaveLandmarkDatas saveLandmark;
    //pause패널이 활성화 되면 호출
    private void OnEnable()
    {
        //코루틴 시작
        StartCoroutine(ExitTimer());
    }

    IEnumerator ExitTimer()
    {
        //TimeScale이 0이므로 Realtime으로 300초 대기
        yield return new WaitForSecondsRealtime(300f);
        //종료전에 데이터를 저장
        saveLandmark.TempSaveDatas("timeOut");
        //5분이 지나면 앱 종료, 종료 방법은 협의 필요
        //Application.Quit(); //250811 수정사항
    }

    //게임 종료의 확인 버튼을 누르면
    public void OnClickExitBtn()
    {
        //프로그램 종료
        //Application.Quit(); //250811 수정사항
        // SendToFlutter.Send("exit"); //250811 수정사항
        SendToFlutter.Send("{\"command\":\"result\", \"reason\":\"exit\"}");

        StaticData.nowMode = Mode.None;
        Time.timeScale = 1;
        SceneManager.LoadScene(0);

    }

    // text finish
    public void OnClickTextFinishBtn()
    {
        //프로그램 종료
        saveLandmark.TempSaveDatas("finish");
    }
}
