using UnityEngine;

public class CalibrationPoint : MonoBehaviour
{
    // 스크립트 참조
    private Calibration calibration;
    public PointsController pointsController;
    public int pointIndex; // 이 캘리브레이션 포인트의 인덱스

    [Tooltip("이 오브젝트가 충돌을 감지할 대상 콜라이더를 여기에 할당하세요.")]
    public Collider targetCollider; // 검사할 대상 콜라이더 (예: ColliderArea 또는 Annotation)

    private Collider myCollider; // 이 오브젝트의 콜라이더
    private bool isOverlapping = false; // 현재 겹치고 있는지 상태를 저장하는 변수

    private void Start()
    {
        // 캘리브레이션 스크립트 찾아서 할당
        calibration = transform.parent.GetComponent<Calibration>();
        // 자신의 콜라이더를 캐싱
        myCollider = GetComponent<Collider>();

        if (myCollider == null)
        {
            Debug.LogError("CalibrationPoint 스크립트가 있는 오브젝트에 Collider가 없습니다!", gameObject);
        }
        // Y-2 후 카메라/추론 시작이 ClickBtn 시점으로 미뤄져 첫 진입 시 PointsController.points가 미할당 상태일 수 있음.
        // GetPoint가 null 반환하면 targetCollider 미할당 → Update의 lazy load(TryAssignTargetCollider)가 매 프레임 retry로 보강.
        TryAssignTargetCollider();
    }

    /// <summary>
    /// PointsController.points 할당 후 GetPoint로 lookup. Y-2 race condition 회피용 lazy load.
    /// pointIndex==99(특수)나 이미 할당된 경우는 스킵 — 매 프레임 호출되어도 무해.
    /// </summary>
    private void TryAssignTargetCollider()
    {
        if (pointIndex == 99 || targetCollider != null || pointsController == null) return;
        var pt = pointsController.GetPoint(pointIndex);
        if (pt != null) targetCollider = pt.gameObject.GetComponent<Collider>();
    }

    private void OnEnable()
    {
        // 스크립트가 시작될 때도 초기 상태를 한 번 체크합니다.
        ForceCheckOverlapState();
    }

    /// <summary>
    /// 현재 겹침 상태를 강제로 확인하고, 그에 맞게 상태를 즉시 업데이트합니다.
    /// 게임을 다시 시작하거나 특정 단계가 시작될 때 외부에서 호출해주세요.
    /// </summary>
    public void ForceCheckOverlapState()
    {
        if (targetCollider == null || myCollider == null || !targetCollider.gameObject.activeInHierarchy)
        {
            // 목표가 없으면 무조건 겹치지 않은 상태로 처리
            if(isOverlapping)
            {
                isOverlapping = false;
                calibration.SetCalibrationPointState(gameObject.name, false);
            }
            return;
        }

        // 현재 겹침 상태를 정밀하게 계산
        isOverlapping = Physics.ComputePenetration(
            myCollider, myCollider.transform.position, myCollider.transform.rotation,
            targetCollider, targetCollider.transform.position, targetCollider.transform.rotation,
            out Vector3 direction, out float distance);
        
        // 계산된 상태를 즉시 calibration에 전달
        calibration.SetCalibrationPointState(gameObject.name, isOverlapping);
        
        if (isOverlapping)
        {
            Debug.Log(gameObject.name + "이(가) 상태 체크 시점에 이미 " + targetCollider.name + " 영역 안에 있습니다.");
        }
    }

    private void Update()
    {
        // points lazy load — Y-2 race condition으로 Start 시점에 할당 못했어도 매 프레임 retry.
        // 이미 할당됐거나 pointIndex==99이면 즉시 return하므로 비용 0.
        TryAssignTargetCollider();

        // targetCollider가 할당되지 않았거나 비활성화 상태이면 검사하지 않음
        if (targetCollider == null || !targetCollider.gameObject.activeInHierarchy)
        {
            // 만약 이전에 겹쳐있던 상태였다면, '나감' 상태로 처리
            if (isOverlapping)
            {
                isOverlapping = false;
                calibration.SetCalibrationPointState(gameObject.name, false);
            }
            return;
        }

        bool currentlyOverlapping = Physics.ComputePenetration(
            myCollider, myCollider.transform.position, myCollider.transform.rotation,
            targetCollider, targetCollider.transform.position, targetCollider.transform.rotation,
            out Vector3 direction, out float distance);

        // 1. 진입하는 순간: 이전에는 안 겹쳤는데, 지금은 겹칠 때
        if (currentlyOverlapping)
        {
            isOverlapping = true;
            calibration.SetCalibrationPointState(gameObject.name, true);
            Debug.Log(gameObject.name + "이(가) " + targetCollider.name + " 영역에 진입했습니다.");
        }
        // 2. 빠져나가는 순간: 이전에는 겹쳤는데, 지금은 안 겹칠 때
        else if (!currentlyOverlapping)
        {
            isOverlapping = false;
            calibration.SetCalibrationPointState(gameObject.name, false);
            Debug.Log(gameObject.name + "이(가) " + targetCollider.name + " 영역에서 나갔습니다.");
        }
    }
}