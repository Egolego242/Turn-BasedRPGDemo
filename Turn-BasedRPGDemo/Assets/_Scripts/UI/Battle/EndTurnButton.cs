using UnityEngine;
using UnityEngine.UI;

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