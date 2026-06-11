using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 玩家状态栏：在探索模式下常驻显示等级/经验/金币
/// 挂载到 baseExploreUI 下的面板上，Update每帧自动刷新
/// </summary>
public class PlayerStatusBar : MonoBehaviour
{
    [Header("=== 经验显示 ===")]
    public TMP_Text levelText;
    public TMP_Text expText;
    public Slider expBar;

    [Header("=== 金币显示 ===")]
    public TMP_Text goldText;

    private PlayerAttr _player;
    private Inventory _inventory;

    private void Start()
    {
        _player = FindObjectOfType<PlayerAttr>();
        if (_player != null)
            _inventory = _player.GetComponent<Inventory>();
    }

    private void Update()
    {
        if (_player == null) return;

        float curEXP = _player.GetAttrValue(AttributeType.CurrentEXP);
        float needEXP = _player.GetAttrValue(AttributeType.EXPToLevelUp);
        int level = Mathf.RoundToInt(_player.GetAttrValue(AttributeType.Level));

        if (levelText != null)
            levelText.text = $"Lv.{level}";

        if (expText != null)
            expText.text = $"{curEXP:F0} / {needEXP:F0}";

        if (expBar != null)
        {
            expBar.maxValue = needEXP > 0 ? needEXP : 1;
            expBar.value = curEXP;
        }

        if (_inventory != null && goldText != null)
            goldText.text = $"金币 {_inventory.currentGold}";
    }
}
