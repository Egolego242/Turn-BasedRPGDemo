using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 行动点显示条：以绿色(可用)/红色(已用)圆点直观展示当前角色的AP剩余量
/// </summary>
public class ActionPointBar : MonoBehaviour
{
    [Header("配置")]
    public GameObject apPointPrefab; // 行动点预制体（绿点/红点）
    public Transform pointParent; // 父物体（Horizontal Layout Group）
    public Color availableColor = Color.green; // 可用点颜色
    public Color usedColor = Color.red; // 已用点颜色

    // 更新行动点显示
    public void UpdateActionPoint(BaseCharacterAttr character)
    {
        // 清空旧的
        foreach (Transform child in pointParent) Destroy(child.gameObject);

        // 获取最大/当前行动点
        float maxAP = character.GetAttrValue(AttributeType.MaxAP);
        float currentAP = character.GetAttrValue(AttributeType.CurrentAP);

        // 生成行动点
        for (int i = 0; i < maxAP; i++)
        {
            GameObject pointObj = Instantiate(apPointPrefab, pointParent);
            Image pointImg = pointObj.GetComponent<Image>();
            // 小于当前AP的为可用（绿），否则为已用（红）
            pointImg.color = i < currentAP ? availableColor : usedColor;
        }
    }
}