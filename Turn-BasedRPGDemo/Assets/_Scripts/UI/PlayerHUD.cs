using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 游玩界面常态HUD - 实时显示生命、护甲、经验
/// 完全适配你的PlayerAttr系统，无需修改旧代码
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("=== 生命相关 TMP UI ===")]
    public Slider HPSlider;
    public TextMeshProUGUI HPText;

    [Header("=== 物理护甲相关 TMP UI ===")]
    public Slider ArmorSlider;
    public TextMeshProUGUI ArmorText;

    [Header("=== 魔法护甲相关 TMP UI ===")]
    public Slider MagicResistSlider;
    public TextMeshProUGUI MagicResistText;

    //[Header("=== 经验等级相关 TMP UI ===")]
    //public TextMeshProUGUI levelText;
    //public TextMeshProUGUI expText;

    private PlayerAttr playerAttr;
    private bool isInit = false;

    void Awake()
    {
        // 找到玩家的PlayerAttr组件（全局唯一）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerAttr = player.GetComponent<PlayerAttr>();
            isInit = true;
        }
        else
        {
            Debug.LogWarning("未找到玩家对象，请检查Player的Tag是否设置为Player");
        }
    }

    void Update()
    {
        if (!isInit || playerAttr == null) return;
        UpdateHUDData();
    }

    /// <summary>
    /// 核心：同步所有属性到UI，和神界原罪2显示格式一致
    /// </summary>
    private void UpdateHUDData()
    {
        // 1. 生命值 更新 (绿色血条)
        float curHp = playerAttr.GetAttrValue(AttributeType.CurrentHP);
        float maxHp = playerAttr.GetAttrValue(AttributeType.MaxHP);
        HPSlider.maxValue = maxHp;
        HPSlider.value = curHp;
        HPText.text = $"{Mathf.Round(curHp)}/{Mathf.Round(maxHp)}"; // 取整显示，更美观

        // 2. 物理护甲 更新 (灰色护甲条，神界原罪2核心)
        float curPhysArmor = playerAttr.GetAttrValue(AttributeType.CurrentPhysArmor);
        float maxPhysArmor = playerAttr.GetAttrValue(AttributeType.MaxPhysArmor);
        ArmorSlider.maxValue = maxPhysArmor;
        ArmorSlider.value = curPhysArmor;
        ArmorText.text = $"{Mathf.Round(curPhysArmor)}/{Mathf.Round(maxPhysArmor)}";

        // 3. 魔法护甲 更新 (蓝色护甲条，神界原罪2核心)
        float curMagicArmor = playerAttr.GetAttrValue(AttributeType.CurrentMagicArmor);
        float maxMagicArmor = playerAttr.GetAttrValue(AttributeType.MaxMagicArmor);
        MagicResistSlider.maxValue = maxMagicArmor;
        MagicResistSlider.value = curMagicArmor;
        MagicResistText.text = $"{Mathf.Round(curMagicArmor)}/{Mathf.Round(maxMagicArmor)}";

        //// 4. 等级+经验 更新
        //int level = Mathf.RoundToInt(playerAttr.GetAttrValue(AttributeType.Level));
        //float curExp = playerAttr.GetAttrValue(AttributeType.CurrentEXP);
        //float needExp = playerAttr.GetAttrValue(AttributeType.EXPToLevelUp);
        //levelText.text = $"Lv.{level}";
        //expText.text = $"经验: {Mathf.Round(curExp)}/{Mathf.Round(needExp)}";
    }
}