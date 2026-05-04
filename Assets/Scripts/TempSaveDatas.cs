using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 앱이 비정상종료 되었을때를 방지하여 저장하는 데이터들
/// </summary>
public class TempSaveDatas : MonoBehaviour
{
    /// <summary>
    /// 데이터들을 로컬에 중간 저장
    /// </summary>
    /// <param name="time">남은 시간</param>
    /// <param name="progress">SetProgress 스크립트</param>
    public void SaveData(float time, SetProgress progress)
    {
        //짝맞추기 세트별 점수 저장
        PlayerPrefs.SetInt("PairSet1Score", StaticData.pairGameScores.section1);
        PlayerPrefs.SetInt("PairSet2Score", StaticData.pairGameScores.section2);
        PlayerPrefs.SetInt("PairSet3Score", StaticData.pairGameScores.section3);

        //블럭밀기 세트별 점수 저장
        PlayerPrefs.SetInt("BlockSet1Score", StaticData.blockGameScores.section1);
        PlayerPrefs.SetInt("BlockSet2Score", StaticData.blockGameScores.section2);
        PlayerPrefs.SetInt("BlockSet3Score", StaticData.blockGameScores.section3);

        //현재 세트 가져옴
        int checkedNum = progress.GetCheckedNum();
        //현재 세트 저장
        PlayerPrefs.SetInt("NowSet", checkedNum);
        //남은 시간 저장
        PlayerPrefs.SetFloat("GameTime", time);

        //게임별 운동 횟수 저장
        PlayerPrefs.SetInt("PairCheckCount1", StaticData.pairGameDatas.count1);
        PlayerPrefs.SetInt("PairCheckCount2", StaticData.pairGameDatas.count2);
        PlayerPrefs.SetInt("PairCheckCount3", StaticData.pairGameDatas.count3);
        PlayerPrefs.SetInt("PushBlockCount1", StaticData.blockGameDatas.count1);
        PlayerPrefs.SetInt("PushBlockCount2", StaticData.blockGameDatas.count2);
        PlayerPrefs.SetInt("PushBlockCount3", StaticData.blockGameDatas.count3);
    }
}
