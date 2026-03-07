using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

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
    }

    // 显示技能提示
    public static void ShowTooltip(SkillBase skill)
    {
        if (Instance == null || skill == null) return;

        Instance.skillNameText.text = skill.skillName;
        Instance.skillDescText.text = $"发射一枚{skill.skillName}，造成{skill.effectValue}点伤害";
        Instance.skillDamageText.text = $"{skill.effectValue - 1}-{skill.effectValue + 1}伤害";
        Instance.skillCostText.text = $"消耗 {skill.apCost} 行动点 / {skill.mpCost} 魔法值";
        Instance.skillRangeText.text = $"{skill.skillRange}m 范围";
        // Instance.skillRequireText.text = $"需要 {skill.skillSchool} 学派 {skill.requireLevel} 级"; // 如果没有这些字段可以注释掉

        // 跟随鼠标位置
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