using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 敌人信息面板：鼠标悬停敌人时，在屏幕顶部显示敌人名称/等级/血条
/// 血条通过 Update 实时刷新（战斗中也能即时反映伤害）
/// </summary>
public class EnemyInfoPanel : MonoBehaviour
{
    public static EnemyInfoPanel Instance { get; private set; }

    [Header("=== 面板根节点 ===")]
    public GameObject panel;

    [Header("=== 文本引用 ===")]
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text hpText;

    [Header("=== 血条 ===")]
    public Slider hpSlider;
    public Image hpFillImage;

    [Header("=== 血条颜色阈值 ===")]
    public Color hpHighColor = Color.green;
    public Color hpMidColor = Color.yellow;
    public Color hpLowColor = Color.red;

    private EnemyAttr _currentEnemy;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void Show(EnemyAttr enemy)
    {
        _currentEnemy = enemy;
        UpdateDisplay();
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        _currentEnemy = null;
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (_currentEnemy != null)
            UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_currentEnemy == null) return;

        float curHP = _currentEnemy.GetAttrValue(AttributeType.CurrentHP);
        float maxHP = _currentEnemy.GetAttrValue(AttributeType.MaxHP);
        float ratio = maxHP > 0 ? curHP / maxHP : 0f;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(_currentEnemy.displayName)
                ? _currentEnemy.gameObject.name
                : _currentEnemy.displayName;

        if (levelText != null)
            levelText.text = $"Lv.{_currentEnemy.GetAttrValue(AttributeType.Level):F0}";

        if (hpText != null)
            hpText.text = $"{curHP:F0} / {maxHP:F0}";

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP > 0 ? maxHP : 1;
            hpSlider.value = curHP;
        }

        if (hpFillImage != null)
        {
            if (ratio > 0.5f)
                hpFillImage.color = hpHighColor;
            else if (ratio > 0.25f)
                hpFillImage.color = hpMidColor;
            else
                hpFillImage.color = hpLowColor;
        }
    }
}
