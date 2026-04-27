using UnityEngine;

/// <summary>
/// 道具基类 - ScriptableObject配置文件
/// 所有道具（消耗品/装备）都继承此类，封装通用道具属性
/// 可扩展：后续新增任何道具类型，只需要继承此类即可
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/ItemBase")]
public class ItemBase : ScriptableObject
{
    [Header("通用道具/武器属性")]
    public string itemName = "新道具"; // 道具名称
    public Sprite itemIcon; // 道具图标
    [TextArea(2, 4)]                          // 多行文本框，方便写多行介绍
    public string itemDesc = "道具详细介绍";  // 道具介绍（鼠标悬停核心显示内容）
    public ItemType itemType = ItemType.None; // 道具类型
    public int itemCount = 1; // 道具数量
    public bool isStackable = true; // 是否可堆叠（消耗品可堆，装备不可堆）

    // 道具使用方法（子类重写）
    public virtual bool UseItem(GameObject target)
    {
        return false;
    }
}

/// <summary>
/// 道具类型枚举（核心扩展点，后续新增道具类型直接加）
/// </summary>
public enum ItemType
{
    None,
    Consumable,  // 消耗品：血瓶、蓝瓶、护甲药剂等，使用后消失
    Weapon,      // 武器：剑、斧、法杖等
    Armor,       // 护甲：胸甲、皮甲、重甲等
    Helmet,      // 头盔：帽子、头盔等（预留扩展）
    Accessory    // 饰品：戒指、项链等（预留扩展）
}

// 新增：装备部位枚举（对应 equipPart 字段）
public enum EquipPart
{
    None,
    Weapon,    // 武器位
    Armor,     // 护甲位
    Helmet,    // 头盔位
    Accessory  // 饰品位
}