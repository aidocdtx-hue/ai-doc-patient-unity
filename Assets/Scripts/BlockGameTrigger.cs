using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 블럭밀기 게임에서 콜라이더에 상호작용을 하는 스크립트
/// </summary>
public class BlockGameTrigger : MonoBehaviour
{
    //왼 쪽인지 오른쪽인지 인스펙터에서 결정
    public bool isLeft = false;
    //몇 번째 줄인지 인스펙터에서 결정
    public int lineNum;
    //스크립트 참조
    public GridController gridController;
    public BlockGame blockGame;
    
    
    
    //터치영역이 콜라이더 영역에 들어오면
    private void OnTriggerEnter(Collider other)
    {
        //현재 모드가 튜토리얼 이라면
        if (StaticData.nowMode == Mode.Tutorial)
        {
            //튜토리얼 상호작용
            blockGame.SetTutorialTriggerState(false);
        }
        //현재 모드가 게임모드면
        else
        {
            //왼쪽 콜라이더라면
            if (isLeft)
            {
                //왼 쪽 줄 밀기
                gridController.LeftShift(lineNum);
            }
            //오른쪽 콜라이더라면
            else
            {
                //오른쪽 줄 밀기
                gridController.RightShift(lineNum);
            }
        }
        
        
    }
}