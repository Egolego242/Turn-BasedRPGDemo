/// <summary>
/// 所有角色的属性类型枚举（核心扩展点！后续加属性，直接在这里新增枚举值即可，无任何其他修改）
/// 分类管理：基础生存属性 | 战斗核心属性 | 行动属性 | 成长属性
/// </summary>
public enum AttributeType
{
    // ===== 基础生存属性（必选）=====
    MaxHP,       // 最大生命值
    CurrentHP,   // 当前生命值
    MaxMP,       // 最大法力值
    CurrentMP,   // 当前法力值
    MaxPhysArmor, CurrentPhysArmor, // 物理护甲（可扣减，核心显示）
    MaxMagicArmor, CurrentMagicArmor, // 魔法护甲（可扣减，核心显示）
    // ===== 行动核心属性（你的核心需求：探索无消耗，战斗消耗）=====
    MaxAP,       // 最大行动点（战斗移动/技能的核心消耗）
    CurrentAP,   // 当前行动点
    // ===== 战斗伤害属性（必选）=====
    Strength,    // 力量 → 物理攻击力加成
    Intelligence,// 智力 → 法术攻击力加成
    Armor,       // 护甲 → 物理伤害减免
    MagicResist, // 魔抗 → 法术伤害减免
    // ===== 成长属性（玩家专属，敌人可留空）=====
    Level,       // 等级
    CurrentEXP,  // 当前经验值
    EXPToLevelUp,// 升级所需经验
    // ===== 【扩展预留】后续想加的属性，直接在这里加即可！ =====
    // Agility,    // 敏捷 → 行动点恢复速度/闪避率
    // CritRate,   // 暴击率
    // DodgeRate,  // 闪避率
    // Luck,       // 幸运 → 掉落率提升
}