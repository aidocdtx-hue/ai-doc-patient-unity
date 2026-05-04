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

        // 인식이 되면 게임 버튼들이 나타남
        startBtn.SetActive(true); 
        startBtn2.SetActive(true);
        //이전 게임기록이 남아있다면 자동으로 실행해주는 부분
        //startScreen.CheckLastPlayed();
    }

    /// <summary>
    /// 관절 포인트 상태를 제어하는 부분
    /// </summary>
    public void SetPointTrigger()
    {
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
        //일시정지 창 끄기
        pauseScreen.SetActive(false);
        //시간을 다시 원래대로 설정
        //Time.timeScale = 1;
        //시간을 돌리지 않고 캘리브레이션 진행
        StaticData.nowMode = Mode.Calibrate;
        calibrationPanel.SetActive(true);
        pairGamePanel.SetActive(false);
        blockGamePanel.SetActive(false);
    }

    //포인트가 화면 밖으로 나간 시간을 재기 위한 변수
    float pauseTime = 0;
    //포인트가 화면 밖으로 나간 상태를 저장하는 변수
    bool pauseFlag = false;

    //움직임을 무시할 범위, 인스펙터에서 설정
    public float minimumMoveCheckValue = 1;

    private void Update()
    {
        //포인트가 할당되었으면
        if (points != null)
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
            //두 손 사이의 거리를 구하기
            handsDistance = Vector2.Distance(points[17].transform.position, points[18].transform.position);

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

        //포인트가 할당되어있고, 일시정지 화면이 떠있지 않고, 현재 모드가 게임모드면
        if (points != null && !pauseScreen.activeInHierarchy && StaticData.nowMode == Mode.Game)
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
    /// <returns></returns>
    public PointAnnotation GetPoint(int i)
    {
        return points[i];
    }

    //앱이 백그라운드로 이동하면 Pause
    private void OnApplicationPause(bool pause) 
    {
        //포인트가 할당되지 않은 상태면 동작하지 않음
        if (points == null) return;
        //일시정지 동작
        PauseApp();
    }

    /// <summary>
    /// 일시정지 기능 부분
    /// </summary>
    public void PauseApp()
    {
        //게임모드가 아니면 동작하지 않음
        if (StaticData.nowMode != Mode.Game) return;

        //미디어파이프 정지
        _baseRunner.Pause();
        //유니티 시간을 흐르지않게 만듦
        Time.timeScale = 0;
        //일시정지 화면 키기
        pauseScreen.SetActive(true);
        // SendToFlutter.Send("pause"); //250811 수정사항
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
