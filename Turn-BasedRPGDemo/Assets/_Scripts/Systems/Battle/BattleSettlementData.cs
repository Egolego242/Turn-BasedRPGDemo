using System.Collections.Generic;

/// <summary>
/// 战斗结算数据（胜利时包含奖励，失败时仅标记结果）
/// </summary>
public class BattleSettlementData
{
    public bool isVictory;
    public int totalEXP;
    public int totalGold;
    public List<ItemBase> rewardItems = new List<ItemBase>();
}
