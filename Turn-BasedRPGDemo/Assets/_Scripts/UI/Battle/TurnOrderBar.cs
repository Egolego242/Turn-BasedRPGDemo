using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// 回合顺序条：顶部头像排列条，战斗开始时初始化，每回合按先攻值排序刷新，当前行动角色高亮置前，死亡角色隐藏
/// </summary>
public class TurnOrderBar : MonoBehaviour
{
    [Header("配置")]
    public GameObject turnItemPrefab;
    public Transform itemParent;

    private List<TurnItem> turnItemList = new List<TurnItem>();

    /// <summary>
    /// 战斗开始时生成头像
    /// </summary>
    public void InitTurnOrder(List<BaseCharacterAttr> combatants)
    {
        // 清空旧头像
        foreach (var item in turnItemList)
            Destroy(item.gameObject);
        turnItemList.Clear();

        // 生成所有角色头像
        foreach (var character in combatants)
        {
            GameObject go = Instantiate(turnItemPrefab, itemParent);
            TurnItem item = go.GetComponent<TurnItem>();
            item.Init(character);
            turnItemList.Add(item);
        }
    }

    /// <summary>
    /// 【核心】每次回合都重新排序
    /// 1. 只保留存活角色
    /// 2. 按最新先攻值从高到低排
    /// 3. 当前角色强制放最左
    /// 4. 死亡角色直接不显示
    /// </summary>
    public void UpdateTurnOrder(BaseCharacterAttr currentActor)
    {
        if (currentActor == null) return;

        // ==========================
        // 1. 只拿【存活】的角色（死亡直接排除）
        // ==========================
        var aliveCharacters = turnItemList
            .Where(item => item.owner != null && !item.owner.isDead)
            .Select(item => item.owner)
            .ToList();

        // ==========================
        // 2. 按【最新先攻值】从高到低排序
        // ==========================
        var sortedByInitiative = aliveCharacters
            .OrderByDescending(c => c.GetInitiative())
            .ToList();

        // ==========================
        // 3. 当前角色 → 强制放最左边
        // ==========================
        sortedByInitiative.Remove(currentActor);
        sortedByInitiative.Insert(0, currentActor);

        // ==========================
        // 4. 根据新顺序重新排列头像
        // ==========================
        foreach (var character in sortedByInitiative)
        {
            TurnItem item = turnItemList.FirstOrDefault(i => i.owner == character);
            if (item != null)
                item.transform.SetAsLastSibling();
        }

        // ==========================
        // 5. 隐藏所有死亡角色的头像（彻底不显示）
        // ==========================
        foreach (var item in turnItemList)
        {
            bool isDead = item.owner != null && item.owner.isDead;
            item.gameObject.SetActive(!isDead);
        }

        // 刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemParent as RectTransform);
    }

    // 旧方法保留，防止报错
    public void HighlightCurrentTurn(BaseCharacterAttr currentActor) { }
}