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

    //ADR-0011 §2.2 (사용자 결정 2026-05-12): 풀링 패턴 + Tutorial의 SetSprite(1/2) 호출이 dispose
    //시점까지 잔존 → 재진입 RebuildGrid가 같은 prefab을 풀에서 가져오면 떠는/찡그린 표정 sprite
    //그대로. 특정 동물(예: 돼지) prefab의 sprites[1/2]이 null이면 visual 자체가 빠짐.
    //OnEnable에서 매 활성화 시 sprites[0](기본 상태)으로 reset해 모든 동물 표시 보장.
    void OnEnable()
    {
        //Start 이전 fire 케이스 — animalImage 미할당. lazy load.
        if (animalImage == null && transform.childCount > 0)
        {
            animalImage = transform.GetChild(0).GetComponent<Image>();
        }
        if (animalImage != null && sprites != null && sprites.Length > 0)
        {
            animalImage.sprite = sprites[0];
        }
    }

    /// <summary>
    /// 동물 표정 이미지 교체를 해주는 부분
    /// </summary>
    /// <param name="i">0 = 기본 상태, 1 = 떠는 표정, 2= 찡그린 표정</param>
    public void SetSprite(int i)
    {
        //ADR-0011 §2.2: animalImage 미할당 가드. Start 이전 호출 시 lazy load.
        if (animalImage == null && transform.childCount > 0)
        {
            animalImage = transform.GetChild(0).GetComponent<Image>();
        }
        if (animalImage != null && sprites != null && i >= 0 && i < sprites.Length)
        {
            animalImage.sprite = sprites[i];
        }
    }
}
