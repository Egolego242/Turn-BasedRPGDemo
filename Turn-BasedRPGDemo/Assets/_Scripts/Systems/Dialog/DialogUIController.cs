using UnityEngine;
using UnityEngine.UI;
using TMPro; 

/// <summary>
/// 对话UI控制器，对接你现有的面板
/// </summary>
public class DialogUIController : MonoBehaviour
{
    public static DialogUIController Instance { get; private set; }

    [Header("你的现有面板组件")]
    [Tooltip("整个对话面板的根物体")]
    public GameObject dialogPanel;

    [Tooltip("面板上的NPC头像Image")]
    public Image avatarImage;

    [Tooltip("面板上的NPC名字Text")]
    public TextMeshProUGUI npcNameText; 

    [Tooltip("面板上的对话内容Text")]
    //public TextMeshProUGUI dialogContentText; 

    // 公开属性，供外部判断
    public bool IsDialogOpen => dialogPanel.activeSelf;

    // 保存当前人设，供后续发送AI消息用
    public string CurrentNpcPrompt { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 初始隐藏面板
        dialogPanel.SetActive(false);
    }

    /// <summary>
    /// 显示面板并更新内容
    /// </summary>
    public void ShowPanel(Sprite avatar, string npcName, string content, string prompt)
    {
        // 1. 保存人设
        CurrentNpcPrompt = prompt;

        // 2. 更新UI组件
        if (avatarImage != null) avatarImage.sprite = avatar;
        if (npcNameText != null) npcNameText.text = npcName;
        //if (dialogContentText != null) dialogContentText.text = content;

        // 3. 显示面板
        dialogPanel.SetActive(true);
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void HidePanel()
    {
        dialogPanel.SetActive(false);
    }
}