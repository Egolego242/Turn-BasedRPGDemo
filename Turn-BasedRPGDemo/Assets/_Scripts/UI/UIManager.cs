using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI组引用")]
    public GameObject baseExploreUI;
    public GameObject overlayBattleUI;

    [Header("子模块引用")]
    public TurnOrderBar turnOrderBar;
    public ActionPointBar actionPointBar;
    public BattleStartTip battleStartTip;
    public SkillTooltip skillTooltip;
    public RangeVisualizer rangeVisualizer;

    // 新增：当前回合提示文本（拖入场景中的TextMeshPro组件）
    [Header("回合提示")]
    public TextMeshProUGUI currentTurnTipText;

    private void Awake()
    {
        // 单例初始化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // 订阅所有战斗事件
        GameStateMgr.OnGameStateChanged += OnGameStateChanged;
        TurnBattleManager.OnBattleStart += OnBattleStart;
        TurnBattleManager.OnTurnChanged += OnTurnChanged;
        TurnBattleManager.OnBattleEnd += OnBattleEnd;
        TurnBattleManager.OnActionPointChanged += OnActionPointChanged;
    }

    private void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        GameStateMgr.OnGameStateChanged -= OnGameStateChanged;
        TurnBattleManager.OnBattleStart -= OnBattleStart;
        TurnBattleManager.OnTurnChanged -= OnTurnChanged;
        TurnBattleManager.OnBattleEnd -= OnBattleEnd;
        TurnBattleManager.OnActionPointChanged -= OnActionPointChanged;
    }

    // 游戏状态切换：探索/战斗UI显隐
    private void OnGameStateChanged(GameStateMgr.GamePlayState state)
    {
        // baseExploreUI.SetActive(true); // 如果初始是隐藏的，这里可以设为true，否则可以省略

        // 只控制叠加战斗UI的显隐：探索时隐藏，战斗时显示
        overlayBattleUI.SetActive(state == GameStateMgr.GamePlayState.BattleState);
    }

    // 战斗开始：初始化回合条、显示入场提示
    private void OnBattleStart(List<BaseCharacterAttr> combatants)
    {
        turnOrderBar.InitTurnOrder(combatants);
        battleStartTip.ShowBattleStartTip();
    }

    // 回合切换：更新回合条高亮、刷新行动点
    private void OnTurnChanged(BaseCharacterAttr currentActor)
    {
        turnOrderBar.UpdateTurnOrder(currentActor);
        actionPointBar.UpdateActionPoint(currentActor);
        // 新增：更新当前回合提示文本
        UpdateCurrentTurnTip(currentActor);
    }

    // 新增：封装回合提示更新逻辑
    private void UpdateCurrentTurnTip(BaseCharacterAttr currentActor)
    {
        if (currentTurnTipText == null)
        {
            Debug.LogWarning("UIManager未赋值currentTurnTipText！");
            return;
        }

        // 判断当前角色是玩家还是敌人，设置对应提示文本
        if (currentActor is PlayerAttr) // 玩家角色（你的BaseCharacterAttr子类）
        {
            currentTurnTipText.text = "你的回合";
            // 可选：设置文本颜色（比如玩家回合用绿色）
            currentTurnTipText.color = Color.green;
        }
        else if (currentActor is EnemyAttr) // 敌人角色
        {
            currentTurnTipText.text = "敌人的回合";
            // 可选：敌人回合用红色
            currentTurnTipText.color = Color.red;
        }
        else // 其他角色（兜底）
        {
            currentTurnTipText.text = $"{currentActor.name}的回合";
        }
    }

    // 行动点变化：刷新行动点UI
    private void OnActionPointChanged()
    {
        if (TurnBattleManager.Instance != null)
        {
            BaseCharacterAttr currentActor = FindObjectsOfType<BaseCharacterAttr>()
                .FirstOrDefault(c => c.isMyTurn);
            if (currentActor != null) actionPointBar.UpdateActionPoint(currentActor);
        }
    }

    // 战斗结束：隐藏战斗UI
    private void OnBattleEnd(bool playerWin)
    {
        // 可扩展：显示胜利/失败面板
        Debug.Log(playerWin ? "战斗胜利！" : "战斗失败！");
    }
}