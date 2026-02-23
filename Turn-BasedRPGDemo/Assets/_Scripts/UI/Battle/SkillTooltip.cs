using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillTooltip : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject panel;
    public Text skillNameText;
    public Text skillDescText;
    public Text skillDamageText;
    public Text skillCostText;
    public Text skillRangeText;
    public Text skillRequireText;

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
        //Instance.skillRangeText.text = $"{skill.skillRange}m 范围";
        //Instance.skillRequireText.text = $"需要 {skill.skillSchool} 学派 {skill.requireLevel} 级";

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

// 技能按钮脚本，挂载到快捷栏的技能按钮上
public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public SkillBase skill; // 绑定的技能资产

    public void OnPointerEnter(PointerEventData eventData)
    {
        SkillTooltip.ShowTooltip(skill);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SkillTooltip.HideTooltip();
    }

    // 点击释放技能
    public void OnClick()
    {
        if (skill == null) return;
        // 通知范围可视化组件，显示技能范围
        UIManager.Instance.rangeVisualizer.ShowSkillRange(skill);
    }
}