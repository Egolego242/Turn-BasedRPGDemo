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

    [HideInInspector] public Animator animator;

    private void Awake()
    {
        // 初始化组件+属性
        animator = GetComponent<Animator>();
        InitAttribute(initMaxHP, initMaxMP, initMaxAP, initStrength, initIntelligence, initArmor);
        currentCamp = CampType.Player;
    }

    // 经验+升级逻辑
    public void AddEXP(float expValue)
    {
        if (expValue <= 0) return;
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

            AddAttrValue(AttributeType.MaxHP, 20);
            AddAttrValue(AttributeType.MaxMP, 10);
            AddAttrValue(AttributeType.MaxAP, 2);
            AddAttrValue(AttributeType.Strength, 2);
            AddAttrValue(AttributeType.Armor, 1);

            // 满血+满AP
            HealHP(GetAttrValue(AttributeType.MaxHP));
            RecoverFullAP();

            // 调用基类战力方法（无报错）
            Debug.Log($"玩家升级！战力：{GetCombatPower()}");
        }
    }

    // 玩家死亡重写
    public override void Die()
    {
        base.Die();
        Debug.Log("玩家死亡！");

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
    }

    // 动画播放（容错）
    public void PlayAttackAnim() => animator?.SetTrigger("Attack");
    public void PlaySkillAnim() => animator?.SetTrigger("Skill");
}