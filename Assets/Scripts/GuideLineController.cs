using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuideLineController : MonoBehaviour
{
    //가이드라인 이미지
    Image lineImage;
    //가이드라인 이미지의 스프라이트들
    public Sprite[] sprites; //0 : Red 1 : Yellow 2 : Green
    //거리를 잴 터치영역 오브젝트
    public Transform triggerArea;
    //타이머 플래그 변수
    bool startCount = false;
    //머무른 시간을 저장할 변수
    float stayTime = 0;
    //거리를 저장할 변수
    float distance;

    
    void Awake()
    {
        //이미지 할당
        lineImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        //라인이 생성될 때 빨간색으로 생성되게 초기화
        lineImage.sprite = sprites[0];
    }

    /// <summary>
    /// 터치영역을 받아서 할당해주는 부분
    /// </summary>
    /// <param name="transform"></param>
    public void SetTriggerArea(Transform transform)
    {
        triggerArea = transform;
    }

    private void OnTriggerStay(Collider other)
    {
        //라인색을 초록색으로 변경
        lineImage.sprite = sprites[2];
        //true가 되면
        if (startCount)
        {
            //머무른 시간을 증가시키며 저장
            stayTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// 시간을 재기 시작
    /// </summary>
    public void GuideLineTimeStart()
    {
        startCount = true;
    }

    /// <summary>
    /// 타이머를 끄고 머무른 시간 반환
    /// </summary>
    /// <returns></returns>
    public float GetGuideLineTime()
    {
        //초기화 전에 머무른 시간 저장
        float nowTime = stayTime;
        //타이머 끄기
        startCount = false;
        //머무른 시간 초기화
        stayTime = 0;
        //저장한 시간 반환
        return nowTime;
    }

    private void Update()
    {
        //터치영역이 지정되어있지 않으면 진입하지 않음
        if (triggerArea == null) return;
        
        // 터치영역과 가이드라인간 거리의 절댓값
        distance = Mathf.Abs(triggerArea.position.x - transform.position.x);
        //거리가 13보다 크고 30보다 작으면
        if (distance < 30 && distance > 13)
        {
            //라인을 노란색으로 변경
            lineImage.sprite = sprites[1];
            //팝업 끄기
            if (popupText.text.Contains("너무"))
            {
                popup.SetActive(false);
            }
            
        }
        //거리가 30보다 멀면
        else if(distance > 30)
        {
            //라인을 빨간색으로 변경
            lineImage.sprite = sprites[0];
            //팝업 키기
            popup.SetActive(true);
            //현재 터치영역 위치가 라인의 우측방향일 때
            if (triggerArea.position.x > transform.position.x)
            {
                //왼 쪽으로 가는중이면
                if (isLeft)
                {
                    //텍스트 설정
                    popupText.text = "너무 느려요";
                }
                else
                {
                    //텍스트 설정
                    popupText.text = "너무 빨라요";
                }
            }
            //현재 터치영역 위치가 라인의 좌측방향일 때
            else
            {
                //왼 쪽으로 가는 중이면
                if (isLeft)
                {
                    //텍스트 설정
                    popupText.text = "너무 빨라요";
                }
                else
                {
                    //텍스트 설정
                    popupText.text = "너무 느려요";
                }
            }
        }
    }

    //가는 방향을 판별하는 변수
    bool isLeft = false;
    //팝업 오브젝트
    GameObject popup;
    //팝업 오브젝트의 텍스트
    TMP_Text popupText;
    //방향과 팝업 오브젝트들을 할당
    public void SetLineState(bool state, GameObject popupObject, TMP_Text text)
    {
        isLeft = state;
        popup = popupObject;
        popupText = text;
    }
}
