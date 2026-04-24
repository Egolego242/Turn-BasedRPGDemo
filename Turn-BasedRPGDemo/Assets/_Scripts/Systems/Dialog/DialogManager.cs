using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 对话系统核心管理器（简化版：完全复用PlayerMovement的移动逻辑）
/// </summary>
public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance { get; private set; }

    [Header("核心引用")]
    [Tooltip("交互距离（米）")]
    public float interactDistance = 3f;

    [Tooltip("直接拖入你场景里的PlayerMovement脚本")]
    public PlayerMovement playerMovement;

    // 内部变量
    private NPCDialog currentNPC;
    private bool isWaitingForArrival = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 外部调用：玩家点击了NPC
    /// </summary>
    public void OnNPCClicked(NPCDialog npc)
    {
        // 如果正在对话，先关闭
        if (DialogUIController.Instance.IsDialogOpen)
        {
            CloseDialog();
        }

        currentNPC = npc;
        float distance = Vector3.Distance(playerMovement.transform.position, npc.transform.position);

        if (distance <= interactDistance)
        {
            // 距离足够，直接打开
            OpenDialog();
        }
        else
        {
            // 距离不足：复用PlayerMovement的移动逻辑，让它移动到NPC身边
            MoveToNPC(npc.transform.position);
        }
    }

    private void MoveToNPC(Vector3 npcPos)
    {
        isWaitingForArrival = true;

        // 1. 计算目标点：在NPC周围找一个可达点
        NavMesh.SamplePosition(npcPos, out NavMeshHit hit, interactDistance, NavMesh.AllAreas);

        // 2. 【核心复用】直接调用PlayerMovement里的“移动到指定点”逻辑
        // 注意：需要你在PlayerMovement里加一个公共方法，专门供外部调用移动
        playerMovement.MoveToTargetPosition(hit.position);
    }

    private void Update()
    {
        // 【简化】不再自己写到达检测，而是监听PlayerMovement的“是否在移动”状态
        if (isWaitingForArrival && currentNPC != null)
        {
            // 监听PlayerMovement的isPlayerMoving（需要把这个变量改成public，或者加一个public属性）
            if (!playerMovement.IsPlayerMoving)
            {
                // PlayerMovement停止了，说明到达了
                isWaitingForArrival = false;
                OpenDialog();
            }
        }
    }

    private void OpenDialog()
    {
        if (currentNPC == null) return;

        // 通知UI显示面板
        DialogUIController.Instance.ShowPanel(
            currentNPC.npcAvatar,
            currentNPC.npcName,
            currentNPC.initialGreeting,
            currentNPC.npcPrompt
        );
    }

    public void CloseDialog()
    {
        DialogUIController.Instance.HidePanel();
        currentNPC = null;
        isWaitingForArrival = false;
    }
}