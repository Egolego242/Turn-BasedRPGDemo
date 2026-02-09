using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using System.Collections;

/// <summary>
/// 敌人属性类（完整版）
/// 保留原有属性/掉落/死亡逻辑 + 补充和玩家一致的行动/战斗规则
/// </summary>
public class EnemyAttr : BaseCharacterAttr
{
    #region 原有核心属性（完全保留）
    [Header("===== 敌人初始属性 =====")]
    public float initMaxHP = 80;
    public float initMaxMP = 30;
    public float initMaxAP = 8; // 最大行动点（和玩家一致）
    public float initStrength = 6;
    public float initIntelligence = 3;
    public float initArmor = 2;

    [Header("===== 掉落配置 =====")]
    public DropTable dropTable;
    public int dropEXP = 20;

    [HideInInspector] public Animator animator;
    [HideInInspector] public NavMeshAgent navAgent;
    #endregion

    #region 行动规则配置（和玩家一致）
    [Header("===== 行动消耗规则 =====")]
    public float moveCostPerUnit = 1f; // 每移动1单位消耗的行动点
    public float normalAttackCost = 2f; // 普攻消耗行动点
    public float skillAttackCost = 3f; // 技能消耗行动点

    [Header("===== 战斗配置 =====")]
    public float attackRange = 2f; // 普攻射程
    public float skillRange = 5f; // 技能射程
    public Transform attackTarget; // 攻击目标
    #endregion

    #region 巡逻配置
    [Header("===== 巡逻配置 =====")]
    public float patrolRange = 5f; // 巡逻范围
    public float patrolWaitTime = 2f; // 停留时间
    private Vector3 originPos; // 初始位置
    private Coroutine patrolCoroutine;
    #endregion

    #region 技能冷却
    [Header("===== 技能冷却 =====")]
    public float skillCoolDown = 2; // 技能冷却回合数
    private float skillCoolDownLeft = 0; // 剩余冷却回合
    #endregion

    #region 行为树适配
    private BehaviorTree _behaviorTree;
    #endregion

    #region 初始化
    private void Awake()
    {
        // 组件容错：避免未挂载时报错
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();

        // 初始化属性（调用基类方法）
        InitAttribute(initMaxHP, initMaxMP, initMaxAP, initStrength, initIntelligence, initArmor);
        currentCamp = CampType.Enemy;

        // 行为树组件初始化（容错）
        if (TryGetComponent<BehaviorTree>(out BehaviorTree bt))
        {
            _behaviorTree = bt;
        }

        // 初始化巡逻
        originPos = transform.position;
        if (!isInBattle)
        {
            patrolCoroutine = StartCoroutine(PatrolCoroutine());
        }
    }
    #endregion

    #region 巡逻逻辑
    // 巡逻协程
    private IEnumerator PatrolCoroutine()
    {
        while (!isInBattle && !isDead)
        {
            // 随机目标点（范围内）
            Vector3 randomPos = originPos + new Vector3(
                Random.Range(-patrolRange, patrolRange),
                0,
                Random.Range(-patrolRange, patrolRange)
            );
            // 寻路到目标点
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.SetDestination(randomPos);
                navAgent.isStopped = false;
            }
            // 等待到达/停留
            yield return new WaitForSeconds(patrolWaitTime);
        }
    }

    // 停止巡逻（战斗开始时调用）
    public void StopPatrol()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
        }
        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }
    }
    #endregion

    #region 回合重置
    // 回合重置：恢复行动点（和玩家回合逻辑一致）
    public void ResetTurn()
    {
        if (isDead) return;
        RecoverFullAP(); // 改用基类方法恢复满AP
        if (navAgent != null) navAgent.ResetPath(); // 清空寻路路径（空引用容错）
    }
    #endregion

    #region 核心行动逻辑
    /// <summary>
    /// AI寻路移动（消耗行动点，和玩家移动规则一致）
    /// </summary>
    public bool MoveTo(Vector3 targetPos)
    {
        // 死亡/无行动点/无寻路组件 → 无法移动
        if (isDead || navAgent == null || !navAgent.enabled)
            return false;

        float currentAP = GetAttrValue(AttributeType.CurrentAP);
        if (currentAP <= 0) return false;

        // 计算移动距离和消耗
        float distance = Vector3.Distance(transform.position, targetPos);
        float cost = distance * moveCostPerUnit;

        // 行动点不足 → 无法移动
        if (cost > currentAP)
            return false;

        // 执行寻路
        navAgent.SetDestination(targetPos);
        navAgent.isStopped = false;

        // 消耗行动点（改用基类方法）
        ConsumeAP(cost);

        return true;
    }

    /// <summary>
    /// 执行普攻（行为树调用，消耗行动点）
    /// </summary>
    public bool DoNormalAttack()
    {
        // 校验：死亡/无目标/行动点不足/超出射程
        if (isDead || attackTarget == null || !ConsumeAP(normalAttackCost) ||
            Vector3.Distance(transform.position, attackTarget.position) > attackRange)
            return false;

        // 播放普攻动画（容错）
        animator?.SetTrigger("Attack");

        // 普攻伤害计算
        if (attackTarget.TryGetComponent<BaseCharacterAttr>(out BaseCharacterAttr targetAttr))
        {
            float strength = GetAttrValue(AttributeType.Strength);
            float armor = targetAttr.GetAttrValue(AttributeType.Armor);
            float damage = CalculateDamage(strength, armor);
            targetAttr.TakeDamage(damage);
            Debug.Log($"{gameObject.name} 普攻 {attackTarget.name}，造成 {damage} 伤害");
        }

        return true;
    }

    /// <summary>
    /// 执行技能（行为树调用，消耗行动点+冷却）
    /// </summary>
    public bool CastSkill()
    {
        // 冷却校验
        if (skillCoolDownLeft > 0)
        {
            Debug.Log($"{gameObject.name} 技能冷却中，剩余{skillCoolDownLeft}回合");
            return false;
        }

        // 基础校验
        if (isDead || attackTarget == null || !ConsumeAP(skillAttackCost) ||
            Vector3.Distance(transform.position, attackTarget.position) > skillRange)
            return false;

        // 播放技能动画（容错）
        animator?.SetTrigger("Skill");

        // 技能伤害计算
        if (attackTarget.TryGetComponent<BaseCharacterAttr>(out BaseCharacterAttr targetAttr))
        {
            float intelligence = GetAttrValue(AttributeType.Intelligence);
            float armor = targetAttr.GetAttrValue(AttributeType.Armor);
            float damage = CalculateDamage(intelligence * 1.5f, armor * 0.5f);
            targetAttr.TakeDamage(damage);
            Debug.Log($"{gameObject.name} 释放技能攻击 {attackTarget.name}，造成 {damage} 伤害");
        }

        // 触发冷却
        skillCoolDownLeft = skillCoolDown;
        return true;
    }

    // 减少技能冷却（全局回合结束时调用）
    public void ReduceSkillCoolDown()
    {
        if (skillCoolDownLeft > 0)
        {
            skillCoolDownLeft--;
        }
    }
    #endregion

    #region 重写基类方法
    // 重写结束个人回合（补充NavMeshAgent逻辑）
    public override void EndPersonalTurn()
    {
        base.EndPersonalTurn(); // 调用基类逻辑（标记已行动、取消回合）
        if (navAgent != null)
        {
            navAgent.isStopped = true; // 停止移动
        }
    }

    // 重写死亡方法（保留掉落/经验/销毁）
    public override void Die()
    {
        base.Die();
        Debug.Log($"{gameObject.name}死亡");

        // 禁用AI和行为树（容错）
        if (navAgent != null) navAgent.enabled = false;
        _behaviorTree?.DisableBehavior();

        // 停止巡逻
        StopPatrol();

        // 给玩家加经验（容错：避免多个PlayerAttr）
        PlayerAttr[] players = FindObjectsOfType<PlayerAttr>();
        foreach (PlayerAttr player in players)
        {
            if (player != null) player.AddEXP(dropEXP);
        }

        // 生成掉落物（容错：DropSystem实例为空）
        if (DropSystem.Instance != null && dropTable != null)
        {
            DropSystem.Instance.SpawnDrop(transform.position, dropTable);
        }

        // 延迟销毁
        Destroy(gameObject, 5f);
    }
    #endregion

    #region 辅助方法
    // 伤害计算（复用/补充，和玩家一致）
    private float CalculateDamage(float attack, float defense)
    {
        return Mathf.Max(1, attack - defense); // 保底1点伤害
    }

    // 动画播放（原有逻辑+容错）
    public void PlayAttackAnim() => animator?.SetTrigger("Attack");
    public void PlaySkillAnim() => animator?.SetTrigger("Skill");

    // 行为树辅助判断
    public bool CanNormalAttack()
    {
        if (isDead || attackTarget == null) return false;

        float currentAP = GetAttrValue(AttributeType.CurrentAP);
        float distance = Vector3.Distance(transform.position, attackTarget.position);

        return currentAP >= normalAttackCost && distance <= attackRange;
    }

    public bool CanCastSkill()
    {
        if (isDead || attackTarget == null || skillCoolDownLeft > 0) return false;

        float currentAP = GetAttrValue(AttributeType.CurrentAP);
        float distance = Vector3.Distance(transform.position, attackTarget.position);

        return currentAP >= skillAttackCost && distance <= skillRange;
    }

    public void SetAttackTarget(Transform target)
    {
        attackTarget = target;
    }
    #endregion
}