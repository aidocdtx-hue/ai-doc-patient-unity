using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 미디어파이프 포인트들을 컨트롤하는 스크립트
/// </summary>
public class PointsController : MonoBehaviour
{
    //미디어파이프 포인트
    private Transform pointParent;
    /// <summary>
    /// 미디어 파이프가 생성한 포인트들
    /// </summary>
    private PointAnnotation[] points;
    //private PointAnnotation[] savePoints;

    //미디어파이프 라인
    private Transform connectionParent;
    /// <summary>
    /// 미디어 파이프가 생성한 라인들
    /// </summary>
    private ConnectionAnnotation[] connections; 

    /// <summary>
    /// 두 손 사이에 생성되는 트리거 포인트
    /// </summary>
    public Transform colliderArea;

    //스크립트 참조
    public SaveLandmarkDatas saveLandmarks;
    private PoseLandmarkerResultAnnotationController PoseLandmarkerResultAnnotationController;
    [SerializeField] private BaseRunner _baseRunner;
    
    //오른손과 왼 손 사이의 거리
    float handsDistance; 
    
    //시작 버튼
    public GameObject startBtn;
    public GameObject startBtn2;
    public StartScreen startScreen;

    //각 화면들
    public GameObject pauseScreen;

    //손 오브젝트들
    public GameObject leftHand;
    public GameObject rightHand;

    public GameObject calibrationPanel;
    public GameObject pairGamePanel;
    public GameObject blockGamePanel;

    //시작 부분
    void Start()
    {
        //코루틴 시작
        StartCoroutine(Initialize());
    }

    //초기 할당
    IEnumerator Initialize()
    {
        //할당이 될 때 까지 무한루프
        while (pointParent == null)
        {
            //카메라에 사람이 인식 되면 포인트가 생성, 포인트가 생성될 때 까지 대기
            if (FindObjectOfType<PointListAnnotation>(true) == null) 
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            //미디어파이프가 생성한 포인트들을 변수에 할당
            pointParent = FindObjectOfType<PointListAnnotation>(true).transform;
            points = pointParent.GetComponentsInChildren<PointAnnotation>(true);
            connectionParent = FindObjectOfType<ConnectionListAnnotation>(true).transform;
            connections = connectionParent.GetComponentsInChildren<ConnectionAnnotation>(true);
            PoseLandmarkerResultAnnotationController = FindAnyObjectByType<PoseLandmarkerResultAnnotationController>();
        }

        // 시작 버튼은 씬에서 처음부터 활성화 — 캘리브레이션 패널(어깨/손 3초 유지)이 실제 인식 게이트 역할
        // 인식이 끝났을 때 사용자가 이미 캘리브레이션 패널에 진입해 있으면 어깨 트리거 즉시 활성화
        if (StaticData.nowMode == Mode.Calibrate)
        {
            SetPointTrigger();
        }
        //이전 게임기록이 남아있다면 자동으로 실행해주는 부분
        //startScreen.CheckLastPlayed();
    }

    /// <summary>
    /// 관절 포인트 상태를 제어하는 부분
    /// </summary>
    public void SetPointTrigger()
    {
        //points 미할당(인식 전) 보호 — 캘리브레이션 진입이 인식 완료보다 빠를 수 있음
        if (points == null) return;
        //모든 포인터의 트리거를 해제
        AllPointFalse();
        //캘리브레이션 단계면 
        if(StaticData.nowMode == Mode.Calibrate)
        {
            //어깨 콜라이더를 켜줌
            points[11].GetComponent<Collider>().isTrigger = true;
            points[12].GetComponent<Collider>().isTrigger = true;
        }
    }

    /// <summary>
    /// 모든 포인터의 트리거를 해제하는 부분
    /// </summary>
    public void AllPointFalse()
    {
        if (points == null) return;
        //포인트의 갯수만큼 반복
        for (int i = 0; i < points.Length; i++)
        {
            //콜라이더 상태 변경
            points[i].GetComponent<Collider>().isTrigger = false;
        }
    }

    /// <summary>
    /// Pause상태일 때 다시 게임으로 돌아가는 부분. 게임 재시작 버튼의 인스펙터에서 접근
    /// </summary>
    public void Resume()
    {
        //코루틴 시작
        StartCoroutine(ClosePauseScreen());
    }

    /// <summary>
    /// 게임 재시작 기능
    /// </summary>
    /// <returns></returns>
    IEnumerator ClosePauseScreen()
    {
        //미디어파이프 부터 먼저 실행
        _baseRunner.Play();
        //2초 대기, timescale이 0인 상태이므로 WaitForSecondsRealtime을 사용해야함
        yield return new WaitForSecondsRealtime(2f);
        //일시정지 창 끄기 (dead path — Flutter SeniorDialog 통합 후 pauseScreen은 활성 안 됨)
        pauseScreen.SetActive(false);
        //시간을 다시 원래대로 설정
        //Time.timeScale = 1;
        //시간을 돌리지 않고 캘리브레이션 진행
        StaticData.nowMode = Mode.Calibrate;
        calibrationPanel.SetActive(true);
        pairGamePanel.SetActive(false);
        blockGamePanel.SetActive(false);
        //_isPaused 리셋 — 다음 PauseApp 호출 가능하게.
        _isPaused = false;
    }

    //포인트가 화면 밖으로 나간 시간을 재기 위한 변수
    float pauseTime = 0;
    //포인트가 화면 밖으로 나간 상태를 저장하는 변수
    bool pauseFlag = false;

    //움직임을 무시할 범위, 인스펙터에서 설정
    public float minimumMoveCheckValue = 1;

    private void Update()
    {
        //points 배열이 할당되고 손 landmark(15~20번) 모두 들어왔을 때만 인덱스 접근.
        //카메라가 사람 일부만 잡거나 인식 부분 실패 시 배열이 짧을 수 있어 IndexOutOfRangeException 방지.
        bool handsLandmarksReady = points != null && points.Length >= 21;
        if (handsLandmarksReady)
        {
            //손 포인트들의 위치에 따라 왼 손과 오른손의 위치 이동
            leftHand.transform.position = (points[20].transform.position + points[16].transform.position) / 2f;
            rightHand.transform.position = (points[19].transform.position + points[15].transform.position) / 2f;
        }

        //현재 모드가 게임모드이거나 캘리브레이션모드이거나 튜토리얼 모드라면
        if (StaticData.nowMode == Mode.Game || StaticData.nowMode == Mode.Calibrate || StaticData.nowMode == Mode.Tutorial)
        {
            //두 손 위치의 가운데 위치를 구하기
            Vector2 center = new Vector2((leftHand.transform.localPosition.x + rightHand.transform.localPosition.x) / 2, (leftHand.transform.localPosition.y + rightHand.transform.localPosition.y) / 2);
            //두 손 사이의 거리를 구하기. landmark 미준비 시 큰 값으로 두면 아래 < 40 분기에 안 걸려 colliderArea 비활성 유지.
            handsDistance = handsLandmarksReady
                ? Vector2.Distance(points[17].transform.position, points[18].transform.position)
                : float.MaxValue;

            //현재 터치영역 위치에서 손을 움직인 값이 무시 범위 이내라면
            if ((colliderArea.localPosition.x - center.x < minimumMoveCheckValue && colliderArea.localPosition.x - center.x > -minimumMoveCheckValue) || 
                (colliderArea.localPosition.y - center.y < minimumMoveCheckValue && colliderArea.localPosition.y - center.y > -minimumMoveCheckValue))
            {
                //아무 동작 하지 않음
            }
            //무시 범위 밖이면
            else
            {
                //터치영역을 손 위치로 이동
                colliderArea.localPosition = center;
            }

            //손 사이 거리가 40보다 가까우면
            if (handsDistance < 40)
            {
                //터치영역 활성화
                colliderArea.GetComponent<Collider>().isTrigger = true;
                colliderArea.gameObject.SetActive(true);
            }
            //40보다 멀면
            else
            {
                //터치영역 비활성화
                colliderArea.GetComponent<Collider>().isTrigger = false;
                colliderArea.gameObject.SetActive(false);
            }
        }
        //다른 모드들이라면
        else
        {
            //터치영역 비활성화
            colliderArea.gameObject.SetActive(false);
        }

        //포인트가 할당되어있고, PoseLandmarker도 준비됨, 일시정지 화면이 떠있지 않고, 현재 모드가 게임모드면
        if (points != null && PoseLandmarkerResultAnnotationController != null && !pauseScreen.activeInHierarchy && StaticData.nowMode == Mode.Game)
        {
            //어깨 포인트가 화면 밖으로 벗어나면
            if (PoseLandmarkerResultAnnotationController.GetLandmark(12).x < 0.05f || PoseLandmarkerResultAnnotationController.GetLandmark(11).x > 0.95f)
            {
                //일시정지 타이머 시작
                pauseFlag = true;
            }
            //화면 밖이 아니면
            else
            {
                //일시정지 타이머 종료
                pauseFlag = false;
            }
        }

        //일시정지 타이머
        if (pauseFlag)
        {
            //시간 재기 시작
            pauseTime += Time.deltaTime;
            //1초 이상 됐을 때
            if (pauseTime > 1f)
            {
                //일시정지 동작
                PauseApp();
            }
            //상태를 풀어줘야 무한정 진입하지 않음
            pauseFlag = false;
        }
        //타이머 종료되면
        else
        {
            //시간을 0으로 초기화
            pauseTime = 0;
        }
    }

    /// <summary>
    /// 포인트를 반환하는 부분
    /// </summary>
    /// <param name="i">미디어파이프 포인트의 인덱스</param>
    /// <returns>points 미할당/인덱스 범위 초과 시 null — 호출처에서 null 가드 필수</returns>
    public PointAnnotation GetPoint(int i)
    {
        //points 미할당(MediaPipe 추론 첫 결과 도달 전)에 호출되면 NRE 발생.
        //Y-2 후 카메라/추론 시작이 ClickBtn 시점으로 미뤄져 캘리브레이션 진입 시 race condition 가능.
        //CalibrationPoint.Start가 GetPoint 호출 시 NRE 발생하면 targetCollider 미할당 → 자세 인식 불가.
        if (points == null || i < 0 || i >= points.Length) return null;
        return points[i];
    }

    /// <summary>
    /// 백그라운드 복귀 또는 캘리브레이션 진입 시 BaseRunner 재개.
    /// Play() 대신 Resume() 호출 — Play()는 Stop+새 Run 사이클을 돌아 webCamTexture nullify +
    /// taskApi.Close → PointListAnnotation/points stale 발생. Resume()은 isPaused=false +
    /// ImageSource.Resume(webCamTexture.Play 재호출)만 — 가벼움 + stale 회피.
    /// IsPaused 가드 — 이미 Play 중이면 무시. 첫 시작용은 StartMediapipe(Play 호출) 사용.
    /// </summary>
    public void ResumeMediapipe()
    {
        if (_baseRunner != null && _baseRunner.IsPaused) _baseRunner.Resume();
    }

    /// <summary>
    /// 앱 백그라운드 진입 시 MediaPipe 추론 + 카메라(WebCamTexture) 정지.
    /// _baseRunner.Pause()는 내부에서 imageSource.Pause() → webCamTexture.Pause() 호출 →
    /// 카메라 LED OFF + 추론 정지. 복귀 시 ResumeMediapipe()로 재개.
    /// idempotent — 이미 Pause 중이어도 무해.
    /// </summary>
    public void PauseMediapipe()
    {
        if (_baseRunner != null) _baseRunner.Pause();
    }

    /// <summary>
    /// BaseRunner의 첫 Play 명시 호출 — autoStart=false로 자동 시작이 막힌 시점에서 카메라/추론 시작.
    /// 시작 화면(게임 선택) → 사용자가 게임 선택(StartScreen.ClickBtn/ClickPairBtn) → 가이드 진입 시 호출.
    /// Play()는 새 Run 코루틴 시작 → imageSource.Play() → webCamTexture 생성 + 카메라 LED ON.
    /// 이미 Play 중이면 LegacySolutionRunner/VisionTaskApiRunner.Play 가 Stop 후 새로 시작 →
    /// stutter 발생 가능. 호출처에서 첫 시작 1회만 호출하도록 보장.
    /// </summary>
    public void StartMediapipe()
    {
        if (_baseRunner != null) _baseRunner.Play();
        if (!_cameraReadyPending) StartCoroutine(NotifyCameraReadyAfterStart());
    }

    //ADR-0011 §2.1: 카메라 ACTIVE 시점 신호 송신.
    //logcat (PID 22286, 2026-05-11 11:43) 측정상 scene_loaded 발화 후 카메라 OPEN까지 ~9초 갭.
    //Flutter 측 1초 timer로는 부족 → 인디케이터 사라진 후 멈춘 frame 노출. 이 coroutine이
    //StartMediapipe 호출 후 카메라/MediaPipe runner 시작까지 wait + Flutter에 camera_ready 송신.
    //
    //시간 기반 7초 마진 — BaseRunner internal imageSource 정확한 첫 frame hook은 MediaPipe 패키지
    //수정 필요해 후속 정밀화. Flutter 측 2초 fallback timer가 추가 안전망.
    //_cameraReadyPending 가드 — 동시 중복 송신 방지(매 진입마다 false로 reset되어 재진입에도 동작).
    private bool _cameraReadyPending = false;
    private IEnumerator NotifyCameraReadyAfterStart()
    {
        _cameraReadyPending = true;
        try
        {
            yield return new WaitForSeconds(7f);
            SendToFlutter.Send("camera_ready");
        }
        finally
        {
            _cameraReadyPending = false;
        }
    }

    /// <summary>
    /// 백그라운드 복귀 후 사용자가 "이어하기" 선택 시 가이드 화면으로 복귀.
    /// 게임 진행 패널/일시정지/캘리브레이션을 닫고 startScreen.gameGuidePanel만 활성화.
    /// 시작 화면 GameObject(StartScreen)는 비활성 그대로 — 사용자는 게임 선택 다시 안 함, 직전 isPairGame 유지.
    /// timeScale + AudioListener.pause 복원. ResumeMediapipe로 카메라/추론 재개도 함께 처리.
    /// </summary>
    public void ReturnToGameGuide()
    {
        if (pauseScreen != null) pauseScreen.SetActive(false);
        if (pairGamePanel != null) pairGamePanel.SetActive(false);
        if (blockGamePanel != null) blockGamePanel.SetActive(false);
        if (calibrationPanel != null) calibrationPanel.SetActive(false);
        if (startScreen != null && startScreen.gameGuidePanel != null)
        {
            startScreen.gameGuidePanel.SetActive(true);
        }

        //ADR-0011 §2.2 (사용자 결정 2026-05-12): 가이드 화면에서 CalibrationPoint(어깨 위치 트리거)
        //주황 볼 시각화 노출 차단. CalibrationPoint이 calibrationPanel과 별도 path에 있어
        //calibrationPanel.SetActive(false)만으로 비활성 안 됨 → Renderer 명시 OFF.
        //Calibration.OnEnable에서 다시 ON되어 캘리브레이션 단계엔 정상 노출.
        var calibrationPoints = FindObjectsOfType<CalibrationPoint>(true);
        foreach (var cp in calibrationPoints)
        {
            var r = cp.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
        _isPaused = false;
        ResumeMediapipe();
    }

    /// <summary>
    /// 운동 중단/종료 후 신규 진입(start) 시 게임 선택 화면으로 강제 복귀.
    /// 게임/일시정지/캘리브레이션 패널을 모두 끄고 StartScreen GameObject를 재활성화.
    /// 직전 게임 상태(isPairGame, isNormalEnd, NormalEnd PlayerPref)와 timeScale, AudioListener.pause도 함께 리셋.
    /// idempotent — 이미 시작 화면 상태에서 호출돼도 무해.
    /// </summary>
    public void ResetToInitial()
    {
        if (pauseScreen != null) pauseScreen.SetActive(false);
        if (pairGamePanel != null) pairGamePanel.SetActive(false);
        if (blockGamePanel != null) blockGamePanel.SetActive(false);
        if (calibrationPanel != null) calibrationPanel.SetActive(false);
        //gameGuidePanel도 명시 정리 — 가이드 도중 종료 후 새 운동 시작 시 시작 화면 위에 가이드 잔존 회피.
        if (startScreen != null && startScreen.gameGuidePanel != null) startScreen.gameGuidePanel.SetActive(false);
        if (startScreen != null) startScreen.gameObject.SetActive(true);

        PlayerPrefs.SetInt("NormalEnd", 0);
        StaticData.isNormalEnd = false;
        StaticData.isPairGame = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        _isPaused = false;
    }

    //앱이 백그라운드로 이동하면 Pause
    private void OnApplicationPause(bool pause)
    {
        //모바일 홈 버튼 / 화면 OFF / 다른 앱 전환 등 백그라운드 진입 시 모든 오디오 음소거.
        //resume 시 자동으로 false 복원되어 효과음/BGM 그대로 이어짐.
        AudioListener.pause = pause;

        //모드 무관하게 MediaPipe 추론도 정지/재개. 시작 화면/캘리브레이션에서 다른 앱 전환 시
        //추론 + 카메라 자원 낭비 방지. Pause/Play는 idempotent — 게임 모드 PauseApp/ClosePauseScreen 흐름과 안전 공존.
        if (_baseRunner != null)
        {
            if (pause) _baseRunner.Pause();
            else _baseRunner.Play();
        }

        //포인트가 할당되지 않은 상태면 동작하지 않음
        if (points == null) return;
        //일시정지 동작
        PauseApp();
    }

    //focus 변경 hook — 메뉴 버튼/다른 앱 전환 시 OnApplicationPause가 발화하지 않을 수 있어 보조.
    //flutter_embed_unity 라이프사이클 브리지 차이로 Pause가 누락되는 케이스에서도 focus는 거의 항상 잃음.
    //AudioListener.pause / _baseRunner Pause/Play 대입은 idempotent이라 OnApplicationPause와 중복 호출되어도 안전.
    private void OnApplicationFocus(bool hasFocus)
    {
        AudioListener.pause = !hasFocus;

        if (_baseRunner != null)
        {
            if (!hasFocus) _baseRunner.Pause();
            else _baseRunner.Play();
        }
    }

    /// <summary>
    /// 일시정지 기능 부분
    /// </summary>
    //_isPaused 가드 — pauseScreen 사용 안 하므로 activeInHierarchy 대신 별도 boolean으로
    //중복 PauseApp 발화 차단. ClosePauseScreen에서 false로 리셋.
    private bool _isPaused = false;
    public bool IsAppPaused => _isPaused;

    public void PauseApp()
    {
        //게임모드가 아니면 동작하지 않음
        if (StaticData.nowMode != Mode.Game) return;
        //이미 일시정지 상태면 중복 송신 회피
        if (_isPaused) return;
        _isPaused = true;

        //미디어파이프 정지
        _baseRunner.Pause();
        //유니티 시간을 흐르지않게 만듦
        Time.timeScale = 0;
        //이전: pauseScreen.SetActive(true)로 Unity 자체 일시정지 화면 표시.
        //변경(사용자 결정): Flutter SeniorDialog로 일시정지 UI 통합 — pause_request 메시지 송신.
        //Flutter가 다이얼로그 표시 → 사용자 "재개" 시 pause_release 메시지 → GetFlutterMessage가 Resume() 호출.
        //Flutter "운동 종료" 시 _abortExercise + navigate → UnityService.dispose → mute → ResetToInitial.
        SendToFlutter.Send("{\"command\":\"pause_request\"}");
        //게임 모드에서 일시정지가 되면 각 포인트들의 데이터를 저장
        if (StaticData.nowMode == Mode.Game)
        {
            saveLandmarks.TempSaveDatas("pause");
        }
    }

    //포인트가 할당될 때 까지 대기하는 코루틴
    Coroutine skeletonDelayCoroutine;

    /// <summary>
    /// 뼈대 보이기 여부 기능동작부분
    /// </summary>
    /// <param name="state"></param>
    public void SetPointandConnectionEnabled(bool state)
    {
        //포인트가 할당되지 않으면
        if (points == null)
        {
            //코루틴이 할당된 상태면 기존 코루틴 멈추기
            if (skeletonDelayCoroutine != null) StopCoroutine(skeletonDelayCoroutine);
            //코루틴 실행
            skeletonDelayCoroutine = StartCoroutine(delayPoint(state));
        }
        //포인트가 할당되어있으면
        else
        {
            //포인트와 스켈레톤 화면에 표시여부 설정
            foreach (PointAnnotation point in points)
            {
                point.GetComponent<Renderer>().enabled = state;
            }
            foreach (ConnectionAnnotation connection in connections)
            {
                connection.GetComponent<Renderer>().enabled = state;
            }
        }
        
    }

    /// <summary>
    /// 할당이 되기전에 접근하는것을 방지
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    IEnumerator delayPoint(bool state)
    {
        //0.1초 대기
        yield return new WaitForSeconds(0.1f);
        //뼈대보이기 재호출
        SetPointandConnectionEnabled(state);
    }

    /// <summary>
    /// 포인트들을 저장
    /// </summary>
    public void StartSaveData()
    {
        //랜드마크들 전달
        saveLandmarks.StartSave(points);
    }
}
