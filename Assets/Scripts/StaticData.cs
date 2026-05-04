using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//모드의 열거형 타입
public enum Mode
{
    None,
    Calibrate,
    Game,
    Rest,
    Tutorial
}

//짝맞추기 열거형 타입
public enum PairType
{
    Giraffe,
    Mouse,
    Pig,
    Tiger
}

//블럭의 열거형 타입
public enum BlockType
{
    Giraffe,
    Frog,
    Mouse,
    Pig,
    Tiger
}

/// <summary>
/// 모든 스크립트에서 참조하는 데이터들을 모아놓은 스크립트
/// </summary>
public class StaticData : MonoBehaviour
{
    //현재 모드
    public static Mode nowMode;
    //카메라 해상도
    public const string camaraResolution = "1920x1080";
    //상세설명 옵션값
    public static bool isExtraDisc = true;
    //정상종료/비정상종료 값
    public static bool isNormalEnd = true;
    //어떤 게임을 선택했는지
    public static bool isPairGame = false;
    //빠른 동작 횟수
    public static int fastTriggerCount = 0;
    //빠른 동작 페널티 강제종료 횟수
    public static int fastQuitCount = 0;
    //각도단계
    public static int level = 0;
    //목표 도달 시간
    public static float destTime = 5f;

    //세트별 점수
    public struct Scores
    {
        public int section1;
        public int section2;
        public int section3;
    }

    //세트별 운동 횟수
    public struct Datas
    {
        public int count1;
        public int count2;
        public int count3;        
    }
    //블록밀기 점수 객체
    public static Scores blockGameScores = new Scores();
    //짝맞추기 점수 객체 
    public static Scores pairGameScores = new Scores();
    //블럭밀기 운동 횟수 객체
    public static Datas blockGameDatas = new Datas();
    //짝맞추기 운동 횟수 객체
    public static Datas pairGameDatas = new Datas();
}

//포인트가 가지고있는 데이터
public class Point
{
    public int pointIndex;
    public float X;
    public float Y;
}