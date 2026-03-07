using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 回合行动顺序条管理器
/// 显示所有参战角色的头像，高亮当前行动角色，死亡角色灰化
/// </summary>
public class TurnOrderBar : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("回合头像预制体")]
    public GameObject turnItemPrefab;
    [Tooltip("头像父物体（需挂Horizontal Layout Group）")]
    public Transform itemParent;

    private List<TurnItem> turnItemList = new List<TurnItem>();

    /// <summary>
    /// 初始化回合顺序
    /// </summary>
    public void InitTurnOrder(List<BaseCharacterAttr> combatants)
    {
        // 清空旧的头像
        foreach (var item in turnItemList)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        turnItemList.Clear();

        // 生成新的头像
        foreach (var combatant in combatants)
        {
            if (combatant == null) continue;

            GameObject itemObj = Instantiate(turnItemPrefab, itemParent);
            TurnItem item = itemObj.GetComponent<TurnItem>();

            if (item != null)
            {
                item.Init(combatant);
                turnItemList.Add(item);
            }
        }
    }

    /// <summary>
    /// 高亮当前行动的角色
    /// </summary>
    public void HighlightCurrentTurn(BaseCharacterAttr currentActor)
    {
        foreach (var item in turnItemList)
        {
            if (item != null)
            {
                item.SetHighlight(item.owner == currentActor);
            }
        }
    }
}