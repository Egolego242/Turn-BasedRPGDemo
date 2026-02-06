using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人属性子类（适配DropSystem，无方法名错误）
/// </summary>
public class EnemyAttr : BaseCharacterAttr
{
    [Header("===== 敌人初始属性 =====")]
    public float initMaxHP = 80;
    public float initMaxMP = 30;
    public float initMaxAP = 8;
    public float initStrength = 6;
    public float initIntelligence = 3;
    public float initArmor = 2;

    [Header("===== 掉落配置 =====")]
    public DropTable dropTable;
    public int dropEXP = 20;

    [HideInInspector] public Animator animator;
    [HideInInspector] public NavMeshAgent navAgent;

    private void Awake()
    {
        // 初始化组件+属性
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        InitAttribute(initMaxHP, initMaxMP, initMaxAP, initStrength, initIntelligence, initArmor);
        currentCamp = CampType.Enemy;
    }

    // 敌人死亡（触发掉落/经验，方法名匹配DropSystem）
    public override void Die()
    {
        base.Die();
        Debug.Log($"{gameObject.name}死亡");

        // 禁用移动/AI
        //navAgent?.enabled = false;
        //GetComponent<EnemyAI>()?.enabled = false;

        // 给玩家加经验
        PlayerAttr player = FindObjectOfType<PlayerAttr>();
        if (player != null) player.AddEXP(dropEXP);

        // 生成掉落物（无编译错误）
        if (DropSystem.Instance != null && dropTable != null)
        {
            DropSystem.Instance.SpawnDrop(transform.position, dropTable);
        }

        // 延迟销毁
        Destroy(gameObject, 5f);
    }

    // 动画播放
    public void PlayAttackAnim() => animator?.SetTrigger("Attack");
    public void PlaySkillAnim() => animator?.SetTrigger("Skill");
}