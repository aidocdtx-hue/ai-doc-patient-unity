using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 버튼을 누를 때 텍스트 색을 같이 바꿔주기 위한 스크립트
/// </summary>
public class ButtonTextColorChanger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    //텍스트 변수
    private TMP_Text buttonText;
    //인스펙터에서 색 지정
    public Color pressedColor = Color.red;
    public Color normalColor = Color.white;

    private void Start()
    {
        //텍스트 찾아서 할당
        buttonText = GetComponentInChildren<TMP_Text>();
    }

    /// <summary>
    /// 클릭했을 때
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData)
    {
        //텍스트가 비어있지 않다면 색 변경
        if (buttonText != null)
            buttonText.color = pressedColor;
    }

    /// <summary>
    /// 클릭이 해제됐을 때
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(PointerEventData eventData)
    {
        //텍스트가 비어있지 않다면 색 변경
        if (buttonText != null)
            buttonText.color = normalColor;
    }
}
