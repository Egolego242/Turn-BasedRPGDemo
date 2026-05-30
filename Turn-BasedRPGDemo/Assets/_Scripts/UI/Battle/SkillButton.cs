using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 技能按钮脚本
/// 挂载在快捷技能栏的每个技能按钮上
/// 功能：鼠标悬停显示技能详情、点击释放技能、显示技能范围
/// </summary>
public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("技能配置")]
    [Tooltip("绑定的技能资产（ScriptableObject）")]
    public SkillBase skill;

    [Header("UI引用")]
    [Tooltip("技能图标Image")]
    public Image skillIcon;
    [Tooltip("技能冷却遮罩Image")]
    public Image coolDownMask;
    [Tooltip("技能消耗Text（可选）")]
    public TMP_Text costText;

    private Button btn;

    private void Awake()
    {
        // 自动获取Button组件
        btn = GetComponent<Button>();

        // 初始化UI显示
        InitSkillUI();
    }

    /// <summary>
    /// 初始化技能按钮UI
    /// </summary>
    private void InitSkillUI()
    {
        if (skill == null)
        {
            Debug.LogWarning($"技能按钮 {gameObject.name} 未绑定技能资产！", this);
            return;
        }

        // 显示技能消耗
        if (costText != null)
        {
            costText.text = $"{skill.apCost}AP";
        }

        // （可选）如果有技能图标Sprite，赋值给skillIcon
        // if (skillIcon != null && skill.skillIcon != null)
        // {
        //     skillIcon.sprite = skill.skillIcon;
        // }

        // 初始化冷却遮罩
        if (coolDownMask != null)
        {
            coolDownMask.fillAmount = 0;
        }
    }

    /// <summary>
    /// 鼠标悬停进入：显示技能详情
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skill == null) return;

        // 调用SkillTooltip显示详情
        SkillTooltip.ShowTooltip(skill);

        // （可选）如果是战斗状态且是玩家回合，显示技能范围
        if (GameStateMgr.Instance != null && GameStateMgr.Instance.IsBattleState())
        {
            UIManager.Instance?.rangeVisualizer.ShowSkillRange(skill);
        }
    }

    /// <summary>
    /// 鼠标悬停退出：隐藏技能详情
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // 隐藏技能详情
        SkillTooltip.HideTooltip();

        // 隐藏技能范围
        UIManager.Instance?.rangeVisualizer.HideAllRange();
    }

    /// <summary>
    /// 鼠标点击：进入目标选择模式，再点击敌人完成技能释放
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (skill == null) return;

        if (GameStateMgr.Instance == null || !GameStateMgr.Instance.IsBattleState())
        {
            Debug.Log("非战斗状态，无法释放技能！");
            return;
        }

        PlayerAttr player = FindObjectOfType<PlayerAttr>();
        if (player == null || !player.isMyTurn || player.isDead)
        {
            Debug.Log("不是玩家回合或玩家已死亡，无法释放技能！");
            return;
        }

        if (SkillTargetSelector.Instance != null)
        {
            SkillTooltip.HideTooltip();
            UIManager.Instance?.rangeVisualizer.HideAllRange();
            SkillTargetSelector.Instance.EnterTargetingMode(skill, player);
        }
        else
        {
            Debug.LogError("场景中缺少SkillTargetSelector组件，请将其挂载到常驻GameObject上！");
        }
    }

    /// <summary>
    /// 更新技能冷却显示（每帧调用，或通过事件调用）
    /// </summary>
    public void UpdateCoolDownDisplay()
    {
        if (skill == null || coolDownMask == null) return;

        // 计算冷却百分比
        float coolDownPercent = (float)skill.currentCoolDown / skill.coolDownRound;
        coolDownMask.fillAmount = coolDownPercent;

        // 冷却中禁用按钮
        if (btn != null)
        {
            btn.interactable = skill.currentCoolDown <= 0;
        }
    }
}