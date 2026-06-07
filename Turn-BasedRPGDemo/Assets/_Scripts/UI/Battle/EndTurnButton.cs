using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结束回合按钮：点击后调用TurnBattleManager.PlayerEndTurn()结束当前玩家的回合
/// </summary>
public class EndTurnButton : MonoBehaviour
{
    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnEndTurnClick);
    }

    private void OnEndTurnClick()
    {
        TurnBattleManager.Instance?.PlayerEndTurn();
    }
}