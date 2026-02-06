using System.Collections.Generic;

/// <summary>
/// 角色属性类型枚举（基础/战斗/成长拆分逻辑分组，物理合并存储）
/// </summary>
public enum AttributeType
{
    // ===== 基础属性（生命/法力/护甲基础值）=====
    MaxHP,       // 最大生命值
    CurrentHP,   // 当前生命值
    MaxMP,       // 最大法力值
    CurrentMP,   // 当前法力值
    MaxPhysArmor,
    CurrentPhysArmor, // 物理护甲（扩展字段）
    MaxMagicArmor,
    CurrentMagicArmor, // 魔法护甲（扩展字段）
    // ===== 战斗属性（行动/攻击/防御）=====
    MaxAP,       // 最大行动点
    CurrentAP,   // 当前行动点
    Strength,    // 力量（物理攻击加成）
    Intelligence,// 智力（魔法攻击加成）
    Armor,       // 物理防御
    MagicResist, // 魔法抗性
    // ===== 成长属性（等级/经验）=====
    Level,       // 等级
    CurrentEXP,  // 当前经验值
    EXPToLevelUp,// 升级所需经验
}

/// <summary>
/// 属性分组静态类（逻辑区分，不拆分物理存储）
/// </summary>
public static class AttrGroup
{
    // 基础属性组：生命/法力/基础护甲相关
    public static readonly List<AttributeType> BasicAttrs = new List<AttributeType>()
    {
        AttributeType.MaxHP, AttributeType.CurrentHP,
        AttributeType.MaxMP, AttributeType.CurrentMP,
        AttributeType.MaxPhysArmor, AttributeType.CurrentPhysArmor,
        AttributeType.MaxMagicArmor, AttributeType.CurrentMagicArmor
    };

    // 战斗属性组：行动点/攻击/防御相关
    public static readonly List<AttributeType> CombatAttrs = new List<AttributeType>()
    {
        AttributeType.MaxAP, AttributeType.CurrentAP,
        AttributeType.Strength, AttributeType.Intelligence,
        AttributeType.Armor, AttributeType.MagicResist
    };

    // 成长属性组：等级/经验相关
    public static readonly List<AttributeType> GrowAttrs = new List<AttributeType>()
    {
        AttributeType.Level, AttributeType.CurrentEXP, AttributeType.EXPToLevelUp
    };
}

/// <summary>
/// 阵营类型枚举
/// </summary>
public enum CampType
{
    Player,
    Enemy,
    Neutral
}