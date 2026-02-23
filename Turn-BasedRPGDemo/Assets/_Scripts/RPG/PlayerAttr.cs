using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 玩家属性子类（无编译错误，继承基类所有功能）
/// </summary>
public class PlayerAttr : BaseCharacterAttr
{
    [Header("===== 玩家初始属性 =====")]
    public float initMaxHP = 100;
    public float initMaxMP = 50;
    public float initMaxAP = 10;
    public float initStrength = 8;
    public float initIntelligence = 5;
    public float initArmor = 3;

    // ========== 新增：玩家战斗配置字段（和EnemyAttr保持一致） ==========
    [Header("===== 行动消耗规则 =====")]
    [Tooltip("每移动1单位消耗的行动点")]
    public float moveCostPerUnit = 1f;

    [Header("===== 战斗配置 =====")]
    [Tooltip("普攻射程")]
    public float attackRange = 2f; // ✅ 新增：普攻射程
    [Tooltip("技能射程")]
    public float skillRange = 5f; // ✅ 新增：技能射程
    [Tooltip("普攻消耗行动点")]
    public float normalAttackCost = 2f; // ✅ 新增：普攻消耗
    [Tooltip("技能消耗行动点")]
    public float skillAttackCost = 3f; // ✅ 新增：技能消耗

    [HideInInspector] public Animator animator;

    private void Awake()
    {
        // 组件容错：避免未挂载Animator报错
        if (TryGetComponent<Animator>(out Animator anim))
        {
            animator = anim;
        }

        // 初始化属性（调用基类方法）
        InitAttribute(initMaxHP, initMaxMP, initMaxAP, initStrength, initIntelligence, initArmor);
        currentCamp = CampType.Player;
    }

    // 经验+升级逻辑
    public void AddEXP(float expValue)
    {
        if (expValue <= 0 || isDead) return; // 死亡后无法升级
        AddAttrValue(AttributeType.CurrentEXP, expValue);
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        float curEXP = GetAttrValue(AttributeType.CurrentEXP);
        float needEXP = GetAttrValue(AttributeType.EXPToLevelUp);

        if (curEXP >= needEXP)
        {
            // 升级属性成长
            SetAttrValue(AttributeType.Level, GetAttrValue(AttributeType.Level) + 1);
            SetAttrValue(AttributeType.CurrentEXP, curEXP - needEXP);
            SetAttrValue(AttributeType.EXPToLevelUp, needEXP * 1.5f);

            // 升级属性加成
            AddAttrValue(AttributeType.MaxHP, 20);
            AddAttrValue(AttributeType.MaxMP, 10);
            AddAttrValue(AttributeType.MaxAP, 2);
            AddAttrValue(AttributeType.Strength, 2);
            AddAttrValue(AttributeType.Armor, 1);

            // 满血+满AP（确保MaxHP更新后CurrentHP生效）
            HealHP(GetAttrValue(AttributeType.MaxHP));
            RecoverFullAP();

            // 调用基类战力方法（无报错）
            Debug.Log($"玩家升级！等级：{GetAttrValue(AttributeType.Level)}，战力：{GetCombatPower()}");
        }
    }

    // 重写结束个人回合（补充玩家专属逻辑）
    public override void EndPersonalTurn()
    {
        base.EndPersonalTurn(); // 调用基类核心逻辑
        // 玩家专属：停止移动
        if (TryGetComponent<NavMeshAgent>(out NavMeshAgent agent))
        {
            agent.isStopped = true;
        }
    }

    // 玩家死亡重写
    public override void Die()
    {
        base.Die();
        Debug.Log("玩家死亡！");

        // NavMeshAgent容错
        if (TryGetComponent<NavMeshAgent>(out NavMeshAgent agent))
        {
            agent.enabled = false;
        }
    }

    // 动画播放（容错）
    public void PlayAttackAnim() => animator?.SetTrigger("Attack");
    public void PlaySkillAnim() => animator?.SetTrigger("Skill");
}