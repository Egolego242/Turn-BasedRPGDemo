using UnityEngine;
using UnityEngine.UI;
using TMPro; // 编译

/// <summary>
/// 战斗开始提示（图片版）
/// </summary>
public class BattleStartTip : MonoBehaviour
{
    [Header("核心配置")]
    [Tooltip("提示面板根物体（图片+文字）")]
    public GameObject panel;
    [Tooltip("战斗提示背景图（你的核心图片）")]
    public Image battleTipImage; // ✅ 新增：图片引用
    [Tooltip("（可选）额外提示文字（TMP版）")]
    public TMP_Text subTipText; // ✅ 修改：从Text改为TMP_Text
    [Tooltip("提示显示时长（秒）")]
    public float showDuration = 3f;

    private void Awake()
    {
        // 初始隐藏提示
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// 显示战斗开始提示
    /// </summary>
    public void ShowBattleStartTip()
    {
        if (panel == null) return;

        // 显示面板（图片+文字）
        panel.SetActive(true);

        // （可选）如果需要切换不同提示图片，可在这里赋值
        // battleTipImage.sprite = 你的战斗提示图片;

        // （可选）设置提示文字
        if (subTipText != null)
        {
            subTipText.text = "点击“结束回合”按钮进行操作";
        }

        // 延迟隐藏面板
        Invoke(nameof(HideBattleStartTip), showDuration);
    }

    /// <summary>
    /// 隐藏战斗开始提示
    /// </summary>
    private void HideBattleStartTip()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}