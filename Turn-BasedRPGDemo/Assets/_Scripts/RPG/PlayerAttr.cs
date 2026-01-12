using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 玩家角色属性子类 - 继承通用基类
/// ✅ 修复：Animator组件判空+升级逻辑容错+死亡逻辑防报错
/// ✅ 核心功能不变：经验升级、属性成长、玩家死亡逻辑
/// </summary>
public class PlayerAttr : BaseCharacterAttr
{
    [Header("===== 玩家初始属性配置 =====")]
    public float initMaxHP = 100;
    public float initMaxMP = 50;
    public float initMaxAP = 10;
    public float initStrength = 8;
    public float initIntelligence = 5;
    public float initArmor = 3;

    [HideInInspector] public Animator animator;

    private void Awake()
    {
        // ✅ 修复：提前获取Animator组件，防止后续调用报空
        animator = GetComponent<Animator>();
        // 初始化玩家属性
        InitAttribute(initMaxHP, initMaxMP, initMaxAP, initStrength, initIntelligence, initArmor);
        currentCamp = CampType.Player;
    }

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
            float level = GetAttrValue(AttributeType.Level);
            SetAttrValue(AttributeType.Level, level + 1);
            SetAttrValue(AttributeType.CurrentEXP, curEXP - needEXP);
            SetAttrValue(AttributeType.EXPToLevelUp, needEXP * 1.5f);

            AddAttrValue(AttributeType.MaxHP, 20);
            AddAttrValue(AttributeType.MaxMP, 10);
            AddAttrValue(AttributeType.MaxAP, 2);
            AddAttrValue(AttributeType.Strength, 2);
            AddAttrValue(AttributeType.Armor, 1);
            HealHP(GetAttrValue(AttributeType.MaxHP));
            RecoverFullAP();
            Debug.Log("玩家升级！当前等级：" + (level + 1));
        }
    }

    public override void Die()
    {
        base.Die();
        Debug.Log("玩家死亡！游戏失败/回城复活");
        // ✅ 修复：禁用移动组件，防止死亡后还能移动
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
    }

    // 动画播放方法+判空
    public void PlayAttackAnim()
    {
        if (animator != null) animator.SetTrigger("Attack");
    }
    public void PlaySkillAnim()
    {
        if (animator != null) animator.SetTrigger("Skill");
    }
}