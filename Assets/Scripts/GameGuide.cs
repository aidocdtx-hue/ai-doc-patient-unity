using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 설명화면의 기능
/// </summary>
public class GameGuide : MonoBehaviour
{
    //설명 화면들
    public GameObject[] pages;
    //버튼 클릭 사운드
    public AudioSource sound;


    /// <summary>
    /// 다음 화면 버튼 이벤트
    /// </summary>
    public void NextPageBtn()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            //페이지가 켜져있으면
            if (pages[i].activeInHierarchy)
            {
                //내 페이지를 끄고 다음 페이지를 킨다
                pages[i].SetActive(false);
                pages[i + 1].SetActive(true);
                break;
            }
        }
        //효과음 재생
        sound.Play();
    }

    /// <summary>
    /// 이전 화면 버튼 이벤트
    /// </summary>
    public void PrevPageBtn()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            //페이지가 켜져있으면
            if (pages[i].activeInHierarchy)
            {
                //내 페이지를 끄고 이전 페이지를 킨다
                pages[i].SetActive(false);
                pages[i - 1].SetActive(true);
                break;
            }
        }
        //효과음 재생
        sound.Play();
    }
}
