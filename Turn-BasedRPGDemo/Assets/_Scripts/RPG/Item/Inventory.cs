using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 背包系统核心脚本 - 挂载在玩家身上
/// 功能：道具添加、移除、使用、装备、卸下，支持所有道具类型，极致可扩展
/// 无缝衔接属性系统：装备属性加成直接调用角色属性方法
/// </summary>
public class Inventory : MonoBehaviour
{
    [Header("背包配置")]
    public int bagCapacity = 20; // 背包容量
    public List<ItemBase> itemList = new List<ItemBase>(); // 背包道具列表
    public List<EquipItem> equipedItemList = new List<EquipItem>(); // 已装备道具列表

    #region 背包核心方法：添加/移除道具
    public bool AddItem(ItemBase item)
    {
        if (itemList.Count >= bagCapacity)
        {
            Debug.Log("背包已满！");
            return false;
        }
        // 可堆叠道具：数量叠加
        if (item.isStackable)
        {
            foreach (var bagItem in itemList)
            {
                if (bagItem.itemName == item.itemName)
                {
                    bagItem.itemCount += item.itemCount;
                    return true;
                }
            }
        }
        // 不可堆叠道具：直接添加
        itemList.Add(item);
        Debug.Log("获得道具：" + item.itemName);
        return true;
    }

    public void RemoveItem(ItemBase item)
    {
        if (itemList.Contains(item))
        {
            itemList.Remove(item);
            Debug.Log("移除道具：" + item.itemName);
        }
    }
    #endregion

    #region 道具使用/装备核心方法
    public void UseItem(ItemBase item)
    {
        if (item == null) return;
        // 消耗品：使用后数量-1，用完移除
        if (item is ConsumableItem)
        {
            bool isUsed = item.UseItem(gameObject);
            if (isUsed)
            {
                item.itemCount--;
                if (item.itemCount <= 0)
                {
                    RemoveItem(item);
                }
            }
        }
        // 装备品：装备/卸下切换
        else if (item is EquipItem)
        {
            EquipItem equip = item as EquipItem;
            if (equipedItemList.Contains(equip))
            {
                UnEquipItem(equip);
            }
            else
            {
                EquipItem(equip);
            }
        }
    }

    // 装备道具：添加属性加成，加入已装备列表
    private void EquipItem(EquipItem equip)
    {
        equip.Equip(gameObject);
        equipedItemList.Add(equip);
    }

    // 卸下道具：还原属性，移出已装备列表
    private void UnEquipItem(EquipItem equip)
    {
        equip.UnEquip(gameObject);
        equipedItemList.Remove(equip);
    }
    #endregion
}