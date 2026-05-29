using UnityEngine;
using UnityEngine.AI;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance { get; private set; }

    [Header("核心配置")]
    public float interactDistance = 3f;

    [Header("引用")]
    public PlayerMovement playerMovement;
    // 【新增】直接引用NavMeshAgent，自己检测状态，不依赖PlayerMovement
    public NavMeshAgent playerNavAgent;
    public LLM chatLLM;

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

        // 【自动获取】如果没拖NavMeshAgent，自动从PlayerMovement里拿
        if (playerNavAgent == null && playerMovement != null)
        {
            playerNavAgent = playerMovement.GetComponent<NavMeshAgent>();
        }
    }

    private void Update()
    {
        if (GameStateMgr.Instance != null && GameStateMgr.Instance.IsBattleState())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI()) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                NPCDialog npc = hit.collider.GetComponent<NPCDialog>();
                if (npc != null)
                {
                    OnNPCClicked(npc);
                }
            }
        }

        // 【核心修改】直接检测NavMeshAgent的真实状态，不依赖IsPlayerMoving
        if (isMovingToNPC && currentNPC != null)
        {
            // 1. 先等NavMesh计算完路径
            if (playerNavAgent.pathPending)
            {
                return; // 还在计算路径，继续等
            }

            // 2. 路径计算完了，检测是否真的到达
            if (!playerNavAgent.pathPending && playerNavAgent.remainingDistance <= playerNavAgent.stoppingDistance)
            {
                // 3. 再确认一下速度是否为0
                if (!playerNavAgent.hasPath || playerNavAgent.velocity.sqrMagnitude == 0f)
                {
                    // 【确认】真的到达了
                    Debug.Log("【确认到达】NavMeshAgent检测到真正到达");
                    isMovingToNPC = false;
                    OpenDialogPanel();
                }
            }
        }
    }

    public void OnNPCClicked(NPCDialog npc)
    {
        ForceResetAllStates();

        if (DialogUIController.Instance.IsDialogOpen)
        {
            CloseDialog();
        }

        currentNPC = npc;
        Vector3 playerPos = playerMovement.transform.position;
        Vector3 npcPos = currentNPC.transform.position;
        float distance = Vector3.Distance(playerPos, npcPos);

        Debug.Log($"【位置】玩家：{playerPos}");
        Debug.Log($"【位置】NPC：{npcPos}");
        Debug.Log($"【距离】{distance:F2}米，交互距离：{interactDistance}米");
        Debug.Log($"【判断】是否小于交互距离：{distance <= interactDistance}");

        if (distance <= interactDistance)
        {
            Debug.Log("【分支】距离足够，直接开面板");
            OpenDialogPanel();
        }
        else
        {
            Debug.Log("【分支】距离不足，开始移动");
            isMovingToNPC = true;

            NavMesh.SamplePosition(npc.transform.position, out NavMeshHit hit, interactDistance, NavMesh.AllAreas);
            Debug.Log($"【移动目标】采样点：{hit.position}");

            playerMovement.MoveWithoutAP(hit.position);
        }
    }

    private void OpenDialogPanel()
    {
        if (currentNPC == null) return;
        Debug.Log($"【打开面板】{currentNPC.npcName}");

        if (chatLLM != null)
        {
            chatLLM.SetCurrentNPC(currentNPC);
        }

        DialogUIController.Instance.ShowPanel(
            currentNPC.npcAvatar,
            currentNPC.npcName,
            currentNPC.initialGreeting,
            currentNPC.npcPrompt
        );
    }

    public void CloseDialog()
    {
        Debug.Log("【关闭面板】");
        DialogUIController.Instance.HidePanel();
        ForceResetAllStates();
    }

    private void ForceResetAllStates()
    {
        currentNPC = null;
        isMovingToNPC = false;
    }

    private bool IsPointerOverUI()
    {
        if (playerMovement == null) return false;
        return playerMovement.CheckUIClick();
    }
}