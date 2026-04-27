using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 基础物品数据（纯数据，可序列化）
/// </summary>
[Serializable]
public class ItemData
{
    public string itemName;
    public int itemCount;
    public bool isStackable;
    public ItemType itemType;

    public ItemData() { }

    // 从ItemBase转换
    public ItemData(ItemBase item)
    {
        itemName = item.itemName;
        itemCount = item.itemCount;
        isStackable = item.isStackable;
        itemType = item.itemType;
    }
}

/// <summary>
/// 装备物品数据（纯数据，可序列化）
/// </summary>
[Serializable]
public class EquipItemData : ItemData
{
    public List<AttrBonusData> attrBonusList = new List<AttrBonusData>();

    public EquipItemData() : base() { }

    // 从EquipItem转换
    public EquipItemData(EquipItem equip) : base(equip)
    {
        foreach (var bonus in equip.attrBonusList)
        {
            attrBonusList.Add(new AttrBonusData(bonus));
        }
    }
}

/// <summary>
/// 属性加成数据（可序列化，替代原AttrBonus结构体）
/// </summary>
[Serializable]
public struct AttrBonusData
{
    public AttributeType attrType;
    public float bonusValue;

    public AttrBonusData(AttrBonus bonus)
    {
        attrType = bonus.attrType;
        bonusValue = bonus.bonusValue;
    }
}

/// <summary>
/// 【核心】游戏存档数据类（纯数据，可序列化）
/// </summary>
[Serializable]
public class GameSaveData
{
    // 1. 玩家属性字典（拆分为两个List，兼容Json序列化）
    public List<AttributeType> attrKeys = new List<AttributeType>();
    public List<float> attrValues = new List<float>();

    // 2. 玩家位置与旋转（Vector3/Quaternion可直接序列化）
    public Vector3 position;
    public Quaternion rotation;

    // 3. 背包与金币
    public int currentGold;
    public List<ItemData> bagItemDatas = new List<ItemData>();
    public List<EquipItemData> equipedItemDatas = new List<EquipItemData>();

    // 4. 等级经验（冗余备份）
    public int level;
    public float currentEXP;
    public float expToLevelUp;

    // 5. 【新增】存档时间（解决CS0117错误的核心）
    public string saveTime;
}