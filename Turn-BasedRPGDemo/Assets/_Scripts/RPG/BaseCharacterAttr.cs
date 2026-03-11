using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 角色属性基类（所有角色共用，消除字段缺失错误）
/// </summary>
public class BaseCharacterAttr : MonoBehaviour
{
    // 通用战斗状态字段（解决isMyTurn/isInBattle/isDead报错）
    [HideInInspector] public bool isInBattle;
    [HideInInspector] public bool isMyTurn;
    [HideInInspector] public bool isDead;

    // 回合制核心字段
    [Header("===== 回合制核心 =====")]
    public float initiative = 5f; // 先攻值（越高行动越靠前）
    [HideInInspector] public bool hasActInRound = false; // 本回合是否已行动（防止重复行动）

    // 核心属性字典（合并管理）
    protected Dictionary<AttributeType, float> attrDic;
    [Header("===== 阵营配置 =====")]
    public CampType currentCamp;


    protected virtual void Awake()
    {
        // 父类原有初始化逻辑（比如属性初始化、组件获取等）
    }

    // 初始化属性（所有角色通用）
    protected void InitAttribute(float maxHP, float maxMP, int maxAP, float strength, float intelligence, float armor)
    {
        // 初始化状态字段
        isInBattle = false;
        isMyTurn = false;
        isDead = false;
        hasActInRound = false;

        // 安全初始化字典（避免空引用）
        attrDic = attrDic ?? new Dictionary<AttributeType, float>();
        attrDic.Clear();

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
        SetAttrValue(AttributeType.CurrentAP, 0); // 初始行动点0，战斗开始时赋值4
        SetAttrValue(AttributeType.Strength, strength);
        SetAttrValue(AttributeType.Intelligence, intelligence);
        SetAttrValue(AttributeType.Armor, armor);
        SetAttrValue(AttributeType.MagicResist, 0);

        // 成长属性
        SetAttrValue(AttributeType.Level, 1);
        SetAttrValue(AttributeType.CurrentEXP, 0);
        SetAttrValue(AttributeType.EXPToLevelUp, 100);

        // 先攻值存入字典（可选）
        SetAttrValue(AttributeType.Initiative, initiative);
    }

    // 先攻值快捷访问
    public float GetInitiative() => initiative;
    public void SetInitiative(float value) => initiative = Mathf.Max(1, value); // 保底1

    // 战力计算（解决GetCombatPower报错）
    public float GetCombatPower()
    {
        float strength = GetAttrValue(AttributeType.Strength);
        float intelligence = GetAttrValue(AttributeType.Intelligence);
        float armor = GetAttrValue(AttributeType.Armor);
        float magicResist = GetAttrValue(AttributeType.MagicResist);
        return (strength * 2) + (intelligence * 2) + armor + magicResist;
    }

    #region 属性操作核心方法（所有子类共用）
    public void SetAttrValue(AttributeType type, float value)
    {
        attrDic = attrDic ?? new Dictionary<AttributeType, float>(); // 懒加载字典
        if (attrDic.ContainsKey(type))
            attrDic[type] = value;
        else
            attrDic.Add(type, value);

        ValueProtect(type);
    }

    // 新增：获取int类型的属性值（专用于AP）
    public int GetAttrIntValue(AttributeType type)
    {
        attrDic = attrDic ?? new Dictionary<AttributeType, float>();
        return attrDic.TryGetValue(type, out float val) ? Mathf.RoundToInt(val) : 0;
    }


    public float GetAttrValue(AttributeType type)
    {
        attrDic = attrDic ?? new Dictionary<AttributeType, float>(); // 兜底初始化
        return attrDic.TryGetValue(type, out float val) ? val : 0; // 更安全的取值方式
    }

    public void AddAttrValue(AttributeType type, float addValue)
    {
        SetAttrValue(type, GetAttrValue(type) + addValue);
    }

    // 新增：int类型属性增加值（专用于AP）
    public void AddAttrIntValue(AttributeType type, int addValue)
    {
        SetAttrValue(type, GetAttrIntValue(type) + addValue);
    }

    // 属性值边界防护
    private void ValueProtect(AttributeType type)
    {
        if (!attrDic.ContainsKey(type)) return;

        switch (type)
        {
            case AttributeType.CurrentHP:
                attrDic[type] = Mathf.Clamp(attrDic[type], 0, GetAttrValue(AttributeType.MaxHP));
                break;
            case AttributeType.CurrentMP:
                attrDic[type] = Mathf.Clamp(attrDic[type], 0, GetAttrValue(AttributeType.MaxMP));
                break;
            case AttributeType.CurrentAP:
                // AP强制转为int后再限制边界
                int intVal = Mathf.RoundToInt(attrDic[type]);
                intVal = Mathf.Clamp(intVal, 0, GetAttrIntValue(AttributeType.MaxAP));
                attrDic[type] = intVal;
                break;
            case AttributeType.Level:
            case AttributeType.CurrentEXP:
                attrDic[type] = Mathf.Max(attrDic[type], 0);
                break;
            default:
                break;
        }
    }
    #endregion

    #region 通用行为方法
    // 受伤方法（所有角色通用）
    public virtual void TakeDamage(float damage)
    {
        if (isDead) return; // 提前判死，减少无效逻辑
        float finalDamage = Mathf.Max(damage - GetAttrValue(AttributeType.Armor), 1);
        AddAttrValue(AttributeType.CurrentHP, -finalDamage);

        // 动画容错：先判断组件是否存在
        if (TryGetComponent<Animator>(out Animator anim))
        {
            anim.SetTrigger("Hurt");
        }

        // 死亡判断：优化浮点精度问题（<=0 比Approximately更可靠）
        if (GetAttrValue(AttributeType.CurrentHP) <= 0.01f)
        {
            isDead = true;
            Die();
        }
    }

    // 死亡
    public virtual void Die()
    {
        isDead = true;
        if (TryGetComponent<Animator>(out Animator anim))
        {
            anim.SetTrigger("Die");
        }
    }

    // 治疗方法（所有角色通用）
    public void HealHP(float healValue)
    {
        if (healValue <= 0 || isDead) return;
        AddAttrValue(AttributeType.CurrentHP, healValue);
    }

    // 治疗法术方法（所有角色通用）
    public void HealMP(float healValue)
    {
        if (healValue <= 0 || isDead) return;
        AddAttrValue(AttributeType.CurrentMP, healValue);
    }

    // 消耗行动点
    public bool ConsumeAP(int costValue)
    {
        if (costValue <= 0 || isDead) return false;
        int currentAP = GetAttrIntValue(AttributeType.CurrentAP);
        if (currentAP >= costValue)// 行动点不足时，不消耗
        {
            AddAttrIntValue(AttributeType.CurrentAP, -costValue);
            TurnBattleManager.TriggerActionPointChanged();
            Debug.Log($"{gameObject.name} 扣减AP：{currentAP} → {currentAP - costValue}", this);
            return true;
        }
        Debug.LogWarning($"{gameObject.name} AP不足：当前{currentAP}，需要{costValue}", this);
        return false;
    }

    // 恢复行动点
    public void RecoverAP(int recoverValue)
    {
        if (recoverValue <= 0 || isDead) return;
        AddAttrIntValue(AttributeType.CurrentAP, recoverValue);
        TurnBattleManager.TriggerActionPointChanged();
    }

    // 恢复满行动点
    public void RecoverFullAP()
    {
        if (isDead) return;
        SetAttrValue(AttributeType.CurrentAP, GetAttrIntValue(AttributeType.MaxAP));
        TurnBattleManager.TriggerActionPointChanged();
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

    // ===== 核心修复：基类新增EndPersonalTurn虚方法 =====
    /// <summary>
    /// 结束个人回合（基类虚方法，子类可重写）
    /// </summary>
    public virtual void EndPersonalTurn()
    {
        hasActInRound = true; // 标记本回合已行动，不再重复行动
        isMyTurn = false;     // 取消当前回合标记
    }
    #endregion
}