using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// ADR-0011 §2.2 Timer race + Tutorial reset 회귀 차단 테스트.
///
/// PoC 단계 — 가장 단순한 검증부터 시작:
///   1. Main 씬 로드 + GetFlutterMessage 컴포넌트 발견 (환경 sanity check)
///   2. Sim/Continue 메시지 → BlockGame._restoredFromIncomplete=true 검증 (Scenario 2)
///
/// 실행:
///   pwsh -File scripts/run-playmode-tests.ps1
///
/// 한계 (PoC):
///   - BaseRunner.Play() PC 웹캠 의존 — _baseRunner null이면 SKIP되도록 PointsController 코드가
///     이미 가드. batchmode -nographics에서도 SKIP 가능.
///   - 씬 흐름(가이드/캘리브레이션 진행)을 SKIP하고 BlockGame.gameObject.SetActive(true)로 직접 활성.
///     이는 실제 사용자 흐름과 약간 다르지만 핵심 분기(Initialize의 isFirst/isNormalEnd 검사) 검증엔 충분.
/// </summary>
public class TutorialReentryTests
{
    private GetFlutterMessage _gfm;
    private MethodInfo _receiveMethod;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        // PlayerPrefs 클린 — 이전 테스트 잔존 상태 격리.
        PlayerPrefs.DeleteAll();

        // Main 씬 로드.
        SceneManager.LoadScene("Main");
        yield return null;
        yield return new WaitForSeconds(1f);  // 씬 초기화 + Awake/OnEnable 완료 대기.

        _gfm = Object.FindObjectOfType<GetFlutterMessage>();
        Assert.IsNotNull(_gfm, "GetFlutterMessage GameObject not found in Main scene. 씬에 Solution GameObject가 있는지 확인.");

        _receiveMethod = typeof(GetFlutterMessage).GetMethod("RecieveMessage",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(_receiveMethod, "RecieveMessage(string) public method not found via reflection.");
    }

    /// <summary>Helper: Flutter 메시지 시뮬레이션.</summary>
    private void SimulateMessage(string msg)
    {
        _receiveMethod.Invoke(_gfm, new object[] { msg });
    }

    /// <summary>
    /// PoC #1 — 환경 sanity check.
    /// 씬 로드 + 핵심 GameObject(Solution.GetFlutterMessage) 발견 + Sim/Start 메시지 송신 후 BlockGame 컴포넌트 존재 확인.
    /// </summary>
    [UnityTest]
    public IEnumerator PoC1_SceneLoadsAndCoreComponentsExist()
    {
        // Setup에서 이미 _gfm 발견 OK.
        SimulateMessage("start,time:60.0,level:1");
        yield return new WaitForSeconds(0.5f);

        var blockGame = Object.FindObjectOfType<BlockGame>(includeInactive: true);
        Assert.IsNotNull(blockGame, "BlockGame component not found in scene (활성/비활성 포함).");

        var pairGame = Object.FindObjectOfType<PairGame>(includeInactive: true);
        Assert.IsNotNull(pairGame, "PairGame component not found in scene (활성/비활성 포함).");

        Debug.Log("[PoC1] PASS — 환경 sanity check 통과.");
    }

    /// <summary>
    /// Scenario 2 — Tutorial Mid-Dispose + Continue.
    /// 비정상종료 후 재진입 시 BlockGame._restoredFromIncomplete=true로 set되어 Tutorial이 다시 시작되는 분기 검증.
    /// </summary>
    [UnityTest]
    public IEnumerator Scenario2_BlockGame_ContinueAfterIncomplete_SetsRestoredFlag()
    {
        // 1. Sim start — Solution.GetFlutterMessage 측 분기 진입.
        SimulateMessage("start,time:60.0,level:1");
        yield return new WaitForSeconds(0.5f);

        // 2. BlockGame 컴포넌트 발견.
        var blockGame = Object.FindObjectOfType<BlockGame>(includeInactive: true);
        Assert.IsNotNull(blockGame, "BlockGame not found.");

        // 3. _restoredFromIncomplete private field accessor (reflection).
        var flagField = typeof(BlockGame).GetField("_restoredFromIncomplete",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(flagField,
            "_restoredFromIncomplete field not found — ADR §2.2 Timer race fix가 BlockGame.cs에 반영됐는지 확인.");

        // 4. 비정상종료 상태 시뮬: PlayerPrefs + StaticData.
        //    실제 사용자 흐름에서는 CheckLastPlayed가 PlayerPref→StaticData 동기화하지만 테스트는 직접 set.
        PlayerPrefs.SetInt("NormalEnd", 0);          // 0 = 비정상종료
        PlayerPrefs.SetString("LastPlayed", "Block"); // 직전 게임 = Block
        StaticData.isNormalEnd = false;               // BlockGame.Initalize의 line 130 분기 활성화

        // 5. BlockGame 직접 활성화 — OnEnable fire로 isFirst=true reset + _restoredFromIncomplete=false reset.
        blockGame.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);  // OnEnable 완료 대기

        // 6. OnEnable 직후: _restoredFromIncomplete은 false여야 (reset 동작 확인).
        Assert.IsFalse((bool)flagField.GetValue(blockGame),
            "OnEnable에서 _restoredFromIncomplete=false reset 안 됨.");

        // 7. Initialize() trigger — SetGameObjectsFade(1f) → DOTween OnComplete → Initialize 호출.
        //    SetGameObjectsFade는 public. DOTween 0.75초 + buffer 대기.
        blockGame.SetGameObjectsFade(1f);
        yield return new WaitForSeconds(1.5f);

        // 8. Initialize 후 검증: 비정상종료 분기(line 130)가 진입했으면 _restoredFromIncomplete=true.
        bool flagValue = (bool)flagField.GetValue(blockGame);
        Assert.IsTrue(flagValue,
            $"[Scenario 2 FAIL] _restoredFromIncomplete=true 기대. 실제={flagValue}. " +
            "BlockGame.Initalize의 line 130 비정상종료 분기가 진입하지 않았거나 우리 fix가 반영 안 됨.");

        Debug.Log("[Scenario2] PASS — _restoredFromIncomplete=true 확인.");
    }
}
