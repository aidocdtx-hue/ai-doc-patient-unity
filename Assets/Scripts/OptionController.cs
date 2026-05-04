using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 항목들을 조정하고 적용하는 스크립트
/// </summary>
public class OptionController : MonoBehaviour
{
    //사운드들
    public AudioSource BGMSource;
    public AudioSource SFXSource;
    public AudioSource[] audios;
    public AudioSource clickSound;

    //옵션 각 항목들
    public Slider masterSlider;
    public Slider BGMSlider;
    public Slider SFXSlider;
    public Toggle boneToggle;
    public Toggle boneToggle2;
    public Toggle extraDiscToggle;
    public Toggle extraDiscToggle2;

    //참조 스크립트
    public PointsController pointsController;

    //전체음량 값을 저장할 변수
    float masterValue = 100;


    private void Start()
    {
        //상세 안내 여부 키가 존재하면
        if (PlayerPrefs.HasKey("ExtraDisc"))
        {
            //값을 가져와서 할당
            int state = PlayerPrefs.GetInt("ExtraDisc");
            //만약 저장된 값이 1이면 (켜기)
            if (state == 1)
            {
                //상세 설명 토글을 켜기
                extraDiscToggle.isOn = true;
            }
            else
            {
                //상세 설명 토글을 끄기
                extraDiscToggle2.isOn = true;
            }
        }
    }

    /// <summary>
    /// 전체 음량 슬라이더 제어, 슬라이더의 OnValueChanged에 따라 자동으로 호출되는 부분
    /// </summary>
    /// <param name="value"></param>
    public void ChangeMasterVolume(float value)
    {
        //1~100의 값이 들어오기 때문에 100을 나눈 값을 저장
        masterValue = value / 100;
        //슬라이더 하위의 텍스트를 검색해서 할당
        TMP_Text nowText = masterSlider.transform.GetComponentInChildren<TMP_Text>(true);
        //텍스트를 슬라이더값%로 변경
        nowText.text = value.ToString("00") + "%";

        //볼륨은 0~1 값을 가지기 때문에 마스터밸류와 각 슬라이더의 값을 100을 나눈 값들을 서로 곱해서 볼륨 설정
        BGMSource.volume = masterValue * (BGMSlider.value / 100);
        SFXSource.volume = masterValue * (SFXSlider.value / 100);
        //효과음 음량 전체 적용
        SetSFXVolumes(SFXSource.volume);

        //설정값 로컬에 저장
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    /// <summary>
    /// 배경 음량 슬라이더 제어 부분, 슬라이더의 OnValueChanged에 따라 자동으로 호출되는 부분
    /// </summary>
    /// <param name="value"></param>
    public void ChangedBGMVolume(float value)
    {
        //BGM 오디오소스 사운드 조절, 마스터(0~1) * 밸류
        BGMSource.volume = value / 100 * masterValue;
        //슬라이더 하위의 텍스트를 검색해서 할당
        TMP_Text nowText = BGMSlider.transform.GetComponentInChildren<TMP_Text>(true);
        //텍스트를 슬라이더값%로 변경
        nowText.text = value.ToString("00") + "%";
        //설정값 로컬에 저장
        PlayerPrefs.SetFloat("BGMVolume", value);
    }

    /// <summary>
    /// 효과음 음량 슬라이더 제어 부분, 슬라이더의 OnValueChanged에 따라 자동으로 호출되는 부분
    /// </summary>
    /// <param name="value"></param>
    public void ChangedSFXVolume(float value)
    {
        //SFX 오디오소스 사운드 조절, 마스터(0~1) * 밸류
        SFXSource.volume = value / 100 * masterValue;
        //슬라이더 하위의 텍스트를 검색해서 할당
        TMP_Text nowText = SFXSlider.transform.GetComponentInChildren<TMP_Text>(true);
        //텍스트를 슬라이더값%로 변경
        nowText.text = value.ToString("00") + "%";
        //효과음 음량 전체 적용
        SetSFXVolumes(SFXSource.volume);
        //설정값 로컬에 저장
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    /// <summary>
    /// 효과음 음량을 모든 효과음 오디오소스에 적용하는 부분
    /// </summary>
    /// <param name="value"></param>
    public void SetSFXVolumes(float value)
    {
        foreach (AudioSource audioSource in audios)
        {
            audioSource.volume = value;
        }
    }

    /// <summary>
    /// 상세 설명 제어 부분, 토글의 OnValueChanged에 따라 자동으로 호출되는 부분
    /// </summary>
    /// <param name="value"></param>
    public void ExtraDiscriptionToggle(bool value)
    {
        //클릭 사운드 재생
        clickSound.Play();
        if (value)
        {
            //StaticData에 값 저장
            StaticData.isExtraDisc = true;
            //로컬에 값 저장
            PlayerPrefs.SetInt("ExtraDisc", 1);
        }
        else
        {
            //StaticData에 값 저장
            StaticData.isExtraDisc = false;
            //로컬에 값 저장
            PlayerPrefs.SetInt("ExtraDisc", 0);
        }
    }

    //코루틴을 담아놓을 변수
    Coroutine textCoroutine;

    /// <summary>
    /// 각 음량의 +버튼을 눌러 값을 조절하는 부분, 인스펙터에서 이벤트 호출
    /// </summary>
    /// <param name="slider">각 버튼의 인스펙터에 있는 버튼이벤트에 할당한 슬라이더</param>
    public void PlusButton(Slider slider)
    {
        //클릭 사운드 재생
        clickSound.Play();
        //슬라이더의 값을 변경
        slider.value = slider.value + 10;
        //실행중인 코루틴 종료
        if(textCoroutine != null) StopCoroutine(textCoroutine);
        //코루틴 실행
        textCoroutine = StartCoroutine(ShowText(slider.transform.GetComponentInChildren<TMP_Text>(true), slider.value));
    }

    /// <summary>
    /// 각 음량의 -버튼을 눌러 값을 조절하는 부분
    /// </summary>
    /// <param name="slider"></param>
    public void MlnusButton(Slider slider)
    {
        //클릭 사운드 재생
        clickSound.Play();
        //슬라이더의 값을 변경
        slider.value = slider.value - 10;
        //실행중인 코루틴 종료
        if (textCoroutine != null) StopCoroutine(textCoroutine);
        //코루틴 실행
        textCoroutine = StartCoroutine(ShowText(slider.transform.GetComponentInChildren<TMP_Text>(true), slider.value));
    }

    /// <summary>
    /// 버튼으로 음량조절 시 텍스트를 5초간 띄우는 부분
    /// </summary>
    /// <param name="text"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    IEnumerator ShowText(TMP_Text text, float value)
    {
        //텍스트 변경 후 표출
        text.text = value.ToString("00") + "%";
        text.gameObject.SetActive(true);
        //5초 대기
        yield return new WaitForSeconds(5f);
        //텍스트 끄기
        text.gameObject.SetActive(false);
    }
}
