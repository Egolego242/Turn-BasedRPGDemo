using UnityEngine;
using BehaviorDesigner.Runtime;

public class EnemyBehaviorTreeAutoInit : MonoBehaviour
{
    private BehaviorTree behaviorTree;

    private void Awake()
    {
        // 1. 自动获取行为树组件（旧版兼容写法）
        behaviorTree = GetComponent<BehaviorTree>();
        if (behaviorTree == null)
        {
            behaviorTree = GetComponentInChildren<BehaviorTree>();
            if (behaviorTree == null)
            {
                Debug.LogError($"[{gameObject.name}] 找不到BehaviorTree组件！", this);
                return;
            }
        }

        // 2. 自动赋值selfObject（旧版设置变量的核心写法）
        var selfVar = behaviorTree.GetVariable("selfObject");
        if (selfVar != null)
        {
            selfVar.SetValue(gameObject); // 赋值为自己的实例
            Debug.Log($"{gameObject.name} selfObject自动赋值成功", this);
        }
        else
        {
            Debug.LogError($"{gameObject.name} 行为树里没有selfObject变量！", this);
        }

        // 3. 初始化战斗状态变量为false（防止初始值为空）
        var combatVar = behaviorTree.GetVariable("isInCombat");
        if (combatVar != null)
        {
            combatVar.SetValue(false);
        }

        // 4. 初始化其他核心变量
        InitVariable("isMyTurn", false);
        InitVariable("isDead", false);
        InitVariable("isPlayerInRange", false);

        // 5. 强制刷新行为树（旧版生效关键）
        behaviorTree.DisableBehavior();
        behaviorTree.EnableBehavior();
    }

    /// <summary>
    /// 初始化变量（旧版兼容）
    /// </summary>
    private void InitVariable(string varName, object value)
    {
        var variable = behaviorTree.GetVariable(varName);
        if (variable != null)
        {
            variable.SetValue(value);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} 行为树没有变量{varName}", this);
        }
    }

    // 外部调用：更新战斗状态（给TriggerBattleByEnemy调用）
    public void UpdateCombatState(bool isInCombat)
    {
        var combatVar = behaviorTree.GetVariable("isInCombat");
        if (combatVar != null)
        {
            combatVar.SetValue(isInCombat);
            // 刷新行为树
            behaviorTree.DisableBehavior();
            behaviorTree.EnableBehavior();
        }
    }
}