using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// 敌人属性类 - 行为树专属适配版
/// 所有AI逻辑完全交由Behavior Designer行为树控制
/// 保留回合制战斗核心规则、属性系统、导航系统，提供行为树可调用的所有方法/条件判断
/// </summary>
public class EnemyAttr : BaseCharacterAttr
{
    #region 核心组件引用
    [Header("===== 行为树核心配置 =====")]
    public BehaviorTree behaviorTree; // 拖拽敌人身上的行为树组件
    [HideInInspector] public Animator animator;
    [HideInInspector] public NavMeshAgent navAgent;
    #endregion

    #region 基础属性配置
    [Header("===== 敌人初始属性 =====")]
    public float initMaxHP = 80;
    public float initMaxMP = 30;
    public int initMaxAP = 8;
    public float initStrength = 6;
    public float initIntelligence = 3;
    public float initArmor = 2;
    // 先加一个移动锁（和isDead同级的类字段）
    private bool isMoving = false;
    // 本回合是否已经执行过移动
    private bool hasMovedThisTurn = false;
    #endregion

    #region 掉落配置
    [Header("===== 掉落配置 =====")]
    public DropTable dropTable;
    public int dropEXP = 20;
    #endregion

    #region 行动消耗规则（和行为树节点完全对应）
    [Header("===== 行动消耗规则 =====")]
    public float moveCostPerUnit = 1f; // 每移动1单位消耗的行动点
    public int normalAttackCost = 2; // 普攻消耗行动点（行为树普攻判断用）
    public int skillAttackCost = 3; // 技能消耗行动点（行为树技能判断用）
    [Header("===== 战斗配置 =====")]
    public float attackRange = 2f; // 普攻射程
    public float skillRange = 5f; // 技能射程
    public Transform attackTarget; // 攻击目标（行为树赋值玩家Transform）
    #endregion

    #region 巡逻&警戒配置
    [Header("===== 巡逻配置 =====")]
    private bool isPatrolling = false;
    public float patrolRange = 5f; // 巡逻范围
    public float patrolWaitTime = 2f; // 巡逻停留时间
    public float moveArriveDistance = 0.5f; // 判定到达目标的距离阈值
    private Vector3 originPos; // 出生初始位置
    private Coroutine patrolCoroutine;

    [Header("===== 警戒圈配置 =====")]
    public float enemyDetectRange = 8f; // 战斗触发警戒范围
    public bool showEnemyRangeGizmo = true; // Scene视图显示警戒圈
    #endregion

    #region 技能冷却
    [Header("===== 技能冷却 =====")]
    public float skillCoolDown = 2; // 技能冷却回合数
    [HideInInspector] public float skillCoolDownLeft = 0; // 剩余冷却回合（行为树可读取）
    #endregion

    [ContextMenu("手动触发战斗")]
    public void TestTriggerBattle()
    {
        TriggerBattleByEnemy();
    }

    #region 初始化
    protected override void Awake()
    {
        base.Awake();
        // 组件自动获取+容错
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        behaviorTree = GetComponent<BehaviorTree>();

        // 根物体无组件时查找子物体
        if (behaviorTree == null)
        {
            behaviorTree = GetComponentInChildren<BehaviorTree>();
            if (behaviorTree == null)
            {
                Debug.LogError($"{gameObject.name} 未找到BehaviorTree组件！请检查挂载", this);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} 行为树挂载在子物体，建议移至根物体", this);
            }
        }

        // 自动给行为树的selfObject赋值（核心！不用手动拖）
        if (behaviorTree != null)
        {
            // 旧版插件通用写法：找到变量→赋值
            var selfVar = behaviorTree.GetVariable("selfObject");
            if (selfVar != null) selfVar.SetValue(gameObject);
            // 初始化战斗状态为false
            var combatVar = behaviorTree.GetVariable("isInCombat");
            if (combatVar != null) combatVar.SetValue(false);
        }

        // 初始化属性（调用基类方法）
        InitAttribute(initMaxHP, initMaxMP, initMaxAP, initStrength, initIntelligence, initArmor);
        currentCamp = CampType.Enemy;
        //originPos = transform.position;
        NavMeshHit originHit;
        if (NavMesh.SamplePosition(transform.position, out originHit, 1f, NavMesh.AllAreas))
        {
            originPos = originHit.position; // 强制校准到NavMesh上
        }
        else
        {
            originPos = transform.position;
            Debug.LogWarning($"{gameObject.name} 出生点不在NavMesh上，巡逻可能异常！", this);
        }

        // 绑定回合结束事件（自动减少技能冷却）
        if (TurnBattleManager.Instance != null)
        {
            TurnBattleManager.Instance.OnTurnEnd += ReduceSkillCoolDown;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} 未找到回合管理器，技能冷却无法自动减少", this);
        }

        // 调试日志：打印初始化状态
        Debug.Log($"{gameObject.name} 行为树初始化 - isInBattle:{isInBattle} | isDead:{isDead}", this);
        InitBehaviorTreeState();
    }

    private void Update()
    {
        // 1. 同步状态到行为树（核心！行为树的条件判断全靠这个）
        SyncStateToBehaviorTree();

        // 2. 敌人巡逻时的行走动画（顺便检查动画）
        if (navAgent != null && animator != null && !isDead)
        {
            bool isMoving = navAgent.velocity.magnitude > 0.1f;
            animator.SetBool("IsWalking", isMoving);
        }
    }

    /// <summary>
    /// 核心：实时同步所有状态到行为树（抽离成独立方法，便于维护）
    /// </summary>
    private void SyncStateToBehaviorTree()
    {
        if (behaviorTree == null) return;

        // 基础战斗状态
        behaviorTree.SetVariableValue("isInCombat", isInBattle);
        behaviorTree.SetVariableValue("isMyTurn", isMyTurn);
        behaviorTree.SetVariableValue("isDead", isDead);

        // 数值型状态
        behaviorTree.SetVariableValue("currentAP", GetAttrIntValue(AttributeType.CurrentAP));
        behaviorTree.SetVariableValue("skillCooldown", skillCoolDownLeft);

        // 目标相关
        if (attackTarget != null)
        {
            behaviorTree.SetVariableValue("targetPlayer", attackTarget.gameObject);
        }

        // 关键：实时同步isHostile（确保行为树能一直判断自身是否为敌对/玩家）
        var hostileVar = behaviorTree.GetVariable("isHostile");
        if (hostileVar != null)
        {
            // 敌人永远是hostile=true，玩家为false（可根据需求调整逻辑）
            hostileVar.SetValue(true);
        }

        // 额外：同步当前血量（可选，便于行为树做血量相关决策）
        var currentHPVar = behaviorTree.GetVariable("currentHP");
        if (currentHPVar != null)
        {
            currentHPVar.SetValue(GetAttrValue(AttributeType.CurrentHP));
        }
    }

    // 销毁时解绑事件，避免内存泄漏
    private void OnDestroy()
    {
        if (TurnBattleManager.Instance != null)
        {
            TurnBattleManager.Instance.OnTurnEnd -= ReduceSkillCoolDown;
        }
    }

    /// <summary>
    /// 初始化行为树状态
    /// </summary>
    private void InitBehaviorTreeState()
    {
        if (behaviorTree == null)
        {
            Debug.LogWarning($"{gameObject.name} 行为树为空，跳过初始化", this);
            return;
        }

        // 强制初始化战斗/死亡状态（避免基类默认值异常）
        isInBattle = false;
        isDead = false;

        // 非战斗状态启动行为树
        if (!isInBattle)
        {
            behaviorTree.EnableBehavior();
            Debug.Log($"{gameObject.name} 启用行为树日常分支", this);
            StartPatrol();
        }
        else
        {
            behaviorTree.DisableBehavior();
            Debug.Log($"{gameObject.name} 禁用行为树（战斗状态）", this);
        }
    }
    #endregion

    #region 行为树 - 日常分支核心方法（巡逻/警戒/触发战斗）
    /// <summary>
    /// 【行为树调用】启动巡逻协程
    /// </summary>
    public void StartPatrol()
    {
        if (isInBattle || isDead) return;
        // 核心：如果已经在巡逻，直接返回，不重复启动
        if (isPatrolling)
        {
            return;
        }
        if (patrolCoroutine != null)
            StopCoroutine(patrolCoroutine);

        patrolCoroutine = StartCoroutine(PatrolCoroutine());
        isPatrolling = true; // 标记为正在巡逻
        //Debug.Log("开始巡逻");
    }

    /// <summary>
    /// 【行为树调用】停止巡逻
    /// </summary>
    public void StopPatrol()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }
        isPatrolling = false;
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            navAgent.ResetPath();
            navAgent.isStopped = true;
        }
        Debug.Log("停止巡逻");
    }

    /// <summary>
    /// 巡逻协程（行为树日常分支驱动）
    /// </summary>
    //private IEnumerator PatrolCoroutine()
    //{
    //    while (!isInBattle && !isDead)
    //    {
    //        // 生成随机巡逻点
    //        Vector3 randomDir = Random.insideUnitSphere * patrolRange;
    //        randomDir.y = 0;
    //        Vector3 targetPos = originPos + randomDir;

    //        // 验证目标点是否在NavMesh上
    //        NavMeshHit hit;
    //        bool isPosValid = NavMesh.SamplePosition(targetPos, out hit, patrolRange, NavMesh.AllAreas);
    //        if (isPosValid && navAgent != null && navAgent.isActiveAndEnabled)
    //        {
    //            targetPos = hit.position;
    //            navAgent.isStopped = false;
    //            navAgent.SetDestination(targetPos);

    //            // 等待到达目标点（距离<0.5f 或 寻路失败）
    //            while (navAgent.remainingDistance > 0.5f && navAgent.pathStatus == NavMeshPathStatus.PathComplete && !isInBattle && !isDead)
    //            {
    //                yield return null; // 每一帧检查状态
    //            }
    //        }
    //        else
    //        {
    //            Debug.LogWarning($"{gameObject.name} 巡逻点无效，跳过本次巡逻", this);
    //        }

    //        // 停留指定时间
    //        yield return new WaitForSeconds(patrolWaitTime);
    //    }
    //    patrolCoroutine = null; // 协程结束后重置引用
    //}

    /// <summary>
    /// 生成有效巡逻点（仅生成一次，确保在NavMesh上且符合范围）
    /// </summary>
    private Vector3 GenerateValidPatrolPoint()
    {
        // 生成平面随机方向（Y轴归零）
        Vector3 randomDir = Random.insideUnitSphere * patrolRange;
        randomDir.y = 0;
        Vector3 candidatePos = originPos + randomDir;

        // 确保最小距离
        float distance = Vector3.Distance(originPos, candidatePos);
        if (distance < patrolRange * 0.2f)
        {
            randomDir = Random.insideUnitSphere * patrolRange;
            randomDir.y = 0;
            candidatePos = originPos + randomDir;
        }

        // NavMesh采样
        NavMeshHit hit;
        if (NavMesh.SamplePosition(candidatePos, out hit, 3f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        Debug.LogWarning("巡逻点采样失败");
        return Vector3.zero;
    }

    /// <summary>
    /// 核心巡逻循环：生成目标→移动到位→停留→循环
    /// 全程不中途更换目标，必须走到才会生成下一个
    /// </summary>
    private IEnumerator PatrolCoroutine()
    {
        while (!isInBattle && !isDead)
        {
            // 1. 生成唯一有效巡逻点（仅生成一次）
            Vector3 targetPos = GenerateValidPatrolPoint();
            if (targetPos == Vector3.zero)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // 2. 移动到目标点（全程不换目标）
            navAgent.isStopped = false;
            navAgent.SetDestination(targetPos);

            bool isArrived = false;
            float timeout = 10f;
            float timer = 0f;

            while (timer < timeout && !isInBattle && !isDead)
            {
                timer += Time.deltaTime;
                if (navAgent.remainingDistance < moveArriveDistance || !navAgent.hasPath)
                {
                    isArrived = true;
                    break;
                }
                yield return null;
            }

            // 3. 到达后停留（核心：单独的停留逻辑）
            if (isArrived && !isInBattle && !isDead)
            {
                navAgent.isStopped = true;
                yield return new WaitForSeconds(patrolWaitTime); // 停指定秒数
            }
            else
            {
                navAgent.ResetPath();
                navAgent.isStopped = true;
                yield return new WaitForSeconds(1f);
            }
        }
        isPatrolling = false; 
        patrolCoroutine = null;
        navAgent.isStopped = true;
    }

    /// <summary>
    /// 【行为树条件判断】玩家是否在警戒范围内
    /// </summary>
    public bool IsPlayerInDetectRange()
    {
        // 1. 找玩家对象（兼容多玩家/玩家死亡）
        PlayerAttr player = FindObjectOfType<PlayerAttr>();
        if (player == null || player.isDead)
        {
            Debug.Log($"{gameObject.name} 没找到存活的玩家", this);
            return false;
        }

        // 2. 计算距离（忽略Y轴，只算平面距离）
        Vector3 enemyPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPos = new Vector3(player.transform.position.x, 0, player.transform.position.z);
        float distance = Vector3.Distance(enemyPos, playerPos);

        // 3. 打印调试日志（关键！看距离和警戒范围）
        //Debug.Log($"{gameObject.name} 玩家距离：{distance:F1} | 警戒范围：{enemyDetectRange}", this);

        // 4. 判断是否在范围内
        bool inRange = distance <= enemyDetectRange;
        return inRange;
    }

    /// <summary>
    /// 【行为树条件判断】玩家是否存在且存活
    /// </summary>
    public bool IsPlayerExistAndAlive()
    {
        PlayerAttr player = FindObjectOfType<PlayerAttr>();
        return player != null && !player.isDead;
    }

    /// <summary>
    /// 【行为树调用】判断目标是否为玩家（配合isHostile使用）
    /// </summary>
    public bool IsTargetPlayer(GameObject targetObj)
    {
        if (targetObj == null) return false;
        return targetObj.GetComponent<PlayerAttr>() != null;
    }


    /// <summary>
    /// 【行为树调用】敌人主动触发战斗
    /// </summary>
    public void TriggerBattleByEnemy()
    {
        PlayerAttr player = FindObjectOfType<PlayerAttr>();
        if (player == null || TurnBattleManager.Instance == null)
        {
            Debug.LogWarning($"{gameObject.name} 触发战斗失败：玩家/回合管理器为空", this);
            return;
        }

        isInBattle = true;
        Debug.Log($"{gameObject.name} 进入战斗状态，isInBattle设为True", this);

        // ========== 核心新增2：给行为树的isInCombat变量赋值（旧版适配） ==========
        if (behaviorTree != null)
        {
            // 旧版BehaviorTree设置变量的通用写法（兼容所有版本）
            var combatVar = behaviorTree.GetVariable("isInCombat");
            if (combatVar != null)
            {
                combatVar.SetValue(true); // 设为战斗状态
                Debug.Log($"{gameObject.name} 行为树isInCombat设为True", this);
            }
            else
            {
                Debug.LogError($"{gameObject.name} 行为树里没有isInCombat变量！", this);
            }

            // 停止日常行为树（保留你的原有逻辑）
            behaviorTree.DisableBehavior();
            // 重新启用行为树，让变量变化立刻生效
            behaviorTree.EnableBehavior();
        }

        // 停止巡逻+禁用行为树日常分支
        StopPatrol();
        //if (behaviorTree != null)
        //{
        //    behaviorTree.DisableBehavior();
        //    Debug.Log($"{gameObject.name} 触发战斗，禁用日常行为树", this);
        //}

        // 收集同范围内所有存活敌人
        List<BaseCharacterAttr> battleEnemies = new List<BaseCharacterAttr>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, enemyDetectRange);
        foreach (var col in colliders)
        {
            EnemyAttr enemy = col.GetComponent<EnemyAttr>();
            if (enemy != null && !enemy.isDead)
                battleEnemies.Add(enemy);
        }

        // 没有敌人则加入自己
        if (battleEnemies.Count == 0)
            battleEnemies.Add(this);

        // 切换全局游戏状态+触发回合制战斗
        GameStateMgr.Instance?.SwitchGameState(GameStateMgr.GamePlayState.BattleState);
        TurnBattleManager.Instance.TriggerBattle(player, battleEnemies);

        // 给自身赋值攻击目标
        attackTarget = player.transform;
    }
    #endregion

    #region 行为树 - 战斗分支核心方法（技能/普攻/移动/回合控制）
    /// <summary>
    /// 【行为树条件判断】是否处于战斗状态
    /// </summary>
    public bool IsInBattle() => isInBattle;

    /// <summary>
    /// 【行为树条件判断】是否处于当前行动回合
    /// </summary>
    public bool IsMyTurn() => isMyTurn;

    /// <summary>
    /// 【行为树条件判断】技能冷却是否就绪
    /// </summary>
    public bool IsSkillCoolDownReady() => skillCoolDownLeft <= 0;

    /// <summary>
    /// 【行为树条件判断】行动点是否足够释放技能
    /// </summary>
    public bool HasEnoughAPForSkill()
    {
        return GetAttrIntValue(AttributeType.CurrentAP) >= skillAttackCost;
    }

    /// <summary>
    /// 【行为树条件判断】行动点是否足够释放普攻
    /// </summary>
    public bool HasEnoughAPForAttack()
    {
        return GetAttrIntValue(AttributeType.CurrentAP) >= normalAttackCost;
    }

    /// <summary>
    /// 【行为树条件判断】目标是否在普攻射程内
    /// </summary>
    public bool IsTargetInAttackRange()
    {
        if (attackTarget == null) return false;
        float distance = Vector3.Distance(transform.position, attackTarget.position);
        Debug.Log($"{gameObject.name}距离玩家{distance}m");
        return distance <= attackRange;
    }

    /// <summary>
    /// 【行为树条件判断】目标是否在技能射程内
    /// </summary>
    public bool IsTargetInSkillRange()
    {
        if (attackTarget == null) return false;
        float distance = Vector3.Distance(transform.position, attackTarget.position);
        Debug.Log($"{gameObject.name}距离玩家{distance}m");
        return distance <= skillRange;
    }
    #region 整合判断(技能普攻和移动)
    /// <summary>
    /// 【行为树调用】合并判断技能所有就绪条件（冷却+AP+射程），结果存到行为树isSkillReady变量
    /// </summary>
    public void CheckSkillReady()
    {
        bool isReady = !isDead && attackTarget != null && IsSkillCoolDownReady() && HasEnoughAPForSkill() && IsTargetInSkillRange();
        if (behaviorTree != null)
        {
            var skillReadyVar = behaviorTree.GetVariable("isSkillReady");
            if (skillReadyVar != null)
            {
                skillReadyVar.SetValue(isReady);
                // 调试日志，看条件是否满足
                Debug.Log($"{gameObject.name} 技能就绪判断：{isReady}（冷却：{IsSkillCoolDownReady()} | AP足够：{HasEnoughAPForSkill()} | 射程够：{IsTargetInSkillRange()}）", this);
            }
            else
            {
                Debug.LogError($"{gameObject.name} 行为树未找到isSkillReady变量，请先创建！", this);
            }
        }
    }

    /// <summary>
    /// 【行为树调用】合并判断普攻所有就绪条件（AP+射程），结果存到行为树isAttackReady变量
    /// </summary>
    public void CheckAttackReady()
    {
        bool isReady = !isDead && attackTarget != null && HasEnoughAPForAttack() && IsTargetInAttackRange();
        if (behaviorTree != null)
        {
            var attackReadyVar = behaviorTree.GetVariable("isAttackReady");
            if (attackReadyVar != null)
            {
                attackReadyVar.SetValue(isReady);
                Debug.Log($"{gameObject.name} 普攻就绪判断：{isReady}（AP足够：{HasEnoughAPForAttack()} | 射程够：{IsTargetInAttackRange()}）", this);
            }
            else
            {
                Debug.LogError($"{gameObject.name} 行为树未找到isAttackReady变量，请先创建！", this);
            }
        }
    }

    /// <summary>
    /// 【行为树调用】合并判断移动所有就绪条件（AP+目标+导航），结果存到行为树isMoveReady变量
    /// </summary>
    public void CheckMoveReady()
    {
        bool isReady = !isDead && attackTarget != null && navAgent != null && navAgent.isActiveAndEnabled && GetAttrValue(AttributeType.CurrentAP) >= 1f && !hasMovedThisTurn && !IsTargetInAttackRange();
        if (behaviorTree != null)
        {
            var moveReadyVar = behaviorTree.GetVariable("isMoveReady");
            if (moveReadyVar != null)
            {
                moveReadyVar.SetValue(isReady);
                Debug.Log($"{gameObject.name} 移动就绪判断：{isReady}（有目标：{attackTarget != null} | 导航有效：{navAgent != null && navAgent.isActiveAndEnabled} | AP≥1：{GetAttrValue(AttributeType.CurrentAP) >= 1f}）", this);
            }
            else
            {
                Debug.LogError($"{gameObject.name} 行为树未找到isMoveReady变量，请先创建！", this);
            }
        }
    }

    
    #endregion

    /// <summary>
    /// 【行为树调用】释放技能（返回是否释放成功）
    /// </summary>
    public bool CastSkill()
    {
        // 全量校验
        if (isDead || attackTarget == null || !IsSkillCoolDownReady()
            || !HasEnoughAPForSkill() || !IsTargetInSkillRange())
        {
            Debug.Log($"{gameObject.name} 技能释放条件不满足");
            return false;
        }

        // 消耗行动点
        if (!ConsumeAP(skillAttackCost)) return false;

        // 播放技能动画
        animator?.SetTrigger("Skill");
        PlaySkillAnim();

        // 技能伤害计算
        if (attackTarget.TryGetComponent<BaseCharacterAttr>(out BaseCharacterAttr targetAttr))
        {
            float intelligence = GetAttrValue(AttributeType.Intelligence);
            float armor = targetAttr.GetAttrValue(AttributeType.Armor);
            float damage = Mathf.Max(1, intelligence * 1.5f - armor * 0.5f);
            targetAttr.TakeDamage(damage);
            Debug.Log($"{gameObject.name} 释放技能攻击 {attackTarget.name}，造成 {damage} 伤害");
        }

        // 触发冷却
        skillCoolDownLeft = skillCoolDown;
        // 攻击后检查战斗是否结束
        TurnBattleManager.Instance?.CheckBattleEnd();
        HasAvailableAction();
        return true;
    }

    /// <summary>
    /// 【行为树调用】执行普攻（返回是否释放成功）
    /// </summary>
    public bool DoNormalAttack()
    {
        // 全量校验
        if (isDead || attackTarget == null || !HasEnoughAPForAttack() || !IsTargetInAttackRange())
        {
            Debug.Log($"{gameObject.name} 普攻条件不满足");
            return false;
        }

        // 消耗行动点
        if (!ConsumeAP(normalAttackCost)) return false;

        // 播放普攻动画
        animator?.SetTrigger("Attack");
        PlayAttackAnim();
        Debug.Log($"{gameObject.name} 播放普攻动画！", this);

        // 普攻伤害计算
        if (attackTarget.TryGetComponent<BaseCharacterAttr>(out BaseCharacterAttr targetAttr))
        {
            float strength = GetAttrValue(AttributeType.Strength);
            float armor = targetAttr.GetAttrValue(AttributeType.Armor);
            float damage = Mathf.Max(1, strength - armor);
            targetAttr.TakeDamage(damage);
            Debug.Log($"{gameObject.name} 普攻 {attackTarget.name}，造成 {damage} 伤害");
        }

        // 攻击后检查战斗是否结束
        TurnBattleManager.Instance?.CheckBattleEnd();
        HasAvailableAction();
        return true;
    }

    /// <summary>
    /// 计算移动消耗的行动点（每单位距离消耗1AP，向上取整）
    /// </summary>
    public override int CalculateMoveAPCost(float distance)
    {
        if (distance <= 0) return 0; // 无距离不消耗
        int cost = Mathf.CeilToInt(distance * moveCostPerUnit);
        return Mathf.Max(1, cost); // 保底：至少消耗1AP（避免短距离0消耗）
    }

    /// <summary>
    /// 向目标移动（根治版：目标点必在NavMesh上，基于路径长度判定完成）
    /// </summary>
    public bool MoveToTarget()
    {
        // 1. 移动锁：避免重复调用
        if (isMoving)
        {
            Debug.Log($"{gameObject.name} 正在移动，跳过重复调用", this);
            return false;
        }

        // 2. 基础校验
        if (isDead || attackTarget == null || navAgent == null || !navAgent.isActiveAndEnabled || !navAgent.isOnNavMesh)
        {
            behaviorTree.SetVariableValue("isMoveReady", false);
            Debug.Log($"{gameObject.name} 移动校验失败：死亡/无目标/导航失效", this);
            return false;
        }

        // 3. 已在攻击范围 → 不移动
        if (IsTargetInAttackRange())
        {
            behaviorTree.SetVariableValue("isMoveReady", false);
            return false;
        }

        // 4. AP校验
        int currentAP = GetAttrIntValue(AttributeType.CurrentAP);
        if (currentAP <= 0)
        {
            behaviorTree.SetVariableValue("isMoveReady", false);
            Debug.Log($"{gameObject.name} 移动失败：AP耗尽（{currentAP}）", this);
            return false;
        }

        // ===================== 核心修复1：目标点强制落在NavMesh上 =====================
        Vector3 enemyPos = transform.position;
        Vector3 targetPos = attackTarget.position;
        Vector3 dir = (targetPos - enemyPos).normalized;

        // 理想目标点（攻击范围边缘，留0.1容错）
        Vector3 idealTargetPos = targetPos - dir * (attackRange - 0.1f);
        Vector3 finalTargetPos = idealTargetPos;

        // 强制采样NavMesh，修正目标点
        NavMeshHit hit;
        if (NavMesh.SamplePosition(idealTargetPos, out hit, 2f, NavMesh.AllAreas))
        {
            finalTargetPos = hit.position; // 修正为NavMesh上的有效点
        }
        else
        {
            // 极端情况：采样失败，退到玩家正前方1m处（再次采样）
            finalTargetPos = enemyPos + dir * (Vector3.Distance(enemyPos, targetPos) - 1f);
            NavMesh.SamplePosition(finalTargetPos, out hit, 1f, NavMesh.AllAreas);
            finalTargetPos = hit.position;
        }
        Debug.Log($"{gameObject.name} 目标点修正：理想={idealTargetPos} → 实际={finalTargetPos}", this);

        // ===================== 核心修复2：计算真实NavMesh路径长度 =====================
        NavMeshPath path = new NavMeshPath();
        navAgent.CalculatePath(finalTargetPos, path);

        // 路径无效 → 直接返回
        if (path.status != NavMeshPathStatus.PathComplete)
        {
            behaviorTree.SetVariableValue("isMoveReady", false);
            Debug.LogError($"{gameObject.name} 路径无效（状态：{path.status}），放弃移动", this);
            return false;
        }

        // 计算路径总长度（不是直线距离！）
        float totalPathLength = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            totalPathLength += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        Debug.Log($"{gameObject.name} 路径总长度：{totalPathLength:F1}m（直线距离：{Vector3.Distance(enemyPos, finalTargetPos):F1}m）", this);

        // ===================== 计算移动消耗 + 扣AP =====================
        int moveCost = CalculateMoveAPCost(totalPathLength);
        if (moveCost > currentAP)
        {
            behaviorTree.SetVariableValue("isMoveReady", false);
            Debug.LogWarning($"{gameObject.name} 移动失败：AP不足（需要{moveCost}，当前{currentAP}）", this);
            return false;
        }

        // 扣AP失败 → 终止
        if (!ConsumeAP(moveCost))
        {
            behaviorTree.SetVariableValue("isMoveReady", false);
            Debug.LogError($"{gameObject.name} 移动失败：AP消耗失败", this);
            return false;
        }

        // ===================== 启动移动 + 解锁协程 =====================
        isMoving = true;
        navAgent.ResetPath();
        navAgent.isStopped = false;
        navAgent.SetDestination(finalTargetPos);

        // 启动精准判定的协程（传入目标点+路径长度）
        StartCoroutine(WaitForMoveComplete(finalTargetPos, totalPathLength));

        Debug.Log($"{gameObject.name} 启动移动：目标={finalTargetPos}，消耗{moveCost}AP", this);
        // 移动成功后，标记本回合已移动
        hasMovedThisTurn = true;
        Debug.Log($"{gameObject.name} 本回合已移动过，后续移动将被禁止", this);
        return true;
    }

    /// <summary>
    /// 移动完成判定协程（基于路径长度，无超时，精准到位）
    /// </summary>
    /// <param name="targetPos">最终目标点</param>
    /// <param name="totalPathLength">NavMesh路径总长度</param>
    private IEnumerator WaitForMoveComplete(Vector3 targetPos, float totalPathLength)
    {
        float movedDistance = 0f;
        Vector3 lastPos = transform.position;

        // 核心判定：已移动距离 ≥ 路径总长度的95% → 判定完成（留5%容错）
        while (movedDistance < totalPathLength * 0.95f)
        {
            // 每帧累加实际移动距离
            movedDistance += Vector3.Distance(transform.position, lastPos);
            lastPos = transform.position;

            // 异常中断：路径失效/死亡/退出战斗 → 直接结束
            if (navAgent.pathStatus != NavMeshPathStatus.PathComplete || isDead || !isInBattle)
            {
                Debug.LogWarning($"{gameObject.name} 移动中断：路径失效/死亡/退出战斗", this);
                break;
            }

            yield return null;
        }

        // 最后补位：强制走到目标点（避免差最后一点）
        navAgent.isStopped = false;
        navAgent.SetDestination(targetPos);
        yield return new WaitForSeconds(0.2f); // 给0.2秒补位时间

        // ===================== 核心：解锁isMoving + 刷新状态 =====================
        isMoving = false;
        navAgent.isStopped = true; // 停止移动，避免滑步

        // 刷新行为树状态（强制判定是否在攻击范围）
        bool inAttackRange = IsTargetInAttackRange();
        behaviorTree.SetVariableValue("isMoveReady", false); // 移动一次就关闭
        behaviorTree.SetVariableValue("isAttackReady", inAttackRange);

        Debug.Log($"{gameObject.name} 移动完成：已走{movedDistance:F1}m / 总长度{totalPathLength:F1}m | 是否在攻击范围：{inAttackRange}", this);
        HasAvailableAction();
    }

    /// <summary>
    /// 【行为树调用】减少技能冷却（全局回合结束时调用）
    /// </summary>
    public void ReduceSkillCoolDown()
    {
        if (skillCoolDownLeft > 0)
            skillCoolDownLeft--;

        // 冷却变化后同步到行为树
        if (behaviorTree != null)
        {
            behaviorTree.SetVariableValue("skillCooldown", skillCoolDownLeft);
        }
    }

    /// <summary>
    /// 【行为树调用】结束个人回合
    /// </summary>
    public override void EndPersonalTurn()
    {
        base.EndPersonalTurn(); // 基类核心逻辑：标记已行动、取消回合
        // 回合结束，重置本回合移动标记
        hasMovedThisTurn = false;
        // 停止移动
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            navAgent.ResetPath();
            navAgent.isStopped = true;
        }
        // 回合结束同步状态到行为树
        SyncStateToBehaviorTree();
        Debug.Log("敌人结束回合");
    }

    /// <summary>
    /// 【行为树调用】敌人无可用行动时自动结束回合
    /// </summary>
    public void AutoEndTurn()
    {
        // 回合结束，重置本回合移动标记
        hasMovedThisTurn = false;
        if (TurnBattleManager.Instance == null)
        {
            Debug.LogError("回合管理器为空，无法结束敌人回合");
            return;
        }
        TurnBattleManager.Instance.EndTurn(this);

        // 自动结束回合后同步状态
        SyncStateToBehaviorTree();
        Debug.Log("敌人结束回合");
    }

    /// <summary>
    /// 判断是否有可用行动（返回bool + 同步到行为树变量）
    /// 行为树可直接调用，也可通过变量判断
    /// </summary>
    /// <returns>true=有可用行动，false=无可用行动</returns>
    public bool HasAvailableAction()
    {
        // 1. 基础校验：死亡/非战斗 → 无行动
        if (isDead || !isInBattle)
        {
            behaviorTree.SetVariableValue("hasAvailableAction", false);
            Debug.Log($"{name} 无可用行动：死亡/非战斗", this);
            return false;
        }

        // 2. 获取当前AP
        int currentAP = GetAttrIntValue(AttributeType.CurrentAP);
        if (currentAP <= 0)
        {
            behaviorTree.SetVariableValue("hasAvailableAction", false);
            Debug.Log($"{name} 无可用行动：AP耗尽（{currentAP}）", this);
            return false;
        }

        // 3. 判断能否普攻/移动（完全删除IsSkillReady/技能相关判断，只保留你实际有的逻辑）
        bool canSkill = IsTargetInSkillRange() && currentAP >= skillAttackCost;
        bool canAttack = IsTargetInAttackRange() && currentAP >= normalAttackCost;
        bool canMove = !IsTargetInAttackRange() && currentAP >= 1 && !hasMovedThisTurn;

        // 4. 核心判断：有任意可执行行动 → true（仅普攻/移动）
        bool hasAction = canSkill || canAttack || canMove;
        // 同步到行为树的Shared Bool变量
        behaviorTree.SetVariableValue("hasAvailableAction", hasAction);
        behaviorTree.SetVariableValue("isMoveReady", canMove);
        behaviorTree.SetVariableValue("isAttackReady", canAttack);
        behaviorTree.SetVariableValue("isSkillReady", canSkill);

        Debug.Log($"{name} 可用行动判断：\nAP={currentAP} | 普攻={canAttack} | 移动={canMove} → {hasAction}", this);
        // 无行动时强制设为false
        if (!canAttack && !canMove)
        {
            behaviorTree.SetVariableValue("hasAvailableAction", false);
        }
        return hasAction;
    }

    // 辅助方法（不变）
    public bool IsInAttackRange()
    {
        if (attackTarget == null) return false;
        Vector3 enemyPos = transform.position;
        enemyPos.y = 0;
        Vector3 targetPos = attackTarget.position;
        targetPos.y = 0;
        return Vector3.Distance(enemyPos, targetPos) <= attackRange;
    }

    public bool IsInSkillRange()
    {
        if (attackTarget == null) return false;
        Vector3 enemyPos = transform.position;
        enemyPos.y = 0;
        Vector3 targetPos = attackTarget.position;
        targetPos.y = 0;
        return Vector3.Distance(enemyPos, targetPos) <= skillRange;
    }

    /// <summary>
    /// 【行为树调用】死亡判定与处理
    /// </summary>
    public void CheckDeathState()
    {
        if (isDead)
        {
            Die();
        }
    }
    #endregion

    #region 重写基类方法（战斗状态联动行为树）
    public override void Die()
    {
        base.Die();
        Debug.Log($"{gameObject.name} 死亡");

        // 终止所有协程，避免残留逻辑
        StopAllCoroutines();
        patrolCoroutine = null;

        // 禁用导航和行为树
        if (navAgent != null) navAgent.enabled = false;
        if (behaviorTree != null)
        {
            behaviorTree.SetVariableValue("isDead", true); // 死亡状态同步到行为树
            behaviorTree.SetVariableValue("isHostile", false); // 死亡后不再敌对
            behaviorTree.DisableBehavior();
        }

        // 停止巡逻
        StopPatrol();

        // 给玩家加经验
        PlayerAttr[] players = FindObjectsOfType<PlayerAttr>();
        foreach (PlayerAttr player in players)
        {
            if (player != null) player.AddEXP(dropEXP);
        }

        // 生成掉落物
        if (DropSystem.Instance != null && dropTable != null)
        {
            DropSystem.Instance.SpawnDrop(transform.position, dropTable);
        }

        // 检查战斗是否结束
        TurnBattleManager.Instance?.CheckBattleEnd();
        // 延迟销毁
        Destroy(gameObject, 5f);
    }

    // 动画播放方法
    public void PlayAttackAnim() => animator?.SetTrigger("Attack");
    public void PlaySkillAnim() => animator?.SetTrigger("Skill");
    #endregion

    #region Scene视图调试
    private void OnDrawGizmos()
    {
        if (!showEnemyRangeGizmo) return;

        // 绘制警戒圈
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, enemyDetectRange);

        // 绘制巡逻范围
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(originPos == Vector3.zero ? transform.position : originPos, patrolRange);

        // 绘制攻击/技能射程
        if (isInBattle)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, skillRange);
        }
    }
    #endregion
}