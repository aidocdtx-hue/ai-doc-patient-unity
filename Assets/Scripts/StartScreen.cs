using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시작 화면에서의 기능
/// </summary>
public class StartScreen : MonoBehaviour
{
    //각 패널들
    public GameObject gameGuidePanel;
    public GameObject calibrationPanel;
    //버튼 사운드
    public AudioSource btnSound;
    //스크립트 참조
    public PointsController pointsController;
    //정상종료인지 확인하는 변수
    int isNormalEnd;
    //실행 직후 호출되는 부분
    private void Awake()
    {
        //정상종료 여부 로컬에서 값 가져옴
        isNormalEnd = PlayerPrefs.GetInt("NormalEnd");
        //비정상 종료라면
        if (isNormalEnd == 1)
        {
            //오늘 날짜
            int todayInt = int.Parse(DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString());
            //로컬에 저장된 날짜가 오늘 날짜와 다르면
            if (PlayerPrefs.GetInt("Date", 0) != todayInt)
            {
                //비정상종료 상태 삭제
                isNormalEnd = 0;
            }
        }
        else
        {
            PlayerPrefs.SetInt("FastQuitCount", 0);
        }
        //빠른동장 페널티 강제종료 키가 로컬에 있으면
        if (PlayerPrefs.HasKey("FastQuitCount"))
        {
            //페널티 횟수를 로컬에서 가져와서 할당
            StaticData.fastQuitCount = PlayerPrefs.GetInt("FastQuitCount");
        }
    }


    /// <summary>
    /// 마지막으로 플레이 한 게임을 자동으로 시작하는 기능
    /// </summary>
    public void CheckLastPlayed()
    {
        //비정상종료면
        if (isNormalEnd == 1)
        {
            //마지막으로 플레이한 게임이 짝맞추기면
            if (PlayerPrefs.GetString("LastPlayed") == "Pair")
            {
                //짝맞추기상태 켜기
                StaticData.isPairGame = true;
            }
            //블럭밀기면
            else
            {
                //짝맞추기상태 끄기 (블럭밀기상태)
                StaticData.isPairGame = false;
            }
            //비정상종료 상태를 변수에 저장
            StaticData.isNormalEnd = false;
            //시작화면 끄기
            gameObject.SetActive(false);
            //캘리브레이션 화면 키기
            calibrationPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 블럭밀기 버튼 이벤트
    /// </summary>
    public void ClickBtn()
    {
        //버튼 사운드 재생
        btnSound.Play();
        //블럭밀기 상태 저장
        StaticData.isPairGame = false;
        //정상종료 여부를 로컬에서 가져옴
        isNormalEnd = PlayerPrefs.GetInt("NormalEnd");
        //비정상종료면
        if (isNormalEnd == 1)
        {
            //비정상종료 상태 저장
            StaticData.isNormalEnd = false;
            //시작화면 끄기
            gameObject.SetActive(false);
            //캘리브레이션 화면 키기
            calibrationPanel.SetActive(true);
        }
        //정상종료면
        else
        {
            //정상종료 상태 저장
            StaticData.isNormalEnd = true;
            //시작화면 끄기
            gameObject.SetActive(false);
            //캘리브레이션 화면 키기
            gameGuidePanel.SetActive(true);
        }
        
    }

    /// <summary>
    /// 짝맞추기 버튼 이벤트
    /// </summary>
    public void ClickPairBtn()
    {
        //버튼 사운드 재생
        btnSound.Play();
        //짝맞추기 상태 저장
        StaticData.isPairGame = true;
        //정상종료 여부를 로컬에서 가져옴
        isNormalEnd = PlayerPrefs.GetInt("NormalEnd");
        //비정상종료면
        if (isNormalEnd == 1)
        {
            //비정상종료 상태 저장
            StaticData.isNormalEnd = false;
            //시작화면 끄기
            gameObject.SetActive(false);
            //캘리브레이션 화면 키기
            calibrationPanel.SetActive(true);
        }
        //정상종료면
        else
        {
            //정상종료 상태 저장
            StaticData.isNormalEnd = true;
            //시작화면 끄기
            gameObject.SetActive(false);
            //캘리브레이션 화면 키기
            gameGuidePanel.SetActive(true);
        }
        
    }



    /// <summary>
    /// 각도 단계를 설정하는 부분, 추후 본앱에서 호출받을 부분
    /// </summary>
    /// <param name="value">단계 1~3</param>
    public void SetLevel(string value)
    {
        StaticData.level = int.Parse(value) - 1;
    }

    /// <summary>
    /// 목표 도달 시간을 설정하는 부분, 추후 본앱에서 호출받을 부분
    /// </summary>
    /// <param name="value">목표 도달 시간 (초)</param>
    public void SetDestTime(string value)
    {
        StaticData.destTime = float.Parse(value);
    }

    /// <summary>
    /// 각도 단계를 설정하는 부분, 추후 본앱에서 호출받을 부분
    /// </summary>
    /// <param name="value">단계 1~3</param>
    public void SetLevel(int value)
    {
        StaticData.level = value - 1;
    }

    /// <summary>
    /// 목표 도달 시간을 설정하는 부분, 추후 본앱에서 호출받을 부분
    /// </summary>
    /// <param name="value">목표 도달 시간 (초)</param>
    public void SetDestTime(float value)
    {
        StaticData.destTime = value;
    }
}
