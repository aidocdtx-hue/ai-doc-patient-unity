using UnityEngine;
using UnityEngine.UI;
using static BlockGame;

/// <summary>
/// 각 블록에 붙어있는 스크립트
/// </summary>
public class Block : MonoBehaviour
{
    //현재 블록의 타입
    public BlockType type;
    //현재 블록의 위치
    public int posX;
    public int posY;
    //타입에 맞는 표정 이미지들
    public Sprite[] sprites;
    //동물 얼굴 이미지
    Image animalImage;

    private void Start()
    {
        //동물 얼굴 이미지를 블럭 자신의 자식의 이미지로 찾아서 설정
        animalImage = transform.GetChild(0).GetComponent<Image>();
    }


    /// <summary>
    /// 현재 블럭의 표정을 다른 이미지로 변경
    /// </summary>
    /// <param name="i">0 : 보통, 1 : 찡그린 표정, 2 : 터지기 직전 표정</param>
    public void SetSprite(int i)
    {
        //ADR-0011 §2.2 (사용자 결정 2026-05-12): animalImage 미할당 가드.
        //GridController.GenerateGrid에서 SetSprite(0) 명시 호출 시 Start 이전 발생 가능.
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
