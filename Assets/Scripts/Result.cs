using DG.Tweening;
using DG.Tweening.Core;
using FlutterUnityIntegration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

/// <summary>
/// 결과화면 스크립트
/// </summary>
public class Result : MonoBehaviour
{
    //유니티 인스펙터에서 참조하는 UI 항목들
    public TMP_Text set1;
    public TMP_Text set2;
    public TMP_Text set3;
    public TMP_Text sumScore;
    public TMP_Text title;
    public GameObject starImage;
    public TMP_Text subText;

    //이전 최고점수를 참조하는 변수
    int prevHighScore;

    //유니티 인스펙터에서 참조하는 오브젝트들
    public GameObject startPanel;
    public GameObject skeleton;

    //별 등장 애니메이션타입
    public Ease ease;
    //점수를 더해줄 변수
    int sum;
    //운동횟수를 더해줄 변수
    int count;

    public SaveLandmarkDatas saveLandmarkDatas;

    //데이터 저장을 위한 클래스
    class SaveDatas
    {
        public string command;
        public string reason;
        public int score;
        public int exercise_count;
        public float exercise_time;
        public string created_at;
        public int panalty;
        public List<SkeletonSample> raw;
    }
    /// <summary>
    /// 결과화면이 켜질 때 실행되는 부분
    /// </summary>
    private void OnEnable()
    {
        //코루틴 실행
        StartCoroutine(Initialize());
        //스켈레톤 끄기
        skeleton.SetActive(false);
        //정상종료 상태를 로컬에 저장
        PlayerPrefs.SetInt("NormalEnd", 0);
    }

    //결과 계산
    IEnumerator Initialize()
    {
        //짝맞추기 게임을 했으면
        if (StaticData.isPairGame)
        {
            //짝맞추기 게임의 점수와 시행횟수를 변수에 저장
            sum = StaticData.pairGameScores.section1 + StaticData.pairGameScores.section2 + StaticData.pairGameScores.section3;
            count = StaticData.pairGameDatas.count1 + StaticData.pairGameDatas.count2 + StaticData.pairGameDatas.count3;

            //결과화면의 세트별 점수 텍스트에 점수 적용
            set1.text = StaticData.pairGameScores.section1.ToString("N0");
            set2.text = StaticData.pairGameScores.section2.ToString("N0");
            set3.text = StaticData.pairGameScores.section3.ToString("N0");

            //로컬에 PairGameHighScore키의 값이 있으면
            if (PlayerPrefs.HasKey("PairGameHighScore"))
            {
                //이전 최고점수에 값을 가져와서 할당
                prevHighScore = PlayerPrefs.GetInt("PairGameHighScore");
            }
            //로컬에 PairGameHighScore키가 없으면
            else
            {
                //이전 최고 점수를 0으로
                prevHighScore = 0;
            }

            //이전에 기록된 최고점보다 현재 점수가 크다면
            if (prevHighScore < sum)
            {
                //별 이미지 켜기
                starImage.SetActive(true);
                //별 크기를 0으로 만들기
                starImage.transform.localScale = Vector3.zero;
                //별 크기를 1로 만드는 애니메이션 효과
                starImage.transform.DOScale(1f, 1f).SetEase(ease);
                //현재 점수를 최고점으로 저장
                PlayerPrefs.SetInt("PairGameHighScore", sum);
            }
            //현재 점수가 더 작다면
            else
            {
                //별 이미지 끄기
                starImage.SetActive(false);
            }
        }
        else
        {
            //블럭밀기 게임의 점수와 시행횟수를 변수에 저장
            sum = StaticData.blockGameScores.section1 + StaticData.blockGameScores.section2 + StaticData.blockGameScores.section3;
            count = StaticData.blockGameDatas.count1 + StaticData.blockGameDatas.count2 + StaticData.blockGameDatas.count3;

            //결과화면의 세트별 점수 텍스트에 점수 적용
            set1.text = StaticData.blockGameScores.section1.ToString("N0");
            set2.text = StaticData.blockGameScores.section2.ToString("N0");
            set3.text = StaticData.blockGameScores.section3.ToString("N0");

            //이전 기록 중 최고점을 가져옴
            if (PlayerPrefs.HasKey("BlockGameHighScore"))
            {
                prevHighScore = PlayerPrefs.GetInt("BlockGameHighScore");
            }
            else
            {
                prevHighScore = 0;
            }

            //이전에 기록된 최고점보다 현재 점수가 크다면
            if (prevHighScore < sum)
            {
                //별 이미지 켜기
                starImage.SetActive(true);
                //별 크기를 0으로 만들기
                starImage.transform.localScale = Vector3.zero;
                //별 크기를 1로 만드는 애니메이션 효과
                starImage.transform.DOScale(1f, 1f).SetEase(ease);
                //현재 점수를 최고점으로 저장
                PlayerPrefs.SetInt("BlockGameHighScore", sum);
            }
            //현재 점수가 더 작다면
            else
            {
                //별 이미지 끄기
                starImage.SetActive(false);
            }
        }

        //합산 점수를 텍스트에 반영
        sumScore.text = sum.ToString("N0");
        //데이터를 JSON으로 저장
        SaveData("finish");
        //5초대기
        yield return new WaitForSeconds(5f);
        //결과화면 끄기
        gameObject.SetActive(false);
        //시작화면 켜기
        startPanel.SetActive(true);
    }

    /// <summary>
    /// 점수를 JSON으로 저장하는 부분
    /// </summary>
    /// <param name="exerTime">총 운동시간, 따로 값을 넣어주지 않으면 총 운동시간으로 설정</param>
    public void SaveData(string reason, float exerTime = 1800f)
    {
        //도중에 저장할 때
        if(exerTime != 1800f)
        {
            //짝맞추기 게임을 했으면
            if (StaticData.isPairGame)
            {
                //짝맞추기 게임의 점수와 시행횟수를 변수에 저장
                sum = StaticData.pairGameScores.section1 + StaticData.pairGameScores.section2 + StaticData.pairGameScores.section3;
                count = StaticData.pairGameDatas.count1 + StaticData.pairGameDatas.count2 + StaticData.pairGameDatas.count3;
            }
            else
            {
                //블럭밀기 게임의 점수와 시행횟수를 변수에 저장
                sum = StaticData.blockGameScores.section1 + StaticData.blockGameScores.section2 + StaticData.blockGameScores.section3;
                count = StaticData.blockGameDatas.count1 + StaticData.blockGameDatas.count2 + StaticData.blockGameDatas.count3;
            }
        }
        //오늘 날짜
        string today = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString("00") + "-" + DateTime.Now.Day.ToString("00");
        //운동 종료 시간
        string endTime = DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString("00") + ":" + DateTime.Now.Second.ToString("00");
        //결과 객체 생성
        SaveDatas data = new SaveDatas();
        //게임 결과 데이터를 객체에 입력
        data.command = "result";
        data.reason = reason;
        data.score = sum;
        data.exercise_count = count; 
        data.exercise_time = exerTime;
        data.created_at = endTime;
        data.panalty = StaticData.fastQuitCount;
        data.raw = saveLandmarkDatas.SaveJsonData();
        //경로를 persistentDataPath로 설정, 파일 이름은 날짜-data.json으로 설정
        string path = Path.Combine(Application.persistentDataPath, today + "-data.json");
        //json에 객체데이터 입력
        string json = JsonUtility.ToJson(data, true);
        //Flutter에 json메시지 전달
        //UnityMessageManager.Instance.SendMessageToFlutter(json);
        SendToFlutter.Send(json); //250811 수정사항
        //JSON파일 저장
        File.WriteAllText(path, json);
    }
}
