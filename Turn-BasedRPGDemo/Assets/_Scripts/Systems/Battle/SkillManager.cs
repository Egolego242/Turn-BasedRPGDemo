using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 技能基类
/// </summary>
public class SkillBase : ScriptableObject
{
    public string skillName;
    public int skillID;
    public float apCost;
    public float mpCost;
    public int coolDownRound;
    public float effectValue;
    public SkillType skillType;
    public TargetType targetType;
    public string animTriggerName;
    public GameObject skillEffectPrefab;
    public Transform effectSpawnPoint;

    [HideInInspector] public int currentCoolDown;

    // 释放技能
    public virtual bool CastSkill(BaseCharacterAttr caster, BaseCharacterAttr target)
    {
        // 冷却+资源校验
        if (currentCoolDown > 0 || !caster.ConsumeAP(apCost) || caster.GetAttrValue(AttributeType.CurrentMP) < mpCost)
        {
            if (!caster.ConsumeAP(apCost)) caster.ConsumeAP(-apCost); // 返还AP
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
        // 动画
        if (caster is PlayerAttr player)
        {
            player.PlaySkillAnim();
            if (!string.IsNullOrEmpty(animTriggerName)) player.animator.SetTrigger(animTriggerName);
        }
        else if (caster is EnemyAttr enemy)
        {
            enemy.PlaySkillAnim();
            if (!string.IsNullOrEmpty(animTriggerName)) enemy.animator.SetTrigger(animTriggerName);
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
        if (!characterSkills.ContainsKey(caster)) return false;
        SkillBase skill = characterSkills[caster].Find(s => s.skillID == skillID);
        return skill?.CastSkill(caster, target) ?? false;
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