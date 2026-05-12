using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Collections.AllocatorManager;

/// <summary>
/// 블럭밀기 게임의 각 영역에서 블럭들을 관리하는 스크립트
/// </summary>
public class GridController : MonoBehaviour
{
    //스크립트 참조
    public BlockGame blockGame;
    public SetProgress progress;
    public ShowScoreImages showScoreImages;

    //가로 세로 칸 수
    public int width = 5;
    public int height = 5;

    //블록 배열과 위치배열
    private Block[,] grid;
    public Vector2[,] gridPositions;

    //왼쪽인지 인스펙터에서 판별하는 변수
    public bool isLeft = false;

    //터치 가능 영역들의 콜라이더
    public Collider[] colliders;

    //블럭 움직이는 애니메이션 시간
    public float moveTime = 1f;


    //연쇄 횟수를 저장하는 변수
    int comboCount;
    
    //효과음
    public AudioSource touchSound;
    public AudioSource matchSound;

    //생성할 동물 타일 오브젝트들
    public List<GameObject[]> objectList = new List<GameObject[]>();
    public GameObject[] frogObjects;
    public GameObject[] giraffeObjects;
    public GameObject[] mouseObjects;
    public GameObject[] pigObjects;
    public GameObject[] tigerObjects;

    //오브젝트들의 인덱스 관리를 위한 변수
    int frogCount = 0;
    int giraffeCount = 0;
    int mouseCount = 0;
    int pigCount = 0;
    int tigerCount = 0;

    //미리 생성해놓은 튜토리얼용 배치, 생성블록
    int[] rightGrid = { 1, 2, 3, 2, 3, 3, 1, 0, 4, 0, 1, 4, 0, 1, 2, 0, 1, 4, 4, 1, 2, 4, 3, 2, 1 };
    int[] leftGrid = { 0, 0, 4, 2, 4, 1, 4, 0, 4, 2, 4, 2, 3, 2, 4, 3, 1, 4, 3, 3, 3, 0, 4, 1, 4 };
    int[] customDrop2 = { 2, 1, 0 };

    //처음인지 체크하는 튜토리얼용 변수
    bool isFirst = true;

    
    //변수 초기화 및 리스트에 오브젝트들 추가
    void Awake()
    {
        grid = new Block[width, height];
        gridPositions = new Vector2[width, height];

        objectList.Add(frogObjects);
        objectList.Add(giraffeObjects);
        objectList.Add(mouseObjects);
        objectList.Add(pigObjects);
        objectList.Add(tigerObjects);
    }

    /// <summary>
    /// 처음인지 판별 후 튜토리얼용 정해진 배치를 생성할 지, 랜덤 배치를 생성할 지 결정하는 부분
    /// </summary>
    /// <param name="first"></param>
    /// <param name="makeGrid"></param>
    public void SetFirst(bool first, bool makeGrid)
    {
        //상태를 저장
        isFirst = first;

        //타일 생성명령이 들어오면, 상태에따라 생성
        if (makeGrid)
        {
            if (isFirst) CustomGenerateGrid();
            else GenerateGrid();
        }
    }

    /// <summary>
    /// 타일을 생성할 때 오브젝트 풀링 방식으로 생성할 타일의 오브젝트를 지정해주는 부분
    /// </summary>
    /// <param name="gameObjects"></param>
    /// <returns></returns>
    public GameObject SelectObject(GameObject[] gameObjects)
    {
        //생성할 타일의 오브젝트를 넣을 변수 선언
        GameObject nowObject = null;
        while (true)
        {
            //어떤 종류의 오브젝트인지 판별 후 nowObject에 할당
            if (gameObjects == objectList[0])
            {
                nowObject = gameObjects[frogCount % frogObjects.Length];
                frogCount++;
            }
            else if (gameObjects == objectList[1])
            {
                nowObject = gameObjects[giraffeCount % giraffeObjects.Length];
                giraffeCount++;
            }
            else if (gameObjects == objectList[2])
            {
                nowObject = gameObjects[mouseCount % mouseObjects.Length];
                mouseCount++;
            }
            else if (gameObjects == objectList[3])
            {
                nowObject = gameObjects[pigCount % pigObjects.Length];
                pigCount++;
            }
            else if (gameObjects == objectList[4])
            {
                nowObject = gameObjects[tigerCount % tigerObjects.Length];
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
        return nowObject;
    }

    /// <summary>
    /// 정해진 배열로 타일 생성
    /// </summary>
    void CustomGenerateGrid()
    {
        for (int i = 0; i < width * height; i++)
        {
            GameObject block;
            if (isLeft)
            {
                //생성할 오브젝트를 정해진 순서로 받아옴
                block = SelectObject(objectList[leftGrid[i]]);
                //생성한 오브젝트의 부모를 그리드 영역으로 변경
                block.transform.SetParent(transform, false);
            }
            else
            {
                //생성할 오브젝트를 정해진 순서로 받아옴
                block = SelectObject(objectList[rightGrid[i]]);
                //생성한 오브젝트의 부모를 그리드 영역으로 변경
                block.transform.SetParent(transform, false);
            }
            
            //x 와 y 좌표를 계산해서 생성
            int x = i % width;
            int y = i / width;

            //각 오브젝트의 Block스크립트에 현재 위치값 전달
            grid[x, y] = block.GetComponent<Block>();
            grid[x, y].posX = x;
            grid[x, y].posY = y;
            //ADR-0011 §2.2 (사용자 결정 2026-05-12): 풀링 prefab의 잔존 sprite/scale/tween 정리.
            ResetBlockState(block);
        }
        //블록 생성이 반영된 뒤 Initialize 호출
        Invoke("Initialize", 1f);
    }
    public void GenerateGrid()
    {
        for (int i = 0; i < width * height; i++)
        {
            //랜덤으로 숫자 생성
            int rand = Random.Range(0, objectList.Count);
            //생성할 오브젝트를 랜덤으로 받아옴
            GameObject block = SelectObject(objectList[rand]);
            //생성한 오브젝트의 부모를 그리드 영역으로 변경
            block.transform.SetParent(transform, false);
            //x 와 y 좌표를 계산해서 생성
            int x = i % width;
            int y = i / width;

            //각 오브젝트의 Block스크립트에 현재 위치값 전달
            grid[x, y] = block.GetComponent<Block>();
            grid[x, y].posX = x;
            grid[x, y].posY = y;
            //ADR-0011 §2.2 (사용자 결정 2026-05-12): 풀링 prefab의 잔존 sprite/scale/tween 정리.
            ResetBlockState(block);
        }
        //블록 생성이 반영된 뒤 Initialize 호출
        Invoke("Initialize", 1f);
    }

    /// <summary>
    /// ADR-0011 §2.2 (사용자 결정 2026-05-12): PairGrid.RebuildGrid 패턴.
    /// 재진입 시 GridController.Start의 SetFirst 미트리거 + 풀링 prefab 잔존 상태 정리.
    /// 자식 모두 (10000,10000)로 이동 + scale/sprite reset → grid 배열 reset → SetFirst 재호출.
    /// </summary>
    public void RebuildGrid()
    {
        foreach (Collider col in colliders) col.enabled = false;
        //진행 중 DOTween 정리 — DOMove/DOScale/Shake가 grid prefab 위치/scale 덮어쓰지 못하게.
        DOTween.Kill("Shake");
        DOTween.Kill("moveGuideLine");
        //모든 자식 정리 (풀링 자식 list 보존, 위치만 (10000,10000)로 이동).
        for (int i = 0; i < transform.childCount; i++)
        {
            ResetBlockState(transform.GetChild(i).gameObject);
            transform.GetChild(i).localPosition = Vector3.one * 10000;
        }
        grid = new Block[width, height];
        GetComponent<GridLayoutGroup>().enabled = true;
        if (isFirst) CustomGenerateGrid();
        else GenerateGrid();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        GetComponent<GridLayoutGroup>().enabled = false;
        CancelInvoke("Initialize");
        Invoke("Initialize", 1f);
    }

    /// <summary>
    /// 단일 block prefab의 잔존 상태 정리. tween kill + localScale/child position reset + SetSprite(0).
    /// 재진입 시 stale 상태로 보이거나 위치 어긋남 회피.
    /// </summary>
    void ResetBlockState(GameObject block)
    {
        if (block == null) return;
        DOTween.Kill(block.transform);
        if (block.transform.childCount > 0)
        {
            DOTween.Kill(block.transform.GetChild(0));
            block.transform.GetChild(0).localPosition = Vector3.zero;
            block.transform.GetChild(0).localScale = Vector3.one;
        }
        block.transform.localScale = Vector3.one;
        var b = block.GetComponent<Block>();
        if (b != null) b.SetSprite(0);
    }

    /// <summary>
    /// 블록이 생성된 뒤 해야하는 작업들
    /// </summary>
    void Initialize()
    {
        //그리드 레이아웃 끄기
        gameObject.GetComponent<GridLayoutGroup>().enabled = false;
        //현재 오브젝트들의 위치를 저장
        for(int i = 0;i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                gridPositions[i, j] = grid[i,j].transform.localPosition;
            }
        }
        //초기생성 이후 블록 매칭 검사
        CheckType();
    }

    //사용자가 해당 영역 타일들에 상호작용하는데 걸리는 시간과 관련된 변수
    float time = 0f;
    bool startTime = false;

    /// <summary>
    /// 시간을 재기 시작함
    /// </summary>
    public void StartTime()
    {
        startTime = true;
    }
    /// <summary>
    /// 타이머를 멈추고 시간을 반환
    /// </summary>
    /// <returns></returns>
    public float EndTime()
    {
        float nowTime = time;
        startTime = false;
        return nowTime;
    }

    /// <summary>
    /// 시간 반환 없이 타이머 정지
    /// </summary>
    public void EndTimeWithoutReturn()
    {
        startTime = false;
    }

    
    private void Update()
    {
        //타이머가 켜지면
        if (startTime)
        {
            //시간 재기 시작
            time += Time.deltaTime;
        }
        //타이머를 끄면
        else
        {
            //시간을 0으로 초기화
            time = 0f;
        }
    }
    //타일들의 움직임이 끝나는 것을 체크하는 변수
    bool isEndMove = false;

    /// <summary>
    /// 콜라이더의 상태를 변경해도 되는지 체크하는 부분
    /// </summary>
    /// <param name="isNextLeft"></param>
    public void CheckColliderState(bool isNextLeft)
    {
        //전달받은 변수의 상태가 스크립트가 가지고있는 방향과 일치하고 타일의 움직임이 끝났으면
        if(isNextLeft == isLeft && isEndMove)
        {
            //콜라이더 켜기
            SetColliderState(true);
        }
    }

    /// <summary>
    /// 콜라이더 상태를 변경하는 부분
    /// </summary>
    /// <param name="state"></param>
    public void SetColliderState(bool state)
    {
        //콜라이더의 갯수만큼
        for (int i = 0; i < colliders.Length; i++)
        {
            //전달받은 상태로 변경
            colliders[i].enabled = state;
        }
    }


    /// <summary>
    /// 블록을 오른쪽으로 미는 부분 (오른쪽 타일 영역만 해당)
    /// </summary>
    /// <param name="lineNum">상호작용한 라인의 숫자. 아래에서부터 0 ~ 4</param>
    public void RightShift(int lineNum)
    {
        //터치 사운드 재생
        touchSound.Play();
        //현재 몇 세트 진행중인지 확인하여 저장
        int num = progress.GetCheckedNum();
        //새로운 터치의 시작이므로 연쇄를 0으로 초기화
        comboCount = 0;

        //세트에 따라 몇 회의 터치를 했는지 저장하는 변수에 횟수 추가
        if (num == 0) StaticData.blockGameDatas.count1++;
        else if (num == 1) StaticData.blockGameDatas.count2++;
        else if (num == 2) StaticData.blockGameDatas.count3++;

        //블록 게임 스크립트에 닿았다고 전달
        blockGame.NowTriggered(isLeft);
        
        //제일 바깥쪽의 영역을 제외하고 한 칸씩 옆의 타일 위치로 이동
        for (int i = 0; i < 4; i++)
        {
            grid[i, lineNum].transform.DOLocalMove(gridPositions[i + 1, lineNum], moveTime);
        }

        //가장 마지막 타일을 임시저장
        GameObject temp = grid[4, lineNum].gameObject;
        //마지막 타일은 내 위치에서 옆으로 120만큼 이동
        grid[4, lineNum].transform.DOLocalMoveX(gridPositions[4, lineNum].x + 120, moveTime).OnComplete(delegate {
            //이동이 끝나면 타일을 보이지 않는곳으로 이동
            temp.transform.localPosition = Vector3.one * 10000;
            //라인넘버를 가지고 위에서 블록을 떨어뜨리는 부분 호출
            DropTop(lineNum); 
        });

        //새로 이동한 위치로 그리드배열의 값 변경
        for (int i = 4; i > 0; i--)
        {
            grid[i, lineNum] = grid[i - 1, lineNum];
        }
        //밀어서 비게 된 부분
        grid[0, lineNum] = null;
    }

    /// <summary>
    /// 블록을 왼 쪽으로 미는 부분 (왼 쪽 타일 영역만 해당)
    /// </summary>
    /// <param name="lineNum">상호작용한 라인의 숫자. 아래에서부터 0 ~ 4</param>
    public void LeftShift(int lineNum)
    {
        //터치 사운드 재생
        touchSound.Play();
        //현재 몇 세트 진행중인지 확인하여 저장
        int num = progress.GetCheckedNum();
        //새로운 터치의 시작이므로 연쇄를 0으로 초기화
        comboCount = 0;

        //세트에 따라 몇 회의 터치를 했는지 저장하는 변수에 횟수 추가
        if (num == 0) StaticData.blockGameDatas.count1++;
        else if (num == 1) StaticData.blockGameDatas.count2++;
        else if (num == 2) StaticData.blockGameDatas.count3++;

        //블록 게임 스크립트에 닿았다고 전달
        blockGame.NowTriggered(isLeft);

        //제일 바깥쪽의 영역을 제외하고 한 칸씩 옆의 타일 위치로 이동
        for (int i = 1; i <= 4; i++)
        {
            grid[i, lineNum].transform.DOLocalMove(gridPositions[i - 1, lineNum], moveTime);
        }

        //가장 마지막 타일을 임시저장
        GameObject temp = grid[0, lineNum].gameObject;
        //마지막 타일은 내 위치에서 옆으로 120만큼 이동
        grid[0, lineNum].transform.DOLocalMoveX(gridPositions[0, lineNum].x - 120, moveTime).OnComplete(delegate {
            //이동이 끝나면 타일을 보이지 않는곳으로 이동
            temp.transform.localPosition = Vector3.one * 10000;
            //라인넘버를 가지고 위에서 블록을 떨어뜨리는 부분 호출
            DropTopLeft(lineNum); 
            });

        //새로 이동한 위치로 그리드배열의 값 변경
        for (int i = 0; i < 4; i++)
        {
            grid[i, lineNum] = grid[i + 1, lineNum];
        }
        //밀어서 비게 된 부분
        grid[4, lineNum] = null;
    }

    /// <summary>
    /// 오른쪽 영역을 밀어서 위에서 블록이 떨어지는 부분
    /// </summary>
    /// <param name="lineNum">몇 번째 라인을 밀었는지의 숫자</param>
    public void DropTop(int lineNum)
    {
        //랜덤 숫자 생성
        int rand = Random.Range(0, objectList.Count);
        //랜덤으로 정해진 종류의 블럭 생성
        GameObject block = SelectObject(objectList[rand]);
        //블럭의 부모를 변경
        block.transform.SetParent(transform, false);
        //새로 생성한 블럭의 위치를 가장 안쪽 블록 위로 변경
        block.transform.localPosition = gridPositions[0,4] + new Vector2(0, 120);

        //상호작용 한 줄 부터 위의 블럭들을 한 칸씩 아래로 이동
        for (int i = 1 + lineNum; i < 5; i++)
        {
            grid[0, i].transform.DOLocalMove(gridPositions[0, i - 1], moveTime);
        }
        //새로 생성한 블럭을 한 칸 아래로 이동
        block.transform.DOLocalMoveY(gridPositions[0, 4].y, moveTime).OnComplete(delegate
        {
            //이동이 끝났으면 블럭 매치 검사
            CheckType();
        });

        //그리드 배열에 바뀐 값 등록
        for (int i = 0 + lineNum; i < 4; i++)
        {
            grid[0, i] = grid[0, i + 1];
        }
        //비게된 위치에 새로 생성한 블럭의 값 입력
        grid[0, 4] = block.GetComponent<Block>();
        //각 블럭이 들고있는 스크립트에 위치값 업데이트
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                grid[i, j].posX = i;
                grid[i, j].posY = j;
            }
        }
    }

    /// <summary>
    /// 왼 쪽 영역을 밀어서 위에서 블록이 떨어지는 부분
    /// </summary>
    /// <param name="lineNum">몇 번째 라인을 밀었는지의 숫자</param>
    public void DropTopLeft(int lineNum)
    {
        //랜덤 숫자 생성
        int rand = Random.Range(0, objectList.Count);
        //랜덤으로 정해진 종류의 블럭 생성
        GameObject block = SelectObject(objectList[rand]);
        //블럭의 부모를 변경
        block.transform.SetParent(transform, false);
        //새로 생성한 블럭의 위치를 가장 안쪽 블록 위로 변경
        block.transform.localPosition = gridPositions[4, 4] + new Vector2(0, 120);

        //상호작용 한 줄 부터 위의 블럭들을 한 칸씩 아래로 이동
        for (int i = 1 + lineNum; i < 5; i++)
        {
            grid[4, i].transform.DOLocalMove(gridPositions[4, i - 1], moveTime);
        }
        //새로 생성한 블럭을 한 칸 아래로 이동
        block.transform.DOLocalMoveY(gridPositions[4, 4].y, moveTime).OnComplete(delegate
        {
            //이동이 끝났으면 블럭 매치 검사
            CheckType();
        });

        //그리드 배열에 바뀐 값 등록
        for (int i = 0 + lineNum; i < 4; i++)
        {
            grid[4, i] = grid[4, i + 1];
        }
        //비게된 위치에 새로 생성한 블럭의 값 입력
        grid[4, 4] = block.GetComponent<Block>();
        //각 블럭이 들고있는 스크립트에 위치값 업데이트
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                grid[i, j].posX = i;
                grid[i, j].posY = j;
            }
        }
    }

    /// <summary>
    /// 블럭이 3개 모였는지 검사하는 부분
    /// </summary>
    public void CheckType()
    {
        //조건에 맞는 블럭을 넣을 리스트 생성
        List<Block> blocks = new List<Block>();
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                //내 옆 타일이 나와 같은 타일이라면
                if(i + 1 < 5 && grid[i, j].type == grid[i+1, j].type)
                {
                    //내 옆타일과 그 옆타일도 같은 타입이라면
                    if(i + 2 < 5 && grid[i + 1, j].type == grid[i + 2, j].type)
                    {
                        //리스트에 들어있지 않다면 리스트에 추가
                        if (!blocks.Contains(grid[i, j]))
                        {
                            blocks.Add(grid[i, j]);
                        }
                        if (!blocks.Contains(grid[i + 1, j]))
                        {
                            blocks.Add(grid[i + 1, j]);
                        }
                        if (!blocks.Contains(grid[i + 2, j]))
                        {
                            blocks.Add(grid[i + 2, j]);
                        }
                    }
                }
                //내 위 타일이 나와 같은 타입이라면
                if (j + 1 < 5 && grid[i, j].type == grid[i, j + 1].type)
                {
                    //내 위 타일과 그 위 타일이 같은 타입이라면
                    if (j + 2 < 5 && grid[i, j + 1].type == grid[i, j + 2].type)
                    {
                        //리스트에 들어있지 않다면 리스트에 추가
                        if (!blocks.Contains(grid[i, j])) blocks.Add(grid[i, j]);
                        if (!blocks.Contains(grid[i, j + 1])) blocks.Add(grid[i, j + 1]);
                        if (!blocks.Contains(grid[i, j + 2])) blocks.Add(grid[i, j + 2]);
                    }
                }

            }
        }
        //블럭계산중이므로 움직이지 않게 설정
        isEndMove = false;

        //반복문 횟수를 체크할 변수
        int count = 0;
        //리스트에 저장된 블록의 수를 저장해놓을 변수
        int tempCount = blocks.Count;
        //사라지는 블록들의 중앙 위치값을 구하기 위해 위치값을 더해줄 변수
        Vector3 blocksPos = Vector3.zero;
        //조건에 맞는 블록의 갯수만큼 반복
        foreach (Block block in blocks)
        {
            //블록의 위치값을 더해줌
            blocksPos += block.transform.position;
            //각 블록의 동물 얼굴표정을 변경
            block.SetSprite(1);
            //블록 속 동물에게 떨림효과
            block.transform.GetChild(0).DOShakePosition(moveTime / 2, 10, 50).OnComplete(delegate
            {
                //효과가 끝나면 크기를 작아졌다 커지는 효과 시작
                block.transform.DOScale(0.5f, moveTime / 2).SetLoops(2, LoopType.Yoyo).OnComplete(delegate
                {
                    //효과가 끝나면 블록이 있던 자리를 배열에 null상태로 변경
                    grid[block.posX, block.posY] = null;
                    //블록을 안보이는 위치로 변경
                    block.transform.localPosition = Vector3.one * 10000;
                    //반복횟수 증가
                    count++;
                    //반복횟수가 블록의 총 갯수와 동일하면 마지막단계
                    if (count == tempCount) 
                    {
                        //블록의 위치값을 총 갯수만큼 나누어 가운데 위치를 구함
                        blocksPos /= tempCount;
                        //점수 이미지를 가운데 위치로 변경
                        showScoreImages.transform.position = blocksPos;
                        //총 갯수를 전달하여 점수계산
                        CalcScore(tempCount);
                        //후처리 시작
                        AfterDestroy(); 
                    }
                    //오브젝트를 추후 다시 사용하기 때문에 이미지와 크기 초기화
                    block.SetSprite(0);
                    block.transform.localScale = Vector3.one;
                });
            });
        }
        //조건에 맞아 터지는 블럭이 하나라도 있으면
        if (blocks.Count > 0)
        {
            //연쇄 카운트 증가
            comboCount++;
            //성공 효과음 재생
            matchSound.Play();
        }
        //터지는 블럭이 없으면
        else
        {
            //움직임 끝난상태 설정
            isEndMove = true;
            //콜라이더 상태 확인 (블록게임의 다음 이동위치를 전달해서 체크)
            CheckColliderState(BlockGame.isNextTriggerLeft);
        }
        //계산이 끝났으면 리스트 초기화
        blocks.Clear();
    }
    
    /// <summary>
    /// 점수 계산
    /// </summary>
    /// <param name="blockCount">터진 블록의 갯수</param>
    public void CalcScore(int blockCount)
    {
        //현재 세트 숫자를 가져옴
        int num = progress.GetCheckedNum();
        //점수를 넣을 변수
        int nowScore = 0;

        //터진 블록의 갯수만큼 점수 설정
        if(blockCount > 6)
        {
            nowScore += 15;
        } 
        else if(blockCount == 6)
        {
            nowScore += 13;
        }
        else if (blockCount == 5)
        {
            nowScore += 10;
        }
        else if (blockCount == 4)
        {
            nowScore += 8;
        }
        else if (blockCount == 3)
        {
            nowScore += 5;
        }

        //연쇄가 2번이상이면 추가점수
        if(comboCount > 1)
        {
            nowScore += 10;
        }

        //세트별 점수에 추가
        if (num == 0) StaticData.blockGameScores.section1 += nowScore;
        else if (num == 1) StaticData.blockGameScores.section2 += nowScore;
        else if (num == 2) StaticData.blockGameScores.section3 += nowScore;

        //텍스트에 현재점수 반영
        blockGame.SetScore();
        //점수 이미지 표출
        showScoreImages.SetScore(nowScore, 2);
        
    }

    /// <summary>
    /// 블록이 터진 이후 처리를 하는 부분
    /// </summary>
    public void AfterDestroy()
    {
        //블록이 터진 빈공간의 갯수를 체크하는 변수
        int null0 = 0;
        int null1 = 0;
        int null2 = 0;
        int null3 = 0;
        int null4 = 0;
        //터지지않은 블록을 넣어둘 리스트들
        List<int> notNull0 = new List<int>();
        List<int> notNull1 = new List<int>();
        List<int> notNull2 = new List<int>();
        List<int> notNull3 = new List<int>();
        List<int> notNull4 = new List<int>();

        //null 체크
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (grid[i, j] == null)
                {
                    if (i == 0)
                    {
                        null0++;
                    }
                    else if (i == 1) null1++;
                    else if (i == 2) null2++;
                    else if (i == 3) null3++;
                    else if (i == 4) null4++;
                }
                else
                {
                    if (i == 0) notNull0.Add(j);
                    if (i == 1) notNull1.Add(j);
                    if (i == 2) notNull2.Add(j);
                    if (i == 3) notNull3.Add(j);
                    if (i == 4) notNull4.Add(j);
                }
            }
        }

        //빈 칸 없으면
        if (null0 == 0 && null1 == 0 && null2 == 0 && null3 == 0 && null4 == 0)
        {
            isEndMove = true;
            CheckColliderState(BlockGame.isNextTriggerLeft);
            return;
        }
        else
        {
            isEndMove = false;
        }

        //빈칸 채우기
        for(int i = 0; i < height; i++)
        {
            //맨 아래칸부터 검사
            //현재칸이 빈칸이면
            if (grid[0,i] == null)
            {
                //터지지않은 블록이 없다면 반복문 종료
                if (notNull0.Count == 0) break;
                //현재 빈 자리에 터지지않은 블록 중 가장 아래에 있던 블록을 넣음
                grid[0, i] = grid[0, notNull0[0]];
                //빠져나간곳은 null처리
                grid[0, notNull0[0]] = null;
                //이동효과로 아래로 내려오게 함
                grid[0, i].transform.DOLocalMove(gridPositions[0, i], moveTime);
                //위에서 사용한 인덱스를 리스트에서 삭제
                notNull0.RemoveAt(0);
            }
            //현재칸이 빈칸이 아니라면
            else
            {
                //제일 앞 인덱스를 리스트에서 삭제
                notNull0.RemoveAt(0);
                //터지지않은 블록이 더 남아있지 않다면 반복문 종료
                if (notNull0.Count == 0) break;
            }
        }

        for (int i = 0; i < height; i++)
        {

            //맨 아래칸부터 검사
            //현재칸이 빈칸이면
            if (grid[1, i] == null)
            {
                //터지지않은 블록이 없다면 반복문 종료
                if (notNull1.Count == 0) break;
                //현재 빈 자리에 터지지않은 블록 중 가장 아래에 있던 블록을 넣음
                grid[1, i] = grid[1, notNull1[0]];
                //빠져나간곳은 null처리
                grid[1, notNull1[0]] = null;
                //이동효과로 아래로 내려오게 함
                grid[1, i].transform.DOLocalMove(gridPositions[1, i], moveTime);
                //위에서 사용한 인덱스를 리스트에서 삭제
                notNull1.RemoveAt(0);
            }
            //현재칸이 빈칸이 아니라면
            else
            {
                //제일 앞 인덱스를 리스트에서 삭제
                notNull1.RemoveAt(0);
                //터지지않은 블록이 더 남아있지 않다면 반복문 종료
                if (notNull1.Count == 0) break;
            }
        }

        for (int i = 0; i < height; i++)
        {
            //맨 아래칸부터 검사
            //현재칸이 빈칸이면
            if (grid[2, i] == null)
            {
                //터지지않은 블록이 없다면 반복문 종료
                if (notNull2.Count == 0) break;
                //현재 빈 자리에 터지지않은 블록 중 가장 아래에 있던 블록을 넣음
                grid[2, i] = grid[2, notNull2[0]];
                //빠져나간곳은 null처리
                grid[2, notNull2[0]] = null;
                //이동효과로 아래로 내려오게 함
                grid[2, i].transform.DOLocalMove(gridPositions[2, i], moveTime);
                //위에서 사용한 인덱스를 리스트에서 삭제
                notNull2.RemoveAt(0);

            }
            //현재칸이 빈칸이 아니라면
            else
            {
                //제일 앞 인덱스를 리스트에서 삭제
                notNull2.RemoveAt(0);
                //터지지않은 블록이 더 남아있지 않다면 반복문 종료
                if (notNull2.Count == 0) break;
                
                
            }
        }

        for (int i = 0; i < height; i++)
        {
            //맨 아래칸부터 검사
            //현재칸이 빈칸이면
            if (grid[3, i] == null)
            {
                //터지지않은 블록이 없다면 반복문 종료
                if (notNull3.Count == 0) break;
                //현재 빈 자리에 터지지않은 블록 중 가장 아래에 있던 블록을 넣음
                grid[3, i] = grid[3, notNull3[0]];
                //빠져나간곳은 null처리
                grid[3, notNull3[0]] = null;
                //이동효과로 아래로 내려오게 함
                grid[3, i].transform.DOLocalMove(gridPositions[3, i], moveTime);
                //위에서 사용한 인덱스를 리스트에서 삭제
                notNull3.RemoveAt(0);
            }
            //현재칸이 빈칸이 아니라면
            else
            {
                //제일 앞 인덱스를 리스트에서 삭제
                notNull3.RemoveAt(0);
                //터지지않은 블록이 더 남아있지 않다면 반복문 종료
                if (notNull3.Count == 0) break;
            }
        }

        for (int i = 0; i < height; i++)
        {
            //맨 아래칸부터 검사
            //현재칸이 빈칸이면
            if (grid[4, i] == null)
            {
                //터지지않은 블록이 없다면 반복문 종료
                if (notNull4.Count == 0) break;
                //현재 빈 자리에 터지지않은 블록 중 가장 아래에 있던 블록을 넣음
                grid[4, i] = grid[4, notNull4[0]];
                //빠져나간곳은 null처리
                grid[4, notNull4[0]] = null;
                //이동효과로 아래로 내려오게 함
                grid[4, i].transform.DOLocalMove(gridPositions[4, i], moveTime);
                //위에서 사용한 인덱스를 리스트에서 삭제
                notNull4.RemoveAt(0);
            }
            //현재칸이 빈칸이 아니라면
            else
            {
                //제일 앞 인덱스를 리스트에서 삭제
                notNull4.RemoveAt(0);
                //터지지않은 블록이 더 남아있지 않다면 반복문 종료
                if (notNull4.Count == 0) break;
            }
        }

        //한 줄에서 3개이상 블록을 생성할 때 동일한 블록이 생성되지않게 방지하는 로직
        //배열을 저장해둘 리스트 생성
        List<int> tempInt = new List<int>();
        //동일한 타입을 체크하는 카운트 변수
        int count = 0;
        
        //비워진 갯수만큼 반복
        for (int i = 0; i < null0; i++)
        {
            //랜덤번호 생성해서 넣음
            tempInt.Add(Random.Range(0, objectList.Count));

            //2번째부터 계산
            if(i > 0)
            {
                //생성된 타입과 이전의 타입이 일치하면 카운트 증가
                if (tempInt[i] == tempInt[i - 1]) count++;
            }

            //같은 항목이 3개 이상이면
            if (count > 1)
            {
                //초기화 후 다시 반복문 처음부터 진행
                tempInt.Clear();
                i = -1;
                count = 0;
            }
        }

        //비워진 갯수만큼 반복
        for (int i = 0; i < null0; i++)
        {
            //위에서 정해둔 인덱스로 블럭 생성
            GameObject block = SelectObject(objectList[tempInt[i]]);
            //부모 변경
            block.transform.SetParent(transform, false);
            //크기를 1로 설정
            block.transform.localScale = Vector3.one;
            //생성한 블록을 해당 줄 상단으로 이동
            block.transform.localPosition = gridPositions[0, 4] + new Vector2(0, 120 + (120 * i));
            // 빈자리까지 이동효과
            block.transform.DOLocalMove(gridPositions[0, 5 - null0 + i], moveTime);
            //그리드에 새로 생성한 블럭의 정보를 입력
            grid[0, 5 - null0 + i] = block.GetComponent<Block>();
        }

        //리스트 초기화
        tempInt.Clear();
        //카운트 초기화
        count = 0;

        //비워진 갯수만큼 반복
        for (int i = 0; i < null1; i++)
        {
            //랜덤번호 생성해서 넣음
            tempInt.Add(Random.Range(0, objectList.Count));
            //2번째부터 계산
            if (i > 0)
            {
                //생성된 타입과 이전의 타입이 일치하면 카운트 증가
                if (tempInt[i] == tempInt[i - 1]) count++;
            }

            //같은 항목이 3개 이상이면
            if (count > 1)
            {
                //초기화 후 다시 반복문 처음부터 진행
                tempInt.Clear();
                i = -1;
                count = 0;
            }
        }
        //비워진 갯수만큼 반복
        for (int i = 0; i < null1; i++)
        {
            //위에서 정해둔 인덱스로 블럭 생성
            GameObject block = SelectObject(objectList[tempInt[i]]);
            //부모 변경
            block.transform.SetParent(transform, false);
            //크기를 1로 설정
            block.transform.localScale = Vector3.one;
            //생성한 블록을 해당 줄 상단으로 이동
            block.transform.localPosition = gridPositions[1, 4] + new Vector2(0, 120 + (120 * i));
            // 빈자리까지 이동효과
            block.transform.DOLocalMove(gridPositions[1, 5 - null1 + i], moveTime);
            //그리드에 새로 생성한 블럭의 정보를 입력
            grid[1, 5 - null1 + i] = block.GetComponent<Block>();
        }

        //리스트 초기화
        tempInt.Clear();
        //카운트 초기화
        count = 0;


        //비워진 갯수만큼 반복
        for (int i = 0; i < null2; i++)
        {
            //랜덤번호 생성해서 넣음
            tempInt.Add(Random.Range(0, objectList.Count));
            //2번째부터 계산
            if (i > 0)
            {
                //생성된 타입과 이전의 타입이 일치하면 카운트 증가
                if (tempInt[i] == tempInt[i - 1]) count++;
            }
            //같은 항목이 3개 이상이면
            if (count > 1)
            {
                //초기화 후 다시 반복문 처음부터 진행
                tempInt.Clear();
                i = -1;
                count = 0;
            }
        }
        //비워진 갯수만큼 반복
        for (int i = 0; i < null2; i++)
        {
            //위에서 정해둔 인덱스로 블럭 생성
            GameObject block = SelectObject(objectList[tempInt[i]]);
            //부모 변경
            block.transform.SetParent(transform, false);
            //크기를 1로 설정
            block.transform.localScale = Vector3.one;
            //생성한 블록을 해당 줄 상단으로 이동
            block.transform.localPosition = gridPositions[2, 4] + new Vector2(0, 120 + (120 * i));
            // 빈자리까지 이동효과
            block.transform.DOLocalMove(gridPositions[2, 5 - null2 + i], moveTime);
            //그리드에 새로 생성한 블럭의 정보를 입력
            grid[2, 5 - null2 + i] = block.GetComponent<Block>();
        }

        //리스트 초기화
        tempInt.Clear();
        //카운트 초기화
        count = 0;

        //비워진 갯수만큼 반복
        for (int i = 0; i < null3; i++)
        {
            //랜덤번호 생성해서 넣음
            tempInt.Add(Random.Range(0, objectList.Count));
            //2번째부터 계산
            if (i > 0)
            {
                //생성된 타입과 이전의 타입이 일치하면 카운트 증가
                if (tempInt[i] == tempInt[i - 1]) count++;
            }
            //같은 항목이 3개 이상이면
            if (count > 1)
            {
                //초기화 후 다시 반복문 처음부터 진행
                tempInt.Clear();
                i = -1;
                count = 0;
            }
        }
        //비워진 갯수만큼 반복
        for (int i = 0; i < null3; i++)
        {
            //블록 오브젝트 생성
            GameObject block;
            if (isFirst)
            {
                //튜토리얼용 정해놓은 오브젝트들 생성
                block = SelectObject(objectList[customDrop2[i]]);
                //부모 변경
                block.transform.SetParent(transform, false);
            }
            else
            {
                //위에서 정해둔 인덱스로 블럭 생성
                block = SelectObject(objectList[tempInt[i]]);
                //부모 변경
                block.transform.SetParent(transform, false);
                //크기를 1로 설정
                block.transform.localScale = Vector3.one;
            }
            //생성한 블록을 해당 줄 상단으로 이동
            block.transform.localPosition = gridPositions[3, 4] + new Vector2(0, 120 + (120 * i));
            // 빈자리까지 이동효과
            block.transform.DOLocalMove(gridPositions[3, 5 - null3 + i], moveTime);
            //그리드에 새로 생성한 블럭의 정보를 입력
            grid[3, 5 - null3 + i] = block.GetComponent<Block>();
        }

        //리스트 초기화
        tempInt.Clear();
        //카운트 초기화
        count = 0;
        //비워진 갯수만큼 반복
        for (int i = 0; i < null4; i++)
        {
            //랜덤번호 생성해서 넣음
            tempInt.Add(Random.Range(0, objectList.Count));
            //2번째부터 계산
            if (i > 0)
            {
                //생성된 타입과 이전의 타입이 일치하면 카운트 증가
                if (tempInt[i] == tempInt[i - 1]) count++;
            }
            //같은 항목이 3개 이상이면
            if (count > 1)
            {
                //초기화 후 다시 반복문 처음부터 진행
                tempInt.Clear();
                i = -1;
                count = 0;
            }
        }
        //비워진 갯수만큼 반복
        for (int i = 0; i < null4; i++)
        {
            //위에서 정해둔 인덱스로 블럭 생성
            GameObject block = SelectObject(objectList[tempInt[i]]);
            //부모 변경
            block.transform.SetParent(transform, false);
            //크기를 1로 설정
            block.transform.localScale = Vector3.one;
            //처음이면 튜토리얼용 블록 생성
            if (isFirst) block = SelectObject(objectList[customDrop2[i]]);
            //생성한 블록을 해당 줄 상단으로 이동
            block.transform.localPosition = gridPositions[4, 4] + new Vector2(0, 120 + (120 * i));
            // 빈자리까지 이동효과
            block.transform.DOLocalMove(gridPositions[4, 5 - null4 + i], moveTime);
            //그리드에 새로 생성한 블럭의 정보를 입력
            grid[4, 5 - null4 + i] = block.GetComponent<Block>();
        }


        //그리드에 있는 블럭들에게 현재 위치 업데이트
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                grid[i, j].posX = i;
                grid[i, j].posY = j;
            }
        }
        //다시 블럭 매칭 검사
        Invoke("CheckType", moveTime + (moveTime / 4));
    }
}
