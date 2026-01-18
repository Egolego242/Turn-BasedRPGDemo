using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public GameObject itemSlotPrefab;
    public Transform gridParent;
    public Sprite emptySprite;

    private Inventory _inventory;
    private List<GameObject> _slots = new List<GameObject>();

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _inventory = player.GetComponent<Inventory>();
        gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        TooltipUI.Hide();
    }

    void Refresh()
    {
        foreach (var s in _slots) if (s != null) Destroy(s);
        _slots.Clear();

        if (_inventory == null || _inventory.itemList == null) return;

        // ===== ✅ 核心修复：遍历物品时，创建【临时变量】接收当前item，解决闭包引用错乱 =====
        foreach (var item in _inventory.itemList)
        {
            if (item == null) continue;
            GameObject slotObj = Instantiate(itemSlotPrefab, gridParent);
            _slots.Add(slotObj);

            Image img = slotObj.GetComponent<Image>();
            TextMeshProUGUI count = slotObj.GetComponentInChildren<TextMeshProUGUI>();
            img.sprite = item.itemIcon ?? emptySprite;
            count.text = item.isStackable && item.itemCount > 1 ? item.itemCount.ToString() : "";
            count.raycastTarget = false;

            // 点击事件-临时变量
            Button btn = slotObj.GetComponent<Button>();
            ItemBase clickItem = item; // ✅ 创建临时变量，绑定当前物品
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => { _inventory.UseItem(clickItem); Refresh(); TooltipUI.Hide(); });

            // ===== ✅ 悬停事件-临时变量【修复显示反向的核心代码】 =====
            EventTrigger trigger = slotObj.GetComponent<EventTrigger>() ?? slotObj.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            // 鼠标进入
            EventTrigger.Entry enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            ItemBase hoverItem = item; // ✅ 创建临时变量，绑定当前物品，根治引用错乱！！！
            enter.callback.AddListener((_) => { TooltipUI.Show(hoverItem, slotRect); });
            trigger.triggers.Add(enter);

            // 鼠标离开
            EventTrigger.Entry exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener((_) => { TooltipUI.Hide(); });
            trigger.triggers.Add(exit);
        }
    }
}