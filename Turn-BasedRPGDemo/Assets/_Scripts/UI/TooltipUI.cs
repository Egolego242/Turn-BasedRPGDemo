using UnityEngine;
using UnityEngine.UI;
using TMPro; // ★★★ TMP命名空间 ★★★

/// <summary>
/// 通用鼠标悬停提示面板 (TextMeshPro新版适配)
/// 适配：道具/装备/技能，悬停显示详情，移开隐藏
/// 毕设加分项，后续无需重构，直接复用
/// </summary>
public class TooltipUI_TMP : MonoBehaviour
{
    public TextMeshProUGUI tooltipTitle;  // 道具名称 TMP
    public TextMeshProUGUI tooltipContent;// 道具详情 TMP
    public RectTransform tooltipRect;     // 提示框的RectTransform

    private static TooltipUI_TMP instance;

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
            tooltipRect.position = new Vector2(mousePos.x + 15, mousePos.y - 15);
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
    /// 隐藏提示信息
    /// </summary>
    public static void HideTooltip()
    {
        if (instance == null) return;
        instance.gameObject.SetActive(false);
    }
}