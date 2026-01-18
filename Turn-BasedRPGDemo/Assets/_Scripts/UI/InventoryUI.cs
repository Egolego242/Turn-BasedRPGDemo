using UnityEngine;
using UnityEngine.UI;
using TMPro; // ★★★ TMP命名空间 ★★★
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// 神界原罪2风格 - 背包道具面板 (TextMeshPro新版适配)
/// 动态生成道具图标+TMP数量文本，点击使用/装备道具
/// 完美对接Inventory背包系统，零修改旧代码，毕设核心版本
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("=== 背包配置 ===")]
    public GameObject itemSlotPrefab;  // 道具槽预制体（Image+Button+TMP文本）
    public Transform itemGridParent;   // GridLayoutGroup的父物体，用于排列道具槽
    public Sprite emptyItemSprite;     // 空道具图标

    private Inventory playerInventory;
    private List<GameObject> spawnedSlots = new List<GameObject>();
    private bool isInit = false;

    void Awake()
    {
        // 初始化背包组件
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInventory = player.GetComponent<Inventory>();
            isInit = true;
        }
        // 默认隐藏背包面板
        gameObject.SetActive(false);
    }

    // ===== 绑定到【背包按钮】的点击事件 =====
    public void OpenInventoryPanel()
    {
        if (!isInit) return;
        gameObject.SetActive(true);
        RefreshInventoryItems();
    }

    // ===== 绑定到【关闭按钮】的点击事件 =====
    public void CloseInventoryPanel()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 核心：刷新背包道具，动态生成/销毁道具槽，防止重复生成
    /// </summary>
    private void RefreshInventoryItems()
    {
        if (playerInventory == null || itemSlotPrefab == null) return;

        // 第一步：清空所有已生成的道具槽，防重叠
        foreach (GameObject slot in spawnedSlots)
        {
            Destroy(slot);
        }
        spawnedSlots.Clear();

        // 第二步：遍历背包道具，生成对应道具槽
        foreach (var item in playerInventory.itemList)
        {
            if (item == null) continue;

            // 生成道具槽
            GameObject newSlot = Instantiate(itemSlotPrefab, itemGridParent);
            newSlot.SetActive(true);
            spawnedSlots.Add(newSlot);

            // 获取道具槽的组件
            Image itemImage = newSlot.GetComponent<Image>();
            TextMeshProUGUI countText = newSlot.GetComponentInChildren<TextMeshProUGUI>();
            Button slotBtn = newSlot.GetComponent<Button>();

            // 设置道具图标
            itemImage.sprite = item.itemIcon == null ? emptyItemSprite : item.itemIcon;
            // 设置道具数量（TMP文本），不可堆叠的道具不显示数量
            countText.text = item.isStackable && item.itemCount > 1 ? item.itemCount.ToString() : "";

            // 绑定点击事件：点击道具 → 使用/装备
            slotBtn.onClick.AddListener(() =>
            {
                playerInventory.UseItem(item);
                RefreshInventoryItems(); // 使用后刷新背包
                // 核心修改：装备/使用道具后刷新角色面板
                CharacterUI characterUI = FindObjectOfType<CharacterUI>();
                if (characterUI != null) characterUI.UpdateCharacterAllInfo();
            });

            // ====== 修复：悬停事件调用正确的重载方法 ======
            EventTrigger trigger = newSlot.GetComponent<EventTrigger>();
            if (trigger == null) trigger = newSlot.AddComponent<EventTrigger>(); // 容错：如果没有EventTrigger组件则添加

            // 鼠标移入 → 显示详情
            EventTrigger.Entry enterEvent = new EventTrigger.Entry();
            enterEvent.eventID = EventTriggerType.PointerEnter;
            enterEvent.callback.AddListener((_) => { TooltipUI.ShowTooltip(item); });
            trigger.triggers.Add(enterEvent);

            // 鼠标移出 → 隐藏详情
            EventTrigger.Entry exitEvent = new EventTrigger.Entry();
            exitEvent.eventID = EventTriggerType.PointerExit;
            exitEvent.callback.AddListener((_) => { TooltipUI.HideTooltip(); });
            trigger.triggers.Add(exitEvent);
        }
    }
}