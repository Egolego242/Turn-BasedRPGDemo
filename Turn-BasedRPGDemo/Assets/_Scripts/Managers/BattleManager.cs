using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 战斗管理器（无字段缺失错误，适配基类）
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("=== 战斗配置 ===")]
    public bool isBattleStart = false;
    public List<BaseCharacterAttr> allBattleUnits = new List<BaseCharacterAttr>();
    public List<BaseCharacterAttr> battleTurnOrder = new List<BaseCharacterAttr>();
    public int currentTurnIndex = 0;

    public int maxAP = 6;
    public int initAP = 4;
    public int recoverAP = 4;

    private void Awake()
    {
        Instance = this;
        CollectAllBattleUnits();
    }

    // 收集所有战斗单位
    public void CollectAllBattleUnits()
    {
        allBattleUnits.Clear();
        allBattleUnits.AddRange(FindObjectsOfType<BaseCharacterAttr>());
    }

    // 启动战斗
    public void StartBattle()
    {
        if (isBattleStart) return;
        isBattleStart = true;

        // 筛选+排序回合顺序
        battleTurnOrder = allBattleUnits
            .Where(unit => unit.currentCamp != CampType.Neutral && !unit.isDead)
            .OrderByDescending(unit => unit.GetAttrValue(AttributeType.Strength) + unit.GetAttrValue(AttributeType.Intelligence))
            .ToList();

        // 初始化回合状态
        foreach (var unit in battleTurnOrder)
        {
            unit.SetAttrValue(AttributeType.CurrentAP, initAP);
            unit.isInBattle = true;
            unit.isMyTurn = false;
        }

        StartCurrentTurn();
    }

    // 启动当前回合
    private void StartCurrentTurn()
    {
        // 重置所有回合状态
        battleTurnOrder.ForEach(unit => unit.isMyTurn = false);

        // 回合轮询完毕：回复AP+刷新技能冷却
        if (currentTurnIndex >= battleTurnOrder.Count)
        {
            currentTurnIndex = 0;
            RecoverAllUnitAP();
            SkillManager.Instance?.RefreshAllSkillCoolDown();
        }

        // 跳过死亡角色
        BaseCharacterAttr currentUnit = battleTurnOrder[currentTurnIndex];
        if (currentUnit.isDead)
        {
            currentTurnIndex++;
            StartCurrentTurn();
            return;
        }

        // 激活当前回合
        currentUnit.isMyTurn = true;
        Debug.Log($"当前行动：{currentUnit.gameObject.name} | 剩余AP：{currentUnit.GetAttrValue(AttributeType.CurrentAP)}");
    }

    // 回复所有角色AP
    private void RecoverAllUnitAP()
    {
        foreach (var unit in battleTurnOrder)
        {
            if (unit.isDead) continue;
            float newAP = unit.GetAttrValue(AttributeType.CurrentAP) + recoverAP;
            unit.SetAttrValue(AttributeType.CurrentAP, Mathf.Min(newAP, maxAP));
        }
    }

    // 结束当前回合
    public void EndCurrentTurn()
    {
        if (!isBattleStart) return;

        // 重置当前回合状态
        battleTurnOrder[currentTurnIndex].isMyTurn = false;

        // 检测胜负
        CheckBattleResult();

        // 切换下一个角色
        currentTurnIndex++;
        StartCurrentTurn();
    }

    // 胜负判定
    private void CheckBattleResult()
    {
        // 玩家全灭
        bool playerAllDead = allBattleUnits
            .Where(unit => unit.currentCamp == CampType.Player)
            .All(unit => unit.isDead);

        // 敌人全灭
        bool enemyAllDead = allBattleUnits
            .Where(unit => unit.currentCamp == CampType.Enemy)
            .All(unit => unit.isDead);

        // 胜负处理
        if (playerAllDead)
        {
            Debug.Log("战斗失败！");
            //SaveSystem.Instance?.LoadGame(FindObjectOfType<PlayerAttr>());
            EndBattle();
        }
        else if (enemyAllDead)
        {
            Debug.Log("战斗胜利！");
            //SaveSystem.Instance?.SaveGame(FindObjectOfType<PlayerAttr>());
            EndBattle();
        }
    }

    // 结束战斗
    public void EndBattle()
    {
        isBattleStart = false;
        currentTurnIndex = 0;
        allBattleUnits.ForEach(unit =>
        {
            unit.isInBattle = false;
            unit.isMyTurn = false;
        });
    }

    // 攻击中立触发战斗
    public void StartBattleByAttackNeutral(BaseCharacterAttr neutralUnit)
    {
        if (isBattleStart) return;
        neutralUnit.currentCamp = CampType.Enemy;
        CollectAllBattleUnits();
        StartBattle();
    }
}