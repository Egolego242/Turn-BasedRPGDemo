using UnityEngine;

/// <summary>
/// 消耗品配置文件 - 继承ItemBase基类（血瓶、蓝瓶、护甲药剂等）
/// 使用后：恢复对应数值 → 数量-1 → 数量为0自动移除
/// </summary>
[CreateAssetMenu(fileName = "NewConsumable", menuName = "RPG/ConsumableItem")]
public class ConsumableItem : ItemBase
{
    [Header("消耗品属性")]
    public AttributeType recoverType; // 恢复类型：HP/MP/AP
    public float recoverValue = 20f; // 恢复数值
    public int recoverApValue = 20; // 恢复AP数值

    // 重写使用方法：使用道具恢复属性
    public override bool UseItem(GameObject target)
    {
        BaseCharacterAttr attr = target.GetComponent<BaseCharacterAttr>();
        if (attr == null) return false;

        switch (recoverType)
        {
            case AttributeType.CurrentHP:
                attr.HealHP(recoverValue);
                break;
            case AttributeType.CurrentMP:
                attr.HealMP(recoverValue);
                break;
            case AttributeType.CurrentAP:
                attr.RecoverAP(recoverApValue);
                break;
        }
        Debug.Log("使用了：" + itemName + "，恢复了" + recoverValue + "点" + recoverType);
        return true; // 使用成功，道具数量-1
    }
}