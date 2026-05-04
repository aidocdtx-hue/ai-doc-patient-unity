using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 슬라이더를 누를 때 텍스트가 출력되는 스크립트
/// </summary>
public class SliderHandleInteraction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    //UI항목들
    private TMP_Text valueText;
    private Slider slider;
    public AudioSource clickSound;

    //하위의 UI항목들 찾아서 할당
    private void Start()
    {
        valueText = transform.GetComponentInChildren<TMP_Text>(true);
        slider = transform.GetComponent<Slider>();
    }

    //클릭/터치를 하기 시작할 때 호출
    public void OnPointerDown(PointerEventData eventData)
    {
        //텍스트 켜기
        valueText.gameObject.SetActive(true);
    }

    //클릭/터치가 끝나면 호출
    public void OnPointerUp(PointerEventData eventData)
    {
        //텍스트 끄기
        valueText.gameObject.SetActive(false);
        clickSound.Play();
    }

    //클릭/터치를 하고있을 때 호출
    public void OnDrag(PointerEventData eventData)
    {
        //텍스트 내용 갱신
        UpdateText(slider.value);
    }

    /// <summary>
    /// 텍스트의 내용을 갱신해주는 부분
    /// </summary>
    /// <param name="value"></param>
    private void UpdateText(float value)
    {
        //텍스트가 비어있지 않으면
        if (valueText != null)
        {
            //텍스트를 슬라이더의 값으로 변경
            valueText.text = slider.value.ToString("00") + "%";
        }
    }
}
