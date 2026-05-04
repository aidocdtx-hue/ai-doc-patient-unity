using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 게임 진행도와 세트를 판별하는 스크립트
/// </summary>
public class SetProgress : MonoBehaviour
{
    //진행도를 나타내는 UI항목들
    public Image progressGauge;
    public Image[] checkPoints;
    public Sprite[] checkPointSprites;

    //현재 세트를 저장할 변수
    static int checkedNum = 0;
    //진행도를 퍼센트로 변환해서 저장할 변수
    float percentage;

    //초기 세팅
    private void Start()
    {
        //처음 세트를 0으로 설정
        SetCheckd(0);
        //게이지를 0으로 만듦
        progressGauge.rectTransform.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// 진행도 게이지 이미지를 조절하는 부분
    /// </summary>
    /// <param name="time">남은 시간</param>
    public void SetGauge(float time) 
    {
        //진행도를 퍼센트로 변환
        percentage = ((600 - time) / 600);
        //퍼센트 0~1에 절반위치 168을 곱해줌
        float value = percentage * 168;

        //첫 세트에서는 게이지가 차지 않음
        if (checkedNum == 0) return;
        //1세트에서는 절반까지 채워지게
        else if (checkedNum == 1) progressGauge.rectTransform.sizeDelta = new Vector2(value, 27);
        //2세트에서는 절반부터 끝까지 채워지게
        else if(checkedNum == 2) progressGauge.rectTransform.sizeDelta = new Vector2(value + 168, 27);
    }

    /// <summary>
    /// 현재 세트를 적용하는 부분
    /// </summary>
    /// <param name="num"></param>
    public void SetCheckd(int num)
    {
        //세트 변수에 전달받은 변수 할당
        checkedNum = num;
        //전달받은 세트가 0보다 크면
        if (num > 0)
        {
            //체크포인트 활성화
            for(int i = num; i > 0; i--)
            {
                checkPoints[i - 1].sprite = checkPointSprites[1];
            }
        }
        //0이면
        else
        {
            //모든 체크포인트 비활성화
            foreach(Image img in checkPoints)
            {
                img.sprite = checkPointSprites[0];
            }
        }
    }

    //현재 세트 반환
    public int GetCheckedNum()
    {
        return checkedNum;
    }

    //현재 퍼센트 반환
    public float GetPercentage()
    {
        return percentage;
    }
}