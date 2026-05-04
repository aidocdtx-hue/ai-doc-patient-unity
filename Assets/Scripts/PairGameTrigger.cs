using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 짝 맞추기 게임에서 사용하는 콜라이더에 상호작용을 체크하는 부분
/// </summary>
public class PairGameTrigger : MonoBehaviour
{
    //왼 쪽과 오른쪽 그리드 스크립트를 맞는 방향으로 인스펙터에서 할당
    public PairGrid gridController;
    //게임 스크립트 참조
    public PairGame pairGame;
    //몇 번째 라인에 상호작용했는지 판별하는 변수. 인스펙터에서 값 부여
    public int lineNum;

    //콜라이더 영역에 터치 영역이 닿이면
    private void OnTriggerEnter(Collider other)
    {
        //현재 튜토리얼이 진행중이면
        if(StaticData.nowMode == Mode.Tutorial)
        {
            //튜토리얼에 상호작용 전달
            pairGame.SetTutorialTriggerState(false);
        }
        // 게임 진행중이면
        else
        {
            //각 영역에 상호작용한 라인넘버를 전달
            gridController.Triggered(lineNum);
        }
        
    }
}
