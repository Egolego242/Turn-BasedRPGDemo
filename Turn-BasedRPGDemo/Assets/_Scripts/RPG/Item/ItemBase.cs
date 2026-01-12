using UnityEngine;

/// <summary>
/// 道具基类 - ScriptableObject配置文件
/// 所有道具（消耗品/装备）都继承此类，封装通用道具属性
/// 可扩展：后续新增任何道具类型，只需要继承此类即可
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/ItemBase")]
public class ItemBase : ScriptableObject
{
    [Header("通用道具属性")]
    public string itemName; // 道具名称
    public Sprite itemIcon; // 道具图标
    public ItemType itemType; // 道具类型
    public int itemCount; // 道具数量
    public bool isStackable; // 是否可堆叠

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
    Consumable,  // 消耗品：血瓶、蓝瓶
    Weapon,      // 武器：剑、弓、法杖
    Armor,       // 防具：头盔、胸甲、鞋子
    Accessory    // 饰品：戒指、项链（后续扩展）
}