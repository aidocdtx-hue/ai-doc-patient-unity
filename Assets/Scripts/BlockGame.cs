using DG.Tweening;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Collections.AllocatorManager;

public class BlockGame : MonoBehaviour
{
    // 양 손을 맞잡을 때 나타나는 터치영역
    public Transform bothHand;

    //팝업 오브젝트와 텍스트
    public GameObject popup;
    public TMP_Text popupText;

    //스크립트 참조
    public Timer timer;
    public SetProgress progress;
    public SaveLandmarkDatas saveLandmark;
    public PointsController pointsController;
    public ShowScoreImages showScoreImages;
    GuideLineController lineController;

    //각 블록이 나타나는 영역의 스크립트 참조
    public GridController leftGrid;
    public GridController rightGrid;

    //다음 진행방향을 기억하는 변수
    public static bool isNextTriggerLeft = false; 

    //게임 진행 시간
    float gameTime = 600f;
    
    //점수 텍스트
    public TMP_Text scoreText;

    //각 화살표의 이미지
    public Image leftArrow;
    public Image rightArrow;
    public Sprite[] arrowSprites;

    //가이드라인 오브젝트와 각 가이드라인의 도착지점
    public GameObject guideLine;
    public Transform guideDestLeft;
    public Transform guideDestRight;

    //가이드라인이 움직이는데 걸리는 시간 (목표 운동 시간)
    public float lineMoveDuration = 5f;

    //게임에 처음 진입하는지 판별하는 변수
    bool isFirst = true;

    //ADR-0011 §2.2 (사용자 결정 2026-05-11): 비정상종료 복원 케이스 추적.
    //true면 Tutorial 끝의 timer.SetRamainTime(gameTime)이 line 175의 복원된 time을 덮어쓰지
    //않도록 SKIP. OnEnable에서 false reset, line 130 분기에서 true set.
    bool _restoredFromIncomplete = false;

    //각 블록 영역의 오브젝트 참조
    public GameObject leftSide;
    public GameObject rightSide;

    //각도 단계
    int level = 0;

    //배경음 소스와 배경음악 클립
    public AudioSource bgmSound;
    public AudioClip blockBGM;
    
    //게임시작시 초기셋팅을 해 주는 부분
    private void Awake()
    {
        //랜드마크 저장 타이머 설정
        saveLandmark.SetTimer(timer);

        //단계와 목표 시간을 받아와서 설정
        level = StaticData.level;
        lineMoveDuration = StaticData.destTime;

    }

    //게임시작 초기 세팅2 (위 보다 나중에 호출됨)
    private void Start()
    {
        lineController = guideLine.GetComponent<GuideLineController>();
        //초기 1회는 중앙에서 움직이기 때문에 목표시간의 절반으로 설정
        lineMoveDuration = StaticData.destTime / 2;
        //BGM을 종료하고
        bgmSound.Stop();
        //블럭밀기용 BGM을 세팅
        bgmSound.clip = blockBGM;

        
        //처음이라면 정해진 배열을 보여주고 튜토리얼 진행
        if (StaticData.isNormalEnd == true && isFirst)
        {
            //정해진 배열 생성 명령 전달
            leftGrid.SetFirst(true, true);
            rightGrid.SetFirst(true, true);
        }
        else //아니라면 랜덤 배열
        {
            //랜덤 배열 생성 명령 전달
            leftGrid.SetFirst(false, true);
            rightGrid.SetFirst(false, true);
        }
        PlayerPrefs.SetString("LastPlayed", "Block"); //마지막으로 플레이 한 게임 기록

        //배열이 나타나는 명령 전달
        SetGameObjectsFade(1f);
    }

    //게임이 켜질 때 호출
    private void OnEnable()
    {
        //단계: Playing. setter가 Flutter로 자동 송신. background 시 사용자에게 confirm 다이얼로그 노출.
        StaticData.stage = ExerciseStage.Playing;
        StaticData.nowMode = Mode.Game; //모드 변경
        //매 진입(이어하기/재진입 포함)에 튜토리얼 다시 실행 — 사용자 결정.
        //Initalize에서 line 200이 다시 false로 set하므로 line 548의 if(!isFirst) 가드는 영향 없음.
        isFirst = true;
        pointsController.SetPointTrigger(); //캘리브레이션에서 킨 어깨 트리거 해제

        //ADR-0011 §2.2 (사용자 결정 2026-05-11): 이전 dispose 시 잔존한 튜토리얼 UI 정리.
        //GameObject.SetActive(false)로 코루틴은 자동 stop되지만 dim/tutorialGuideLine은
        //별도 parent일 수 있어 잔존 가능. 매 OnEnable에서 명시 초기화.
        if (dim != null) dim.SetActive(false);
        if (tutorialGuideLine != null) tutorialGuideLine.SetActive(false);
        if (tutorialCollider != null) tutorialCollider.enabled = false;
        triggeredTutorial = true;
        _restoredFromIncomplete = false;

        //각도 단계별로 다른 위치에 있도록 위치 설정
        sideMoveValue = sideMoveValue - 130 * level;


    }

    //ADR-0011 §2.2: dispose 시 명시 코루틴 stop + UI 잔존 정리.
    //GameObject 비활성 시 Unity가 코루틴 자동 stop하지만 dim 등 child가 다른 parent에 있으면
    //활성 상태 잔존 가능 — 명시 비활성으로 정리. OnEnable의 초기화와 짝.
    private void OnDisable()
    {
        StopAllCoroutines();
        if (dim != null) dim.SetActive(false);
        if (tutorialGuideLine != null) tutorialGuideLine.SetActive(false);
    }

    void Initalize()
    {
        if (StaticData.isNormalEnd == false && isFirst) //false면 비정상종료
        {
            //처음 실행 상태 해제
            isFirst = false;
            //팝업 표출
            popup.SetActive(true);
            //이전 비정상종료 원인이 빠른동작 5회누적인지 확인
            if (PlayerPrefs.HasKey("FastQuit") && PlayerPrefs.GetInt("FastQuit") == 1)
            {
                popupText.text = "빠른 동작 누적으로 강제종료 되었습니다.";
            }
            else
            {
                popupText.text = "이전 게임 상태를 불러옵니다.";
            }
            //2초 뒤 팝업 끄기
            Invoke("OffPopup", 2f);

            //진행중이던 각 세트별 점수 로드
            StaticData.blockGameScores.section1 = PlayerPrefs.GetInt("BlockSet1Score");
            StaticData.blockGameScores.section2 = PlayerPrefs.GetInt("BlockSet2Score");
            StaticData.blockGameScores.section3 = PlayerPrefs.GetInt("BlockSet3Score");
            SetScore();

            //몇 세트 진행중이였는지 확인 후 적용
            progress.SetCheckd(PlayerPrefs.GetInt("NowSet"));
            timer.SetEndCount(PlayerPrefs.GetInt("NowSet"));

            //남은 시간 로드
            float time = PlayerPrefs.GetFloat("GameTime");
            if (time <= 0)
            {
                progress.SetCheckd(PlayerPrefs.GetInt("NowSet") - 1);
                timer.SetEndCount(PlayerPrefs.GetInt("NowSet") - 1);
            }

            //현재까지 오브젝트 생성갯수와 성공 갯수 적용
            StaticData.pairGameDatas.count1 = PlayerPrefs.GetInt("PairCheckCount1");
            StaticData.pairGameDatas.count2 = PlayerPrefs.GetInt("PairCheckCount2");
            StaticData.pairGameDatas.count3 = PlayerPrefs.GetInt("PairCheckCount3");
            StaticData.blockGameDatas.count1 = PlayerPrefs.GetInt("PushBlockCount1");
            StaticData.blockGameDatas.count2 = PlayerPrefs.GetInt("PushBlockCount2");
            StaticData.blockGameDatas.count3 = PlayerPrefs.GetInt("PushBlockCount3");

            //타이머에 남은 시간 적용
            timer.SetRamainTime(time);
            //timer.SetState(true) ← 제거. Tutorial 동안 카운트다운 진행되면 만료 race 위험.
            //Tutorial 끝(line 381)에서 timer.SetState(true) 호출되어 그때 카운트다운 시작.

            //ADR-0011 §2.2 (사용자 결정 2026-05-11): 비정상종료 후 매 진입마다 튜토리얼 재생.
            //복원 로직(PlayerPrefs 로드 + timer 재설정) 후 Tutorial 시작.
            //_restoredFromIncomplete=true로 Tutorial 끝의 timer.SetRamainTime(gameTime)을 SKIP →
            //복원된 time 보존 → 사용자 의도("튜토리얼 먼저 → 끝나면 이어하기") 정합.
            _restoredFromIncomplete = true;
            leftGrid.SetColliderState(false);
            rightGrid.SetColliderState(false);
            StaticData.nowMode = Mode.Tutorial;
            StartCoroutine(Tutorial());
        }
        //비정상종료가 아닌 첫 실행이라면
        else if (isFirst)
        {
            //좌우 기존 콜라이더 동작 안되게 막음
            leftGrid.SetColliderState(false);
            rightGrid.SetColliderState(false);
            //현재 모드를 튜토리얼로 설정
            StaticData.nowMode = Mode.Tutorial;
            //튜토리얼 시작
            StartCoroutine(Tutorial());
        }
        else
        {
            //강제종료를 대비하여 비정상종료 상태로 만듦
            PlayerPrefs.SetInt("NormalEnd", 1);
            //게임 시작
            GameStart();
        }

        //가이드라인이 판별하는 터치영역 설정
        lineController.SetTriggerArea(bothHand);

        //BGM이 재생중이 아닐경우 재생
        if (!bgmSound.isPlaying) bgmSound.Play();
        //초기상태 해제
        isFirst = false;
        //빠른동작 누적 강제종료 상태 해제
        PlayerPrefs.SetInt("FastQuit", 0);
    }

    [Header("튜토리얼 항목들")]
    public GameObject dim; //튜토리얼의 검은 배경
    public Collider tutorialCollider; //튜토리얼에서 사용하는 블럭의 콜라이더
    public TMP_Text tutorialText; //튜토리얼의 안내 텍스트
    public GameObject[] tempObject; //튜토리얼에 나타나는 오브젝트들
    public GameObject tutorialGuideLine; //튜토리얼의 가이드라인
    public GameObject[] tempHeaderObjects; //튜토리얼의 상단 UI
    public GameObject tempScore; //튜토리얼에서 나타나는 점수
    bool triggeredTutorial = true; //튜토리얼 진행 중 사용자의 터치를 기다리기 위한 변수

    //사용자가 터치를 하면 호출되는 부분
    public void SetTutorialTriggerState(bool state)
    {
        triggeredTutorial = state;
    }
    

    IEnumerator Tutorial()
    {
        //튜토리얼 검은 배경 켜기
        dim.SetActive(true);
        //텍스트 설정
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

        //텍스트 설정
        tutorialText.text = "아래의 화살표를 보고 터치 방향을 알 수 있습니다.";
        //화살표의 부모를 변경하기 전 현재 부모를 저장
        Transform tempLeftParent = leftArrow.transform.parent;
        //화살표의 부모를 검은 배경으로 지정하여 배경 위로 나타나게 함
        leftArrow.transform.SetParent(dim.transform);
        //왼 쪽 화살표의 이미지를 불이 들어온 이미지로 교체
        leftArrow.sprite = arrowSprites[1];
        //화살표의 부모를 변경하기 전 현재 부모를 저장
        Transform tempRightParent = rightArrow.transform.parent;
        //화살표의 부모를 검은 배경으로 지정하여 배경 위로 나타나게 함
        rightArrow.transform.SetParent(dim.transform);
        //3초 대기
        yield return new WaitForSeconds(3f);

        //화살표의 부모를 원래 부모로 설정
        leftArrow.transform.SetParent(tempLeftParent);
        rightArrow.transform.SetParent(tempRightParent);
        //튜토리얼용 오브젝트 켜기
        tempObject[0].SetActive(true);
        tempObject[1].SetActive(true);
        tempObject[2].SetActive(true);
        tempObject[3].SetActive(true);
        tempObject[4].SetActive(true);
        //텍스트 설정
        tutorialText.text = "표시된 방향의 5개의 동물 중 원하는 동물을 터치해주세요.";
        //3초 대기
        yield return new WaitForSeconds(3f);

        //튜토리얼용 가이드라인 표출
        tutorialGuideLine.SetActive(true);
        //텍스트 설정
        tutorialText.text = "표시된 시간동안 움직이는 가이드라인 속도에 맞춰\n동물 캐릭터를 터치할 때 운동 효과가 높습니다.";
        //3초 대기
        yield return new WaitForSeconds(3f);

        //텍스트 설정
        tutorialText.text = "표시된 동물을 선택해보세요.\n연습에서는 시간이 지나지 않습니다.";
        //튜토리얼용 가이드라인 끄기
        tutorialGuideLine.SetActive(false);
        //튜토리얼용 콜라이더를 켜서 상호작용이 가능하도록 변경
        tutorialCollider.enabled = true;
        //상호작용이 가능한 오브젝트 제외하고 모두 끄기
        tempObject[0].SetActive(false);
        tempObject[2].SetActive(false);
        tempObject[3].SetActive(false);
        tempObject[4].SetActive(false);

        //사용자가 상호작용을 할 때 까지 대기
        while (triggeredTutorial)
        {
            yield return new WaitForSeconds(0.1f);
            continue;
        }
        //왼 쪽 영역이 보이도록 튜토리얼용 화면의 하이어라키 순서를 변경
        dim.transform.SetSiblingIndex(6);
        //상호작용 한 튜토리얼용 오브젝트 끄기
        tempObject[1].SetActive(false);
        //상호작용하여 상태가 변경된 대기용 변수를 다시 설정
        triggeredTutorial = true;
        //상호작용이 불가능하게 콜라이더의 상태 변경
        tutorialCollider.enabled = false;
        //상호작용 영역에 맞춰 왼 쪽 영역 상호작용
        leftGrid.LeftShift(1);
        //텍스트 설정
        tutorialText.text = "선택된 줄은 밀려나게 되고\n3개의 같은 동물이 모이면 성공입니다.";
        //3초 대기
        yield return new WaitForSeconds(3f);

        //텍스트 설정
        tutorialText.text = "성공한 동물의 숫자가 많을수록\n높은 점수를 획득할 수 있어요.";
        //튜토리얼용 점수 이미지 표출
        tempScore.SetActive(true);
        //3초 대기
        yield return new WaitForSeconds(3f);

        //튜토리얼용 점수 이미지 끄기
        tempScore.SetActive(false);
        //검은 배경의 순서를 다시 원래대로 변경
        dim.transform.SetSiblingIndex(13);
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

        //텍스트 설정
        tutorialText.text = "지금부터 게임을 시작하겠습니다.";
        //3초 대기
        yield return new WaitForSeconds(3f);

        //검은 배경 끄기
        dim.SetActive(false);
        //현재 모드를 튜토리얼에서 게임모드로 변경
        StaticData.nowMode = Mode.Game;
        //종료 상태를 비정상 종료 상태로 변경
        PlayerPrefs.SetInt("NormalEnd", 1);
        //마지막 플레이 게임을 블럭밀기로 설정
        PlayerPrefs.SetString("LastPlayed", "Block");
        //가이드라인 움직임 시작
        MoveGuideLine(true);
        //왼 쪽 영역을 상호작용 가능하게 변경
        leftGrid.SetColliderState(true);
        //각 영역에 상태 전달
        leftGrid.SetFirst(false, false);
        rightGrid.SetFirst(false, false);
        //ADR-0011 §2.2: 비정상종료 복원이면 line 175에서 복원된 time 유지 (gameTime 덮어쓰기 SKIP).
        //정상 첫 진입이면 기존대로 gameTime으로 새 게임 시간 설정.
        if (!_restoredFromIncomplete)
        {
            //타이머에 게임 시간 설정
            timer.SetRamainTime(gameTime);
        }
        //타이머 시작 (복원/신규 둘 다)
        timer.SetState(true);
        //화살표 상태 왼쪽으로 변경
        leftArrow.sprite = arrowSprites[1];
        rightArrow.sprite = arrowSprites[0];
    }

    /// <summary>
    /// 각 영역에 상호작용 하면 호출
    /// </summary>
    /// <param name="isLeft">왼 쪽 영역에 닿였다면 true</param>
    public void NowTriggered(bool isLeft)
    {
        //왼쪽이 닿였으면
        if (isLeft)
        {
            //화살표 상태 오른쪽으로 변경
            leftArrow.sprite = arrowSprites[0];
            rightArrow.sprite = arrowSprites[1];
            //왼 쪽 영역에 도달하는데 걸린 시간 체크
            float elapsedTime = leftGrid.EndTime();
            //만약 튜토리얼모드가 아니라면
            if (StaticData.nowMode != Mode.Tutorial)
            {
                //목표 시간 - 2초 보다 더 빠르게 도달하면
                if (elapsedTime < lineMoveDuration - 2)
                {
                    //팝업 표출
                    popup.SetActive(true);
                    //팝업의 텍스트 설정
                    popupText.text = "너무 빨라요.";
                    //2초 뒤 팝업 종료
                    Invoke("OffPopup", 2f);

                    //강제종료 스택 + 1
                    StaticData.fastTriggerCount++;
                    //스택 5면 종료
                    if (StaticData.fastTriggerCount == 5)
                    {
                        //강제종료 횟수 +1
                        StaticData.fastQuitCount++;
                        //누적된 강제종료 횟수를 저장
                        PlayerPrefs.SetInt("FastQuitCount", StaticData.fastQuitCount);
                        //강제종료로 인해 비정상종료 됐다고 저장
                        PlayerPrefs.SetInt("FastQuit", 1);
                        //종료전에 데이터를 저장
                        saveLandmark.TempSaveDatas("penalty");
                        //게임 종료, 종료 방식에 대해서는 협의 필요
                        //Application.Quit(); //250811 수정사항
                    }
                }
                //목표시간 + 2초보다 더 느리게 도달하면
                else if (elapsedTime > lineMoveDuration + 2)
                {
                    //팝업 표출
                    popup.SetActive(true);
                    //팝업 텍스트 설정
                    popupText.text = "너무 느려요.";
                    //2초뒤 팝업 종료
                    Invoke("OffPopup", 2f);
                }
            }
            //화살표, 콜라이더 상태 반대로 설정
            isNextTriggerLeft = false;
            leftGrid.SetColliderState(false);
            leftGrid.CheckColliderState(isNextTriggerLeft);
            rightGrid.CheckColliderState(isNextTriggerLeft);
        }
        else
        {
            //화살표 상태 왼 쪽으로 변경
            leftArrow.sprite = arrowSprites[1];
            rightArrow.sprite = arrowSprites[0];
            //오른쪽 영역에 도달하는데 걸린 시간 체크
            float elapsedTime = rightGrid.EndTime();
            //목표 시간 - 2초 보다 더 빠르게 도달하면
            if (elapsedTime < lineMoveDuration - 2)
            {
                //팝업 표출
                popup.SetActive(true);
                //팝업의 텍스트 설정
                popupText.text = "너무 빨라요.";
                //2초 뒤 팝업 종료
                Invoke("OffPopup", 2f);

                //강제종료 스택 + 1
                StaticData.fastTriggerCount++;
                //스택 5면 종료
                if (StaticData.fastTriggerCount == 5)
                {
                    //강제종료 횟수 +1
                    StaticData.fastQuitCount++;
                    //누적된 강제종료 횟수를 저장
                    PlayerPrefs.SetInt("FastQuitCount", StaticData.fastQuitCount);
                    //강제종료로 인해 비정상종료 됐다고 저장
                    PlayerPrefs.SetInt("FastQuit", 1);
                    //종료전에 데이터를 저장
                    saveLandmark.TempSaveDatas("penalty");
                    //게임 종료, 종료 방식에 대해서는 협의 필요
                    //Application.Quit(); //250811 수정사항
                }
            }
            //목표시간 + 2초보다 더 느리게 도달하면
            else if (elapsedTime > lineMoveDuration + 2)
            {
                //팝업 표출
                popup.SetActive(true);
                //팝업 텍스트 설정
                popupText.text = "너무 느려요.";
                //2초뒤 팝업 종료
                Invoke("OffPopup", 2f);
            }
            //화살표, 콜라이더 상태 반대로 설정
            isNextTriggerLeft = true;
            rightGrid.SetColliderState(false);
            leftGrid.CheckColliderState(isNextTriggerLeft);
            rightGrid.CheckColliderState(isNextTriggerLeft);
        }
        //가이드라인은 상호작용 한 반대쪽으로 움직이기 시작
        MoveGuideLine(!isLeft);
    }

    void GameStart() //정상 종료일 때
    {
        //타이머에 게임시간 설정
        timer.SetRamainTime(gameTime);
        //타이머 시작
        timer.SetState(true);
    }

    public void OffPopup() //팝업 끄기
    {
        popup.SetActive(false);
    }

    /// <summary>
    /// 점수 텍스트에 점수반영하는 부분
    /// </summary>
    public void SetScore()
    {
        //현재 세트를 가져옴
        int num = progress.GetCheckedNum();

        //현재 점수를 텍스트에 반영
        if (num == 0) scoreText.text = StaticData.blockGameScores.section1.ToString() + "점";
        else if (num == 1) scoreText.text = (StaticData.blockGameScores.section1 +StaticData.blockGameScores.section2).ToString() + "점";
        else if (num == 2) scoreText.text = (StaticData.blockGameScores.section1 + StaticData.blockGameScores.section2 +StaticData.blockGameScores.section3).ToString() + "점";
    }

    //가이드라인 움직임이 처음인지 판별하는 변수
    bool firstGuide = true;
    //1단계 기준의 값, 단계에 맞게 자동으로 변동
    float sideMoveValue = 670f;
    public void SetGameObjectsFade(float value)
    {
        //오브젝트 켜기/끄기
        if (value == 1f)
        {
            //각 영역과 화살표가 화면 안으로 이동
            leftSide.transform.DOLocalMoveX(leftSide.transform.localPosition.x + sideMoveValue, 0.75f).SetEase(Ease.OutBack);
            leftArrow.transform.DOLocalMoveX(leftArrow.transform.localPosition.x + 670, 0.75f).SetEase(Ease.OutBack);
            rightArrow.transform.DOLocalMoveX(rightArrow.transform.localPosition.x - 670, 0.75f).SetEase(Ease.OutBack);
            rightSide.transform.DOLocalMoveX(rightSide.transform.localPosition.x - sideMoveValue, 0.75f).SetEase(Ease.OutBack).OnComplete(delegate
            {
                //영역과 화살표 이동이 끝나면
                //콜라이더 활성화
                leftGrid.SetColliderState(true);
                rightGrid.SetColliderState(true);
                //게임 세팅
                Initalize();
                //완전 첫 진입이 아니면(튜토리얼 단계가 아니면) 가이드라인 움직임 시작
                if(!isFirst) MoveGuideLine(true);
            });
        }
        else
        {
            //콜라이더 끄기
            leftGrid.SetColliderState(false);
            rightGrid.SetColliderState(false);
            //움직이고 있는 가이드라인 멈추기
            DOTween.Kill("moveGuideLine");
            //첫 가이드라인 움직임을 위해 초기세팅
            lineMoveDuration = StaticData.destTime / 2;
            guideLine.transform.localPosition = Vector3.zero;
            firstGuide = true;
            //각 영역의 타이머 종료
            leftGrid.EndTimeWithoutReturn();
            rightGrid.EndTimeWithoutReturn();
            
            //가이드라인 끄기
            guideLine.SetActive(false);
            //각 영역과 화살표 화면 밖으로 이동
            leftSide.transform.DOLocalMoveX(leftSide.transform.localPosition.x - sideMoveValue, 0.75f).SetEase(Ease.InBack);
            rightSide.transform.DOLocalMoveX(rightSide.transform.localPosition.x + sideMoveValue, 0.75f).SetEase(Ease.InBack);
            leftArrow.transform.DOLocalMoveX(leftArrow.transform.localPosition.x - 670, 0.75f).SetEase(Ease.InBack);
            rightArrow.transform.DOLocalMoveX(rightArrow.transform.localPosition.x + 670, 0.75f).SetEase(Ease.InBack);
        }
    }

    /// <summary>
    /// 가이드라인을 움직이는 부분
    /// </summary>
    /// <param name="isLeft">움직일 방향이 왼쪽인지 체크하는 변수</param>
    public void MoveGuideLine(bool isLeft)
    {
        //현재 모드가 튜토리얼이라면 동작하지 않게
        if (StaticData.nowMode == Mode.Tutorial) return;
        //라인이 사라질 때 점수를 위해 현재 세트받아오기
        int num = progress.GetCheckedNum();
        //이전에 움직이고 있는 라인을 멈추기
        DOTween.Kill("moveGuideLine");
        //가이드라인 켜기
        guideLine.SetActive(true);
        //라인에 얼마나 붙어있는지 체크 시작
        lineController.GuideLineTimeStart();
        //방향과 팝업을 전달하여 상태체크
        lineController.SetLineState(isLeft, popup, popupText);
        //왼쪽으로 이동한다면
        if (isLeft)
        {
            //왼쪽 그리드에 시간초 재라고 알리기
            leftGrid.StartTime();
            //가이드라인 움직임 시작
            guideLine.transform.DOMove(guideDestLeft.position, lineMoveDuration).SetEase(Ease.Linear).SetId("moveGuideLine").OnKill(delegate
            {
                //가이드라인이 도착하거나 도중에 kill되면 호출
                //가이드라인의 위치를 도착지점으로 변경
                guideLine.transform.position = guideDestLeft.position;
                
                //몇 초동안 라인에 붙어서 움직였는지 체크
                float time = lineController.GetGuideLineTime();
                int score;
                //붙어있던 시간별 점수 차등 지급
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

                //차등 지급된 점수를 저장
                if (num == 0) StaticData.blockGameScores.section1 += score;
                else if (num == 1) StaticData.blockGameScores.section2 += score;
                else if (num == 2) StaticData.blockGameScores.section3 += score;

                //텍스트에 점수 반영
                SetScore();

                //처음 이동이였다면
                if (firstGuide)
                {
                    //목표 시간으로 설정
                    lineMoveDuration = StaticData.destTime;
                    //처음 상태 해제
                    firstGuide = false;
                }
            });
        }
        //오른쪽으로 이동한다면
        else
        {
            //오른쪽 영역 시간재기 시작
            rightGrid.StartTime();
            //가이드라인 움직임 시작
            guideLine.transform.DOMove(guideDestRight.position, lineMoveDuration).SetEase(Ease.Linear).SetId("moveGuideLine").OnKill(delegate
            {
                //가이드라인이 도착하거나 도중에 kill되면 호출
                //가이드라인의 위치를 도착지점으로 변경
                guideLine.transform.position = guideDestRight.position;
                //몇 초동안 라인에 붙어서 움직였는지 체크
                float time = lineController.GetGuideLineTime();
                int score;
                //붙어있던 시간별 점수 차등 지급
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

                //차등 지급된 점수를 저장
                if (num == 0) StaticData.blockGameScores.section1 += score;
                else if (num == 1) StaticData.blockGameScores.section2 += score;
                else if (num == 2) StaticData.blockGameScores.section3 += score;

                //텍스트에 점수 반영
                SetScore();

                //처음 이동이였다면
                if (firstGuide)
                {
                    //목표 시간으로 설정
                    lineMoveDuration = StaticData.destTime;
                    //처음 상태 해제
                    firstGuide = false;
                }
            });
        }

        
    }
}
