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

//운동 진행 단계 — Flutter와 동기화하기 위한 전역 상태.
//Selection=시작 화면(게임 선택), Guide=가이드, Calibration=스켈레톤 인식, Playing=게임 진행 중.
//단계별로 background/resume 동작이 다름:
//- Selection/Guide/Calibration: 운동 시작 전 → background 시 Flutter는 새로 시작 처리 (cancelled_pre_game)
//- Playing: 운동 진행 중 → background 시 Flutter는 confirm 다이얼로그 → 이어하기 시 가이드부터 재진입
public enum ExerciseStage
{
    Selection,
    Guide,
    Calibration,
    Playing
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

    //운동 진행 단계 (Selection/Guide/Calibration/Playing).
    //setter에서 매 set마다 Flutter로 stage 메시지 송신 → unity_exercise_screen이 _currentStage 갱신.
    //이전엔 `if (_stage != value)` 가드가 있었으나 _stage 초기값(Selection)과 첫 set(StartScreen.Awake의 Selection)이
    //동일해 setter 진입 안 함 → Flutter _currentStage가 영원히 null → resumed 분기의 `_currentStage == 'playing'`
    //가드 못 통과 → confirm 다이얼로그 안 뜨던 결함 차단. 가드 제거 — 동일 값이어도 송신 (idempotent 송신).
    private static ExerciseStage _stage = ExerciseStage.Selection;
    public static ExerciseStage stage
    {
        get => _stage;
        set
        {
            _stage = value;
            SendToFlutter.Send("{\"command\":\"stage\", \"stage\":\"" + value.ToString().ToLower() + "\"}");
        }
    }

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