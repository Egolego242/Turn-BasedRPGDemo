using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 回合战斗核心管理器（原TurnBattleManager重构版）
/// 负责回合流程、行动点管理、全局回合轮换、战斗胜负判定
/// </summary>
public class TurnBattleManager : MonoBehaviour
{
    public static TurnBattleManager Instance { get; private set; }

    #region 战斗配置
    [Header("Battle Settings")]
    [Tooltip("全局最大行动点上限")]
    public int globalMaxAP = 6;
    [Tooltip("每轮恢复的行动点数量")]
    public int roundRecoverAP = 4;
    #endregion

    #region 战斗状态
    // 所有参战角色（玩家+敌人）
    private List<BaseCharacterAttr> allCombatants = new List<BaseCharacterAttr>();
    // 按先手值排序后的参战角色
    private List<BaseCharacterAttr> sortedCombatants = new List<BaseCharacterAttr>();
    // 当前行动角色索引
    private int currentActorIndex = -1;
    // 战斗是否激活
    private bool isBattleActive = false;
    // 是否处于结算阶段（阻止重复触发）
    private bool isSettling = false;
    // 当前结算数据
    private BattleSettlementData currentSettlementData;
    #endregion

    #region 战斗事件
    /// <summary>战斗开始（参数：排序后的参战角色列表）</summary>
    public static event Action<List<BaseCharacterAttr>> OnBattleStart;
    /// <summary>回合切换（参数：当前行动角色）</summary>
    public static event Action<BaseCharacterAttr> OnTurnChanged;
    /// <summary>战斗结束（参数：玩家是否胜利）</summary>
    public static event Action<bool> OnBattleEnd;
    /// <summary>行动点变更（刷新UI用）</summary>
    public static event Action OnActionPointChanged;
    /// <summary>单个角色回合结束</summary>
    public event Action OnTurnEnd;
    /// <summary>战斗结算触发（参数：结算数据，含奖励/胜负）</summary>
    public static event Action<BattleSettlementData> OnBattleSettlement;
    #endregion

    private BaseCharacterAttr currentActor; // 当前正在行动的角色

    // ========== 新增：专门用于触发行动点变化事件的公共静态方法 ==========
    public static void TriggerActionPointChanged()
    {
        // 只有在定义事件的类内部，才能直接调用事件
        OnActionPointChanged?.Invoke();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// 触发战斗（玩家进入警戒范围/敌人主动攻击）
    /// </summary>
    public void TriggerBattle(BaseCharacterAttr player, List<BaseCharacterAttr> enemies)
    {
        if (isBattleActive) return;
        isBattleActive = true;
        isSettling = false;

        // 战前自动存档（防崩溃/异常导致进度丢失）
        SaveManager.Instance?.QuickSaveBeforeBattle(player.gameObject);

        // 收集战斗角色
        allCombatants.Clear();
        allCombatants.Add(player);
        allCombatants.AddRange(enemies);

        // 初始化所有参战角色战斗状态
        foreach (var combatant in allCombatants)
        {
            combatant.isInBattle = true;
            combatant.isMyTurn = false;
            combatant.hasActInRound = false;

            // 初始化行动点
            combatant.SetAttrValue(AttributeType.MaxAP, globalMaxAP);
            combatant.SetAttrValue(AttributeType.CurrentAP, roundRecoverAP);

            // 战斗动画切换
            Animator anim = combatant.GetComponent<Animator>();
            if (anim != null) anim.SetBool("BattleMode", true);

            // 敌人处理：停止巡逻+启用行为树战斗分支+赋值攻击目标
            if (combatant is EnemyAttr enemy)
            {
                enemy.StopPatrol();
                enemy.attackTarget = player.transform;
                enemy.behaviorTree?.EnableBehavior(); // 启用行为树
            }
        }

        // 先攻值排序
        sortedCombatants = allCombatants.OrderByDescending(c => c.GetInitiative()).ToList();
        // 打印排序日志（关键：验证先攻值）
        Debug.Log("战斗开始！先攻顺序：" + string.Join(" > ",
            sortedCombatants.Select(c => $"{c.name} (先攻值：{c.GetInitiative()})")));
        for (int i = 0; i < sortedCombatants.Count; i++)
        {
            var c = sortedCombatants[i];
            Debug.Log($"{i + 1}. {c.name} | 先攻值：{c.GetInitiative()} | 等级：{c.GetAttrValue(AttributeType.Level)} | 智力：{c.GetAttrValue(AttributeType.Intelligence)}");
        }

        // ========== 新增：战斗开始事件触发 ==========
        OnBattleStart?.Invoke(sortedCombatants);

        // 启动第一个角色回合
        StartNextActorTurn();
    }

    /// <summary>
    /// 切换下一个角色回合
    /// </summary>
    public void StartNextActorTurn()
    {
        // 检查本回合是否所有角色都已行动
        if (sortedCombatants.All(c => c.hasActInRound))
        {
            EnterNewGlobalRound();
            return;
        }

        // 找到下一个未行动、未死亡的角色
        if (currentActorIndex == -1) currentActorIndex = 0;
        else currentActorIndex = (currentActorIndex + 1) % sortedCombatants.Count;

        BaseCharacterAttr nextActor = sortedCombatants[currentActorIndex];
        while (nextActor.hasActInRound || nextActor.isDead)
        {
            currentActorIndex = (currentActorIndex + 1) % sortedCombatants.Count;
            nextActor = sortedCombatants[currentActorIndex];
        }

        // 激活角色回合
        nextActor.isMyTurn = true;
        Debug.Log($"{nextActor.name} 的回合！剩余行动点：{nextActor.GetAttrValue(AttributeType.CurrentAP)}");

        // ========== 新增：回合切换事件触发 ==========
        OnTurnChanged?.Invoke(nextActor);
        OnActionPointChanged?.Invoke(); // 刷新行动点UI

        // ========== 核心修改：敌人回合交由行为树执行 ==========
        if (nextActor is EnemyAttr enemy)
        {
            // 延迟1秒执行，给行为树留执行时间，和原逻辑保持一致
            Invoke(nameof(ExecuteEnemyBehaviorTree), 1f);
        }
    }

    /// <summary>
    /// 执行敌人行为树（自动行动/自动结束回合）
    /// </summary>
    private void ExecuteEnemyBehaviorTree()
    {
        BaseCharacterAttr currentEnemy = sortedCombatants[currentActorIndex];
        // 全量校验：死亡/非当前回合 → 直接结束回合
        if (currentEnemy.isDead || !currentEnemy.isMyTurn)
        {
            EndTurn(currentEnemy); // 改用通用方法
            return;
        }
        // 行为树会自动执行战斗分支的技能/普攻/移动逻辑，最终调用EndPersonalTurn结束回合
        // 行为树执行完毕后，会自动触发回合切换，无需额外硬编码
    }

    /// <summary>
    /// 全局新回合：恢复行动点+重置冷却
    /// </summary>
    private void EnterNewGlobalRound()
    {
        foreach (var combatant in allCombatants)
        {
            if (combatant.isDead) continue;

            // 恢复行动点
            int newAP = Mathf.Min(
                combatant.GetAttrIntValue(AttributeType.CurrentAP) + roundRecoverAP,
                globalMaxAP
            );
            combatant.SetAttrValue(AttributeType.CurrentAP, newAP);
            combatant.hasActInRound = false;

            // 敌人技能冷却减少
            if (combatant is EnemyAttr enemy)
            {
                enemy.ReduceSkillCoolDown();
            }
        }

        // 刷新所有技能冷却
        SkillManager.Instance?.RefreshAllSkillCoolDown();
        Debug.Log("===== 新回合开始！所有存活角色恢复行动点 =====");
        StartNextActorTurn();
    }


    /// <summary>
    /// 通用回合结束方法（处理玩家/敌人的回合结束逻辑）
    /// </summary>
    /// <param name="actor">要结束回合的角色</param>
    public void EndTurn(BaseCharacterAttr actor)
    {
        // 容错：空值/非当前回合/已死亡角色直接返回
        if (actor == null || !actor.isMyTurn || actor.isDead)
        {
            Debug.LogWarning($"[{actor?.name}] 无法结束回合：角色为空/非当前回合/已死亡");
            return;
        }

        // 核心状态重置
        actor.isMyTurn = false;
        actor.hasActInRound = true; // 标记本全局回合已行动
        Debug.Log($"{actor.name} 回合结束，剩余AP：{actor.GetAttrValue(AttributeType.CurrentAP)}");

        // 触发AP变更事件（刷新UI）
        OnActionPointChanged?.Invoke();

        // 检查战斗是否结束（比如敌人攻击后玩家死亡）
        CheckBattleEnd();

        // 结算阶段阻止进入下一回合，等待玩家确认
        if (isSettling) return;

        // 启动下一个角色的回合
        StartNextActorTurn();

    }

    /// <summary>
    /// 判断当前是否是玩家的回合（玩家所有战斗行为的核心校验）
    /// </summary>
    /// <returns>true=玩家回合，false=非玩家回合</returns>
    public bool IsPlayerTurn()
    {
        // 1. 非战斗状态（探索模式）：玩家可自由行动（返回true）
        if (!isBattleActive) return true;

        // 2. 战斗状态：找到当前行动角色，判断是否是玩家类型
        BaseCharacterAttr currentActor = GetCurrentActor(); // 需实现「获取当前行动角色」的方法
        return currentActor != null && currentActor is PlayerAttr;
    }

    // 配套：获取当前行动角色（TurnBattleManager 中新增）
    private BaseCharacterAttr GetCurrentActor()
    {
        if (currentActorIndex < 0 || currentActorIndex >= sortedCombatants.Count)
            return null;
        return sortedCombatants[currentActorIndex];
    }

    /// <summary>
    /// 玩家手动结束回合
    /// </summary>
    public void PlayerEndTurn()
    {
        BaseCharacterAttr player = allCombatants.FirstOrDefault(c => c is PlayerAttr);
        if (player == null)
        {
            Debug.LogError("玩家角色不存在，无法结束回合");
            return;
        }

        // 调用通用回合结束方法
        EndTurn(player);
    }

    /// <summary>
    /// 检查战斗结束条件（触发结算流程，暂停等待玩家确认）
    /// </summary>
    public void CheckBattleEnd()
    {
        if (!isBattleActive || isSettling) return;

        bool allPlayerDead = allCombatants.Where(c => c is PlayerAttr).All(c => c.isDead);
        bool allEnemyDead = allCombatants.Where(c => c is EnemyAttr).All(c => c.isDead);

        if (allPlayerDead)
        {
            isSettling = true;
            Debug.Log("[TurnBattleManager] 玩家方全灭！进入失败结算");
            Debug.Log($"[TurnBattleManager] OnBattleSettlement 订阅者数量: {(OnBattleSettlement != null ? OnBattleSettlement.GetInvocationList().Length : 0)}");
            currentSettlementData = new BattleSettlementData { isVictory = false };
            OnBattleSettlement?.Invoke(currentSettlementData);
        }
        else if (allEnemyDead)
        {
            isSettling = true;
            Debug.Log("[TurnBattleManager] 敌方全灭！进入胜利结算");
            Debug.Log($"[TurnBattleManager] OnBattleSettlement 订阅者数量: {(OnBattleSettlement != null ? OnBattleSettlement.GetInvocationList().Length : 0)}");
            currentSettlementData = CollectBattleRewards();
            OnBattleSettlement?.Invoke(currentSettlementData);
        }
    }

    /// <summary>
    /// 结束战斗，恢复探索状态
    /// </summary>
    private void EndBattle(bool playerWin)
    {
        isBattleActive = false;
        isSettling = false;
        currentSettlementData = null;
        GameStateMgr.Instance?.SwitchGameState(GameStateMgr.GamePlayState.ExploreState);

        foreach (var combatant in allCombatants)
        {
            combatant.isInBattle = false;
            combatant.isMyTurn = false;

            // 退出战斗动画
            Animator anim = combatant.GetComponent<Animator>();
            if (anim != null) anim.SetBool("BattleMode", false);

            // 敌人处理：禁用行为树战斗分支，恢复巡逻
            if (combatant is EnemyAttr enemy && !enemy.isDead)
            {
                enemy.StartPatrol();
            }
        }

        // 清空战斗数据
        allCombatants.Clear();
        sortedCombatants.Clear();
        currentActorIndex = 0;

        // ========== 新增：战斗结束事件触发 ==========
        OnBattleEnd?.Invoke(playerWin);
    }

    /// <summary>
    /// 收集所有死亡敌人的奖励数据（经验+金币+掉落物品，仅汇总，不发放）
    /// </summary>
    private BattleSettlementData CollectBattleRewards()
    {
        var data = new BattleSettlementData { isVictory = true };
        foreach (var combatant in allCombatants)
        {
            if (combatant is EnemyAttr enemy)
            {
                data.totalEXP += enemy.dropEXP;
                if (enemy.dropTable != null)
                {
                    DropResult drop = enemy.dropTable.GenerateRandomDrop();
                    data.totalGold += drop.dropGold;
                    data.rewardItems.AddRange(drop.dropItemList);
                }
            }
        }
        return data;
    }

    /// <summary>
    /// 胜利结算确认：发放EXP/金币/物品到玩家背包，然后结束战斗
    /// </summary>
    public void ConfirmVictorySettlement()
    {
        if (currentSettlementData == null) return;

        PlayerAttr player = allCombatants.FirstOrDefault(c => c is PlayerAttr) as PlayerAttr;
        if (player != null)
        {
            player.AddEXP(currentSettlementData.totalEXP);

            Inventory inventory = player.GetComponent<Inventory>();
            if (inventory != null)
            {
                inventory.AddGold(currentSettlementData.totalGold);
                foreach (var item in currentSettlementData.rewardItems)
                    inventory.AddItem(item);
            }
        }

        EndBattle(true);
    }

    /// <summary>
    /// 失败结算-退出（退出游戏/停止编辑器运行）
    /// </summary>
    public void ConfirmDefeatExit()
    {
        EndBattle(false);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 失败结算-清理战斗（读档由MainMenuUI存档位面板处理）
    /// </summary>
    public void ConfirmDefeatLoadSave()
    {
        EndBattle(false);
    }

    // ========== 废弃：原全局触发战斗逻辑，改为敌人自身警戒圈触发 ==========
    // private void OnTriggerEnter(Collider other) { ... }
}