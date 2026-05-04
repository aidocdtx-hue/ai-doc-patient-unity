using Mediapipe;
using Mediapipe.Unity;
using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 포인트들의 위치를 바이너리 파일로 저장하는 스크립트
/// </summary>
public class SaveLandmarkDatas : MonoBehaviour
{
    //스크립트 참조
    private PoseLandmarkerResultAnnotationController poseLandmarkerResultAnnotationController;
    public Timer timer;
    public Result result;

    [Header("관절 포인트 저장 주기")]
    public float cycleTime = 0.1f;


    //데이터 저장에 필요한 변수
    //남은 시간
    float remainTime = 0;
    //저장경로
    string path;
    //포인트 정보들을 담을 리스트 생성
    Skeleton tempSkeleton = new Skeleton();
    List<Point> landmarkList = new List<Point>();
    static List<SkeletonSample> skeletonList = new List<SkeletonSample>();
    //포인트정보 리스트를 담을 리스트 생성
    public List<List<Point>> landmarkData = new List<List<Point>>();

    bool isFirst = true;

    private void Start()
    {
        //스크립트를 찾아서 할당
        poseLandmarkerResultAnnotationController = FindAnyObjectByType<PoseLandmarkerResultAnnotationController>();
    }

    /// <summary>
    /// 타이머 할당을 위한 부분
    /// </summary>
    /// <param name="timer"></param>
    public void SetTimer(Timer timer)
    {
        //전달받은 타이머를 이 스크립트의 타이머에 할당
        this.timer = timer;
    }

    bool isRunningCoroutine = false;
    /// <summary>
    /// 저장을 시작하는 부분
    /// </summary>
    /// <param name="points">저장 할 포인트들</param>
    public void StartSave(PointAnnotation[] points)
    {
        Debug.Log("현재 세트 : " + timer.GetEndCount().ToString());
        //오늘 날짜 + 세트
        string date = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00") + timer.GetEndCount().ToString();
        //경로를 persistentDataPath로 설정, 파일 이름은 날짜세트.bin파일로 설정
        path = Path.Combine(Application.persistentDataPath, date + ".bin");
        //비정상 종료시에 진행중이던 상태의 바이너리 데이터를 읽어서 landmarkData에 저장, 새로 쌓이는 부분은 이후에 add처리, 처음 한 번만 분기를 타도록 변경
        if (StaticData.isNormalEnd == false && isFirst)
        {
            ReadBin();
            ReadJSON();
        }
        //첫 실행 이후 false처리
        isFirst = false;
        //타이머에서 남은시간을 가져와서 할당
        remainTime = timer.GetRemainTime();
        //현재 모드가 게임이면 랜드마크 저장 시작
        if (StaticData.nowMode == Mode.Game && isRunningCoroutine == false)
        {
            Debug.Log("저장 시작");
            StartCoroutine(SaveLandmarks(points));
        }
    }

    /// <summary>
    /// 포인트들을 저장하는 부분
    /// </summary>
    /// <param name="points">포인트들</param>
    /// <returns></returns>
    IEnumerator SaveLandmarks(PointAnnotation[] points)
    {
        isRunningCoroutine = true;
        //남은시간이 0이 될 때 까지 반복
        while (remainTime > 0)
        {
            //저장 주기만큼 대기
            yield return new WaitForSeconds(cycleTime);
            //포인트의 갯수만큼 반복
            for (int i = 0; i < points.Length; i++) 
            {
                //포인트가 꺼져있는 상태면 저장하지 않음
                if (!points[i].gameObject.activeInHierarchy) continue;
                //포인트의 값 저장
                SaveLandmark(i); 
            }
            //33개 포인드들의 값 저장
            EndSaveLandmarks(600 - remainTime); 
        }
        //남은시간이 0이면 바이너리로 데이터 저장
        SaveBinary(landmarkData, path); 
        //저장한 리스트 비우기
        landmarkData.Clear();
        isRunningCoroutine = false;
    }

    //저장했는지 여부를 판단하는 변수
    bool isSave = false;

    /// <summary>
    /// 중간저장을 하는 부분
    /// </summary>
    public void TempSaveDatas(string reason)
    {
        //저장하지 않았으면
        if (isSave == false)
        {
            //저장한 상태로 변경
            isSave = true;
            //데이터 저장
            SaveBinary(landmarkData, path); 
            //결과화면에 운동시간을 계산해서 저장명령
            result.SaveData(reason, (600f - timer.GetRemainTime()) + (timer.GetEndCount() * 600)); //600-남은시간 + (현재세트 * 600)
        }
        //이미 저장했다면 동작하지 않음
        else return;
    }

    public List<SkeletonSample> SaveJsonData()
    {
        return skeletonList;
    }

    /// <summary>
    /// 저장된 모든 데이터들을 바이너리 파일로 저장
    /// </summary>
    /// <param name="data">포인트 정보리스트의 리스트</param>
    /// <param name="path">저장경로</param>
    void SaveBinary(List<List<Point>> data, string path) //바이너리 파일로 저장하는 방식
    {
        //파일스트림과 바이너리라이터로 데이터 저장
        using (var fs = new FileStream(path, FileMode.Create))
        using (var writer = new BinaryWriter(fs))
        {
            // 외부 리스트 수 (프레임 수)
            writer.Write(data.Count); 

            foreach (var frame in data)
            {
                // 33개 포인트
                writer.Write(frame.Count); 
                foreach (var point in frame)
                {
                    //인덱스, X, Y값
                    writer.Write(point.pointIndex);
                    writer.Write(point.X);
                    writer.Write(point.Y);
                }
            }
        }
        //세이브 상태를 저장하지 않은 상태로 변경
        isSave = false;
    }

    /// <summary>
    /// 각각 포인트들의 값을 리스트에 저장하는 부분
    /// </summary>
    /// <param name="i">포인트의 인덱스</param>
    public void SaveLandmark(int i)
    {
        //포인트 객체 생성
        Point temp = new Point();
        
        //인덱스를 전달받은 매개변수로 설정
        temp.pointIndex = i;
        
        //X,Y값을 정규화된 값으로 받아서 설정
        temp.X = poseLandmarkerResultAnnotationController.GetLandmark(i).x;
        temp.X = Mathf.Round(temp.X * 10000f) / 10000f;
        temp.Y = poseLandmarkerResultAnnotationController.GetLandmark(i).y;
        temp.Y = Mathf.Round(temp.Y * 10000f) / 10000f;

        switch (i)
        {
            case 11:
                tempSkeleton.right_shoulder = new float[2];
                tempSkeleton.right_shoulder[0] = temp.X;
                tempSkeleton.right_shoulder[1] = temp.Y;
                break;
            case 12:
                tempSkeleton.left_shoulder = new float[2];
                tempSkeleton.left_shoulder[0] = temp.X;
                tempSkeleton.left_shoulder[1] = temp.Y;
                break;
            case 13:
                tempSkeleton.right_elbow = new float[2];
                tempSkeleton.right_elbow[0] = temp.X;
                tempSkeleton.right_elbow[1] = temp.Y;
                break;
            case 14:
                tempSkeleton.left_elbow = new float[2];
                tempSkeleton.left_elbow[0] = temp.X;
                tempSkeleton.left_elbow[1] = temp.Y;
                break;
            case 15:
                tempSkeleton.right_wrist = new float[2];
                tempSkeleton.right_wrist[0] = temp.X;
                tempSkeleton.right_wrist[1] = temp.Y;
                break;
            case 16:
                tempSkeleton.left_wrist = new float[2];
                tempSkeleton.left_wrist[0] = temp.X;
                tempSkeleton.left_wrist[1] = temp.Y;
                break;
            case 23:
                tempSkeleton.right_hip = new float[2];
                tempSkeleton.right_hip[0] = temp.X;
                tempSkeleton.right_hip[1] = temp.Y;
                break;
            case 24:
                tempSkeleton.left_hip = new float[2];
                tempSkeleton.left_hip[0] = temp.X;
                tempSkeleton.left_hip[1] = temp.Y;
                break;
        }
        //포인트가 값을 전달할 수 없는 상태면 -999를 반환
        if (temp.X != -999f)
        {
            //데이터 저장
            landmarkList.Add(temp);
            
        }
        else
        {
            //저장하지 않음
        }

    }

    /// <summary>
    /// 정규화된 데이터 반환을 위한 부분
    /// </summary>
    /// <param name="index">포인트의 인덱스</param>
    /// <returns></returns>
    public Vector3 GetPointData(int index)
    {
        //미디어파이프에서 정규화된 값을 받아옴
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark temp = poseLandmarkerResultAnnotationController.GetLandmark(index);
        //값을 저장
        float x = temp.x;
        float y = temp.y;
        float z = temp.z;
        //vector3로 반환
        return new Vector3(x,y,z);
    }

    /// <summary>
    /// 모든 포인트들의 값을 받으면 종합하여 리스트에 저장하는 부분
    /// </summary>
    public void EndSaveLandmarks(float time)
    {
        //저장한 랜드마크 리스트를 랜드마크 데이터에 추가
        landmarkData.Add(new List<Point>(landmarkList));
        //넣은 리스트 초기화
        landmarkList.Clear();

        SkeletonSample skeletonSample = new SkeletonSample();
        time = time + 600 * timer.GetEndCount();
        string timeString = ((int)(time / 60)).ToString("00") + ":" + (Mathf.Round((time % 60) * 10) / 10).ToString("00.0");
        skeletonSample.time = timeString;
        skeletonSample.skeleton = tempSkeleton;
        skeletonList.Add(skeletonSample);
        
    }

    public void Update()
    {
        //현재 모드가 게임모드면 남은 시간 갱신
        if(StaticData.nowMode == Mode.Game)
        remainTime = timer.GetRemainTime();
    }

    /// <summary>
    /// 바이너리파일을 읽는 부분
    /// </summary>
    public void ReadBin()
    {
        //결과를 담을 포인트리스트의 리스트 생성
        var result = new List<List<Point>>();

        //예외처리
        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.Log("파일 경로가 null이거나 비어 있습니다.");
            return;
        }
        else if (!File.Exists(path))
        {
            Debug.Log("파일이 존재하지 않습니다: " + path);
            return;
        }

        //파일스트림과 바이너리 리더로 파일 읽어오기
        using (var fs = new FileStream(path, FileMode.Open))
        using (var reader = new BinaryReader(fs))
        {
            //총 저장 갯수
            int frameCount = reader.ReadInt32();

            for (int i = 0; i < frameCount; i++)
            {
                //포인트의 갯수
                int pointCount = reader.ReadInt32();
                //포인트 리스트 생성
                var pointList = new List<Point>();

                for (int j = 0; j < pointCount; j++)
                {
                    //각 포인터의 값을 읽어와서 넣어줌
                    int index = reader.ReadInt32();
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    Point temp = new Point();
                    temp.pointIndex = index;
                    temp.X = x;
                    temp.Y = y;
                    //포인트리스트에 추가
                    pointList.Add(temp);
                }
                //리스트를 결과에 추가
                result.Add(pointList);
            }
        }
        //현재 데이터를 결과로 대체
        landmarkData = result;
    }

    public void ReadJSON()
    {
        string today = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString("00") + "-" + DateTime.Now.Day.ToString("00") + "-data";
        string jsonpath = Path.Combine(Application.persistentDataPath, today + ".json");
        //예외처리
        if (string.IsNullOrWhiteSpace(jsonpath))
        {
            Debug.Log("파일 경로가 null이거나 비어 있습니다.");
            return;
        }
        else if (!File.Exists(jsonpath))
        {
            Debug.Log("파일이 존재하지 않습니다: " + jsonpath);
            return;
        }

        //var loadedJson = Resources.Load<TextAsset>("JSON/" + jsonpath);
        Debug.Log("1");
        string json = File.ReadAllText(jsonpath);
        Debug.Log("2");
        RawData tempRaw = new RawData();
        tempRaw = JsonUtility.FromJson<RawData>(json);
        Debug.Log("데이터 로드 " + tempRaw.raw[0].time);
        //skeletonList.Add(tempRaw.raw[0]);
        skeletonList = tempRaw.raw;
    }
}

[System.Serializable]
public class Skeleton
{
    public float[] right_shoulder;
    public float[] left_shoulder;
    public float[] right_elbow;
    public float[] left_elbow;
    public float[] right_wrist;
    public float[] left_wrist;
    public float[] right_hip;
    public float[] left_hip;
}

[System.Serializable]
public class SkeletonSample
{
    public string time;
    public Skeleton skeleton;
}

[System.Serializable]
public class RawData
{
    public List<SkeletonSample> raw;
}
