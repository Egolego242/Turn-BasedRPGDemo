using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人角色属性子类 - 继承通用基类
/// ✅ 修复：找不到玩家判空+掉落物空引用+延迟销毁容错
/// ✅ 核心功能不变：敌人属性初始化、死亡掉落经验/道具
/// </summary>
public class EnemyAttr : BaseCharacterAttr
{
    [Header("===== 敌人初始属性配置 =====")]
    public float initMaxHP = 80;
    public float initMaxMP = 20;
    public float initMaxAP = 8;
    public float initStrength = 6;
    public float initIntelligence = 3;
    public float initArmor = 2;
    [Header("===== 敌人掉落配置 =====")]
    public int dropEXP = 50;
    public ItemBase dropItem;

    private void Awake()
    {
        InitAttribute(initMaxHP, initMaxMP, initMaxAP, initStrength, initIntelligence, initArmor);
        currentCamp = CampType.Enemy;
    }

    public override void Die()
    {
        base.Die();
        // ✅ 修复：找不到玩家时不报错，防止空引用
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerAttr playerAttr = player.GetComponent<PlayerAttr>();
            Inventory playerBag = player.GetComponent<Inventory>();
            if (playerAttr != null) playerAttr.AddEXP(dropEXP);
            if (playerBag != null && dropItem != null) playerBag.AddItem(dropItem);
        }
        Debug.Log("敌人死亡！掉落经验：" + dropEXP);
        // ✅ 修复：销毁前先禁用组件，防止报错
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        Destroy(gameObject, 1f);
    }
}