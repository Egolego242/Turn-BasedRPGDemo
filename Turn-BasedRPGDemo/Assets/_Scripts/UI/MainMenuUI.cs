using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System;

/// <summary>
/// 主菜单界面：ESC唤出菜单面板，管理存档/读档功能，控制3个SaveSlotUI存档位的显示与交互
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // ========== 1. UI引用（在Unity编辑器里拖进去） ==========
    [Header("主菜单面板")]
    public GameObject mainMenuPanel;
    public GameObject DefeatPanel;
    public Button btnSaveGame;
    public Button btnLoadGame;
    public Button btnExitGame;

    [Header("存档位面板")]
    public GameObject saveSlotPanel;
    public TMP_Text txtSlotPanelTitle;
    public SaveSlotUI[] saveSlots = new SaveSlotUI[3];
    public Button btnBackFromSlot;

    [Header("确认覆盖弹窗")]
    public GameObject confirmPanel;
    public TMP_Text txtConfirmMessage;
    public Button btnConfirm;
    public Button btnCancel;

    // ========== 2. 内部变量 ==========
    private enum MenuMode { None, Save, Load }
    private MenuMode currentMode = MenuMode.None;
    private int pendingSaveSlot = -1;
    private GameObject playerObj;

    // ========== 3. 存档位UI结构体 ==========
    [System.Serializable]
    /// <summary>
    /// 单个存档槽UI：显示存档时间/等级信息或"空"状态，点击触发存档或读档
    /// </summary>
    public class SaveSlotUI
    {
        public GameObject root;
        public TMP_Text infoText; 
        public Button clickBtn;
    }

    // ========== 4. Unity生命周期 ==========
    private void Awake()
    {
        // 初始化：所有面板默认隐藏
        mainMenuPanel.SetActive(false);
        saveSlotPanel.SetActive(false);
        confirmPanel.SetActive(false);

        // 查找玩家对象
        playerObj = GameObject.FindGameObjectWithTag("Player");

        // 绑定主菜单按钮事件
        btnSaveGame.onClick.AddListener(OnClickSaveGameMenu);
        btnLoadGame.onClick.AddListener(OnClickLoadGameMenu);
        btnExitGame.onClick.AddListener(OnClickExitGame);

        // 绑定存档位面板按钮事件
        btnBackFromSlot.onClick.AddListener(OnClickBackFromSlot);

        // 绑定3个存档位点击事件
        for (int i = 0; i < saveSlots.Length; i++)
        {
            int slotIndex = i;
            saveSlots[i].clickBtn.onClick.AddListener(() => OnClickSaveSlot(slotIndex));
        }

        // 绑定确认弹窗按钮事件
        btnConfirm.onClick.AddListener(OnConfirmSave);
        btnCancel.onClick.AddListener(OnCancelSave);
    }

    private void Update()
    {
        // 按ESC键开关主菜单
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMainMenu();
        }
    }

    // ========== 5. 主菜单逻辑 ==========
    private void ToggleMainMenu()
    {
        if (saveSlotPanel.activeSelf)
        {
            saveSlotPanel.SetActive(false);
            return;
        }
        if (confirmPanel.activeSelf)
        {
            confirmPanel.SetActive(false);
            return;
        }

        bool isActive = !mainMenuPanel.activeSelf;
        mainMenuPanel.SetActive(isActive);
        Time.timeScale = isActive ? 0f : 1f;
    }

    private void OnClickSaveGameMenu()
    {
        currentMode = MenuMode.Save;
        mainMenuPanel.SetActive(false);
        saveSlotPanel.SetActive(true);
        txtSlotPanelTitle.text = "存储游戏";
        RefreshAllSaveSlots();
    }

    public void OnClickLoadGameMenu()
    {
        currentMode = MenuMode.Load;
        mainMenuPanel.SetActive(false);
        DefeatPanel.SetActive(false);
        saveSlotPanel.SetActive(true);
        txtSlotPanelTitle.text = "加载游戏";
        RefreshAllSaveSlots();
    }

    private void OnClickExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ========== 6. 存档位面板逻辑 ==========
    private void RefreshAllSaveSlots()
    {
        for (int i = 0; i < saveSlots.Length; i++)
        {
            RefreshSingleSaveSlot(i);
        }
    }

    private void RefreshSingleSaveSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= saveSlots.Length) return;

        var slotUI = saveSlots[slotIndex];
        bool hasSave = SaveManager.Instance.HasSaveFile(slotIndex);

        if (hasSave)
        {
            GameSaveData saveData = SaveManager.Instance.GetSaveInfo(slotIndex);
            slotUI.infoText.text = $"存档位 {slotIndex + 1}\n" +
                                    $"角色等级：Lv.{saveData.level}\n" +
                                    $"存档时间：{saveData.saveTime}";
            slotUI.clickBtn.interactable = currentMode == MenuMode.Load || currentMode == MenuMode.Save;
        }
        else
        {
            slotUI.infoText.text = $"存档位 {slotIndex + 1}\n空存档";
            slotUI.clickBtn.interactable = currentMode == MenuMode.Save;
        }
    }

    private void OnClickSaveSlot(int slotIndex)
    {
        if (currentMode == MenuMode.Save)
        {
            bool hasSave = SaveManager.Instance.HasSaveFile(slotIndex);
            if (hasSave)
            {
                pendingSaveSlot = slotIndex;
                txtConfirmMessage.text = $"确定要覆盖「存档位 {slotIndex + 1}」吗？\n上次存档时间：{SaveManager.Instance.GetSaveInfo(slotIndex).saveTime}";
                confirmPanel.SetActive(true);
            }
            else
            {
                DoSaveGame(slotIndex);
            }
        }
        else if (currentMode == MenuMode.Load)
        {
            bool hasSave = SaveManager.Instance.HasSaveFile(slotIndex);
            if (hasSave)
            {
                DoLoadGame(slotIndex);
            }
        }
    }

    private void OnClickBackFromSlot()
    {
        saveSlotPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        currentMode = MenuMode.None;
    }

    // ========== 7. 确认覆盖弹窗逻辑 ==========
    private void OnConfirmSave()
    {
        if (pendingSaveSlot >= 0)
        {
            DoSaveGame(pendingSaveSlot);
        }
        confirmPanel.SetActive(false);
        pendingSaveSlot = -1;
    }

    private void OnCancelSave()
    {
        confirmPanel.SetActive(false);
        pendingSaveSlot = -1;
    }

    // ========== 8. 核心存档/读档调用 ==========
    private void DoSaveGame(int slotIndex)
    {
        if (playerObj == null) playerObj = GameObject.FindGameObjectWithTag("Player");
        Debug.Log($"对象：{playerObj}");
        SaveManager.Instance.SaveGame(slotIndex, playerObj);
        RefreshSingleSaveSlot(slotIndex);
        Debug.Log($"存储成功！存档位：{slotIndex + 1}");
    }

    private void DoLoadGame(int slotIndex)
    {
        if (playerObj == null) playerObj = GameObject.FindGameObjectWithTag("Player");
        bool isSuccess = SaveManager.Instance.LoadGame(slotIndex, playerObj);
        if (isSuccess)
        {
            saveSlotPanel.SetActive(false);
            mainMenuPanel.SetActive(false);
            Time.timeScale = 1f;
            currentMode = MenuMode.None;
            Debug.Log($"加载成功！存档位：{slotIndex + 1}");
        }
    }
}