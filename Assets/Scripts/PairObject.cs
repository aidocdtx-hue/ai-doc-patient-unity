using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 짝맞추기 타일이 가지고있는 스크립트
/// </summary>
public class PairObject : MonoBehaviour
{
    //타입을 인스펙터에서 설정
    public PairType type;
    //위치값을 저장할 변수
    public int posX;
    public int posY;
    //표정 이미지들
    public Sprite[] sprites;
    //동물 이미지
    Image animalImage;

    private void Start()
    {
        //동물 이미지를 찾아서 할당
        animalImage = transform.GetChild(0).GetComponent<Image>();
    }

    /// <summary>
    /// 동물 표정 이미지 교체를 해주는 부분
    /// </summary>
    /// <param name="i">0 = 기본 상태, 1 = 떠는 표정, 2= 찡그린 표정</param>
    public void SetSprite(int i)
    {
        animalImage.sprite = sprites[i];
    }
}
