using UnityEngine;
using UnityEngine.UI;
using TMPro; // ★★★ TMP命名空间 ★★★

/// <summary>
/// 通用鼠标悬停提示面板 (TextMeshPro新版适配)
/// 适配：道具/装备/技能，悬停显示详情，移开隐藏
/// 毕设加分项，后续无需重构，直接复用
/// </summary>
public class TooltipUI : MonoBehaviour
{
    public TextMeshProUGUI tooltipTitle;  // 道具名称 TMP
    public TextMeshProUGUI tooltipContent;// 道具详情 TMP
    public RectTransform tooltipRect;     // 提示框的RectTransform

    private static TooltipUI instance;

    void Awake()
    {
        instance = this;
        gameObject.SetActive(false); // 默认隐藏
    }

    void Update()
    {
        // 跟随鼠标移动，神界原罪2风格
        if (gameObject.activeSelf)
        {
            Vector2 mousePos = Input.mousePosition;
            // 限制提示面板在屏幕内，避免超出可视区域
            Vector2 pos = new Vector2(mousePos.x + 15, mousePos.y - 15);
            pos.x = Mathf.Clamp(pos.x, 0, Screen.width - tooltipRect.rect.width);
            pos.y = Mathf.Clamp(pos.y, tooltipRect.rect.height, Screen.height);
            tooltipRect.position = pos;
        }
    }

    /// <summary>
    /// 显示提示信息，外部调用这个方法即可
    /// </summary>
    public static void ShowTooltip(string title, string content)
    {
        if (instance == null) return;
        instance.tooltipTitle.text = title;
        instance.tooltipContent.text = content;
        instance.gameObject.SetActive(true);
    }

    /// <summary>
    /// 重载：显示道具/装备的完整信息（核心修改）
    /// </summary>
    public static void ShowTooltip(ItemBase item)
    {
        if (instance == null || item == null) return;

        string title = item.itemName;
        // 内容拼接：描述 + 专属属性（消耗品/装备）
        string content = item.itemDesc + "\n\n";
        if (item is ConsumableItem consumable)
        {
            content += $"✨ 恢复类型：{consumable.recoverType}\n";
            content += $"✨ 恢复数值：{consumable.recoverValue}";
        }
        else if (item is EquipItem equip)
        {
            content += "📌 属性加成：\n";
            foreach (var bonus in equip.attrBonusList)
            {
                content += bonus.bonusValue > 0 ? $"+{bonus.bonusValue} {bonus.attrType}\n" : $"{bonus.bonusValue} {bonus.attrType}\n";
            }
        }
        content += $"\n堆叠：{(item.isStackable ? "是" : "否")}";

        instance.tooltipTitle.text = title;
        instance.tooltipContent.text = content;
        instance.gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏提示信息
    /// </summary>
    public static void HideTooltip()
    {
        if (instance == null) return;
        instance.gameObject.SetActive(false);
    }
}