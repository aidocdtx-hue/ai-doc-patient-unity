using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캘리브레이션 단계
/// </summary>
public class Calibration : MonoBehaviour
{
    
    // 각 부분에 포인트가 인식되었는지 여부를 판단하는 변수
    private bool isCheckShoulderLeft;
    private bool isCheckShoulderRight;
    private bool isCheckHands;

    //3초간 자세 유지를 판별하고 계산하는 변수
    float time = 0;
    bool timerState;

    public GameObject discription; //캘리브레이션 설명 UI
    public GameObject discription2; //게임 시작 안내 UI

    public GameObject blockGamePanel; //메인 게임 패널
    public GameObject pairGamePanel; //메인 게임 패널

    //스크립트 참조
    public PointsController pointsController;

    //3 2 1 타이머 이미지
    public Image watchImage;
    public Image watchNumber;
    public Sprite[] numberSprites;

    //3 2 1 카운트 사운드
    public AudioSource countSound;


    //세트를 저장하는 변수
    int checkedNum;

    //현재 세트 반환
    public int GetCheckedNum()
    {
        return checkedNum;
    }

    //캘리브레이션 화면이 켜지면
    private void OnEnable() 
    {
        //스켈레톤 켜기
        pointsController.SetPointandConnectionEnabled(true);

        //정상종료였다면
        if (PlayerPrefs.GetInt("NormalEnd") == 0) 
        {
            //세트를 0으로
            checkedNum = 0;
        }
        //비정상종료였다면
        else
        {
            //세트를 저장된 값 받아와서 설정
            checkedNum = PlayerPrefs.GetInt("NowSet");
        }
        //설명 팝업 표출
        discription.SetActive(true);
        //시작 팝업 끄기
        discription2.SetActive(false);
        //시계 이미지 끄기
        watchImage.gameObject.SetActive(false);
        //시간 초기화
        timerState = false;
        time = 0;

        //콜라이더 변수들 초기화
        isCheckShoulderLeft = false;
        isCheckShoulderRight = false;
        isCheckHands = false;

        //캘리브레이션 시작
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        StaticData.nowMode = Mode.Calibrate; //모드 변경
        yield return new WaitForEndOfFrame(); //모드 변경이 적용되는동안 1프레임 대기
        pointsController.SetPointTrigger(); //어깨 인식가능하게 변경
    }

    /// <summary>
    /// 각 부분에서 인식되었는지 여부를 전달받는 부분
    /// </summary>
    /// <param name="name">콜라이더 이름</param>
    /// <param name="state">콜라이더 상태</param>
    public void SetCalibrationPointState(string name, bool state)
    {
        //콜라이더 이름과 일치하면 전달받은 상태로 변경
        switch (name)
        {
            case "LeftShoulder":
                isCheckShoulderLeft = state;
                break;
            case "RightShoulder":
                isCheckShoulderRight = state;
                break;
            case "Hands":
                isCheckHands = state;
                break;
        }

        //셋 다 범위내에 들어왔으면 카운트 시작 변수 상태 변경
        if(isCheckShoulderLeft == true && isCheckShoulderRight == true && isCheckHands == true)
        {
            timerState = true;
        }
        else
        {
            timerState = false;
        }

        Debug.Log($"Left:{isCheckShoulderLeft}, Right:{isCheckShoulderRight}, Hands:{isCheckHands}");
    }

    private void Update()
    {
        if (timerState) //카운트 시작
        {
            //카운트 효과음 재생
            if(!countSound.isPlaying) countSound.Play();
            //time변수에 시간 저장
            time += Time.unscaledDeltaTime;
            //시계 이미지 채우기
            watchImage.fillAmount = time % 1f;
            //시계 이미지 켜기
            watchImage.gameObject.SetActive(true);
            //설명 팝업 끄기
            discription.SetActive(false);
            //게임 시작 팝업 켜기
            discription2.SetActive(true);
            //초에 맞춰서 숫자 이미지 교체
            if (time > 2f) watchNumber.sprite = numberSprites[0];
            else if (time > 1f) watchNumber.sprite = numberSprites[1];
            else watchNumber.sprite = numberSprites[2];
        }
        else //카운트 초기화
        {
            //카운트 효과음 정지
            if (countSound.isPlaying) countSound.Stop();
            //설명 팝업 켜기
            discription.SetActive(true);
            //게임시작 팝업 끄기
            discription2.SetActive(false);
            //시계 이미지 끄기
            watchImage.gameObject.SetActive(false);
            //시간 0으로 초기화
            time = 0;
        }

        if (time > 3f) //3초간 지속시
        {
            //스켈레톤 끄기
            pointsController.SetPointandConnectionEnabled(false);
            //캘리브레이션 끄기
            gameObject.SetActive(false);
            //일치하는 게임 켜기
            if(StaticData.isPairGame) pairGamePanel.SetActive(true);
            else blockGamePanel.SetActive(true);
            Time.timeScale = 1f; //시간 정상화

        }

    }
}
