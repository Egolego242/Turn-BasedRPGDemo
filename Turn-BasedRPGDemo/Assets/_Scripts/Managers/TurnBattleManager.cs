using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 回合制战斗管理器（单例）
/// 核心：先攻值排序、行动点回复、回合流程控制
/// </summary>
public class TurnBattleManager : MonoBehaviour
{
    public static TurnBattleManager Instance { get; private set; }

    [Header("===== 战斗规则配置 =====")]
    public float globalMaxAP = 6f; // 行动点上限
    public float roundRecoverAP = 4f; // 每回合回复行动点
    public float detectRange = 8f; // 敌人探测范围（触发战斗）

    // 战斗参与方
    private List<BaseCharacterAttr> allCombatants = new List<BaseCharacterAttr>();
    private List<BaseCharacterAttr> sortedCombatants = new List<BaseCharacterAttr>(); // 按先攻排序后的角色
    private int currentActorIndex = 0; // 当前行动角色索引
    private bool isBattleActive = false; // 是否在战斗中

    // 单例初始化
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// 触发战斗（玩家进入探测范围/主动攻击中立单位）
    /// </summary>
    public void TriggerBattle(BaseCharacterAttr player, List<BaseCharacterAttr> enemies)
    {
        if (isBattleActive) return;
        isBattleActive = true;

        // 收集所有战斗角色
        allCombatants.Clear();
        allCombatants.Add(player);
        allCombatants.AddRange(enemies);

        // 初始化战斗状态
        foreach (var combatant in allCombatants)
        {
            combatant.isInBattle = true;
            combatant.isMyTurn = false;
            combatant.hasActInRound = false;
            // 初始行动点4，上限6
            combatant.SetAttrValue(AttributeType.MaxAP, globalMaxAP);
            combatant.SetAttrValue(AttributeType.CurrentAP, roundRecoverAP);
            // 切换战斗动画（假设Animator有"BattleMode"布尔参数）
            Animator anim = combatant.GetComponent<Animator>();
            if (anim != null) anim.SetBool("BattleMode", true);

            // 敌人停止巡逻
            if (combatant is EnemyAttr enemy)
            {
                enemy.StopPatrol();
            }
        }

        // 按先攻值降序排序（先攻高先行动）
        sortedCombatants = allCombatants.OrderByDescending(c => c.GetInitiative()).ToList();

        // 显示战斗UI提示（后续对接UI层）
        Debug.Log("战斗开始！按先攻顺序行动：" + string.Join(" > ", sortedCombatants.Select(c => c.name)));
        // 触发UI显示逻辑（比如：ShowBattleTip("战斗开始！")，2秒后隐藏）

        // 开始第一个角色的回合
        StartNextActorTurn();
    }

    /// <summary>
    /// 切换到下一个角色的回合
    /// </summary>
    public void StartNextActorTurn()
    {
        // 1. 检查当前回合是否所有角色都已行动
        if (sortedCombatants.All(c => c.hasActInRound))
        {
            EnterNewGlobalRound(); // 全角色行动完毕，进入新全局回合
            return;
        }

        // 2. 找到下一个未行动的角色
        currentActorIndex = (currentActorIndex + 1) % sortedCombatants.Count;
        BaseCharacterAttr nextActor = sortedCombatants[currentActorIndex];

        while (nextActor.hasActInRound || nextActor.isDead)
        {
            currentActorIndex = (currentActorIndex + 1) % sortedCombatants.Count;
            nextActor = sortedCombatants[currentActorIndex];
        }

        // 3. 激活该角色的回合
        nextActor.isMyTurn = true;
        Debug.Log($"{nextActor.name} 的回合！剩余行动点：{nextActor.GetAttrValue(AttributeType.CurrentAP)}");

        // 敌人AI自动行动（玩家回合等待输入）
        if (nextActor is EnemyAttr enemy)
        {
            Invoke("EnemyAutoAct", 1f); // 延迟1秒执行AI行动
        }
    }

    /// <summary>
    /// 全局回合结束（所有角色行动完毕）→ 回复行动点+重置状态
    /// </summary>
    private void EnterNewGlobalRound()
    {
        foreach (var combatant in allCombatants)
        {
            if (combatant.isDead) continue;
            // 回复4点行动点，不超上限6
            float newAP = Mathf.Min(
                combatant.GetAttrValue(AttributeType.CurrentAP) + roundRecoverAP,
                globalMaxAP
            );
            combatant.SetAttrValue(AttributeType.CurrentAP, newAP);
            combatant.hasActInRound = false; // 重置已行动标记

            // 敌人技能冷却减少
            if (combatant is EnemyAttr enemy)
            {
                enemy.ReduceSkillCoolDown();
            }
        }
        Debug.Log("===== 新回合开始！所有存活角色回复4点行动点 =====");
        StartNextActorTurn(); // 开始新回合的第一个角色行动
    }

    /// <summary>
    /// 敌人AI自动行动（简化版，可扩展）
    /// </summary>
    private void EnemyAutoAct()
    {
        BaseCharacterAttr currentEnemy = sortedCombatants[currentActorIndex];
        if (currentEnemy.isDead || !currentEnemy.isMyTurn) return;

        EnemyAttr enemy = currentEnemy as EnemyAttr;
        if (enemy == null) return;

        // AI决策：优先普攻→技能→移动（根据行动点判断）
        bool acted = false;
        if (enemy.CanNormalAttack())
        {
            acted = enemy.DoNormalAttack();
        }
        else if (enemy.CanCastSkill())
        {
            acted = enemy.CastSkill();
        }
        // 修复：替换enemy.currentAP为GetAttrValue
        else if (enemy.GetAttrValue(AttributeType.CurrentAP) >= enemy.moveCostPerUnit)
        {
            // 向玩家移动（简化：取第一个玩家为目标）
            BaseCharacterAttr player = allCombatants.First(c => c is PlayerAttr);
            acted = enemy.MoveTo(player.transform.position);
        }

        // 无论是否行动，结束个人回合（调用基类方法）
        enemy.EndPersonalTurn();
        StartNextActorTurn();
    }

    /// <summary>
    /// 玩家手动结束回合
    /// </summary>
    public void PlayerEndTurn()
    {
        BaseCharacterAttr player = allCombatants.First(c => c is PlayerAttr);
        if (!player.isMyTurn || player.isDead) return;

        player.EndPersonalTurn(); // 调用基类方法
        StartNextActorTurn();
    }

    /// <summary>
    /// 检查战斗结束条件
    /// </summary>
    public void CheckBattleEnd()
    {
        // 判定双方存活状态
        bool allPlayerDead = allCombatants.Where(c => c is PlayerAttr).All(c => c.isDead);
        bool allEnemyDead = allCombatants.Where(c => c is EnemyAttr).All(c => c.isDead);

        if (allPlayerDead)
        {
            // 玩家失败→存档读取界面
            Debug.Log("玩家方全灭！打开存档读取界面");
            // UIManager.Instance.ShowSaveLoadPanel(); // 对接存档UI
            EndBattle(false);
        }
        else if (allEnemyDead)
        {
            // 玩家胜利→回到探索模式
            Debug.Log("敌方全灭！战斗胜利，回到自由探索");
            EndBattle(true);
        }
    }

    /// <summary>
    /// 结束战斗
    /// </summary>
    private void EndBattle(bool playerWin)
    {
        isBattleActive = false;
        foreach (var combatant in allCombatants)
        {
            combatant.isInBattle = false;
            combatant.isMyTurn = false;
            // 退出战斗动画
            Animator anim = combatant.GetComponent<Animator>();
            if (anim != null) anim.SetBool("BattleMode", false);

            // 敌人死亡处理（掉落物）
            if (combatant is EnemyAttr enemy && enemy.isDead)
            {
                enemy.Die(); // 触发掉落逻辑（原Die方法已包含掉落）
            }
        }

        allCombatants.Clear();
        sortedCombatants.Clear();
        currentActorIndex = 0;
    }

    // 检测玩家进入敌人探测范围（挂载在敌人身上，或全局检测）
    private void OnTriggerEnter(Collider other)
    {
        if (isBattleActive) return;
        PlayerAttr player = other.GetComponent<PlayerAttr>();
        if (player != null)
        {
            // 收集探测范围内的所有敌人
            List<BaseCharacterAttr> enemies = Physics.OverlapSphere(transform.position, detectRange)
                .Select(col => col.GetComponent<EnemyAttr>())
                .Where(enemy => enemy != null && !enemy.isDead)
                .Cast<BaseCharacterAttr>()
                .ToList();

            if (enemies.Count > 0)
            {
                TriggerBattle(player, enemies);
            }
        }
    }
}