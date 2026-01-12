using UnityEngine;

/// <summary>
/// 全局游戏状态管理器 - 单例模式
/// ✅ 修复：单例初始化容错+状态切换判空+恢复AP逻辑防报错
/// ✅ 核心功能不变：探索/战斗状态切换，相机全程可动，行动点规则管控
/// </summary>
public class GameStateMgr : MonoBehaviour
{
    public static GameStateMgr Instance;

    public enum GamePlayState
    {
        ExploreState,
        BattleState
    }

    [Header("当前游戏状态")]
    public GamePlayState currentState = GamePlayState.ExploreState;

    private void Awake()
    {
        // ✅ 修复：单例双重校验，彻底解决多实例冲突+空引用
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SwitchGameState(GamePlayState targetState)
    {
        currentState = targetState;
        Debug.Log("游戏状态切换为：" + targetState);
        RecoverAllCharacterAP();
    }

    private void RecoverAllCharacterAP()
    {
        // ✅ 修复：找不到角色时不报错
        BaseCharacterAttr[] allChars = FindObjectsOfType<BaseCharacterAttr>();
        if (allChars == null || allChars.Length == 0) return;
        foreach (var charAttr in allChars)
        {
            if (charAttr != null) charAttr.RecoverFullAP();
        }
    }

    public bool IsExploreState()
    {
        return Instance != null && currentState == GamePlayState.ExploreState;
    }

    public bool IsBattleState()
    {
        return Instance != null && currentState == GamePlayState.BattleState;
    }
}