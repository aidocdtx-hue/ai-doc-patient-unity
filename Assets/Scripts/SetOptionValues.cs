using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 앱이 실행될 때 마지막에 설정한 설정값들을 적용하는 스크립트
/// </summary>
public class SetOptionValues : MonoBehaviour
{
    //옵션창의 항목들
    public Slider masterSlider;
    public Slider BGMSlider;
    public Slider SFXSlider;

    //스크립트 참조
    public PointsController pointsController;

    public AudioSource[] audios; // 효과음 사운드들

    private void Awake()
    {
        //해당 키가 있으면
        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            //키에 맞는 값을 가져와서 할당
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume");
            //전체음량 슬라이더의 값을 설정
            masterSlider.value = masterVolume;
        }
        //해당 키가 있으면
        if (PlayerPrefs.HasKey("BGMVolume"))
        {
            //키에 맞는 값을 가져와서 할당
            float BGMVolume = PlayerPrefs.GetFloat("BGMVolume");
            //배경음량 슬라이더의 값을 설정
            BGMSlider.value = BGMVolume;
        }
        //해당 키가 있으면
        if (PlayerPrefs.HasKey("SFXVolume"))
        {
            //키에 맞는 값을 가져와서 할당
            float SFXVolume = PlayerPrefs.GetFloat("SFXVolume");
            //효과음 슬라이더의 값을 설정
            SFXSlider.value = SFXVolume;
            //모든 효과음의 음량을 조절
            foreach (AudioSource audioSource in audios)
            {
                audioSource.volume = SFXVolume/100;
            }
        }
        //해당 키가 있으면
        if (PlayerPrefs.HasKey("ExtraDisc"))
        {
            //키에 맞는 값을 가져와서 할당
            int state = PlayerPrefs.GetInt("ExtraDisc");
            //1이면
            if (state == 1)
            {
                //상세설명 켜기
                StaticData.isExtraDisc = true;
            }
            //0이면
            else
            {
                //상세설명 끄기
                StaticData.isExtraDisc = false;
            }
        }
    }
}
