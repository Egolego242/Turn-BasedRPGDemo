using UnityEngine;

/// <summary>
/// 挂载到每个NPC物体上，存储该NPC的专属对话数据
/// </summary>
public class NPCDialog : MonoBehaviour
{
    [Header("NPC基础配置")]
    [Tooltip("NPC的唯一ID（不能重复）")]
    public string npcId = "npc_001";

    [Tooltip("NPC的显示名称")]
    public string npcName = "无名NPC";

    [Header("对话UI配置")]
    [Tooltip("NPC的头像图片（显示在对话面板）")]
    public Sprite npcAvatar;

    [Header("对话内容配置")]
    [Tooltip("NPC的初始问候语（打开面板时显示）")]
    [TextArea] public string initialGreeting = "你好，旅行者。";

    [Header("AI人设配置")]
    [Tooltip("NPC的人设提示词（传给大模型）")]
    [TextArea(3, 10)] public string npcPrompt = "你是一个中世纪的村民，性格温和，说话简洁。";
}