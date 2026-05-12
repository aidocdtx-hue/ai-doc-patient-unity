using DG.Tweening;
using Google.Protobuf.WellKnownTypes;
using Mediapipe.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using static DG.Tweening.DOTweenAnimation;

/// <summary>
/// 짝맞추기 게임 스크립트
/// </summary>
public class PairGame : MonoBehaviour
{
    //스크립트 참조
    public PointsController pointsController;
    public PairGrid leftGrid;
    public PairGrid rightGrid;
    public Timer timer;
    public SetProgress progress;
    public SaveLandmarkDatas saveLandmark;
    public ShowScoreImages showScoreImages;

    //각 방향에서 타일을 선택했는지 여부를 판별하는 변수
    bool isLeftSet = false;
    bool isRightSet = false;

    //타일의 스크립트를 담을 변수
    PairObject leftPairObject;
    PairObject rightPairObject;

    //팝업 오브젝트와 텍스트
    public GameObject popup;
    public TMP_Text popupText;

    //점수 텍스트
    public TMP_Text scoreText;

    //게임 시간
    float gameTime = 600f;

    //화살표 이미지와 스프라이트
    public Image leftArrow;
    public Image rightArrow;
    public Sprite[] arrowSprites;

    //가이드라인 오브젝트
    public GameObject guideLine;

    //가이드라인 스크립트
    GuideLineController lineController;

    //가이드라인 도착지점들
    public Transform guideDestLeft;
    public Transform guideDestRight;

    // 목표 도달시간
    public float lineMoveDuration = 5f;  
    bool nowLeft = true;
    bool isFirst = true;

    //ADR-0011 §2.2 (사용자 결정 2026-05-11): 비정상종료 복원 케이스 추적.
    //true면 Tutorial 끝의 timer.SetRamainTime(gameTime)이 line 195의 복원된 time을 덮어쓰지
    //않도록 SKIP. OnEnable에서 false reset, line 148 분기에서 true set.
    bool _restoredFromIncomplete = false;

    //타일이 나타나는 양 쪽 오브젝트 변수
    public GameObject leftSide;
    public GameObject rightSide;

    //오브젝트의 위치를 이동시킬때의 거리를 계산하는 변수
    float sideMoveValue = 600f;

    //사운드들
    public AudioSource bgmSound;
    public AudioClip pairBGM;
    public AudioSource matchSound;
    public AudioClip[] matchSounds;

    //튜토리얼 검은배경과 설명 텍스트
    public GameObject dim;
    public TMP_Text tutorialText;

    //각도 단계
    int level = 0;

    //ADR-0011 §2.2 (사용자 결정 2026-05-12): Awake에서 leftSide/rightSide 초기 localPosition 캐싱.
    //OnEnable에서 매 재진입마다 이 값으로 reset하여 SetGameObjectsFade(1f) DOTween 누적 어긋남 회피.
    Vector3 _leftSideInitialPos;
    Vector3 _rightSideInitialPos;

    //ADR-0011 §2.2 (사용자 결정 2026-05-12, 추가): Tutorial 전용 tempObject 배열의 초기 위치 +
    //활성 상태 캐싱. 재진입 시 reset해 (a) DOMove(leftDest/rightDest) stale 위치 회복 +
    //(b) Tutorial 단계별 SetActive(true) 잔존 → 처음부터 보이는 결함 차단.
    Vector3[] _tempObjectInitialPos;
    bool[] _tempObjectInitialActive;

    //초기 설정
    private void Awake()
    {
        //랜드마크를 저장하는 스크립트에 해당 타이머 할당
        saveLandmark.SetTimer(timer);
        //각도와 도달시간 받아와서 설정
        level = StaticData.level;
        lineMoveDuration = StaticData.destTime;
        //leftSide/rightSide 초기 위치 캐싱 (씬 인스펙터 할당 시점의 transform)
        if (leftSide != null) _leftSideInitialPos = leftSide.transform.localPosition;
        if (rightSide != null) _rightSideInitialPos = rightSide.transform.localPosition;
        //tempObject 초기 상태 캐싱 — 재진입 reset에 사용
        if (tempObject != null)
        {
            _tempObjectInitialPos = new Vector3[tempObject.Length];
            _tempObjectInitialActive = new bool[tempObject.Length];
            for (int i = 0; i < tempObject.Length; i++)
            {
                if (tempObject[i] != null)
                {
                    _tempObjectInitialPos[i] = tempObject[i].transform.localPosition;
                    _tempObjectInitialActive[i] = tempObject[i].activeSelf;
                }
            }
        }
    }

    //초기 설정2 (위 보다 늦게 실행되는 부분)
    private void Start()
    {
        //첫 움직임은 도달시간의 절반
        lineMoveDuration = StaticData.destTime / 2;
        //게임선택 BGM종료
        bgmSound.Stop();
        //짝맞추기게임용 BGM으로 교체
        bgmSound.clip = pairBGM;
        //가이드라인의 스크립트 할당
        lineController = guideLine.GetComponent<GuideLineController>();
        //마지막 플레이한 게임을 짝맞추기로 로컬에 저장
        PlayerPrefs.SetString("LastPlayed", "Pair");


        //콜라이더 모두 끄기
        foreach (Collider collider in leftColliders)
        {
            collider.enabled = false;
        }
        foreach (Collider collider in rightColliders)
        {
            collider.enabled = false;
        }

        //좌우에서 타일영역 등장
        SetGameObjectsFade(1f);
        //ADR-0011 §2.2: 첫 진입에서만 Start fire. 재진입 OnEnable의 가드용 플래그.
        _startedOnce = true;
    }

    //켜질 때 호출
    private void OnEnable()
    {
        //단계: Playing. setter가 Flutter로 자동 송신. background 시 사용자에게 confirm 다이얼로그 노출.
        StaticData.stage = ExerciseStage.Playing;
        //모드 변경
        StaticData.nowMode = Mode.Game;
        //매 진입(이어하기/재진입 포함)에 튜토리얼 다시 실행 — 사용자 결정.
        //Initalize에서 line 501이 다시 false로 set하므로 line 933의 if(!isFirst) 가드는 영향 없음.
        isFirst = true;
        //캘리브레이션에서 킨 어깨 트리거 해제
        pointsController.SetPointTrigger();

        //ADR-0011 §2.2 (사용자 결정 2026-05-11): 이전 dispose 시 잔존한 튜토리얼 UI 정리.
        //GameObject.SetActive(false)로 코루틴은 자동 stop되지만 dim/tutorialGuideLine은
        //별도 parent일 수 있어 잔존 가능. 매 OnEnable에서 명시 초기화.
        if (dim != null) dim.SetActive(false);
        if (tutorialGuideLine != null) tutorialGuideLine.SetActive(false);
        if (tutorialColliders != null)
        {
            foreach (var c in tutorialColliders)
            {
                if (c != null) c.enabled = false;
            }
        }
        triggeredTutorial = true;
        _restoredFromIncomplete = false;

        //ADR-0011 §2.2 (사용자 결정 2026-05-12): sideMoveValue를 매 OnEnable마다 절대값으로 reset.
        //이전엔 매번 -50*level 차감되어 재진입 누적시 0 또는 음수가 되며 SetGameObjectsFade 방향 반전.
        //초기값 600(씬에서 정의된 sideMoveValue 기본값)에서 level만큼 차감해 동일 결과.
        sideMoveValue = 600f - 50 * level;

        Debug.Log($"[PairGame.OnEnable] isFirst={isFirst} _startedOnce={_startedOnce} isNormalEnd={StaticData.isNormalEnd}");

        //ADR-0011 §2.2 (사용자 결정 2026-05-12): 재진입 시 전체 초기화 로직 강화.
        //원인: Unity 생명주기상 Start()는 첫 활성화에만 호출 → SetGameObjectsFade(1f)/grid 생성/
        //guideLine 위치 reset 등 모두 미트리거. dispose 시점의 stale 상태(leftSide/rightSide 위치,
        //타일 풀링 위치, guideLine 위치 등) 잔존.
        if (_startedOnce)
        {
            //leftSide/rightSide localPosition을 Awake에서 캐싱한 초기값으로 reset.
            //이전 SetGameObjectsFade(1f) 누적 이동으로 화면 밖에 머문 케이스 방지.
            if (leftSide != null) leftSide.transform.localPosition = _leftSideInitialPos;
            if (rightSide != null) rightSide.transform.localPosition = _rightSideInitialPos;
            //가이드라인 중앙 reset — 게임 진행 중 좌우 이동 후 dispose 시점 위치 잔존 방지.
            if (guideLine != null) guideLine.transform.localPosition = Vector3.zero;
            //leftGrid/rightGrid 타일 재생성 — PairGrid.Start의 GenerateGrid가 첫 활성화만이라
            //재진입시 grid 배열 stale + 일부 타일이 (10000,10000)에 머무는 케이스 방지.
            if (leftGrid != null) leftGrid.RebuildGrid();
            if (rightGrid != null) rightGrid.RebuildGrid();
            //ADR-0011 §2.2 (사용자 결정 2026-05-12, 추가): tempObject(Tutorial 전용 별도 GameObject
            //배열) reset. 사용자 통찰 "충돌위치(부모 transform)는 리셋되는데 이미지 위치(자식
            //transform)는 변경 안 됨" — 부모 + 자식(image) 두 레이어 모두 reset 필요.
            //원인: Tutorial 코루틴이 tempObject[0/4].transform.DOMove(leftDest/rightDest)로
            //부모 이동 + tempObject[0/4].transform.GetChild(0).DOShakePosition으로 자식 shake.
            //mute로 코루틴 중단 시 부모/자식 모두 stale. 또 SetActive(true) 잔존.
            if (tempObject != null)
            {
                for (int i = 0; i < tempObject.Length; i++)
                {
                    if (tempObject[i] == null) continue;
                    //부모 + 자식의 진행 중 tween 정리.
                    DOTween.Kill(tempObject[i].transform);
                    if (tempObject[i].transform.childCount > 0)
                    {
                        DOTween.Kill(tempObject[i].transform.GetChild(0));
                        //자식(image) 위치/스케일 reset.
                        tempObject[i].transform.GetChild(0).localPosition = Vector3.zero;
                        tempObject[i].transform.GetChild(0).localScale = Vector3.one;
                    }
                    //부모 위치/스케일 reset.
                    if (_tempObjectInitialPos != null && i < _tempObjectInitialPos.Length)
                    {
                        tempObject[i].transform.localPosition = _tempObjectInitialPos[i];
                    }
                    tempObject[i].transform.localScale = Vector3.one;
                    //sprite 기본 상태로 reset.
                    var po = tempObject[i].GetComponent<PairObject>();
                    if (po != null) po.SetSprite(0);
                    //활성 상태를 Awake 시점(첫 진입 초기값)으로 복귀.
                    if (_tempObjectInitialActive != null && i < _tempObjectInitialActive.Length)
                    {
                        tempObject[i].SetActive(_tempObjectInitialActive[i]);
                    }
                }
            }
            //SetGameObjectsFade(1f) 호출로 fade-in DOTween + OnComplete에서 Initalize() 호출.
            //첫 진입(Start 경로)과 동일 흐름 — Tutorial/PlayerPrefs 복원 일관성.
            SetGameObjectsFade(1f);
        }
    }

    //ADR-0011 §2.2: 첫 진입(Start)과 재진입(OnEnable)의 Initalize 트리거 분기 가드.
    private bool _startedOnce = false;

    //ADR-0011 §2.2: dispose 시 명시 코루틴 stop + UI 잔존 정리. OnEnable과 짝.
    private void OnDisable()
    {
        Debug.Log($"[PairGame.OnDisable] isFirst={isFirst} _restoredFromIncomplete={_restoredFromIncomplete} isNormalEnd={StaticData.isNormalEnd} dim={(dim != null && dim.activeSelf)} tutorialGuideLine={(tutorialGuideLine != null && tutorialGuideLine.activeSelf)}");
        StopAllCoroutines();
        if (dim != null) dim.SetActive(false);
        if (tutorialGuideLine != null) tutorialGuideLine.SetActive(false);
    }

    //=== PlayMode 테스트 helper (ADR-0011 검증용) ============================
    [ContextMenu("Log/Pair State")]
    private void LogPairState()
    {
        Debug.Log($"[PairGame.State] isFirst={isFirst} _restoredFromIncomplete={_restoredFromIncomplete} isNormalEnd={StaticData.isNormalEnd} stage={StaticData.stage} mode={StaticData.nowMode} triggeredTutorial={triggeredTutorial} dim={(dim != null && dim.activeSelf)} tutorialText='{(tutorialText != null ? tutorialText.text : "<null>")}'");
    }

    [ContextMenu("Force Disable (dispose 시뮬)")]
    private void ForceDisable() => gameObject.SetActive(false);

    [ContextMenu("Force Enable (재진입 시뮬)")]
    private void ForceEnable() => gameObject.SetActive(true);

    //게임 시작 단계
    void Initalize()
    {
        
        //비정상종료와 첫 진입인지 확인
        if (StaticData.isNormalEnd == false && isFirst) 
        {
            //팝업 표출
            popup.SetActive(true);
            //이전 강제종료 원인이 빠른동작에 의한 페널티인지 확인
            if(PlayerPrefs.HasKey("FastQuit") && PlayerPrefs.GetInt("FastQuit") == 1)
            {
                popupText.text = "빠른 동작 누적으로 강제종료 되었습니다.";
            }
            else
            {
                popupText.text = "이전 게임 상태를 불러옵니다.";
            }
            //2초뒤에 팝업 끄기
            Invoke("OffPopup", 2f);

            //각 세트별 점수 로드해서 저장
            StaticData.pairGameScores.section1 = PlayerPrefs.GetInt("PairSet1Score");
            StaticData.pairGameScores.section2 = PlayerPrefs.GetInt("PairSet2Score");
            StaticData.pairGameScores.section3 = PlayerPrefs.GetInt("PairSet3Score");
            

            //몇 세트 진행중이였는지 확인 후 적용
            progress.SetCheckd(PlayerPrefs.GetInt("NowSet"));
            timer.SetEndCount(PlayerPrefs.GetInt("NowSet"));

            //텍스트에도 점수 반영
            SetScore();

            //남은 시간 로드
            float time = PlayerPrefs.GetFloat("GameTime");
            //시간이 0초일 때 세트가 꼬이는 경우를 방지
            if (time <= 0)
            {
                progress.SetCheckd(PlayerPrefs.GetInt("NowSet") - 1);
                timer.SetEndCount(PlayerPrefs.GetInt("NowSet") - 1);
            }

            //운동횟수 로드하여 저장
            StaticData.pairGameDatas.count1 = PlayerPrefs.GetInt("PairCheckCount1");
            StaticData.pairGameDatas.count2 = PlayerPrefs.GetInt("PairCheckCount2");
            StaticData.pairGameDatas.count3 = PlayerPrefs.GetInt("PairCheckCount3");
            StaticData.blockGameDatas.count1 = PlayerPrefs.GetInt("PushBlockCount1");
            StaticData.blockGameDatas.count2 = PlayerPrefs.GetInt("PushBlockCount2");
            StaticData.blockGameDatas.count3 = PlayerPrefs.GetInt("PushBlockCount3");

            //타이머에 남은 시간 적용
            timer.SetRamainTime(time);
            //timer.SetState(true) ← 제거. Tutorial 동안 카운트다운 진행되면 만료 race 위험.
            //Tutorial 끝에서 timer.SetState(true) 호출되어 그때 카운트다운 시작.
            //콜라이더 설정
            SetCollider();
            //처음상태 해제
            isFirst = false;

            //ADR-0011 §2.2 (사용자 결정 2026-05-11): 비정상종료 후 매 진입마다 튜토리얼 재생.
            //_restoredFromIncomplete=true로 Tutorial 끝의 timer.SetRamainTime(gameTime)을 SKIP →
            //복원된 time 보존 → "튜토리얼 먼저 → 끝나면 이어하기" 사용자 의도 정합.
            _restoredFromIncomplete = true;
            StaticData.nowMode = Mode.Tutorial;
            StartCoroutine(Tutorial());
        }
        //정상종료에 첫 실행이면
        else if (isFirst)
        {
            //튜토리얼 모드로 변경
            StaticData.nowMode = Mode.Tutorial;
            //튜토리얼 시작
            StartCoroutine(Tutorial());
        }
        else
        {
            //비정상종료 상태 저장
            PlayerPrefs.SetInt("NormalEnd", 1);
            //게임 시작
            GameStart();
            //콜라이더 설정
            SetCollider();
        }
        //bgm이 꺼져있으면 켜기
        if(!bgmSound.isPlaying) bgmSound.Play();
        //빠른동작 페널티 강제종료 상태 해제
        PlayerPrefs.SetInt("FastQuit", 0);
    }

    //튜토리얼용 상호작용 대기 변수
    bool triggeredTutorial = true;
    //상호작용시 호출하는 상태변경 부분
    public void SetTutorialTriggerState(bool state)
    {
        triggeredTutorial = state;
    }

    [Header("튜토리얼 항목들")]
    //튜토리얼에 나타나는 타일 오브젝트들
    public GameObject[] tempObject;
    //튜토리얼에서 나타나는 가이드라인
    public GameObject tutorialGuideLine;
    //튜토리얼의 타일이 이동하는 목적지들
    public Transform leftDest;
    public Transform rightDest;
    //튜토리얼에서 나타나는 상단 UI
    public GameObject[] tempHeaderObjects;
    //튜토리얼용 파티클
    public ParticleSystem[] starEffect;
    //튜토리얼용 점수 표출
    public ShowScoreImages tutorialScore;
    //튜토리얼용 콜라이더들
    public Collider[] tutorialColliders;

    IEnumerator Tutorial()
    {
        //검은배경 켜기
        dim.SetActive(true);
        //설명 텍스트 변경
        tutorialText.text = "지금부터 연습을 시작하겠습니다.";
        //3초 대기
        yield return new WaitForSeconds(3f);

        //설명 텍스트 변경
        tutorialText.text = "3 Set로 진행되는 운동 중 현재 Set가 표시됩니다.";
        //세트 UI 켜기
        tempHeaderObjects[0].SetActive(true);
        //3초 대기
        yield return new WaitForSeconds(3f);

        //설명 텍스트 변경
        tutorialText.text = "남은 운동 시간, 휴식 시간이 표시됩니다.";
        //세트 UI 끄기
        tempHeaderObjects[0].SetActive(false);
        //타이머 UI 켜기
        tempHeaderObjects[1].SetActive(true);
        //3초 대기
        yield return new WaitForSeconds(3f);

        //설명 텍스트 변경
        tutorialText.text = "이번 게임에서 획득한 점수가 표시됩니다.";
        //타이머 UI 끄기
        tempHeaderObjects[1].SetActive(false);
        //점수 UI 키기
        tempHeaderObjects[2].SetActive(true);
        //3초 대기
        yield return new WaitForSeconds(3f);

        //점수 UI 끄기
        tempHeaderObjects[2].SetActive(false);

        //설명 텍스트 변경
        tutorialText.text = "아래의 화살표를 보고 터치 방향을 알 수 있습니다.";
        //화살표의 현재 부모를 저장
        Transform tempLeftParent = leftArrow.transform.parent;
        //화살표의 부모 변경
        leftArrow.transform.SetParent(dim.transform);
        //화살표 상태 변경
        leftArrow.sprite = arrowSprites[1];
        //화살표의 현재 부모를 저장
        Transform tempRightParent = rightArrow.transform.parent;
        //화살표의 부모 변경
        rightArrow.transform.SetParent(dim.transform);
        //3초 대기
        yield return new WaitForSeconds(3f);

        //화살표들의 부모를 원래 부모로 변경
        leftArrow.transform.SetParent(tempLeftParent);
        rightArrow.transform.SetParent(tempRightParent);
        //오브젝트들 키기
        tempObject[0].SetActive(true);
        tempObject[1].SetActive(true);
        tempObject[2].SetActive(true);
        //설명 텍스트 변경
        tutorialText.text = "표시된 방향의 3개의 동물 중 원하는 동물을 터치해주세요.";
        //3초 대기
        yield return new WaitForSeconds(3f);

        //가이드라인 켜기
        tutorialGuideLine.SetActive(true);
        //설명 텍스트 변경
        tutorialText.text = "표시된 시간동안 움직이는 가이드라인 속도에 맞춰\n동물 캐릭터를 터치할 때 운동 효과가 높습니다.";
        //3초 대기
        yield return new WaitForSeconds(3f);

        //가이드라인 끄기
        tutorialGuideLine.SetActive(false);
        //설명 텍스트 변경
        tutorialText.text = "표시된 동물을 선택해보세요.\n연습에서는 시간이 지나지 않습니다.";
        //콜라이더 켜서 상호작용 가능하게 변경
        tutorialColliders[0].enabled = true;
        //상호작용 가능한 타일 이외에는 끄기
        tempObject[1].SetActive(false);
        tempObject[2].SetActive(false);
        //사용자가 상호작용 할 때 까지 대기
        while (triggeredTutorial)
        {
            yield return new WaitForSeconds(0.1f);
            continue;
        }

        //다시 상호작용 가능하도록 변수 상태 변경
        triggeredTutorial = true;
        //상호작용 완료한 콜라이더 끄기
        tutorialColliders[0].enabled = false;
        //설명 텍스트 변경
        tutorialText.text = "선택된 동물은 가운데로 이동하게 됩니다.";
        //선택한 오브젝트의 표정 이미지 변경
        tempObject[0].GetComponent<PairObject>().SetSprite(1);
        //선택한 오브젝트에 떨고있는 효과 추가
        tempObject[0].transform.GetChild(0).DOShakePosition(9999f, 10, 50).SetId("Shake");
        //원래 있던 위치를 저장
        Vector3 firstLeftPos = tempObject[0].transform.position;
        //선택한 타일이 중앙으로 이동
        tempObject[0].transform.DOMove(leftDest.position, 1f);
        //3초대기
        yield return new WaitForSeconds(3f);

        //설명 텍스트 변경
        tutorialText.text = "운동은 항상 왼 쪽, 오른쪽을 반복해야 합니다.\n이제 오른쪽에서 표시된 동물을 선택해주세요.";
        //상호작용 할 콜라이더 켜기
        tutorialColliders[2].enabled = true;
        //상호작용 위치 타일 켜기
        tempObject[4].SetActive(true);
        //사용자가 상호작용 할 때 까지 대기
        while (triggeredTutorial)
        {
            yield return new WaitForSeconds(0.1f);
            continue;
        }

        //다시 상호작용 가능하도록 변수 상태 변경
        triggeredTutorial = true;
        //상호작용이 끝난 콜라이더 끄기
        tutorialColliders[2].enabled = false;
        //선택한 오브젝트의 표정 이미지 교체
        tempObject[4].GetComponent<PairObject>().SetSprite(1);
        //선택한 오브젝트에 떨고있는 효과 추가
        tempObject[4].transform.GetChild(0).DOShakePosition(9999f, 10, 50).SetId("Shake");
        //원래 있던 위치 저장
        Vector3 firstRIghtPos = tempObject[4].transform.position;
        //중앙으로 이동
        tempObject[4].transform.DOMove(rightDest.position, 1f);
        //3초대기
        yield return new WaitForSeconds(3f);

        //설명 텍스트 변경
        tutorialText.text = "선택된 동물들이 다른 종류일 경우\n실패가 되며 원래 자리로 돌아갑니다.";
        //떨고있는 효과 종료
        DOTween.Kill("Shake");
        //오브젝트의 표정 이미지 원래 표정으로 전환
        tempObject[0].GetComponent<PairObject>().SetSprite(0);
        tempObject[4].GetComponent<PairObject>().SetSprite(0);
        //오브젝트 각각 원래 있던 위치로 다시 이동
        tempObject[0].transform.DOMove(firstLeftPos, 1f);
        tempObject[4].transform.DOMove(firstRIghtPos, 1f);
        //3초 대기
        yield return new WaitForSeconds(3f);

        //오브젝트 끄기
        tempObject[4].SetActive(false);
        //설명 텍스트 변경
        tutorialText.text = "다시 표시된 동물을 선택해보세요.";
        //다시 첫 번째 콜라이더 켜기
        tutorialColliders[0].enabled = true;
        //사용자가 상호작용 할 때 까지 대기
        while (triggeredTutorial)
        {
            yield return new WaitForSeconds(0.1f);
            continue;
        }

        //다시 상호작용 가능하도록 상태 변경
        triggeredTutorial = true;
        //콜라이더 끄기
        tutorialColliders[0].enabled = false;
        //선택한 오브젝트 표정 변경
        tempObject[0].GetComponent<PairObject>().SetSprite(1);
        //선택한 오브젝트 떨리는 효과 실행
        tempObject[0].transform.GetChild(0).DOShakePosition(9999f, 10, 50).SetId("Shake");
        //이전 위치 저장
        firstLeftPos = tempObject[0].transform.position;
        //중앙으로 이동
        tempObject[0].transform.DOMove(leftDest.position, 1f);
        //설명 텍스트 변경
        tutorialText.text = "반대쪽에서도 똑같은 동물을 찾아 터치해봅시다.";
        //콜라이더 켜기
        tutorialColliders[1].enabled = true;
        //동일 오브젝트 켜기
        tempObject[3].SetActive(true);
        //사용자가 상호작용 할 때 까지 대기
        while (triggeredTutorial)
        {
            yield return new WaitForSeconds(0.1f);
            continue;
        }

        //다시 상호작용 가능하도록 상태 변경
        triggeredTutorial = true;
        //콜라이더 끄기
        tutorialColliders[1].enabled = false;
        //선택한 오브젝트 표정 변경
        tempObject[3].GetComponent<PairObject>().SetSprite(1);
        //선택한 오브젝트 떨리는 효과 실행
        tempObject[3].transform.GetChild(0).DOShakePosition(9999f, 10, 50).SetId("Shake");
        //이전 위치 저장
        firstRIghtPos = tempObject[3].transform.position;
        //설명 텍스트 변경
        tutorialText.text = "똑같은 동물을 선택하면 짝이 맞춰집니다.\n짝이 맞춰진 동물들은 사라지며, 점수를 얻습니다.";
        //중앙으로 이동
        tempObject[3].transform.DOMove(rightDest.position, 1f).OnComplete(delegate
        {
            //중앙으로 이동이 끝나면
            //튜토리얼용 점수 표출
            tutorialScore.SetScore(50, 1);
            //표정 변경
            tempObject[0].GetComponent<PairObject>().SetSprite(2);
            tempObject[3].GetComponent<PairObject>().SetSprite(2);
            //크기 작아졌다 커지는 애니메이션
            tempObject[0].transform.DOScale(0.5f, 1f / 4).SetLoops(2, LoopType.Yoyo);
            tempObject[3].transform.DOScale(0.5f, 1f / 4).SetLoops(2, LoopType.Yoyo).OnComplete(delegate
            {
                //끝나면 크기를 0으로 만들기
                tempObject[0].transform.DOScale(0f, 1f / 4);
                tempObject[3].transform.DOScale(0f, 1f / 4);
                //터지는 파티클이펙트 출력
                starEffect[0].Play();
                starEffect[1].Play();
            });
        });
        //3초 대기
        yield return new WaitForSeconds(3f);

        //텍스트 설정
        tutorialText.text = "정해진 시간 내에 최대한 많은 점수를 획득해보세요.\n가이드선에 속도를 맞추는 등 추가점수가 있어요!";

        //타이머 UI 켜기
        tempHeaderObjects[1].SetActive(true);
        //점수 UI 켜기
        tempHeaderObjects[2].SetActive(true);
        //3초 대기
        yield return new WaitForSeconds(3f);

        //타이머 UI 끄기
        tempHeaderObjects[1].SetActive(false);
        //점수 UI 끄기
        tempHeaderObjects[2].SetActive(false);

        //설명 텍스트 변경
        tutorialText.text = "지금부터 게임을 시작하겠습니다.";
        //3초 대기
        yield return new WaitForSeconds(3f);

        //검은 화면 끄기
        dim.SetActive(false);
        //모드 변경
        StaticData.nowMode = Mode.Game; 
        //비정상종료 상태로 저장
        PlayerPrefs.SetInt("NormalEnd", 1);
        //ADR-0011 §2.2: 비정상종료 복원이면 line 195의 복원된 time 유지 (gameTime 덮어쓰기 SKIP).
        //정상 첫 진입이면 기존대로 gameTime으로 새 게임 시간 설정.
        if (!_restoredFromIncomplete)
        {
            //타이머에 시간 할당
            timer.SetRamainTime(gameTime);
        }
        //타이머에 게임시작 전달 (복원/신규 둘 다)
        timer.SetState(true);
        //가이드라인 움직이기 시작
        MoveGuideLine();
        //콜라이더 상태 설정
        SetCollider();
        //처음 상태 해제
        isFirst = false;
    }

    //가이드라인이 처음 움직이는건지 판단하는 변수
    bool firstGuide = true;

    /// <summary>
    /// 가이드라인을 움직이게 만드는 부분
    /// </summary>
    public void MoveGuideLine()
    {
        //현재 세트를 가져옴
        int num = progress.GetCheckedNum();
        //가이드라인 켜기
        guideLine.SetActive(true);
        //라인에 머무른시간 재기 시작
        lineController.GuideLineTimeStart();
        //라인의 방향과 팝업 오브젝트들을 전달
        lineController.SetLineState(nowLeft, popup, popupText);
        //왼쪽으로 이동할 때
        if (nowLeft)
        {
            //왼쪽 영역 도달시간 재기 시작
            leftGrid.StartTime();
            //화살표 상태 왼쪽으로 변경
            leftArrow.sprite = arrowSprites[1];
            rightArrow.sprite = arrowSprites[0];
            //왼쪽으로 움직이는 효과
            guideLine.transform.DOMove(guideDestLeft.position, lineMoveDuration).SetEase(Ease.Linear).SetId("moveGuideLine").OnKill(delegate
            {
                //효과가 끝날 때
                //게임시간이 다 되어 끝난 경우에는 처리하지 않음
                if (isEnd) return;

                //가이드라인 위치를 도착점으로 지정
                guideLine.transform.position = guideDestLeft.position;
                //머무른 시간 확인
                float time = lineController.GetGuideLineTime();
                //점수를 담을 변수
                int score;
                //라인에 머무른 시간별로 점수 차등 제공
                if (lineMoveDuration - time < lineMoveDuration * 0.2f)
                {
                    showScoreImages.SetScore(20, 0);
                    score = 20;
                }
                else if (lineMoveDuration - time < lineMoveDuration * 0.6f)
                {
                    showScoreImages.SetScore(10, 1);
                    score = 10;
                }
                else if (lineMoveDuration - time < lineMoveDuration * 0.8f)
                {
                    showScoreImages.SetScore(0, 2);
                    score = 0;
                }
                else
                {
                    showScoreImages.SetScore(-5, 2);
                    score = -5;
                }

                //일치하는 세트에 점수 증가
                if (num == 0) StaticData.pairGameScores.section1 += score;
                else if (num == 1) StaticData.pairGameScores.section2 += score;
                else if (num == 2) StaticData.pairGameScores.section3 += score;

                //변동된 점수 텍스트에 반영
                SetScore();

                //첫 움직임이였을 경우
                if (firstGuide)
                {
                    //도달시간 원래 시간으로
                    lineMoveDuration = StaticData.destTime;
                    //처음상태 해제
                    firstGuide = false;
                }
            });
        }
        //오른쪽으로 이동할 때
        else
        {
            //오른쪽 영역 도달시간 재기 시작
            rightGrid.StartTime();
            //화살표 상태 오른쪽으로 변경
            leftArrow.sprite = arrowSprites[0];
            rightArrow.sprite = arrowSprites[1];
            //가이드라인 움직임 효과
            guideLine.transform.DOMove(guideDestRight.position, lineMoveDuration).SetEase(Ease.Linear).SetId("moveGuideLine").OnKill(delegate
            {
                //효과가 끝날 때
                //게임시간이 다 되어 끝난 경우에 처리하지 않음
                if (isEnd) return;

                //가이드라인 위치를 도착지점으로 이동
                guideLine.transform.position = guideDestRight.position;
                //라인에 머무른 시간 확인
                float time = lineController.GetGuideLineTime();
                //점수를 담을 변수
                int score;
                //라인에 머무른 시간에 따라 점수 차등 제공
                if (lineMoveDuration - time < lineMoveDuration * 0.2f)
                {
                    showScoreImages.SetScore(20, 0);
                    score = 20;
                }
                else if (lineMoveDuration - time < lineMoveDuration * 0.6f)
                {
                    showScoreImages.SetScore(10, 1);
                    score = 10;
                }
                else if (lineMoveDuration - time < lineMoveDuration * 0.8f)
                {
                    showScoreImages.SetScore(0, 2);
                    score = 0;
                }
                else
                {
                    showScoreImages.SetScore(-5, 2);
                    score = -5;
                }
                //현재세트에 맞게 점수 증가
                if (num == 0) StaticData.pairGameScores.section1 += score;
                else if (num == 1) StaticData.pairGameScores.section2 += score;
                else if (num == 2) StaticData.pairGameScores.section3 += score;
                //변동된 점수 텍스트에 반영
                SetScore();
                //첫 움직임이면
                if (firstGuide)
                {
                    //도달 시간 원래 시간으로
                    lineMoveDuration = StaticData.destTime;
                    //처음 상태 해제
                    firstGuide = false;
                }
            });
        }
    }

    //휴식시간 이후 게임 다시 시작시 호출됨
    void GameStart()
    {
        //타이머에 시간전달, 타이머 시작
        timer.SetRamainTime(gameTime);
        timer.SetState(true);
    }

    //팝업을 끄는 부분
    public void OffPopup()
    {
        popup.SetActive(false);
    }

    /// <summary>
    /// 각 타일에 트리거 되었을 때 호출되는 부분
    /// </summary>
    /// <param name="isLeft"></param>
    public void SetNowLeft(bool isLeft)
    {
        //현재 방향을 터치한 반대 방향으로 설정
        nowLeft = !isLeft;
        //왼쪽을 터치했을 때
        if (isLeft)
        {
            //도달하는데 걸린 시간
            float elapsedTime = leftGrid.EndTime();
            //목표 도달 시간 - 2초보다 빠른 시간에 들어왔을 때
            if (elapsedTime < lineMoveDuration - 2)
            {
                //팝업 켜기
                popup.SetActive(true);
                //팝업 텍스트 설정
                popupText.text = "너무 빨라요.";
                //2초뒤 팝업 끄기
                Invoke("OffPopup", 2f);

                //강제종료 스택 + 1
                StaticData.fastTriggerCount++;
                //스택 5면 종료
                if (StaticData.fastTriggerCount == 5)
                {
                    //빠른 강제종료 카운트 변수 값 증가
                    StaticData.fastQuitCount++;
                    //증가된 값을 로컬에 저장
                    PlayerPrefs.SetInt("FastQuitCount", StaticData.fastQuitCount);
                    //강제종료 원인이 빠른동작에 의한 페널티인것을 로컬에 저장
                    PlayerPrefs.SetInt("FastQuit", 1);
                    //종료전에 데이터를 저장
                    saveLandmark.TempSaveDatas("penalty");
                    //게임 종료, 종료 방식에 대해서는 협의 필요
                    //Application.Quit(); //250811 수정사항
                }
            }
            //목표 도달 시간 + 2초보다 느린 시간에 들어왔을 때
            else if (elapsedTime > lineMoveDuration + 2)
            {
                //팝업 켜기
                popup.SetActive(true);
                //팝업 텍스트 설정
                popupText.text = "너무 느려요.";
                //2초뒤 팝업 끄기
                Invoke("OffPopup", 2f);
            }
        }
        else
        {
            float elapsedTime = rightGrid.EndTime();
            //목표 도달 시간 - 2초보다 빠른 시간에 들어왔을 때
            if (elapsedTime < lineMoveDuration - 2)
            {
                popup.SetActive(true);
                popupText.text = "너무 빨라요.";
                Invoke("OffPopup", 2f);

                //강제종료 스택 + 1
                StaticData.fastTriggerCount++;
                //스택 5면 종료
                if (StaticData.fastTriggerCount == 5)
                {
                    //빠른 강제종료 카운트 변수 값 증가
                    StaticData.fastQuitCount++;
                    //증가된 값을 로컬에 저장
                    PlayerPrefs.SetInt("FastQuitCount", StaticData.fastQuitCount);
                    //강제종료 원인이 빠른동작에 의한 페널티인것을 로컬에 저장
                    PlayerPrefs.SetInt("FastQuit", 1);
                    //종료전에 데이터를 저장
                    saveLandmark.TempSaveDatas("penalty");
                    //게임 종료, 종료 방식에 대해서는 협의 필요
                    //Application.Quit(); //250811 수정사항
                }
            }
            //목표 도달 시간 + 2초보다 느린 시간에 들어왔을 때
            else if (elapsedTime > lineMoveDuration + 2)
            {
                //팝업 켜기
                popup.SetActive(true);
                //팝업 텍스트 설정
                popupText.text = "너무 느려요.";
                //2초뒤 팝업 끄기
                Invoke("OffPopup", 2f);
            }
        }
    }

    /// <summary>
    /// 타일에 상호작용을 했을 때, 타일이 중앙으로 움직이는 과정이 끝나면 호출되는 부분
    /// </summary>
    /// <param name="isLeft">현재 상호작용 한 영역의 방향</param>
    /// <param name="pairObject">상호작용 한 오브젝트</param>
    public void Setting(bool isLeft, PairObject pairObject)
    {
        //왼쪽 영역에 상호작용 했으면
        if (isLeft)
        {
            //왼쪽 오브젝트가 선택된 상황이 아니면
            if(isLeftSet == false)
            {
                //선택한 상태로 설정
                isLeftSet = true;
                //왼쪽 오브젝트를 상호작용 한 오브젝트로 설정
                leftPairObject = pairObject;
            }
        }
        //오른쪽 영역에 상호작용 했으면
        else
        {
            //오른쪽 오브젝트가 선택된 상황이 아니면
            if (isRightSet == false)
            {
                //선택한 상태로 설정
                isRightSet = true;
                //오른쪽 오브젝트를 상호작용 한 오브젝트로 설정
                rightPairObject = pairObject;
            }
        }

        //둘 다 선택되어있는 상태라면
        if(isLeftSet && isRightSet)
        {
            //동일한 모양인지 확인
            Check();
        }
        //한 쪽만 선택한 상태라면
        else
        {
            //콜라이더 상태 설정
            SetCollider();
        }
    }

    //상호작용 가능한 콜라이더들
    public Collider[] leftColliders;
    public Collider[] rightColliders;
    /// <summary>
    /// 좌우 상태에 따라 자동으로 콜라이더들을 활성화/비활성화 해 주는 부분
    /// </summary>
    public void SetCollider()
    {
        //왼쪽을 상호작용 해야하면
        if (nowLeft)
        {
            //왼쪽 콜라이더 전체
            foreach (Collider collider in leftColliders)
            {
                //활성화
                collider.enabled = true;
            }
            //오른쪽 콜라이더 전체
            foreach (Collider collider in rightColliders)
            {
                //비활성화
                collider.enabled = false;
            }
        }
        //오른쪽을 상호작용 해야하면
        else
        {
            //왼 쪽 콜라이더 전체
            foreach (Collider collider in leftColliders)
            {
                //비활성화
                collider.enabled = false;
            }
            //오른쪽 콜라이더 전체
            foreach (Collider collider in rightColliders)
            {
                //활성화
                collider.enabled = true;
            }
        }
    }

    /// <summary>
    /// 타일간 동일한 타입인지 확인하는 부분
    /// </summary>
    public void Check()
    {
        //오브젝트들의 타입이 일치하면
        if (leftPairObject.type == rightPairObject.type)
        {
            //양 쪽 영역에 정답임을 전달
            leftGrid.Correct();
            rightGrid.Correct();
            //점수계산
            CalculateScore(true);
            //사운드를 정답으로 지정
            matchSound.clip = matchSounds[0];
        }
        else
        {
            //양 쪽 영역에 오답임을 전달
            leftGrid.Fail();
            rightGrid.Fail();
            //점수계산
            CalculateScore(false);
            //사운드를 오답으로 지정
            matchSound.clip = matchSounds[1];
        }
        //지정된 사운드 재생
        matchSound.Play();
        //각 변수들 초기화
        isLeftSet = false;
        isRightSet = false;
        leftPairObject = null;
        rightPairObject = null;
    }

    /// <summary>
    /// 현재 타일 배치에서 게임이 진행 불가능 상태인지 확인하는 부분
    /// </summary>
    public void CheckUnable()
    {
        //각 영역의 상호작용 가능한 타일들의 정보
        PairObject[] leftObjs = leftGrid.GetObjects();
        PairObject[] rightObjs = rightGrid.GetObjects();
        //상호작용 가능한 경우의 수를 저장할 변수
        int count = 0;
        //타일의 갯수만큼 반복
        for(int i = 0; i < leftObjs.Length; i++)
        {
            for (int j = 0; j < rightObjs.Length; j++)
            {
                //왼쪽 영역과 오른쪽 영역의 타입이 일치하면
                if (leftObjs[i].type == rightObjs[j].type)
                {
                    //카운트 증가
                    count++;
                }
            }
        }

        //타입이 일치하는 경우가 하나도 없을 경우
        if(count == 0)
        {
            //팝업 켜기
            popup.SetActive(true);
            //팝업 텍스트 설정
            popupText.text = "게임이 진행 불가 상태입니다. 퍼즐을 재배치합니다.";
            //2초뒤 팝업 종료
            Invoke("OffPopup", 2f);
            //양쪽 영역에 타일 재배치 호출
            leftGrid.ResetGrid();
            rightGrid.ResetGrid();
        }
        else
        {

            //콜라이더 상태 변경
            SetCollider();
        }
    }

    /// <summary>
    /// 타일 영역 나타내기/숨기기 기능을 해주는 부분
    /// </summary>
    /// <param name="value">1 = 나타내기, 0 = 숨기기</param>
    public void SetGameObjectsFade(float value)
    {
        //영역 나타내기
        if(value == 1f)
        {
            //끝난 상태를 false로 변경
            isEnd = false;
            //타일영역 화면 밖에서 안으로 들어오는 효과
            leftSide.transform.DOLocalMoveX(leftSide.transform.localPosition.x + sideMoveValue, 0.75f).SetEase(Ease.OutBack);
            rightSide.transform.DOLocalMoveX(rightSide.transform.localPosition.x - sideMoveValue, 0.75f).SetEase(Ease.OutBack).OnComplete(delegate
            {
                //움직임이 끝나면
                //게임 시작 단계 호출
                Initalize();
                //첫 세트가 아니면 바로 가이드라인 움직임
                if(!isFirst) MoveGuideLine();
            });
        }
        //영역 숨기기
        else
        {
            //끝난 상태를 true로 변경
            isEnd = true;
            //가이드라인 첫 움직임 변수를 true로 설정
            firstGuide = true;
            //움직이고있는 가이드라인 멈추기
            DOTween.Kill("moveGuideLine");
            //가이드라인 끄기
            guideLine.SetActive(false);
            //가이드라인 첫 움직임은 목표 도달 시간의 절반
            lineMoveDuration = StaticData.destTime / 2;
            //가이드라인 위치를 중앙으로 이동
            guideLine.transform.localPosition = Vector3.zero;
            //선택된 모든 오브젝트들 상태 초기화
            isLeftSet = false;
            isRightSet = false;
            leftPairObject = null;
            rightPairObject = null;
            //양 영역에 게임끝난 상태를 전달
            leftGrid.EndSet();
            rightGrid.EndSet();
            //양 영역 화면 밖으로 움직이는 효과
            leftSide.transform.DOLocalMoveX(leftSide.transform.localPosition.x - sideMoveValue, 0.75f).SetEase(Ease.InBack);
            rightSide.transform.DOLocalMoveX(rightSide.transform.localPosition.x + sideMoveValue, 0.75f).SetEase(Ease.InBack);
        }
    }

    //게임이 끝난 상태인지 판별하는 변수
    bool isEnd = false;
    //콤보 보너스를 저장하는 변수
    int comboCount = 0;

    /// <summary>
    /// 점수 계산을 해주는 부분
    /// </summary>
    /// <param name="isSuccess">정답여부</param>
    public void CalculateScore(bool isSuccess)
    {
        //현재 세트를 확인
        int num = progress.GetCheckedNum();

        //짝 맞추기 시도 횟수를 세트에 맞게 증가
        if (num == 0) StaticData.pairGameDatas.count1++;
        else if (num == 1) StaticData.pairGameDatas.count2++;
        else if (num == 2) StaticData.pairGameDatas.count3++;

        //정답이면
        if (isSuccess)
        {
            //콤보 횟수 증가
            comboCount++;

            //짝 맞추기 고정점수
            if (num == 0) StaticData.pairGameScores.section1 += 30;
            else if (num == 1) StaticData.pairGameScores.section2 += 30;
            else if (num == 2) StaticData.pairGameScores.section3 += 30;
            //점수를 저장할 변수
            int nowScore = 30;

            //콤보별 추가점수
            if (comboCount > 7)
            {
                if (num == 0) StaticData.pairGameScores.section1 += 15;
                else if (num == 1) StaticData.pairGameScores.section2 += 15;
                else if (num == 2) StaticData.pairGameScores.section3 += 15;

                nowScore += 15;
            }
            else if (comboCount == 7)
            {
                if (num == 0) StaticData.pairGameScores.section1 += 13;
                else if (num == 1) StaticData.pairGameScores.section2 += 13;
                else if (num == 2) StaticData.pairGameScores.section3 += 13;

                nowScore += 13;
            }
            else if (comboCount == 6)
            {
                if (num == 0) StaticData.pairGameScores.section1 += 10;
                else if (num == 1) StaticData.pairGameScores.section2 += 10;
                else if (num == 2) StaticData.pairGameScores.section3 += 10;

                nowScore += 10;
            }
            else if (comboCount == 5)
            {
                if (num == 0) StaticData.pairGameScores.section1 += 8;
                else if (num == 1) StaticData.pairGameScores.section2 += 8;
                else if (num == 2) StaticData.pairGameScores.section3 += 8;

                nowScore += 8;
            }
            else if (comboCount == 4)
            {
                if (num == 0) StaticData.pairGameScores.section1 += 5;
                else if (num == 1) StaticData.pairGameScores.section2 += 5;
                else if (num == 2) StaticData.pairGameScores.section3 += 5;

                nowScore += 5;
            }
            else if (comboCount == 3)
            {
                if (num == 0) StaticData.pairGameScores.section1 += 3;
                else if (num == 1) StaticData.pairGameScores.section2 += 3;
                else if (num == 2) StaticData.pairGameScores.section3 += 3;

                nowScore += 3;
            }

            //얻은 점수를 화면에 표시
            showScoreImages.SetScore(nowScore, 2);
            
        }
        //오답이면
        else
        {
            //콤보를 0으로 변경
            comboCount = 0;
            //얻은 점수 화면에 표시
            showScoreImages.SetScore(0, 2);
        }
        //텍스트에 현재 점수 반영
        SetScore();
    }

    /// <summary>
    /// 점수 텍스트를 업데이트 해 주는 부분
    /// </summary>
    void SetScore()
    {
        //세트 수 확인
        int num = progress.GetCheckedNum();

        //현재 점수를 세트에 따라 텍스트에 반영
        if (num == 0) scoreText.text = StaticData.pairGameScores.section1.ToString() + "점";
        else if (num == 1) scoreText.text = (StaticData.pairGameScores.section1 + StaticData.pairGameScores.section2).ToString() + "점";
        else if (num == 2) scoreText.text = (StaticData.pairGameScores.section1 + StaticData.pairGameScores.section2 + StaticData.pairGameScores.section3).ToString() + "점";
    }
}
