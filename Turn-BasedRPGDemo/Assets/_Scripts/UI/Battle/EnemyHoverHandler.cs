using UnityEngine;

/// <summary>
/// 敌人悬停检测：鼠标进入敌人时在顶部显示血条面板，离开时隐藏
/// 挂载在敌人 GameObject 上（需要有 Collider）
/// </summary>
public class EnemyHoverHandler : MonoBehaviour
{
    [Header("悬停光标（可选）")]
    public Texture2D hoverCursor;
    public Vector2 cursorHotspot = Vector2.zero;

    private EnemyAttr _enemyAttr;

    private void Awake()
    {
        _enemyAttr = GetComponent<EnemyAttr>();
    }

    private void OnMouseEnter()
    {
        if (_enemyAttr == null || _enemyAttr.isDead) return;

        if (hoverCursor != null)
            Cursor.SetCursor(hoverCursor, cursorHotspot, CursorMode.Auto);

        EnemyInfoPanel.Instance?.Show(_enemyAttr);
    }

    private void OnMouseExit()
    {
        if (hoverCursor != null)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        EnemyInfoPanel.Instance?.Hide();
    }

    private void OnDisable()
    {
        // 防止物体被禁用后光标没恢复
        if (hoverCursor != null)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        EnemyInfoPanel.Instance?.Hide();
    }
}
