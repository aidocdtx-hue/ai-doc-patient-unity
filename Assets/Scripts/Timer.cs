using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;

/// <summary>
/// 타이머 관련 스크립트
/// </summary>
public class Timer : MonoBehaviour
{
    //스크립트 참조
    public SetProgress progress;
    public TempSaveDatas tempSaveDatas;
    public PointsController pointsController;
    public SaveLandmarkDatas saveLandmarks;
    public PairGame pairGame;
    public BlockGame blockGame;

    //팝업 관련 내용
    public GameObject restPopup;
    public GameObject gamePopup;
    public TMP_Text gamePopupText;
    public GameObject restPopupText1;
    public GameObject restPopupText3;

    //시간 관련 변수
    private float remainTime = 0;
    public TMP_Text timerText;
    float gameTime = 600f;
    float restTime = 60f;
    
    //상태 체크 변수
    bool isPlayGame = false;
    bool isPaused = false;
    int endCount = 0;
    
    //각 패널
    public GameObject gamePanel;
    public GameObject calibrationPanel;
    public GameObject resultPanel;
    
    void Update()
    {
        //남은 시간이 있으면
        if(remainTime > 0)
        {
            //튜토리얼 모드에서는 동작하지 않음
            if (StaticData.nowMode == Mode.Tutorial) return;
            //남은 시간을 실제시간 흐름만큼 없애기
            remainTime -= Time.deltaTime;
            //텍스트에 남은 시간 반영
            timerText.text = ((int)(remainTime / 60)).ToString("00") + ":" + ((int)remainTime % 60f).ToString("00");

            //1초마다 데이터를 중간저장
            if((int)(remainTime*10) % 10 == 0 && StaticData.nowMode == Mode.Game)
            {
                tempSaveDatas.SaveData(remainTime, progress);
            }

            //게임 시간이 5초 이하로 남았을 때, 옵션의 상세설명에 체크를 했다면 추가 팝업 표출
            if (remainTime < 5 && StaticData.nowMode == Mode.Game && StaticData.isExtraDisc == true)
            {
                gamePopup.SetActive(true);
                gamePopupText.text = "얼마 남지 않았어요! 힘내요!";
            }

            //휴식 시간이 5초 이하로 남았을 때, 옵션의 상세설명에 체크를 했다면 추가 팝업 표출
            if (remainTime < 5 && StaticData.nowMode == Mode.Rest && StaticData.isExtraDisc == true)
            {
                restPopupText1.SetActive(false);
                restPopupText3.SetActive(true);
            }
        }
        //시간이 다 되면
        else
        {
            //게임플레이 중 시간이 다 되면
            if (isPlayGame)
            {
                //게임중 상태 끄기
                isPlayGame = false;
                //휴식 상태 켜기
                isPaused = true;
                //게이지에 상태 반영
                progress.SetGauge(remainTime);
                //카운트를 올려 세트수에 반영
                endCount++;
                //팝업 끄기
                gamePopup.SetActive(false);
                //진행도에도 변경된 카운트 전달
                progress.SetCheckd(endCount);
                //마지막 세트가 아니면
                if (endCount < 3) 
                {
                    //휴식모드로 변경
                    StaticData.nowMode = Mode.Rest;
                    //데이터 중간저장
                    tempSaveDatas.SaveData(remainTime, progress);
                    //짝맞추기 게임중이면 짝맞추기 게임 영역 밖으로 숨기기
                    if (pairGame != null) pairGame.SetGameObjectsFade(0f);
                    //블럭밀기 게임중이면 블럭밀기 게임 영역 밖으로 숨기기
                    else blockGame.SetGameObjectsFade(0f);
                    //휴식 팝업 표출
                    restPopup.SetActive(true);
                    //휴식 시간 타이머 실행
                    SetRamainTime(restTime); 
                }
                //마지막 세트일 때
                else
                {
                    //모드를 none상태로 변경
                    StaticData.nowMode = Mode.None;
                    //플레이하던 게임 화면 끄기
                    if (pairGame != null) pairGame.gameObject.SetActive(false); 
                    else blockGame.gameObject.SetActive(false);
                    //결과 화면 켜기
                    resultPanel.SetActive(true); 
                    //세트 수 0으로 초기화
                    endCount = 0;
                }
                
            }
            //휴식 중 시간이 다 되면
            else if (isPaused)
            {
                //휴식중 상태 끄기
                isPaused = false;
                //게임 상태 켜기
                isPlayGame = true;
                //휴식 팝업 끄기
                restPopup.SetActive(false); 
                //휴식 추가 팝업 끄기
                restPopupText1.SetActive(true);
                restPopupText3.SetActive(false);
                //게임모드로 변경
                StaticData.nowMode = Mode.Game;
                //게임 시간 타이머 실행
                SetRamainTime(gameTime); 
                //플레이중이던 게임 영역 안으로 이동시켜 표시
                if (pairGame != null) {
                    pairGame.SetGameObjectsFade(1f);
                }
                else {
                    blockGame.SetGameObjectsFade(1f);
                }
            }
        }
        
        //현재 게임 플레이중이면 게이지에 남은 시간 전달
        if(isPlayGame) progress.SetGauge(remainTime);

    }

    //시간 할당
    public void SetRamainTime(float time)
    {
        remainTime = time;
        //휴식중이 아니면
        if (!isPaused) 
        {
            Debug.Log("호출");
            //포인트 저장 시작
            pointsController.StartSaveData();
        }
    }

    //현재 상태 변경
    public void SetState(bool state)
    {
        isPlayGame = state;
        isPaused = !state;
    }

    //세트 수를 체크하는 변수 설정
    public void SetEndCount(int num)
    {
        endCount = num;
    }

    //세트 수를 반환하는 부분
    public int GetEndCount()
    {
        return endCount;
    }

    //값 저장에 남은 시간을 반환해 주는 부분
    public float GetRemainTime()
    {
        return remainTime;
    }

    public void ClickTest()
    {
        remainTime = 3;
    }
}