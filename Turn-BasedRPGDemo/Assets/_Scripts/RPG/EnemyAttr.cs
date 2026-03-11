using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using System.Collections;
using System.Collections.Generic;

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
    #endregion

    #region 掉落配置
    [Header("===== 掉落配置 =====")]
    public DropTable dropTable;
    public int dropEXP = 20;
    #endregion

    #region 行动消耗规则（和行为树节点完全对应）
    [Header("===== 行动消耗规则 =====")]
    public int moveCostPerUnit = 1; // 每移动1单位消耗的行动点
    public int normalAttackCost = 2; // 普攻消耗行动点（行为树普攻判断用）
    public int skillAttackCost = 3; // 技能消耗行动点（行为树技能判断用）
    [Header("===== 战斗配置 =====")]
    public float attackRange = 2f; // 普攻射程
    public float skillRange = 5f; // 技能射程
    public Transform attackTarget; // 攻击目标（行为树赋值玩家Transform）
    #endregion

    #region 巡逻&警戒配置
    [Header("===== 巡逻配置 =====")]
    public float patrolRange = 5f; // 巡逻范围
    public float patrolWaitTime = 2f; // 巡逻停留时间
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
        originPos = transform.position;

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
        if (patrolCoroutine != null)
            StopCoroutine(patrolCoroutine);

        patrolCoroutine = StartCoroutine(PatrolCoroutine());
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
    private IEnumerator PatrolCoroutine()
    {
        while (!isInBattle && !isDead)
        {
            // 生成随机巡逻点
            Vector3 randomDir = Random.insideUnitSphere * patrolRange;
            randomDir.y = 0;
            Vector3 targetPos = originPos + randomDir;

            // 验证目标点是否在NavMesh上
            NavMeshHit hit;
            bool isPosValid = NavMesh.SamplePosition(targetPos, out hit, patrolRange, NavMesh.AllAreas);
            if (isPosValid && navAgent != null && navAgent.isActiveAndEnabled)
            {
                targetPos = hit.position;
                navAgent.isStopped = false;
                navAgent.SetDestination(targetPos);

                // 等待到达目标点（距离<0.5f 或 寻路失败）
                while (navAgent.remainingDistance > 0.5f && navAgent.pathStatus == NavMeshPathStatus.PathComplete && !isInBattle && !isDead)
                {
                    yield return null; // 每一帧检查状态
                }
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} 巡逻点无效，跳过本次巡逻", this);
            }

            // 停留指定时间
            yield return new WaitForSeconds(patrolWaitTime);
        }
        patrolCoroutine = null; // 协程结束后重置引用
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
        return distance <= attackRange;
    }

    /// <summary>
    /// 【行为树条件判断】目标是否在技能射程内
    /// </summary>
    public bool IsTargetInSkillRange()
    {
        if (attackTarget == null) return false;
        float distance = Vector3.Distance(transform.position, attackTarget.position);
        return distance <= skillRange;
    }
    #region 整合判断
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
        bool isReady = !isDead && attackTarget != null && navAgent != null && navAgent.isActiveAndEnabled && GetAttrValue(AttributeType.CurrentAP) >= 1f;
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

    /// <summary>
    /// 【仅新增】计算移动指定距离需要消耗的AP（0-4米=1点，4-8米=2点...）
    /// </summary>
    /// <param name="moveDistance">要移动的距离（米）</param>
    /// <returns>消耗的AP点数</returns>
    private int CalculateMoveAPCost(float moveDistance)
    {
        if (moveDistance <= 0) return 0;
        // 核心规则：向上取整，和玩家侧逻辑完全一致
        return Mathf.CeilToInt(moveDistance / 4f);
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
        return true;
    }

    /// <summary>
    /// 【行为树调用】向目标移动（返回是否移动成功）
    /// </summary>
    public bool MoveToTarget()
    {
        // 【强制日志】只要进方法就打印，排除没执行的可能
        Debug.Log($"===== {gameObject.name} 进入MoveToTarget方法 =====", this);
        if (isDead)
        {
            Debug.LogWarning($"{gameObject.name} 移动失败：已死亡", this);
            return false;
        }
        if (attackTarget == null)
        {
            Debug.LogWarning($"{gameObject.name} 移动失败：攻击目标为空", this);
            return false;
        }
        if (navAgent == null || !navAgent.isActiveAndEnabled)
        {
            Debug.LogWarning($"{gameObject.name} 移动失败：导航组件无效", this);
            return false;
        }
        Debug.Log($"【2】{gameObject.name} 判空检查通过", this);

        int currentAP = GetAttrIntValue(AttributeType.CurrentAP);
        Debug.Log($"【3-4】{gameObject.name} 获取当前AP：{currentAP}", this);
        if (currentAP <= 0)
        {
            Debug.LogWarning($"{gameObject.name} 移动失败：行动点不足（当前{currentAP}）", this);
            return false;
        }


        // 计算移动距离和消耗
        Vector3 targetPos = attackTarget.position;
        Debug.Log($"【3-1】{gameObject.name} 获取目标位置：{targetPos}", this);

        float distance = Vector3.Distance(transform.position, targetPos);
        Debug.Log($"【3-2】{gameObject.name} 计算距离：{distance:F1}", this);

        int cost = CalculateMoveAPCost(distance);
        Debug.Log($"【3-3】{gameObject.name} 计算移动消耗：{cost}", this);

        if (cost > currentAP)
        {
            Debug.LogWarning($"{gameObject.name} 移动失败：行动点不足（需要{cost}，当前{currentAP}）", this);
            return false;
        }
        Debug.Log($"【4】{gameObject.name} AP校验通过", this);


        // 重置导航状态+执行寻路
        navAgent.ResetPath();
        Debug.Log($"【5-1】{gameObject.name} 重置导航路径", this);
        navAgent.isStopped = false;
        Debug.Log($"【5-2】{gameObject.name} 启用导航", this);
        navAgent.SetDestination(targetPos);
        Debug.Log($"【5-3】{gameObject.name} 设置导航目标：{targetPos}", this);

        // 消耗行动点（增加容错）
        bool consumeSuccess = ConsumeAP(cost);
        Debug.Log($"【6-1】{gameObject.name} 调用ConsumeAP，消耗{cost:F1}AP", this);
        if (!consumeSuccess)
        {
            Debug.LogError($"{gameObject.name} 移动失败：行动点消耗失败", this);
            navAgent.ResetPath();
            navAgent.isStopped = true;
            return false;
        }
        Debug.Log($"【6-2】{gameObject.name} AP消耗成功", this);

        Debug.Log($"{gameObject.name} 向{attackTarget.name}移动，距离{distance:F1}，消耗{cost:F1}行动点", this);
        return true;
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