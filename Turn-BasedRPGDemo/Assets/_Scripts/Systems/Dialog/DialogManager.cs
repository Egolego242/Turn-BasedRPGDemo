using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance { get; private set; }

    [Header("核心配置")]
    [Tooltip("交互距离（米）")]
    public float interactDistance = 3f;

    [Header("引用")]
    [Tooltip("拖入场景里的PlayerMovement")]
    public PlayerMovement playerMovement;
    // 【新增】引用你的ChatDeepSeek脚本（拖入场景里的ChatDeepSeek物体）
    public LLM chatLLM;

    // 内部变量
    private NPCDialog currentNPC;
    private bool isMovingToNPC = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // 1. 只有探索状态才能点击NPC
        if (GameStateMgr.Instance != null && GameStateMgr.Instance.IsBattleState())
            return;

        // 2. 鼠标左键点击：判断是NPC还是地面
        if (Input.GetMouseButtonDown(0))
        {
            // 复用PlayerMovement里的UI检测（或者你自己的UI检测方法）
            if (IsPointerOverUI()) return;

            // 发射射线
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // 优先判断是否点击了NPC
                NPCDialog npc = hit.collider.GetComponent<NPCDialog>();
                if (npc != null)
                {
                    OnNPCClicked(npc);
                }
                // 如果没点到NPC，PlayerMovement的Update会自己处理点击地面，这里不管
            }
        }

        // 3. 检测是否移动到达NPC
        if (isMovingToNPC && currentNPC != null)
        {
            // 复用PlayerMovement的公共属性判断是否在移动
            if (!playerMovement.IsPlayerMoving)
            {
                isMovingToNPC = false;
                OpenDialogPanel();
            }
        }
    }

    // 处理点击NPC
    public void OnNPCClicked(NPCDialog npc)
    {
        // 如果正在对话，先关闭
        if (DialogUIController.Instance.IsDialogOpen)
        {
            CloseDialog();
            return;
        }

        currentNPC = npc;
        float distance = Vector3.Distance(playerMovement.transform.position, npc.transform.position);

        if (distance <= interactDistance)
        {
            // 距离足够，直接开面板
            OpenDialogPanel();
        }
        else
        {
            // 距离不足，【核心复用】调用PlayerMovement封装好的纯移动方法
            isMovingToNPC = true;

            // 计算目标点：在NPC周围找一个可达点
            NavMesh.SamplePosition(npc.transform.position, out NavMeshHit hit, interactDistance, NavMesh.AllAreas);

            // 调用你PlayerMovement里的MoveWithoutAP
            playerMovement.MoveWithoutAP(hit.position);
        }
    }

    // 【修改】打开面板时，把当前NPC传给AI系统
    private void OpenDialogPanel()
    {
        if (currentNPC == null) return;

        // 1. 先告诉AI系统：现在要跟这个NPC对话了
        if (chatLLM != null)
        {
            chatLLM.SetCurrentNPC(currentNPC);
        }

        // 2. 再显示UI面板
        DialogUIController.Instance.ShowPanel(
            currentNPC.npcAvatar,
            currentNPC.npcName,
            currentNPC.initialGreeting,
            currentNPC.npcPrompt
        );
    }

    // 关闭对话面板
    public void CloseDialog()
    {
        DialogUIController.Instance.HidePanel();
        currentNPC = null;
        isMovingToNPC = false;
    }

    // 辅助方法：UI穿透检测（直接复制你PlayerMovement里的IsMouseClickOnUI代码即可）
    private bool IsPointerOverUI()
    {
        if (playerMovement == null) return false;
        return playerMovement.CheckUIClick();
    }
}