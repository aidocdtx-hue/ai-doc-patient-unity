using Mediapipe.Unity.Sample;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// 앱이 켜지자마자 해야하는 초기 세팅 스크립트
/// </summary>
public class SetMediapipeCamera : MonoBehaviour
{
    //미디어파이프 스크립트 참조
    [SerializeField] private BaseRunner _baseRunner;
    
    void Start()
    {
        //60프레임으로 고정
        Application.targetFrameRate = 60; 
        //초기세팅 시작
        StartCoroutine(StartApp());
        //화면 꺼짐 방지
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    /// <summary>
    /// 초기 세팅을 해주는 부분
    /// </summary>
    /// <returns></returns>
    public IEnumerator StartApp()
    {
        //미디어 파이프 로딩을 위해 1초 대기
        yield return new WaitForSeconds(1f);
        
#if UNITY_EDITOR
#elif PLATFORM_ANDROID //안드로이드에서 카메라를 설정하는 부분
//미디어파이프를 멈추고 이미지 소스를 전면카메라로 변경
            _baseRunner.Pause();
            var imageSource = ImageSourceProvider.ImageSource;
            imageSource.SelectSource(1);

            //카메라의 해상도를 설정
            var imageSource2 = ImageSourceProvider.ImageSource;
            var resolutions = imageSource2.availableResolutions;
            var options = resolutions.Select(resolution => resolution.ToString()).ToList();
            int count = 0;
            //옵션에서 원하는 해상도의 인덱스 찾기
            for(int i = 0; i < options.Count; i++)
            {
                if (options[i].Contains(StaticData.camaraResolution))
                {
                    count = i;
                }
            }
            //원하는 옵션 반영
            imageSource2.SelectResolution(count);
            //미디어파이프 재실행
            _baseRunner.Play();
#endif

        

    }
}
