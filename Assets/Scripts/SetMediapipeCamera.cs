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

    //BaseRunner 자동 Play 막기 — 시작 화면(게임 선택)에서 카메라/추론 OFF 유지.
    //Awake는 모든 컴포넌트 Awake 완료 후 Start 호출되므로 BaseRunner.Start의 if(_autoStart) 진입 전에 false 보장.
    //가이드 진입 시 StartScreen.ClickBtn/ClickPairBtn → pointsController.StartMediapipe()에서 명시 Play.
    void Awake()
    {
        if (_baseRunner != null) _baseRunner.autoStart = false;
    }

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
            //전면 카메라는 WebCamSource.Initialize()에서 default로 이미 선택되므로 SelectSource 호출 불필요.
            //해상도 변경만 적용 — Pause→SelectResolution→Play 순환은 webCamTexture 재시작 비용을 동반하므로
            //원하는 해상도가 device default와 같으면 Pause/Play 자체를 스킵해 흐림 구간을 추가로 제거.
            var imageSource = ImageSourceProvider.ImageSource;
            var resolutions = imageSource.availableResolutions;
            var options = resolutions.Select(resolution => resolution.ToString()).ToList();
            int count = -1;
            for(int i = 0; i < options.Count; i++)
            {
                if (options[i].Contains(StaticData.camaraResolution))
                {
                    count = i;
                    break;
                }
            }
            if (count >= 0 && imageSource.resolution.ToString() != options[count])
            {
                _baseRunner.Pause();
                imageSource.SelectResolution(count);
                _baseRunner.Play();
            }

            //시작 화면 부하 절감용 _baseRunner.Pause() 제거 — WebCamSource.Pause가
            //webCamTexture.Pause()까지 호출해 카메라 LED가 꺼졌다가 캘리브레이션 진입 시
            //ResumeMediapipe로 다시 켜지면서 사용자 시각으로 카메라가 두 번 켜져 보임.
            //시작 버튼은 Q 묶음에서 처음부터 활성화돼 있어 인식 게이트 의존 없음 →
            //시작 화면 동안 추론이 계속 돌아도 버튼 응답성 영향 최소.
#endif

        

    }
}
