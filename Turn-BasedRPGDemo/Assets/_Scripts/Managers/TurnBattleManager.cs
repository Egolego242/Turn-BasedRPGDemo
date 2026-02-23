using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class TurnBattleManager : MonoBehaviour
{
    public static TurnBattleManager Instance { get; private set; }
    [Header("===== 战斗规则配置 =====")]
    public float globalMaxAP = 6f;
    public float roundRecoverAP = 4f;
    // 注意：detectRange探测范围已废弃，改为敌人自身的enemyDetectRange警戒圈
    // public float detectRange = 8f; 

    // 战斗参与方
    private List<BaseCharacterAttr> allCombatants = new List<BaseCharacterAttr>();
    private List<BaseCharacterAttr> sortedCombatants = new List<BaseCharacterAttr>();
    private int currentActorIndex = 0;
    private bool isBattleActive = false;

    // ========== 新增：战斗流程事件，UI订阅 ==========
    public static event Action<List<BaseCharacterAttr>> OnBattleStart; // 战斗开始，传入所有参战角色
    public static event Action<BaseCharacterAttr> OnTurnChanged; // 回合切换，传入当前行动角色
    public static event Action<bool> OnBattleEnd; // 战斗结束，传入是否玩家胜利
    public static event Action OnActionPointChanged; // 行动点变化事件


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
        Debug.Log("战斗开始！先攻顺序：" + string.Join(" > ", sortedCombatants.Select(c => c.name)));

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
        currentActorIndex = (currentActorIndex + 1) % sortedCombatants.Count;
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
    /// 执行敌人行为树（替代原硬编码EnemyAutoAct）
    /// </summary>
    private void ExecuteEnemyBehaviorTree()
    {
        BaseCharacterAttr currentEnemy = sortedCombatants[currentActorIndex];
        // 安全校验：死亡/非当前回合，直接结束回合
        if (currentEnemy.isDead || !currentEnemy.isMyTurn)
        {
            currentEnemy.EndPersonalTurn();
            StartNextActorTurn();
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
            float newAP = Mathf.Min(
                combatant.GetAttrValue(AttributeType.CurrentAP) + roundRecoverAP,
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
    /// 玩家手动结束回合
    /// </summary>
    public void PlayerEndTurn()
    {
        BaseCharacterAttr player = allCombatants.FirstOrDefault(c => c is PlayerAttr);
        if (player == null || !player.isMyTurn || player.isDead) return;

        player.EndPersonalTurn();
        StartNextActorTurn();
    }

    /// <summary>
    /// 检查战斗结束条件
    /// </summary>
    public void CheckBattleEnd()
    {
        bool allPlayerDead = allCombatants.Where(c => c is PlayerAttr).All(c => c.isDead);
        bool allEnemyDead = allCombatants.Where(c => c is EnemyAttr).All(c => c.isDead);

        if (allPlayerDead)
        {
            Debug.Log("玩家方全灭！战斗失败");
            EndBattle(false);
        }
        else if (allEnemyDead)
        {
            Debug.Log("敌方全灭！战斗胜利");
            EndBattle(true);
        }
    }

    /// <summary>
    /// 结束战斗，恢复探索状态
    /// </summary>
    private void EndBattle(bool playerWin)
    {
        isBattleActive = false;
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

    // ========== 废弃：原全局触发战斗逻辑，改为敌人自身警戒圈触发 ==========
    // private void OnTriggerEnter(Collider other) { ... }
}