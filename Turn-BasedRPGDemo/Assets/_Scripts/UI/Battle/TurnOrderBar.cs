using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TurnOrderBar : MonoBehaviour
{
    [Header("配置")]
    public GameObject turnItemPrefab; // 回合头像预制体（包含Image、RawImage、灰化遮罩）
    public Transform itemParent; // 头像父物体（Horizontal Layout Group）

    private List<TurnItem> turnItemList = new List<TurnItem>();

    // 初始化回合顺序
    public void InitTurnOrder(List<BaseCharacterAttr> combatants)
    {
        // 清空旧的
        foreach (var item in turnItemList) Destroy(item.gameObject);
        turnItemList.Clear();

        // 生成新的头像
        foreach (var combatant in combatants)
        {
            GameObject itemObj = Instantiate(turnItemPrefab, itemParent);
            TurnItem item = itemObj.GetComponent<TurnItem>();
            item.Init(combatant);
            turnItemList.Add(item);
        }
    }

    // 高亮当前行动的角色
    public void HighlightCurrentTurn(BaseCharacterAttr currentActor)
    {
        foreach (var item in turnItemList)
        {
            item.SetHighlight(item.owner == currentActor);
        }
    }
}

// 单个回合头像类
public class TurnItem : MonoBehaviour
{
    public Image headIcon; // 角色头像
    public Image highlightFrame; // 高亮边框
    public GameObject deadMask; // 死亡灰化遮罩
    [HideInInspector] public BaseCharacterAttr owner;

    public void Init(BaseCharacterAttr character)
    {
        owner = character;
        // 可扩展：给headIcon赋值角色头像Sprite
        deadMask.SetActive(character.isDead);
        highlightFrame.gameObject.SetActive(false);
    }

    public void SetHighlight(bool isHighlight)
    {
        highlightFrame.gameObject.SetActive(isHighlight);
        deadMask.SetActive(owner.isDead);
    }
}