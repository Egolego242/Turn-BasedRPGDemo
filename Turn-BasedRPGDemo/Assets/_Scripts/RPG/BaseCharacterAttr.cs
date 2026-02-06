using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// 角色属性基类（所有角色共用，消除字段缺失错误）
/// </summary>
public class BaseCharacterAttr : MonoBehaviour
{
    // 通用战斗状态字段（解决isMyTurn/isInBattle/isDead报错）
    [HideInInspector] public bool isInBattle;
    [HideInInspector] public bool isMyTurn;
    [HideInInspector] public bool isDead;

    // 核心属性字典（合并管理）
    protected Dictionary<AttributeType, float> attrDic = new Dictionary<AttributeType, float>();
    [Header("===== 阵营配置 =====")]
    public CampType currentCamp;

    // 初始化属性（所有角色通用）
    protected void InitAttribute(float maxHP, float maxMP, float maxAP, float strength, float intelligence, float armor)
    {
        // 初始化状态字段
        isInBattle = false;
        isMyTurn = false;
        isDead = false;

        // 清空并初始化字典
        attrDic?.Clear();
        attrDic ??= new Dictionary<AttributeType, float>();

        // 基础属性
        SetAttrValue(AttributeType.MaxHP, maxHP);
        SetAttrValue(AttributeType.CurrentHP, maxHP);
        SetAttrValue(AttributeType.MaxMP, maxMP);
        SetAttrValue(AttributeType.CurrentMP, maxMP);
        SetAttrValue(AttributeType.MaxPhysArmor, 0);
        SetAttrValue(AttributeType.CurrentPhysArmor, 0);
        SetAttrValue(AttributeType.MaxMagicArmor, 0);
        SetAttrValue(AttributeType.CurrentMagicArmor, 0);

        // 战斗属性
        SetAttrValue(AttributeType.MaxAP, maxAP);
        SetAttrValue(AttributeType.CurrentAP, maxAP);
        SetAttrValue(AttributeType.Strength, strength);
        SetAttrValue(AttributeType.Intelligence, intelligence);
        SetAttrValue(AttributeType.Armor, armor);
        SetAttrValue(AttributeType.MagicResist, 0);

        // 成长属性
        SetAttrValue(AttributeType.Level, 1);
        SetAttrValue(AttributeType.CurrentEXP, 0);
        SetAttrValue(AttributeType.EXPToLevelUp, 100);
    }

    // 战力计算（解决GetCombatPower报错）
    public float GetCombatPower()
    {
        float strength = GetAttrValue(AttributeType.Strength);
        float intelligence = GetAttrValue(AttributeType.Intelligence);
        float armor = GetAttrValue(AttributeType.Armor);
        float magicResist = GetAttrValue(AttributeType.MagicResist);
        return (strength * 2) + (intelligence * 2) + armor + magicResist;
    }

    // 属性操作核心方法（所有子类共用）
    public void SetAttrValue(AttributeType type, float value)
    {
        attrDic ??= new Dictionary<AttributeType, float>();
        if (attrDic.ContainsKey(type)) attrDic[type] = value;
        else attrDic.Add(type, value);
        ValueProtect(type);
    }

    public float GetAttrValue(AttributeType type)
    {
        if (attrDic == null || !attrDic.ContainsKey(type)) return 0;
        return attrDic[type];
    }

    public void AddAttrValue(AttributeType type, float addValue)
    {
        if (attrDic == null) return;
        SetAttrValue(type, GetAttrValue(type) + addValue);
    }

    // 属性值边界防护
    private void ValueProtect(AttributeType type)
    {
        if (attrDic == null || !attrDic.ContainsKey(type)) return;

        switch (type)
        {
            case AttributeType.CurrentHP:
                attrDic[type] = Mathf.Clamp(attrDic[type], 0, GetAttrValue(AttributeType.MaxHP));
                break;
            case AttributeType.CurrentMP:
                attrDic[type] = Mathf.Clamp(attrDic[type], 0, GetAttrValue(AttributeType.MaxMP));
                break;
            case AttributeType.CurrentAP:
                attrDic[type] = Mathf.Clamp(attrDic[type], 0, GetAttrValue(AttributeType.MaxAP));
                break;
            case AttributeType.Level:
            case AttributeType.CurrentEXP:
                attrDic[type] = Mathf.Max(attrDic[type], 0);
                break;
            default:
                break;
        }
    }

    // 通用行为方法
    public virtual void TakeDamage(float damage)
    {
        if (attrDic == null || isDead) return;
        float finalDamage = Mathf.Max(damage - GetAttrValue(AttributeType.Armor), 1);
        AddAttrValue(AttributeType.CurrentHP, -finalDamage);

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Hurt");

        if (Mathf.Approximately(GetAttrValue(AttributeType.CurrentHP), 0))
        {
            isDead = true;
            Die();
        }
    }

    public virtual void Die()
    {
        isDead = true;
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Die");
    }

    public void HealHP(float healValue)
    {
        if (attrDic == null || healValue <= 0 || isDead) return;
        AddAttrValue(AttributeType.CurrentHP, healValue);
    }

    public void HealMP(float healValue)
    {
        if (attrDic == null || healValue <= 0 || isDead) return;
        AddAttrValue(AttributeType.CurrentMP, healValue);
    }

    public bool ConsumeAP(float costValue)
    {
        if (attrDic == null || costValue <= 0 || isDead) return false;
        if (GetAttrValue(AttributeType.CurrentAP) >= costValue)
        {
            AddAttrValue(AttributeType.CurrentAP, -costValue);
            return true;
        }
        return false;
    }

    public void RecoverAP(float recoverValue)
    {
        if (attrDic == null || recoverValue <= 0 || isDead) return;
        AddAttrValue(AttributeType.CurrentAP, recoverValue);
    }

    public void RecoverFullAP()
    {
        if (attrDic == null || isDead) return;
        SetAttrValue(AttributeType.CurrentAP, GetAttrValue(AttributeType.MaxAP));
    }

    // 阵营判断
    public bool IsEnemy(BaseCharacterAttr targetAttr)
    {
        if (targetAttr == null) return false;
        return (currentCamp == CampType.Player && targetAttr.currentCamp == CampType.Enemy) ||
               (currentCamp == CampType.Enemy && targetAttr.currentCamp == CampType.Player);
    }

    public bool IsAlly(BaseCharacterAttr targetAttr)
    {
        if (targetAttr == null) return false;
        return currentCamp == targetAttr.currentCamp && currentCamp != CampType.Neutral;
    }
}