using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 通用角色属性基类 - 玩家/敌人全部继承此类
/// ✅ 修复：字典初始化容错+空引用判空+数值保护增强
/// ✅ 核心功能不变：属性管理、扣血、回血、行动点消耗/恢复、阵营判定
/// </summary>
public class BaseCharacterAttr : MonoBehaviour
{
    // 核心：字典存储所有属性，初始化时直接创建空字典，防止空引用
    protected Dictionary<AttributeType, float> attrDic = new Dictionary<AttributeType, float>();

    [Header("===== 阵营属性 =====")]
    public CampType currentCamp; // 阵营类型

    #region 初始化属性（子类调用，给所有属性赋初始值）
    protected void InitAttribute(float maxHP, float maxMP, float maxAP, float strength, float intelligence, float armor)
    {
        // 先清空字典，防止重复赋值
        attrDic.Clear();
        // 基础属性赋值（满状态）
        SetAttrValue(AttributeType.MaxHP, maxHP);
        SetAttrValue(AttributeType.CurrentHP, 20);
        SetAttrValue(AttributeType.MaxMP, maxMP);
        SetAttrValue(AttributeType.CurrentMP, maxMP);
        SetAttrValue(AttributeType.MaxAP, maxAP);
        SetAttrValue(AttributeType.CurrentAP, maxAP);
        // 战斗属性赋值
        SetAttrValue(AttributeType.Strength, strength);
        SetAttrValue(AttributeType.Intelligence, intelligence);
        SetAttrValue(AttributeType.Armor, armor);
        SetAttrValue(AttributeType.MagicResist, 0);
        // 成长属性默认值
        SetAttrValue(AttributeType.Level, 1);
        SetAttrValue(AttributeType.CurrentEXP, 0);
        SetAttrValue(AttributeType.EXPToLevelUp, 100);
    }
    #endregion

    #region 核心属性操作方法（✅ 修复：字典空键容错+判空，彻底解决KeyNotFoundException）
    public void SetAttrValue(AttributeType type, float value)
    {
        if (attrDic == null) attrDic = new Dictionary<AttributeType, float>();
        if (attrDic.ContainsKey(type))
        {
            attrDic[type] = value;
        }
        else
        {
            attrDic.Add(type, value);
        }
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
        float curValue = GetAttrValue(type);
        SetAttrValue(type, curValue + addValue);
    }

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
            default:
                break;
        }
    }
    #endregion

    #region 通用行为方法（✅ 修复：全方法加判空，防止空引用）
    public virtual void TakeDamage(float damage)
    {
        if (attrDic == null) return;
        float finalDamage = Mathf.Max(damage - GetAttrValue(AttributeType.Armor), 1);
        AddAttrValue(AttributeType.CurrentHP, -finalDamage);
        // 播放受击动画前先判空Animator
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Hurt");
        if (GetAttrValue(AttributeType.CurrentHP) <= 0)
        {
            Die();
        }
    }

    public void HealHP(float healValue)
    {
        if (attrDic == null) return;
        AddAttrValue(AttributeType.CurrentHP, healValue);
    }

    public void HealMP(float healValue)
    {
        if (attrDic == null) return;
        AddAttrValue(AttributeType.CurrentMP, healValue);
    }

    public bool ConsumeAP(float costValue)
    {
        if (attrDic == null || costValue <= 0) return false;
        if (GetAttrValue(AttributeType.CurrentAP) >= costValue)
        {
            AddAttrValue(AttributeType.CurrentAP, -costValue);
            return true;
        }
        return false;
    }

    public void RecoverAP(float recoverValue)
    {
        if (attrDic == null) return;
        AddAttrValue(AttributeType.CurrentAP, recoverValue);
    }

    public void RecoverFullAP()
    {
        if (attrDic == null) return;
        SetAttrValue(AttributeType.CurrentAP, GetAttrValue(AttributeType.MaxAP));
    }

    public virtual void Die()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Die");
    }
    #endregion

    #region 阵营判定方法（✅ 修复：参数判空，防止传入null）
    public bool IsEnemy(BaseCharacterAttr targetAttr)
    {
        if (targetAttr == null) return false;
        return (this.currentCamp == CampType.Player && targetAttr.currentCamp == CampType.Enemy) ||
               (this.currentCamp == CampType.Enemy && targetAttr.currentCamp == CampType.Player);
    }

    public bool IsAlly(BaseCharacterAttr targetAttr)
    {
        if (targetAttr == null) return false;
        return this.currentCamp == targetAttr.currentCamp && this.currentCamp != CampType.Neutral;
    }
    #endregion
}

public enum CampType
{
    Player,
    Enemy,
    Neutral
}
