using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 技能基类
/// </summary>
 [CreateAssetMenu(fileName = "Skill", menuName = "战斗系统/技能")]
public class SkillBase : ScriptableObject
{
    public string skillName;
    public int skillID;

    [Header("技能描述")]
    [Tooltip("为空时自动生成，填写后优先使用")]
    [TextArea(2, 5)]
    public string description;
    public int apCost;
    public float mpCost;
    public int coolDownRound;
    public float effectValue;
    public SkillType skillType;
    public TargetType targetType;
    public string animTriggerName;
    public GameObject skillEffectPrefab;
    public Transform effectSpawnPoint;

    // ========== 新增：技能射程字段（供RangeVisualizer使用） ==========
    [Header("技能范围配置")]
    [Tooltip("技能释放射程（米）")]
    public float skillRange = 5f; // ✅ 新增：技能射程

    [System.NonSerialized] public int currentCoolDown;

    // 释放技能
    public virtual bool CastSkill(BaseCharacterAttr caster, BaseCharacterAttr target)
    {
        // 冷却+资源校验（先校验，再扣AP，避免退款失败）
        if (currentCoolDown > 0)
        {
            Debug.Log($"技能 {skillName} 处于冷却中");
            return false;
        }
        if (caster.GetAttrValue(AttributeType.CurrentMP) < mpCost)
        {
            Debug.Log($"技能 {skillName} MP不足：需要{mpCost}，当前{caster.GetAttrValue(AttributeType.CurrentMP)}");
            return false;
        }
        // apCost>0时才校验+扣减，0消耗技能直接跳过
        if (apCost > 0 && !caster.ConsumeAP(apCost))
        {
            Debug.Log($"技能 {skillName} AP不足：需要{apCost}");
            return false;
        }

        // 消耗MP
        caster.AddAttrValue(AttributeType.CurrentMP, -mpCost);

        // 执行效果
        ExecuteEffect(caster, target);

        // 动画+特效
        TriggerAnimAndEffect(caster);

        // 冷却
        currentCoolDown = coolDownRound;

        return true;
    }

    // 执行技能效果
    protected virtual void ExecuteEffect(BaseCharacterAttr caster, BaseCharacterAttr target)
    {
        if (target == null) return;
        switch (skillType)
        {
            case SkillType.Attack:
                target.TakeDamage(effectValue + caster.GetAttrValue(AttributeType.Strength) * 0.5f);
                break;
            case SkillType.Heal:
                target.HealHP(effectValue + caster.GetAttrValue(AttributeType.Intelligence) * 0.3f);
                break;
            case SkillType.Buff:
                target.AddAttrValue(AttributeType.Strength, effectValue);
                break;
        }
    }

    // 触发动画+特效
    protected virtual void TriggerAnimAndEffect(BaseCharacterAttr caster)
    {
        // 动画：填了自定义trigger则用它，否则播默认"Skill"
        if (caster is PlayerAttr player)
        {
            if (!string.IsNullOrEmpty(animTriggerName))
                player.animator?.SetTrigger(animTriggerName);
            else
                player.PlaySkillAnim();
        }
        else if (caster is EnemyAttr enemy)
        {
            if (!string.IsNullOrEmpty(animTriggerName))
                enemy.animator?.SetTrigger(animTriggerName);
            else
                enemy.PlaySkillAnim();
        }

        // 特效
        if (skillEffectPrefab != null && effectSpawnPoint != null)
        {
            GameObject effect = Instantiate(skillEffectPrefab, effectSpawnPoint.position, effectSpawnPoint.rotation);
            Destroy(effect, 2f);
        }
    }

    // 刷新冷却
    public void RefreshCoolDown() => currentCoolDown = Mathf.Max(currentCoolDown - 1, 0);
}

// 技能类型
public enum SkillType { Attack, Heal, Buff }
// 目标类型
public enum TargetType { Self, Enemy, Ally }

/// <summary>
/// 技能管理器
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;
    private Dictionary<BaseCharacterAttr, List<SkillBase>> characterSkills = new Dictionary<BaseCharacterAttr, List<SkillBase>>();

    private void Awake() => Instance = this;

    // 添加技能
    public void AddSkillToCharacter(BaseCharacterAttr character, SkillBase skill)
    {
        if (!characterSkills.ContainsKey(character))
            characterSkills.Add(character, new List<SkillBase>());
        characterSkills[character].Add(skill);
    }

    // 释放技能
    public bool CastCharacterSkill(BaseCharacterAttr caster, int skillID, BaseCharacterAttr target)
    {
        if (!characterSkills.ContainsKey(caster))
        {
            Debug.LogError($"[SkillManager] 角色 {caster?.name} 没有注册任何技能！请检查PlayerAttr的defaultSkills列表");
            return false;
        }
        SkillBase skill = characterSkills[caster].Find(s => s.skillID == skillID);
        if (skill == null)
        {
            Debug.LogError($"[SkillManager] 角色 {caster.name} 未找到skillID={skillID}的技能！已注册：{string.Join(", ", characterSkills[caster].Select(s => $"{s.skillName}(ID:{s.skillID})"))}");
            return false;
        }
        return skill.CastSkill(caster, target);
    }

    // 刷新所有冷却
    public void RefreshAllSkillCoolDown()
    {
        foreach (var kvp in characterSkills)
        {
            foreach (var skill in kvp.Value)
            {
                skill.RefreshCoolDown();
            }
        }
    }
}