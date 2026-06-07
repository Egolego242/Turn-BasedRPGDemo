using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 范围可视化：用LineRenderer绘制移动范围(绿色圆)和技能射程(红色圆)，战斗中以世界空间文字提示移动消耗
/// </summary>
public class RangeVisualizer : MonoBehaviour
{
    [Header("配置")]
    public LineRenderer moveRangeLine; // 移动范围线
    public LineRenderer skillRangeLine; // 技能范围线
    public GameObject moveCostTipPrefab; // 移动消耗提示预制体（"2行动点数 9米"）
    public LayerMask terrainLayer; // 地形层

    private Camera mainCamera;
    private GameObject currentCostTip;

    private void Awake()
    {
        mainCamera = Camera.main;
        moveRangeLine.gameObject.SetActive(false);
        skillRangeLine.gameObject.SetActive(false);
    }

    // 显示移动范围（玩家回合时调用）
    public void ShowMoveRange(BaseCharacterAttr player)
    {
        // ✅ 优化：安全类型转换+判空
        PlayerAttr playerAttr = player as PlayerAttr;
        if (playerAttr == null)
        {
            Debug.LogWarning("ShowMoveRange：传入的角色不是PlayerAttr类型！");
            return;
        }

        // ✅ 修复：现在可以正常访问 moveCostPerUnit 了
        float currentAP = player.GetAttrValue(AttributeType.CurrentAP);
        float maxMoveDistance = currentAP / playerAttr.moveCostPerUnit;

        DrawCircle(moveRangeLine, player.transform.position, maxMoveDistance, Color.green);
        moveRangeLine.gameObject.SetActive(true);
    }

    // 显示技能范围
    public void ShowSkillRange(SkillBase skill)
    {
        PlayerAttr player = FindObjectOfType<PlayerAttr>();
        if (player == null) return;

        DrawCircle(skillRangeLine, player.transform.position, skill.skillRange, Color.red);
        skillRangeLine.gameObject.SetActive(true);
    }

    // 隐藏所有范围
    public void HideAllRange()
    {
        moveRangeLine.gameObject.SetActive(false);
        skillRangeLine.gameObject.SetActive(false);
        if (currentCostTip != null) Destroy(currentCostTip);
    }

    // 绘制圆形范围
    private void DrawCircle(LineRenderer line, Vector3 center, float radius, Color color)
    {
        line.positionCount = 50;
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.startColor = color;
        line.endColor = color;

        for (int i = 0; i < 50; i++)
        {
            float angle = i * Mathf.PI * 2 / 50;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            line.SetPosition(i, center + new Vector3(x, 0.1f, z));
        }
    }

    // 更新移动消耗提示（鼠标悬停地面时调用）
    public void UpdateMoveCostTip(Vector3 hitPos, float cost, float distance)
    {
        if (currentCostTip == null)
        {
            currentCostTip = Instantiate(moveCostTipPrefab, transform);
        }
        currentCostTip.transform.position = hitPos + Vector3.up * 0.5f;
        currentCostTip.GetComponent<TextMesh>().text = $"{cost}行动点数\n{distance:F1}米";
    }
}