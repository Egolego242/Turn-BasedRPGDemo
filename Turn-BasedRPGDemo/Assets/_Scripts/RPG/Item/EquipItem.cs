using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEquip", menuName = "RPG/EquipItem")]
public class EquipItem : ItemBase
{
    [Header("装备属性加成（核心：你的需求5）")]
    public List<AttrBonus> attrBonusList = new List<AttrBonus>(); // 属性加成列表

    // 装备方法：给角色添加属性加成
    public void Equip(GameObject target)
    {
        BaseCharacterAttr attr = target.GetComponent<BaseCharacterAttr>();
        if (attr == null) return;

        foreach (var bonus in attrBonusList)
        {
            attr.AddAttrValue(bonus.attrType, bonus.bonusValue);
        }
        Debug.Log("装备了：" + itemName + "，属性加成生效！");
    }

    // 卸下方法：还原角色属性
    public void UnEquip(GameObject target)
    {
        BaseCharacterAttr attr = target.GetComponent<BaseCharacterAttr>();
        if (attr == null) return;

        foreach (var bonus in attrBonusList)
        {
            attr.AddAttrValue(bonus.attrType, -bonus.bonusValue);
        }
        Debug.Log("卸下了：" + itemName + "，属性加成还原！");
    }
}

/// <summary>
/// 属性加成结构体：配置装备的「属性类型+加成数值」
/// 可扩展：后续加百分比加成，只需要新增字段即可
/// </summary>
[System.Serializable]
public struct AttrBonus
{
    public AttributeType attrType; // 加成的属性类型
    public float bonusValue;       // 加成的数值
}