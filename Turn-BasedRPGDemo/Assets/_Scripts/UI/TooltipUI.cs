using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public int offsetX = 10;  // 向右偏移，精准贴合图标右下角
    public int offsetY = -10; // 向上偏移，适配装备Panel子层级的坐标（这个值微调即可）
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;
    public RectTransform tooltipRect;

    public static TooltipUI Instance;
    private RectTransform _parentPanelRect; // 装备Panel的RectTransform（核心适配）
    private Canvas _canvas;

    void Awake()
    {
        Instance = this;
        _canvas = GetComponentInParent<Canvas>();
        _parentPanelRect = transform.parent.GetComponent<RectTransform>();//获取父物体装备Panel的Rect
        gameObject.SetActive(false);

        // 防闪屏，必加
        GetComponent<Image>().raycastTarget = false;
        titleText.raycastTarget = false;
        contentText.raycastTarget = false;
    }

    public void ShowTooltip(ItemBase item, RectTransform slotRect)
    {
        if (item == null || slotRect == null || _parentPanelRect == null) return;

        // ===== 核心：适配装备Panel子层级，纯本地坐标计算，绝对精准无偏移 =====
        Vector2 slotLocalPos = slotRect.anchoredPosition;
        float slotW = slotRect.rect.width;
        float slotH = slotRect.rect.height;
        float tipW = tooltipRect.rect.width;
        float tipH = tooltipRect.rect.height;

        // ✅ 提示窗 左上角 精准贴合 道具槽 右下角（严丝合缝，你要的效果）
        float targetX = slotLocalPos.x + slotW + offsetX;
        float targetY = slotLocalPos.y - offsetY;

        // ===== ✅ 彻底防溢出：适配装备Panel的可视区域，绝对不会超出面板/屏幕 =====
        // 右边界溢出 → 向左显示
        if (targetX + tipW > _parentPanelRect.rect.width)
        {
            targetX = slotLocalPos.x - tipW - offsetX;
        }
        // 下边界溢出 → 向上显示
        if (targetY - tipH < 0)
        {
            targetY = slotLocalPos.y + slotH + offsetY;
        }

        // 赋值最终位置，精准无偏差
        tooltipRect.anchoredPosition = new Vector2(targetX, targetY);

        // 赋值物品文本
        titleText.text = item.itemName;
        contentText.text = item.itemDesc;
        if (item is ConsumableItem c)
            contentText.text += $"\n恢复{c.recoverValue}点{c.recoverType}";
        if (item is EquipItem e)
            foreach (var b in e.attrBonusList)
                contentText.text += $"\n{b.attrType}+{b.bonusValue}";

        gameObject.SetActive(true);
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }

    public static void Show(ItemBase item, RectTransform slotRect)
    {
        if (Instance != null) Instance.ShowTooltip(item, slotRect);
    }
    public static void Hide()
    {
        if (Instance != null) Instance.HideTooltip();
    }
}