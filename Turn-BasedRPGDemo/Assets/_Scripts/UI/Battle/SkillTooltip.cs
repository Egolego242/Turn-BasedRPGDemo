using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 技能提示框：鼠标悬停技能按钮时跟随鼠标显示技能详情（名称/描述/伤害/消耗/范围/目标类型），不拦截射线防闪烁
/// </summary>
public class SkillTooltip : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject panel;
    public TMP_Text skillNameText; // ✅ 修改：Text → TMP_Text
    public TMP_Text skillDescText; // ✅ 修改
    public TMP_Text skillDamageText; // ✅ 修改
    public TMP_Text skillCostText; // ✅ 修改
    public TMP_Text skillRangeText; // ✅ 修改

    private static SkillTooltip Instance;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);

        // 阻止提示面板拦截鼠标射线，避免 OnPointerEnter/Exit 来回触发导致闪烁
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
    }

    // 显示技能提示
    public static void ShowTooltip(SkillBase skill)
    {
        if (Instance == null || skill == null) return;
        if (Instance.panel == null) return;

        if (Instance.skillNameText != null)
            Instance.skillNameText.text = skill.skillName;

        if (Instance.skillDescText != null)
        {
            // 优先使用自定义描述，为空则自动生成
            if (!string.IsNullOrEmpty(skill.description))
                Instance.skillDescText.text = skill.description;
            else
                Instance.skillDescText.text = skill.skillType switch
                {
                    SkillType.Attack => $"对敌人造成 {skill.effectValue:F0} 点伤害",
                    SkillType.Heal => $"恢复目标 {skill.effectValue:F0} 点生命值",
                    SkillType.Buff => $"提升目标 {skill.effectValue:F0} 点力量",
                    _ => skill.skillName
                };
        }

        if (Instance.skillDamageText != null)
        {
            Instance.skillDamageText.text = skill.skillType == SkillType.Attack
                ? $"伤害：{skill.effectValue - 1} ~ {skill.effectValue + 1}"
                : $"效果值：{skill.effectValue:F0}";
        }

        if (Instance.skillCostText != null)
        {
            string cost = "";
            if (skill.apCost > 0) cost += $"AP {skill.apCost}";
            if (skill.mpCost > 0) cost += (cost.Length > 0 ? " / " : "") + $"MP {skill.mpCost:F0}";
            if (cost.Length == 0) cost = "无消耗";
            Instance.skillCostText.text = $"消耗：{cost}";
        }

        if (Instance.skillRangeText != null)
        {
            string targetLabel = skill.targetType switch
            {
                TargetType.Enemy => "敌人",
                TargetType.Ally => "友方",
                TargetType.Self => "自身",
                _ => "目标"
            };
            Instance.skillRangeText.text = $"射程：{skill.skillRange}m | 目标：{targetLabel}";
        }

        Instance.panel.SetActive(true);
        Instance.UpdateTooltipPosition();
    }

    // 隐藏技能提示
    public static void HideTooltip()
    {
        if (Instance != null) Instance.panel.SetActive(false);
    }

    // 面板跟随鼠标
    private void Update()
    {
        if (panel.activeSelf) UpdateTooltipPosition();
    }

    private void UpdateTooltipPosition()
    {
        Vector2 mousePos = Input.mousePosition;
        // 防止面板超出屏幕
        float xOffset = mousePos.x + panel.GetComponent<RectTransform>().rect.width > Screen.width ? -300 : 20;
        float yOffset = mousePos.y - panel.GetComponent<RectTransform>().rect.height < 0 ? 50 : -50;
        panel.transform.position = new Vector2(mousePos.x + xOffset, mousePos.y + yOffset);
    }
}

// ✅ 这里原来的 SkillButton 类已经删除了！