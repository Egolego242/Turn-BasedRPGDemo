using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 神界原罪2风格 - 角色详细属性面板 (TextMeshPro新版适配)
/// 显示所有角色基础属性+已装备道具，弹窗式，点击开关
/// 无旧代码修改，无缝对接属性/背包系统
/// </summary>
public class CharacterUI : MonoBehaviour
{
    [Header("=== 角色核心属性 TMP文本 ===")]
    public TextMeshProUGUI txt_Level;
    public TextMeshProUGUI txt_Strength;
    public TextMeshProUGUI txt_Intelligence;
    public TextMeshProUGUI txt_MaxHP;
    public TextMeshProUGUI txt_MaxMP;
    //public TextMeshProUGUI txt_MaxAP;

    [Header("=== 防御属性 TMP文本 ===")]
    public TextMeshProUGUI txt_Armor;
    public TextMeshProUGUI txt_MagicResist;

    [Header("=== 装备槽 图片 ===")]
    public Image img_WeaponSlot;  // 武器槽
    public Image img_ArmorSlot;   // 护甲槽

    [Header("=== 装备槽占位图 ===")]
    public Sprite emptySlotSprite; // 无装备时显示的空槽图

    private PlayerAttr playerAttr;
    private Inventory inventory;
    private bool isInit = false;

    void Awake()
    {
        // 初始化组件
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerAttr = player.GetComponent<PlayerAttr>();
            inventory = player.GetComponent<Inventory>();
            isInit = true;
        }
        // 默认隐藏角色面板，毕设核心逻辑
        gameObject.SetActive(false);
        // 空槽初始化
        img_WeaponSlot.sprite = emptySlotSprite;
        img_ArmorSlot.sprite = emptySlotSprite;
    }

    // ===== 绑定到【角色按钮】的点击事件 =====
    public void OpenCharacterPanel()
    {
        if (!isInit) return;
        gameObject.SetActive(true);
        UpdateCharacterAllInfo();
    }

    // ===== 绑定到【关闭按钮】的点击事件 =====
    public void CloseCharacterPanel()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 同步角色所有详细属性+装备到UI
    /// 外部可调用，确保装备/使用道具后数值同步
    /// </summary>
    public void UpdateCharacterAllInfo()
    {
        if (playerAttr == null || inventory == null) return;

        // 基础属性赋值
        txt_Level.text = $"角色等级 : {Mathf.RoundToInt(playerAttr.GetAttrValue(AttributeType.Level))}";
        txt_Strength.text = $"力量 : {Mathf.Round(playerAttr.GetAttrValue(AttributeType.Strength))}";
        txt_Intelligence.text = $"智力 : {Mathf.Round(playerAttr.GetAttrValue(AttributeType.Intelligence))}";
        txt_MaxHP.text = $"最大生命 : {Mathf.Round(playerAttr.GetAttrValue(AttributeType.MaxHP))}";
        txt_MaxMP.text = $"最大法力 : {Mathf.Round(playerAttr.GetAttrValue(AttributeType.MaxMP))}";
        //txt_MaxAP.text = $"最大行动点 : {Mathf.Round(playerAttr.GetAttrValue(AttributeType.MaxAP))}";

        // 防御属性赋值
        txt_Armor.text = $"物理防御 : {Mathf.Round(playerAttr.GetAttrValue(AttributeType.Armor))}";
        txt_MagicResist.text = $"魔法抗性 : {Mathf.Round(playerAttr.GetAttrValue(AttributeType.MagicResist))}";

        // 装备槽刷新 - 核心
        UpdateEquipSlots();
    }

    /// <summary>
    /// 刷新装备槽：有装备显示图标，无装备显示空槽
    /// </summary>
    private void UpdateEquipSlots()
    {
        // 重置装备槽为空
        img_WeaponSlot.sprite = emptySlotSprite;
        img_ArmorSlot.sprite = emptySlotSprite;

        // 遍历已装备列表，显示对应装备
        foreach (var equip in inventory.equipedItemList)
        {
            if (equip == null) continue;
            if (equip.itemType == ItemType.Weapon)
            {
                img_WeaponSlot.sprite = equip.itemIcon;
            }
            else if (equip.itemType == ItemType.Armor)
            {
                img_ArmorSlot.sprite = equip.itemIcon;
            }
        }
    }

    // 新增：外部调用刷新（比如装备后主动触发）
    public void ForceRefresh()
    {
        UpdateCharacterAllInfo();
    }
}