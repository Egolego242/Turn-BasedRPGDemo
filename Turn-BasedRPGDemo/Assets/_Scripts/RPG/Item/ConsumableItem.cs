using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "RPG/ConsumableItem")]
public class ConsumableItem : ItemBase
{
    [Header("消耗品属性")]
    public AttributeType recoverType; // 恢复类型：HP/MP/AP
    public float recoverValue; // 恢复数值

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
                attr.RecoverAP(recoverValue);
                break;
        }
        Debug.Log("使用了：" + itemName + "，恢复了" + recoverValue + "点" + recoverType);
        return true; // 使用成功，道具数量-1
    }
}