using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 敌人属性类 - 行为树专属适配版
/// 所有AI逻辑完全交由Behavior Designer行为树控制
/// 保留回合制战斗核心规则、属性系统、导航系统，提供行为树可调用的所有方法/条件判断
/// </summary>
public class EnemyAttr : BaseCharacterAttr
{
    #region 核心组件引用
    [Header("===== 行为树核心配置 =====")]
    public BehaviorTree behaviorTree; // 拖拽敌人身上的行为树组件
    [HideInInspector] public Animator animator;
    [HideInInspector] public NavMeshAgent navAgent;
    #endregion

    #region 基础属性配置
    [Header("===== 敌人初始属性 =====")]
    public float initMaxHP = 80;
    public float initMaxMP = 30;
    public float initMaxAP = 8;
    public float initStrength = 6;
    public float initIntelligence = 3;
    public float initArmor = 2;
    #endregion

    #region 掉落配置
    [Header("===== 掉落配置 =====")]
    public DropTable dropTable;
    public int dropEXP = 20;
    #endregion

    #region 行动消耗规则（和行为树节点完全对应）
    [Header("===== 行动消耗规则 =====")]
    public float moveCostPerUnit = 1f; // 每移动1单位消耗的行动点
    public float normalAttackCost = 2f; // 普攻消耗行动点（行为树普攻判断用）
    public float skillAttackCost = 3f; // 技能消耗行动点（行为树技能判断用）
    [Header("===== 战斗配置 =====")]
    public float attackRange = 2f; // 普攻射程
    public float skillRange = 5f; // 技能射程
    public Transform attackTarget; // 攻击目标（行为树赋值玩家Transform）
    #endregion

    #region 巡逻&警戒配置
    [Header("===== 巡逻配置 =====")]
    public float patrolRange = 5f; // 巡逻范围
    public float patrolWaitTime = 2f; // 巡逻停留时间
    private Vector3 originPos; // 出生初始位置
    private Coroutine patrolCoroutine;

    [Header("===== 警戒圈配置 =====")]
    public float enemyDetectRange = 8f; // 战斗触发警戒范围
    public bool showEnemyRangeGizmo = true; // Scene视图显示警戒圈
    #endregion

    #region 技能冷却
    [Header("===== 技能冷却 =====")]
    public float skillCoolDown = 2; // 技能冷却回合数
    [HideInInspector] public float skillCoolDownLeft = 0; // 剩余冷却回合（行为树可读取）
    #endregion

    #region 初始化
    protected override void Awake()
    {
        base.Awake();
        // 组件自动获取+容错
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        if (behaviorTree == null)
            behaviorTree = GetComponent<BehaviorTree>();

        // 初始化属性（调用基类方法）
        InitAttribute(initMaxHP, initMaxMP, initMaxAP, initStrength, initIntelligence, initArmor);
        currentCamp = CampType.Enemy;
        originPos = transform.position;

        // 初始化行为树：非战斗状态默认启用日常分支，禁用战斗分支
        InitBehaviorTreeState();
    }

    /// <summary>
    /// 初始化行为树状态
    /// </summary>
    private void InitBehaviorTreeState()
    {
        if (behaviorTree == null) return;
        // 非战斗状态启动行为树
        if (!isInBattle)
        {
            behaviorTree.EnableBehavior();
            StartPatrol();
        }
        else
        {
            behaviorTree.DisableBehavior();
        }
    }
    #endregion

    #region 行为树 - 日常分支核心方法（巡逻/警戒/触发战斗）
    /// <summary>
    /// 【行为树调用】启动巡逻协程
    /// </summary>
    public void StartPatrol()
    {
        if (isInBattle || isDead) return;
        if (patrolCoroutine != null)
            StopCoroutine(patrolCoroutine);

        patrolCoroutine = StartCoroutine(PatrolCoroutine());
    }

    /// <summary>
    /// 【行为树调用】停止巡逻
    /// </summary>
    public void StopPatrol()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            navAgent.ResetPath();
            navAgent.isStopped = true;
        }
    }

    /// <summary>
    /// 巡逻协程（行为树日常分支驱动）
    /// </summary>
    private IEnumerator PatrolCoroutine()
    {
        while (!isInBattle && !isDead)
        {
            // 生成随机巡逻点
            Vector3 randomDir = Random.insideUnitSphere * patrolRange;
            randomDir.y = 0;
            Vector3 targetPos = originPos + randomDir;

            // 验证目标点是否在NavMesh上
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, patrolRange, NavMesh.AllAreas))
            {
                targetPos = hit.position;
                if (navAgent != null && navAgent.isActiveAndEnabled)
                {
                    navAgent.isStopped = false;
                    navAgent.SetDestination(targetPos);
                }
            }

            // 等待到达+停留
            yield return new WaitForSeconds(patrolWaitTime);
        }
    }

    /// <summary>
    /// 【行为树条件判断】玩家是否在警戒范围内
    /// </summary>
    public bool IsPlayerInDetectRange()
    {
        PlayerAttr player = FindObjectOfType<PlayerAttr>();
        if (player == null || player.isDead) return false;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= enemyDetectRange;
    }

    /// <summary>
    /// 【行为树条件判断】玩家是否存在且存活
    /// </summary>
    public bool IsPlayerExistAndAlive()
    {
        PlayerAttr player = FindObjectOfType<PlayerAttr>();
        return player != null && !player.isDead;
    }

    /// <summary>
    /// 【行为树调用】敌人主动触发战斗
    /// </summary>
    public void TriggerBattleByEnemy()
    {
        PlayerAttr player = FindObjectOfType<PlayerAttr>();
        if (player == null || TurnBattleManager.Instance == null) return;

        // 收集同范围内所有存活敌人
        List<BaseCharacterAttr> battleEnemies = new List<BaseCharacterAttr>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, enemyDetectRange);
        foreach (var col in colliders)
        {
            EnemyAttr enemy = col.GetComponent<EnemyAttr>();
            if (enemy != null && !enemy.isDead)
                battleEnemies.Add(enemy);
        }

        // 没有敌人则加入自己
        if (battleEnemies.Count == 0)
            battleEnemies.Add(this);

        // 切换全局游戏状态+触发回合制战斗
        GameStateMgr.Instance?.SwitchGameState(GameStateMgr.GamePlayState.BattleState);
        TurnBattleManager.Instance.TriggerBattle(player, battleEnemies);

        // 给自身赋值攻击目标
        attackTarget = player.transform;
    }
    #endregion

    #region 行为树 - 战斗分支核心方法（技能/普攻/移动/回合控制）
    /// <summary>
    /// 【行为树条件判断】是否处于战斗状态
    /// </summary>
    public bool IsInBattle() => isInBattle;

    /// <summary>
    /// 【行为树条件判断】是否处于当前行动回合
    /// </summary>
    public bool IsMyTurn() => isMyTurn;

    /// <summary>
    /// 【行为树条件判断】技能冷却是否就绪
    /// </summary>
    public bool IsSkillCoolDownReady() => skillCoolDownLeft <= 0;

    /// <summary>
    /// 【行为树条件判断】行动点是否足够释放技能
    /// </summary>
    public bool HasEnoughAPForSkill()
    {
        return GetAttrValue(AttributeType.CurrentAP) >= skillAttackCost;
    }

    /// <summary>
    /// 【行为树条件判断】行动点是否足够释放普攻
    /// </summary>
    public bool HasEnoughAPForAttack()
    {
        return GetAttrValue(AttributeType.CurrentAP) >= normalAttackCost;
    }

    /// <summary>
    /// 【行为树条件判断】目标是否在普攻射程内
    /// </summary>
    public bool IsTargetInAttackRange()
    {
        if (attackTarget == null) return false;
        float distance = Vector3.Distance(transform.position, attackTarget.position);
        return distance <= attackRange;
    }

    /// <summary>
    /// 【行为树条件判断】目标是否在技能射程内
    /// </summary>
    public bool IsTargetInSkillRange()
    {
        if (attackTarget == null) return false;
        float distance = Vector3.Distance(transform.position, attackTarget.position);
        return distance <= skillRange;
    }

    /// <summary>
    /// 【行为树调用】释放技能（返回是否释放成功）
    /// </summary>
    public bool CastSkill()
    {
        // 全量校验
        if (isDead || attackTarget == null || !IsSkillCoolDownReady()
            || !HasEnoughAPForSkill() || !IsTargetInSkillRange())
        {
            Debug.Log($"{gameObject.name} 技能释放条件不满足");
            return false;
        }

        // 消耗行动点
        if (!ConsumeAP(skillAttackCost)) return false;

        // 播放技能动画
        animator?.SetTrigger("Skill");
        PlaySkillAnim();

        // 技能伤害计算
        if (attackTarget.TryGetComponent<BaseCharacterAttr>(out BaseCharacterAttr targetAttr))
        {
            float intelligence = GetAttrValue(AttributeType.Intelligence);
            float armor = targetAttr.GetAttrValue(AttributeType.Armor);
            float damage = Mathf.Max(1, intelligence * 1.5f - armor * 0.5f);
            targetAttr.TakeDamage(damage);
            Debug.Log($"{gameObject.name} 释放技能攻击 {attackTarget.name}，造成 {damage} 伤害");
        }

        // 触发冷却
        skillCoolDownLeft = skillCoolDown;
        // 攻击后检查战斗是否结束
        TurnBattleManager.Instance?.CheckBattleEnd();
        return true;
    }

    /// <summary>
    /// 【行为树调用】执行普攻（返回是否释放成功）
    /// </summary>
    public bool DoNormalAttack()
    {
        // 全量校验
        if (isDead || attackTarget == null || !HasEnoughAPForAttack() || !IsTargetInAttackRange())
        {
            Debug.Log($"{gameObject.name} 普攻条件不满足");
            return false;
        }

        // 消耗行动点
        if (!ConsumeAP(normalAttackCost)) return false;

        // 播放普攻动画
        animator?.SetTrigger("Attack");
        PlayAttackAnim();

        // 普攻伤害计算
        if (attackTarget.TryGetComponent<BaseCharacterAttr>(out BaseCharacterAttr targetAttr))
        {
            float strength = GetAttrValue(AttributeType.Strength);
            float armor = targetAttr.GetAttrValue(AttributeType.Armor);
            float damage = Mathf.Max(1, strength - armor);
            targetAttr.TakeDamage(damage);
            Debug.Log($"{gameObject.name} 普攻 {attackTarget.name}，造成 {damage} 伤害");
        }

        // 攻击后检查战斗是否结束
        TurnBattleManager.Instance?.CheckBattleEnd();
        return true;
    }

    /// <summary>
    /// 【行为树调用】向目标移动（返回是否移动成功）
    /// </summary>
    public bool MoveToTarget()
    {
        if (isDead || attackTarget == null || navAgent == null || !navAgent.isActiveAndEnabled)
            return false;

        float currentAP = GetAttrValue(AttributeType.CurrentAP);
        if (currentAP <= 0) return false;

        // 计算移动距离和消耗
        Vector3 targetPos = attackTarget.position;
        float distance = Vector3.Distance(transform.position, targetPos);
        float cost = distance * moveCostPerUnit;

        // 行动点不足无法移动
        if (cost > currentAP) return false;

        // 执行寻路
        navAgent.isStopped = false;
        navAgent.SetDestination(targetPos);
        // 消耗行动点
        ConsumeAP(cost);
        return true;
    }

    /// <summary>
    /// 【行为树调用】减少技能冷却（全局回合结束时调用）
    /// </summary>
    public void ReduceSkillCoolDown()
    {
        if (skillCoolDownLeft > 0)
            skillCoolDownLeft--;
    }

    /// <summary>
    /// 【行为树调用】结束个人回合
    /// </summary>
    public override void EndPersonalTurn()
    {
        base.EndPersonalTurn(); // 基类核心逻辑：标记已行动、取消回合
        // 停止移动
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            navAgent.ResetPath();
            navAgent.isStopped = true;
        }
        // 通知回合管理器切换下一个角色
        TurnBattleManager.Instance?.StartNextActorTurn();
    }

    /// <summary>
    /// 【行为树调用】死亡判定与处理
    /// </summary>
    public void CheckDeathState()
    {
        if (isDead)
        {
            Die();
        }
    }
    #endregion

    #region 重写基类方法（战斗状态联动行为树）
    public override void Die()
    {
        base.Die();
        Debug.Log($"{gameObject.name} 死亡");

        // 禁用导航和行为树
        if (navAgent != null) navAgent.enabled = false;
        behaviorTree?.DisableBehavior();

        // 停止巡逻
        StopPatrol();

        // 给玩家加经验
        PlayerAttr[] players = FindObjectsOfType<PlayerAttr>();
        foreach (PlayerAttr player in players)
        {
            if (player != null) player.AddEXP(dropEXP);
        }

        // 生成掉落物
        if (DropSystem.Instance != null && dropTable != null)
        {
            DropSystem.Instance.SpawnDrop(transform.position, dropTable);
        }

        // 检查战斗是否结束
        TurnBattleManager.Instance?.CheckBattleEnd();
        // 延迟销毁
        Destroy(gameObject, 5f);
    }

    // 动画播放方法
    public void PlayAttackAnim() => animator?.SetTrigger("Attack");
    public void PlaySkillAnim() => animator?.SetTrigger("Skill");
    #endregion

    #region Scene视图调试
    private void OnDrawGizmos()
    {
        if (!showEnemyRangeGizmo) return;

        // 绘制警戒圈
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, enemyDetectRange);

        // 绘制巡逻范围
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(originPos == Vector3.zero ? transform.position : originPos, patrolRange);

        // 绘制攻击/技能射程
        if (isInBattle)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, skillRange);
        }
    }
    #endregion
}