using UnityEngine;

/// <summary>
/// 技能目标选择器：点击技能按钮后进入"目标选择模式"，再点击敌人完成技能释放
/// </summary>
public class SkillTargetSelector : MonoBehaviour
{
    public static SkillTargetSelector Instance { get; private set; }
    public static bool IsTargeting => Instance != null && Instance._isTargeting;

    private bool _isTargeting = false;
    private SkillBase _pendingSkill;
    private BaseCharacterAttr _caster;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 进入目标选择模式（由SkillButton调用）
    /// </summary>
    public void EnterTargetingMode(SkillBase skill, BaseCharacterAttr caster)
    {
        if (_isTargeting) ExitTargetingMode();

        _isTargeting = true;
        _pendingSkill = skill;
        _caster = caster;

        UIManager.Instance?.rangeVisualizer.ShowSkillRange(skill);
        Debug.Log($"目标选择模式：{skill.skillName} — 点击敌人释放，右键/ESC取消");
    }

    /// <summary>
    /// 退出目标选择模式
    /// </summary>
    public void ExitTargetingMode()
    {
        _isTargeting = false;
        _pendingSkill = null;
        _caster = null;
        UIManager.Instance?.rangeVisualizer.HideAllRange();
    }

    /// <summary>
    /// 玩家点击了敌人（由PlayerMovement调用）
    /// </summary>
    public void OnTargetSelected(EnemyAttr enemy)
    {
        if (!_isTargeting || _pendingSkill == null || _caster == null) return;
        if (enemy == null || enemy.isDead) return;

        float dist = Vector3.Distance(_caster.transform.position, enemy.transform.position);
        if (dist > _pendingSkill.skillRange)
        {
            Debug.Log($"目标超出技能范围！（距离：{dist:F1}，射程：{_pendingSkill.skillRange}）");
            return;
        }

        if (_caster.isDead || !_caster.isMyTurn)
        {
            ExitTargetingMode();
            return;
        }

        bool success = SkillManager.Instance?.CastCharacterSkill(_caster, _pendingSkill.skillID, enemy) ?? false;
        Debug.Log(success
            ? $"对 {enemy.name} 释放 {_pendingSkill.skillName}！"
            : $"技能释放失败：{_pendingSkill.skillName}");

        ExitTargetingMode();
    }

    public void CancelTargeting()
    {
        Debug.Log("取消目标选择");
        ExitTargetingMode();
    }

    private void Update()
    {
        if (!_isTargeting) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelTargeting();
            return;
        }

        if (_caster == null || _caster.isDead || !_caster.isMyTurn)
            ExitTargetingMode();
    }
}
