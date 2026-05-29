using System.Collections.Generic;

/// <summary>
/// 角色属性类型枚举（基础/战斗/成长属性，统一存储）
/// </summary>
public enum AttributeType
{
    // ===== 基础属性：生命/法力/物理/魔法护甲 =====
    MaxHP,       // 最大生命值
    CurrentHP,   // 当前生命值
    MaxMP,       // 最大法力值
    CurrentMP,   // 当前法力值
    MaxPhysArmor,// 最大物理护甲
    CurrentPhysArmor, // 当前物理护甲（扩展字段）
    MaxMagicArmor,// 最大魔法护甲
    CurrentMagicArmor, // 当前魔法护甲（扩展字段）
    // ===== 战斗属性：行动点/力量/智力/防御/先攻 =====
    MaxAP,       // 最大行动点
    CurrentAP,   // 当前行动点
    Strength,    // 力量（物理伤害加成）
    Intelligence,// 智力（魔法伤害加成）
    Armor,       // 物理防御
    MagicResist, // 魔法抗性
    Initiative,  // 先攻值（新增：解决CS0117报错）
    // ===== 成长属性：等级/经验值 =====
    Level,       // 等级
    CurrentEXP,  // 当前经验值
    EXPToLevelUp,// 升级所需经验
}

/// <summary>
/// 属性分组静态类（逻辑分类，方便批量处理）
/// </summary>
public static class AttrGroup
{
    // 基础属性：生命/法力/护甲相关
    public static readonly IReadOnlyList<AttributeType> BasicAttrs = new List<AttributeType>()
    {
        AttributeType.MaxHP, AttributeType.CurrentHP,
        AttributeType.MaxMP, AttributeType.CurrentMP,
        AttributeType.MaxPhysArmor, AttributeType.CurrentPhysArmor,
        AttributeType.MaxMagicArmor, AttributeType.CurrentMagicArmor
    }.AsReadOnly();

    // 战斗属性：行动点/攻击力/防御/先攻相关
    public static readonly IReadOnlyList<AttributeType> CombatAttrs = new List<AttributeType>()
    {
        AttributeType.MaxAP, AttributeType.CurrentAP,
        AttributeType.Strength, AttributeType.Intelligence,
        AttributeType.Armor, AttributeType.MagicResist,
        AttributeType.Initiative // 新增：先攻值加入战斗属性分组
    }.AsReadOnly();

    // 成长属性：等级/经验相关
    public static readonly IReadOnlyList<AttributeType> GrowAttrs = new List<AttributeType>()
    {
        AttributeType.Level, AttributeType.CurrentEXP, AttributeType.EXPToLevelUp
    }.AsReadOnly();
}

/// <summary>
/// 阵营类型枚举
/// </summary>
public enum CampType
{
    Player,   // 玩家阵营
    Enemy,    // 敌人阵营
    Neutral   // 中立阵营
}