using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Collections.AllocatorManager;

/// <summary>
/// 짝맞추기 게임의 타일들이 있는 영역을 관리하는 스크립트
/// </summary>
public class PairGrid : MonoBehaviour
{
    //왼쪽인지 오른쪽인지 판별하는 변수. 인스펙터에서 설정
    public bool isLeft = false;
    //짝맞추기 게임 스크립트 참조
    public PairGame game;
    //영역의 가로, 세로 오브젝트 수
    public int width = 3;
    public int height = 3;

    
    //각 오브젝트들. 인스펙터에서 할당
    public List<GameObject[]> objectList = new List<GameObject[]>();
    public GameObject[] giraffeObjects;
    public GameObject[] mouseObjects;
    public GameObject[] pigObjects;
    public GameObject[] tigerObjects;

    //영역내의 타일 상태를 가지고있는 배열
    private PairObject[,] grid;
    //타일 선택 후 이동할 목적지
    public Transform dest;

    //타일이 이동하기전에 원래 위치를 저장해둘 변수
    Vector3 tempPos;

    //현재 선택된 오브젝트를 담아둘 변수
    GameObject nowObject;
    
    //상호작용하는 콜라이더들
    public Collider[] colliders;
    //가이드라인 오브젝트
    public GameObject guideLine;
    //터치 사운드
    public AudioSource touchSound;

    public Transform tileParent;

    //각 타일의 인덱스
    int giraffeCount = 0;
    int mouseCount = 0;
    int pigCount = 0;
    int tigerCount = 0;

    //초기 세팅
    void Start()
    {
        //그리드를 가로 세로에 맞게 초기화
        grid = new PairObject[width, height];
        //오브젝트 리스트에 각 타입별 오브젝트 추가
        objectList.Add(giraffeObjects);
        objectList.Add(mouseObjects);
        objectList.Add(pigObjects);
        objectList.Add(tigerObjects);
        //타일 생성
        GenerateGrid();
        //게임모드로 변경
        StaticData.nowMode = Mode.Game;
        //1초뒤에 게임 준비단계 실행
        Invoke("Initialize", 1f);
    }

    /// <summary>
    /// ADR-0011 §2.2 (사용자 결정 2026-05-12): 재진입 시 GenerateGrid 미트리거 + 자식 타일들이
    /// 직전 dispose 시점 위치(일부는 (10000,10000)) 그대로 잔존 → 동물 표시 안 됨. ResetGrid는
    /// DOTween 1.25초 + OnComplete 비동기라 Tutorial과 race. 이 메서드는 DOTween 없이 즉시
    /// 동기로 grid 재생성. PairGame.OnEnable에서 호출.
    /// </summary>
    public void RebuildGrid()
    {
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        //기존 자식 타일들 모두 tileParent로 보냄 + (10000,10000) 위치 → SelectObject 풀링에서
        //재선택 가능 상태로
        while (transform.childCount > 0)
        {
            Transform temp = transform.GetChild(0);
            temp.SetParent(tileParent);
            temp.localPosition = new Vector3(10000, 10000, 0);
        }
        //grid 배열 reset
        grid = new PairObject[width, height];
        //GridLayoutGroup 켜서 자동 배치 받음
        GetComponent<GridLayoutGroup>().enabled = true;
        //타일 생성 (풀에서 round-robin 가져옴)
        GenerateGrid();
        //레이아웃 강제 재계산
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.GetComponent<RectTransform>());
        //GridLayoutGroup 끄기 — 이후 게임 흐름에서 자유 배치
        GetComponent<GridLayoutGroup>().enabled = false;
        //Initialize 호출과 같은 효과 (idempotent)
        CancelInvoke("Initialize");
        Invoke("Initialize", 1f);
    }

    //게임 준비단계
    void Initialize()
    {
        //그리드 레이아웃을 꺼서 자유롭게 배치 가능하도록 변경
        GetComponent<GridLayoutGroup>().enabled = false;
    }

    /// <summary>
    /// 타일 재배치 기능
    /// </summary>
    public void ResetGrid()
    {
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        //그리드 초기화
        grid = new PairObject[width, height];
        //왼쪽영역 이동
        if(isLeft) transform.DOLocalMoveX(transform.localPosition.x - 600f, 0.75f).SetEase(Ease.OutBack).SetDelay(0.5f).OnComplete(delegate
        {
            //이동이 끝나면
            //가로x세로 만큼 반복
            for (int i = 0; i < width * height; i++)
            {
                //기존 타일들 안보이는곳으로 이동
                Transform temp = transform.GetChild(0);
                temp.SetParent(tileParent);
                temp.localPosition = new Vector3(10000, 10000, 0);
            }
            //그리드 레이아웃을 켜서 자동정렬 시킴
            GetComponent<GridLayoutGroup>().enabled = true;
            //타일 생성
            GenerateGrid();
            //레이아웃 리빌드
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.GetComponent<RectTransform>());
            //그리드 끄기
            GetComponent<GridLayoutGroup>().enabled = false;
            //0.5초 딜레이 후 다시 안쪽으로 들어옴
            transform.DOLocalMoveX(transform.localPosition.x + 600f, 0.75f).SetEase(Ease.OutBack).SetDelay(0.5f);
        });
        //오른쪽 영역 이동
        else transform.DOLocalMoveX(transform.localPosition.x + 600f, 0.75f).SetEase(Ease.OutBack).SetDelay(0.5f).OnComplete(delegate
        {
            //이동이 끝나면
            //가로x세로 만큼 반복
            for (int i = 0; i < width * height; i++)
            {
                //기존 타일들 안보이는곳으로 이동
                Transform temp = transform.GetChild(0);
                temp.SetParent(tileParent);
                temp.localPosition = new Vector3(10000, 10000, 0);
            }
            //그리드 레이아웃을 켜서 자동정렬시킴
            GetComponent<GridLayoutGroup>().enabled = true;
            //타일 생성
            GenerateGrid();
            //레이아웃 리빌드
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.GetComponent<RectTransform>());
            //그리드 끄기
            GetComponent<GridLayoutGroup>().enabled = false;
            //0.5초 딜레이 후 다시 안쪽으로 들어옴
            transform.DOLocalMoveX(transform.localPosition.x - 600f, 0.75f).SetEase(Ease.OutBack).SetDelay(0.5f).OnComplete(delegate 
            {
                game.SetCollider();
            });
        });
    }

    //도달시간을 저장하는 변수
    float time = 0f;
    //도달 시간 타이머 스위치 변수
    bool startTime = false;

    /// <summary>
    /// 도달시간 재기 시작하는 부분
    /// </summary>
    public void StartTime()
    {
        startTime = true;
    }
    /// <summary>
    /// 도달시간 타이머를 끄고 시간을 반환해주는 부분
    /// </summary>
    /// <returns></returns>
    public float EndTime()
    {
        float nowTime = time;
        startTime = false;
        return nowTime;
    }

    // Update is called once per frame
    void Update()
    {
        //타이머 변수가 켜지면
        if (startTime)
        {
            //시간 재기
            time += Time.deltaTime;
        }
        //타이머 변수가 꺼지면
        else
        {
            //시간을 0으로 초기화
            time = 0f;
        }
    }

    /// <summary>
    /// 타일을 생성할 때 오브젝트 풀링 방식으로 생성할 타일의 오브젝트를 지정해주는 부분
    /// </summary>
    /// <param name="gameObjects">전달받은 오브젝트 타입</param>
    /// <returns></returns>
    public GameObject SelectObject(GameObject[] gameObjects)
    {
        //오브젝트를 저장할 변수
        GameObject nowObject = null;
        //무한반복
        while (true)
        {
            //매개변수로 전달받은 타입과 동일한 타입의 오브젝트를 nowObject에 할당
            if (gameObjects == objectList[0])
            {
                nowObject = gameObjects[giraffeCount % 5];
                giraffeCount++;
            }
            else if (gameObjects == objectList[1])
            {
                nowObject = gameObjects[mouseCount % 5];
                mouseCount++;
            }
            else if (gameObjects == objectList[2])
            {
                nowObject = gameObjects[pigCount % 5];
                pigCount++;
            }
            else if (gameObjects == objectList[3])
            {
                nowObject = gameObjects[tigerCount % 5];
                tigerCount++;
            }
            else
            {
            }

            //현재 생성되어있는 타일이 아니면, 반복문 종료
            if (nowObject.transform.localPosition.x > 9000)
            {
                break;
            }
        }
        //할당된 오브젝트 반환
        return nowObject;
    }

    //랜덤으로 숫자를 넣을 변수
    int rand;
    /// <summary>
    /// 타일을 생성하는 부분
    /// </summary>
    void GenerateGrid()
    {
        //왼 쪽이면 0~1, 오른쪽이면 2~3의 값을 할당
        if (isLeft) rand = Random.Range(0, objectList.Count / 2);
        else rand = Random.Range(objectList.Count / 2, objectList.Count);

        //가로x세로만큼 반복
        for (int i = 0; i < width; i++)
        {
            for (int j = 0;j < height; j++)
            {
                //오브젝트를 라운드로빈 방식으로 하나씩 생성
                GameObject block = SelectObject(objectList[rand % objectList.Count]);
                //블록의 부모를 변경
                block.transform.SetParent(transform, false);
                //다음 타입을 생성하기 위해 값 증가
                rand++;

                //그리드에 할당
                grid[i, j] = block.GetComponent<PairObject>();
                //블록이 가지고있는 스크립트에 현재 위치 할당
                grid[i, j].posX = i;
                grid[i, j].posY = j;
                //ADR-0011 §2.2 (사용자 결정 2026-05-12): 풀링 패턴에서 직전 SetSprite(1/2)이
                //잔존한 prefab이 grid에 들어가면 일부 동물(예: 돼지)의 sprites[1/2]이 null인
                //케이스 visual 빠짐. 매 GenerateGrid에 명시 sprites[0] 기본 상태로 reset.
                grid[i, j].SetSprite(0);
            }
        }
    }

    /// <summary>
    /// 진행 불가능 상태를 검사하기 위해 상호작용 가능한 위치의 오브젝트들을 반환
    /// </summary>
    /// <returns></returns>
    public PairObject[] GetObjects()
    {
        PairObject[] objects = { grid[0,0], grid[0, 1], grid[0, 2] };

        return objects;
    }

    //상호작용 한 라인 넘버를 저장해놓을 변수
    int nowLineNum;
    /// <summary>
    /// 타일에 상호작용했을 때 호출되는 부분
    /// </summary>
    /// <param name="lineNum">아래에서부터 몇 번째 줄에 상호작용 했는지 판별하는 변수</param>
    public void Triggered(int lineNum)
    {
        //비어있을 때 트리거 하는 경우 동작하지 않음
        if (grid[0, lineNum] == null) return;
        //게임 스크립트에 방향 전달
        game.SetNowLeft(isLeft);
        //가이드라인 움직임 멈추기
        DOTween.Kill("moveGuideLine");
        //터치사운드 재생
        touchSound.Play();
        //콜라이더 끄기
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        //라인 넘버 저장
        nowLineNum = lineNum;
        //왼쪽이면
        if (isLeft)
        {
            //이동하기 전 위치를 저장
            tempPos = grid[0, lineNum].transform.localPosition;
            //현재 오브젝트 저장
            nowObject = grid[0, lineNum].gameObject;
            //목표 지점으로 이동
            grid[0, lineNum].transform.DOMove(dest.position, 1f).OnComplete(delegate
            {
                //이동이 끝나면 방향과 오브젝트를 전달
                game.Setting(isLeft, grid[0, lineNum]);
            });
        }
        else
        {
            //이동하기 전 위치를 저장
            tempPos = grid[0, lineNum].transform.localPosition;
            //현재 오브젝트 저장
            nowObject = grid[0, lineNum].gameObject;
            //목표 지점으로 이동
            grid[0, lineNum].transform.DOMove(dest.position, 1f).OnComplete(delegate
            {
                //이동이 끝나면 방향과 오브젝트를 전달
                game.Setting(isLeft, grid[0, lineNum]);
            });
        }
        //동물 표정 변경
        grid[0, lineNum].SetSprite(1);
        //동물 떠는 효과 시작
        nowObject.transform.GetChild(0).DOShakePosition(9999f, 10, 50).SetId("Shake");
        //가이드라인 움직임 시작
        game.MoveGuideLine();

    }

    /// <summary>
    /// 정답일 때의 동작
    /// </summary>
    public void Correct()
    {
        //떠는 효과 종료
        DOTween.Kill("Shake");
        //위치가 틀어진 내부 동물 이미지를 다시 원위치
        nowObject.transform.GetChild(0).localPosition = Vector3.zero;
        //동물 표정 이미지 변경
        nowObject.GetComponent<PairObject>().SetSprite(2);
        //작아졌다 커지는 효과
        nowObject.transform.DOScale(0.5f, 1f / 4).SetLoops(2, LoopType.Yoyo).OnComplete(delegate
        {
            //끝나면 코루틴 실행
            StartCoroutine(DestEffect());
        });
    }

    //별모양 이펙트
    public ParticleSystem[] starEffect;

    /// <summary>
    /// 정답 동작 이어서 진행
    /// </summary>
    /// <returns></returns>
    IEnumerator DestEffect()
    {
        //오브젝트 크기를 0으로 만듦
        nowObject.transform.DOScale(0f, 1f / 4);
        // 터지는 이펙트
        starEffect[0].Play();
        // 0.5초 대기
        yield return new WaitForSeconds(0.5f);
        //오브젝트를 보이지않는 곳으로 이동
        Transform temp = nowObject.transform;
        temp.SetParent(tileParent);
        temp.localPosition = new Vector3(10000, 10000, 0);// Vector3.one * 10000;
        temp.localScale = Vector3.one;
        //동물 표정 이미지 변경
        nowObject.GetComponent<PairObject>().SetSprite(0);
        //현재 오브젝트를 비워줌
        nowObject = null;
        
        //왼 쪽일 때
        if (isLeft)
        {
            //오브젝트 생성
            GameObject block = SelectObject(objectList[rand % objectList.Count]);
            //오브젝트의 부모 변경
            block.transform.SetParent(transform, false);
            //오브젝트의 크기를 1로 설정 (없앨 때 0으로 만들기 때문)
            block.transform.localScale = Vector3.one;
            //오브젝트의 위치를 가장 바깥쪽 타일의 옆으로 이동
            block.transform.localPosition = grid[2, nowLineNum].transform.localPosition - new Vector3(185, 0, 0);
            //라운드로빈 변수 값 증가
            rand++;
            //생성한 오브젝트 위치를 보이는 가장 바깥열의 타일 위치로 이동
            block.transform.DOLocalMove(grid[2, nowLineNum].transform.localPosition, 1f);
            //타일들을 한 칸씩 안쪽으로 이동
            grid[2, nowLineNum].transform.DOLocalMove(grid[1, nowLineNum].transform.localPosition, 1f);
            grid[1, nowLineNum].transform.DOLocalMove(tempPos, 1f).OnComplete(delegate
            {
                //타일들의 이동이 끝나면
                //그리드에 현재 상태 반영
                grid[0, nowLineNum] = grid[1, nowLineNum];
                grid[1, nowLineNum] = grid[2, nowLineNum];
                grid[2, nowLineNum] = block.GetComponent<PairObject>();
            });
        }
        //오른쪽일 때
        else
        {
            //오브젝트 생성
            GameObject block = SelectObject(objectList[rand % objectList.Count]);
            //오브젝트의 부모 변경
            block.transform.SetParent(transform, false);
            //오브젝트의 크기를 1로 설정 (없앨 때 0으로 만들기 때문)
            block.transform.localScale = Vector3.one;
            //오브젝트의 위치를 가장 바깥쪽 타일의 옆으로 이동
            block.transform.localPosition = grid[2, nowLineNum].transform.localPosition + new Vector3(185, 0, 0);
            //라운드로빈 변수 값 증가
            rand++;
            //생성한 오브젝트 위치를 보이는 가장 바깥열의 타일 위치로 이동
            block.transform.DOLocalMove(grid[2, nowLineNum].transform.localPosition, 1f);
            //타일들을 한 칸씩 안쪽으로 이동
            grid[2, nowLineNum].transform.DOLocalMove(grid[1, nowLineNum].transform.localPosition, 1f);
            grid[1, nowLineNum].transform.DOLocalMove(tempPos, 1f).OnComplete(delegate
            {
                //타일들의 이동이 끝나면
                //그리드에 현재 상태 반영
                grid[0, nowLineNum] = grid[1, nowLineNum];
                grid[1, nowLineNum] = grid[2, nowLineNum];
                grid[2, nowLineNum] = block.GetComponent<PairObject>();
                //현재 상태에서 짝맞추기가 가능한 상태인지 확인
                game.CheckUnable();
            });
        }
    }


    /// <summary>
    /// 오답일 때의 동작
    /// </summary>
    public void Fail()
    {
        //왼 쪽일 때
        if (isLeft)
        {
            //타일이 원래 위치로 이동
            nowObject.transform.DOLocalMove(tempPos, 1f);
        }
        //오른쪽일 때
        else
        {
            //타일이 원래 위치로 이동
            nowObject.transform.DOLocalMove(tempPos, 1f).OnComplete(delegate
            {
                //이동이 끝나면 콜라이더 상태 변경
                game.SetCollider();
            });
        }
        //동물 표정 원래대로 변경
        nowObject.GetComponent<PairObject>().SetSprite(0);
        //떠는 효과 끄기
        DOTween.Kill("Shake");
        //위치가 틀어진 내부 동물 이미지를 다시 원위치
        nowObject.transform.GetChild(0).localPosition = Vector3.zero;
    }

    /// <summary>
    /// 게임시간이 다 되어서 휴식시간이 되면 호출하는 부분
    /// </summary>
    public void EndSet()
    {
        //콜라이더 키기
        foreach(Collider col in colliders)
        { 
            col.enabled = true;
        }
        //타이머 스위치 false로 변경
        startTime = false;

        //왼쪽일 때
        if (isLeft)
        {
            //타일이 선택 된 상태라면
            if (nowObject != null)
            {
                //원래 위치로 즉시 이동
                nowObject.transform.localPosition = tempPos;
                //동물 표정 원래대로 변경
                nowObject.GetComponent<PairObject>().SetSprite(0);
                //떠는 효과 종료
                DOTween.Kill("Shake");
                //위치가 틀어진 내부 동물 이미지를 다시 원위치
                nowObject.transform.GetChild(0).localPosition = Vector3.zero;
            }
        }
        //오른쪽일 때
        else
        {
            //타일이 선택 된 상태라면
            if (nowObject != null)
            {
                //원래 위치로 즉시 이동
                nowObject.transform.localPosition = tempPos;
                //동물 표정 원래대로 변경
                nowObject.GetComponent<PairObject>().SetSprite(0);
                //떠는 효과 종료
                DOTween.Kill("Shake");
                //위치가 틀어진 내부 동물 이미지를 다시 원위치
                nowObject.transform.GetChild(0).localPosition = Vector3.zero;
            }
        }

        
    }
}
