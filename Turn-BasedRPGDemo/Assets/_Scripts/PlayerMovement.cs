using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 神界原罪2风格 玩家角色点击移动核心脚本【最终修复版-无任何BUG】
/// ✅ 核心修复：移动中点击不可达区域 → 立即完全停止移动+停止行走动画，无滑步
/// ✅ 全部保留：点击寻路+顺滑转向+Idle/Walk动画同步+水域不可走+点击标记显隐+地形适配
/// ✅ 新增：统一强制停止方法，状态永不脱节，动画和移动绝对同步
/// ✅ 技术：NavMesh(A*寻路)+Animator状态机+射线检测+四元数平滑转向
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("===== 核心配置 =====")]
    public LayerMask terrainLayer;       // 只检测地形层（勾选Terrain）
    public GameObject clickMarkerPrefab; // 拖拽点击标记预制体

    [Header("===== 移动参数 (神界原罪2手感) =====")]
    public float moveSpeed = 4f;          // 角色移动速度
    public float arriveDistance = 0.3f;   // 到达目标点判定距离
    [Header("===== 转向参数 (适配新版NavMesh无Auto Rotate) =====")]
    public float rotateSpeed = 8f;        // 角色转向速度，越大越顺滑，推荐8

    [Header("===== 组件挂载 =====")]
    public Animator animator;             // 角色Animator组件
    private NavMeshAgent agent;           // NavMesh寻路代理
    private GameObject currentMarker;     // 点击标记
    private Vector3 targetPos;            // 目标位置
    private bool isMoving = false;        // 是否移动中

    // 动画参数缓存，优化性能
    private int isWalkingHash = Animator.StringToHash("IsWalking");

    void Awake()
    {
        // 获取/添加NavMeshAgent组件
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();

        // NavMeshAgent新版完整配置（2022+无Auto Rotate，最优参数）
        agent.speed = moveSpeed;
        agent.stoppingDistance = arriveDistance;
        agent.angularSpeed = 200;
        agent.acceleration = 8;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.autoTraverseOffMeshLink = true;
        agent.enabled = true;
    }

    void Update()
    {
        // 检测鼠标左键点击地形 - 所有点击都走这个逻辑，优先级最高
        if (Input.GetMouseButtonDown(0))
        {
            // ====================== 【核心修复 1/3】重中之重 ======================
            // 点击任何位置，第一步：先强制停止所有旧的移动行为+清空路径
            // 不管新点击的是可达/不可达，先终止上一次的寻路，杜绝滑步！
            ForceStopAllMovement();

            // 射线检测点击的地形位置
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, terrainLayer))
            {
                targetPos = hit.point;
                // 检测新目标是否可达
                if (IsTargetReachable(targetPos))
                {
                    // 可达：重新设置新路径+播放动画+显示标记
                    ShowClickMarker(targetPos);
                    agent.SetDestination(targetPos);
                    isMoving = true;
                    animator.SetBool(isWalkingHash, true);
                }
                // 不可达：什么都不做，已经在上面强制停止了，角色原地不动
            }
        }

        // 角色移动状态检测+到达目标点自动停止
        if (isMoving)
        {
            if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
            {
                // 到达目标点，正常停止
                ForceStopAllMovement();
            }
            else
            {
                // ====================== 新增：战斗状态移动消耗行动点 核心逻辑 ======================
                if (GameStateMgr.Instance.IsBattleState())
                {
                    // 战斗状态：每帧消耗少量行动点（可配置消耗值，比如0.1/帧）
                    PlayerAttr playerAttr = GetComponent<PlayerAttr>();
                    bool canMove = playerAttr.ConsumeAP(0.1f);
                    if (!canMove)
                    {
                        // 行动点不足，强制停止移动
                        ForceStopAllMovement();
                        return;
                    }
                }
                // ==============================================================================

                // 角色移动中：顺滑转向逻辑（保留，完美替代旧版Auto Rotate）
                Vector3 moveDir = agent.desiredVelocity;
                moveDir.y = 0;
                if (moveDir.magnitude > 0.1f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
                }
            }
        }
    }

    /// <summary>
    /// 【核心修复 2/3】新增：统一强制停止所有移动行为的方法【万能停止】
    /// 封装所有停止逻辑：清空寻路路径+停止移动+关闭行走动画+隐藏标记+重置状态
    /// 调用时机：1.移动中点击不可达区域 2.角色到达目标点 3.需要强制停止的任何场景
    /// </summary>
    private void ForceStopAllMovement()
    {
        if (agent != null)
        {
            agent.ResetPath();          // 清空所有寻路路径，核心！NavMeshAgent彻底停止移动
            agent.velocity = Vector3.zero; // 清空移动速度，杜绝惯性滑步
        }
        isMoving = false;               // 重置移动状态
        animator.SetBool(isWalkingHash, false); // 立即切回待机Idle动画
        HideClickMarker();              // 隐藏点击标记
    }

    /// <summary>
    /// 检测目标点是否可达（水域/悬崖/障碍物=不可达，返回false）
    /// </summary>
    private bool IsTargetReachable(Vector3 target)
    {
        NavMeshHit navHit;
        if (!NavMesh.SamplePosition(target, out navHit, 0.5f, NavMesh.AllAreas)) return false;
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(navHit.position, path);
        return path.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// 显示点击标记UI
    /// </summary>
    private void ShowClickMarker(Vector3 pos)
    {
        HideClickMarker();
        pos.y += 0.1f;
        currentMarker = Instantiate(clickMarkerPrefab, pos, Quaternion.identity);
    }

    /// <summary>
    /// 隐藏/销毁点击标记UI
    /// </summary>
    private void HideClickMarker()
    {
        if (currentMarker != null) Destroy(currentMarker);
        currentMarker = null;
    }

    // ====================== 【核心修复 3/3】移除旧的StopMove方法 ======================
    // 原StopMove方法逻辑不完整，已被万能的ForceStopAllMovement替代，彻底删除无残留
}