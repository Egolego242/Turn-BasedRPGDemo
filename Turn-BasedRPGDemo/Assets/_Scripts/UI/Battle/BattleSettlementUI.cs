using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战斗结算UI控制器
/// 监听TurnBattleManager.OnBattleSettlement事件，展示胜利/失败面板
/// </summary>
public class BattleSettlementUI : MonoBehaviour
{
    [Header("结算根面板")]
    public GameObject settlementPanel;

    [Header("胜利结算子面板")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryExpText;
    public TextMeshProUGUI victoryGoldText;
    public TextMeshProUGUI victoryItemsText;
    public Button victoryConfirmButton;

    [Header("失败结算子面板")]
    public GameObject defeatPanel;
    public Button defeatExitButton;
    public Button defeatLoadSaveButton;

    private void Awake()
    {
        TurnBattleManager.OnBattleSettlement += OnBattleSettlement;
        Debug.Log("[BattleSettlementUI] 已订阅 OnBattleSettlement 事件");

        if (victoryConfirmButton != null)
            victoryConfirmButton.onClick.AddListener(OnVictoryConfirmClicked);
        if (defeatExitButton != null)
            defeatExitButton.onClick.AddListener(OnDefeatExitClicked);
        if (defeatLoadSaveButton != null)
            defeatLoadSaveButton.onClick.AddListener(OnDefeatLoadSaveClicked);
    }

    private void OnDestroy()
    {
        TurnBattleManager.OnBattleSettlement -= OnBattleSettlement;
        if (victoryConfirmButton != null)
            victoryConfirmButton.onClick.RemoveListener(OnVictoryConfirmClicked);
        if (defeatExitButton != null)
            defeatExitButton.onClick.RemoveListener(OnDefeatExitClicked);
        if (defeatLoadSaveButton != null)
            defeatLoadSaveButton.onClick.RemoveListener(OnDefeatLoadSaveClicked);
    }

    private void Start()
    {
        if (settlementPanel != null)
            settlementPanel.SetActive(false);
    }

    private void OnBattleSettlement(BattleSettlementData data)
    {
        if (settlementPanel != null)
            settlementPanel.SetActive(true);

        if (data.isVictory)
        {
            ShowVictoryPanel(data);
        }
        else
        {
            ShowDefeatPanel();
        }
    }

    private void ShowVictoryPanel(BattleSettlementData data)
    {
        if (victoryPanel != null) victoryPanel.SetActive(true);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        if (victoryExpText != null)
            victoryExpText.text = $"+{data.totalEXP} EXP";
        if (victoryGoldText != null)
            victoryGoldText.text = $"+{data.totalGold} G";
        if (victoryItemsText != null)
        {
            if (data.rewardItems.Count > 0)
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (var item in data.rewardItems)
                    names.Add(item.itemName);
                victoryItemsText.text = string.Join("\n", names);
            }
            else
            {
                victoryItemsText.text = "无";
            }
        }
    }

    private void ShowDefeatPanel()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(true);
    }

    private void OnVictoryConfirmClicked()
    {
        TurnBattleManager.Instance?.ConfirmVictorySettlement();
        if (settlementPanel != null)
            settlementPanel.SetActive(false);
    }

    private void OnDefeatExitClicked()
    {
        TurnBattleManager.Instance?.ConfirmDefeatExit();
    }

    private void OnDefeatLoadSaveClicked()
    {
        TurnBattleManager.Instance?.ConfirmDefeatLoadSave();
        if (settlementPanel != null)
            settlementPanel.SetActive(false);

        // 复用MainMenuUI的存档位选择面板
        MainMenuUI mainMenu = FindObjectOfType<MainMenuUI>();
        if (mainMenu != null)
            mainMenu.OnClickLoadGameMenu();
    }
}
