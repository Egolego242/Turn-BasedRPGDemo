using UnityEngine;

public class NPCClickHandler : MonoBehaviour
{
    [Header("悬停设置")]
    [Tooltip("鼠标悬停时的光标（可选，不填则不变）")]
    public Texture2D hoverCursor;

    [Tooltip("光标热点")]
    public Vector2 cursorHotspot = Vector2.zero;

    // 鼠标按下（这里只是转发给DialogManager，核心逻辑在DialogManager里）
    private void OnMouseDown()
    {
        NPCDialog npc = GetComponent<NPCDialog>();
        if (npc != null)
        {
            DialogManager.Instance.OnNPCClicked(npc);
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
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}