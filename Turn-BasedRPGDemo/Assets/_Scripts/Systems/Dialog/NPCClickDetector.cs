using UnityEngine;

/// <summary>
/// 挂载到NPC物体上，处理点击和悬停
/// </summary>
public class NPCClickDetector : MonoBehaviour
{
    [Header("悬停设置")]
    [Tooltip("鼠标悬停时的光标")]
    public Texture2D hoverCursor;

    [Tooltip("光标热点（一般设为(0,0)）")]
    public Vector2 cursorHotspot = Vector2.zero;

    // 缓存组件
    private NPCDialog npcDialog;
    private Collider npcCollider;

    private void Awake()
    {
        npcDialog = GetComponent<NPCDialog>();
        npcCollider = GetComponent<Collider>();
    }

    // 鼠标按下
    private void OnMouseDown()
    {
        if (npcDialog != null)
        {
            DialogManager.Instance.OnNPCClicked(npcDialog);
        }
    }

    // 鼠标悬停进入
    private void OnMouseEnter()
    {
        if (hoverCursor != null)
        {
            Cursor.SetCursor(hoverCursor, cursorHotspot, CursorMode.Auto);
        }
    }

    // 鼠标悬停离开
    private void OnMouseExit()
    {
        if (hoverCursor != null)
        {
            // 恢复默认光标
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}