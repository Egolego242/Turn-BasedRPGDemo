using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

/// <summary>
/// 存档管理器：XOR加密JSON存档，支持3个手动槽位+战前自动存档(QuickSave)，BuildSaveData构建全场景快照，ApplySaveData恢复全部角色状态（含行为树重置）
/// </summary>
public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SaveManager");
                    _instance = obj.AddComponent<SaveManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    private const string ENCRYPT_KEY = "RPG_Save_2026_WangDingLi";
    private const int MAX_SAVE_SLOT = 3;

    private string SaveFolderPath
    {
        get
        {
            string path = Path.Combine(Application.persistentDataPath, "SaveData");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"创建存档文件夹：{path}");
            }
            return path;
        }
    }

    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(SaveFolderPath, $"save_{slotIndex}.json");
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("SaveManager初始化完成");
    }

    // ============================================================
    //  核心：共享的数据构建 / 数据恢复方法（消除 SaveGame 与
    //  QuickSave 的重复代码）
    // ============================================================

    // 物品名称→SO缓存（读档时自动构建，不需要Resources文件夹）
    private Dictionary<string, ItemBase> _itemCache;
    private Dictionary<string, EquipItem> _equipCache;

    private void EnsureItemCache()
    {
        if (_itemCache != null) return;
        _itemCache = new Dictionary<string, ItemBase>();
        _equipCache = new Dictionary<string, EquipItem>();

        ItemBase[] allItems = Resources.FindObjectsOfTypeAll<ItemBase>();
        foreach (var item in allItems)
        {
            if (string.IsNullOrEmpty(item.itemName)) continue;
            if (item is EquipItem equip && !_equipCache.ContainsKey(item.itemName))
                _equipCache[item.itemName] = equip;
            if (!_itemCache.ContainsKey(item.itemName))
                _itemCache[item.itemName] = item;
        }
        Debug.Log($"物品缓存已构建：{_itemCache.Count} 个物品，{_equipCache.Count} 个装备");
    }

    /// <summary>
    /// 从当前游戏状态构建存档数据对象
    /// </summary>
    private GameSaveData BuildSaveData(GameObject playerObj)
    {
        PlayerAttr playerAttr = playerObj.GetComponent<PlayerAttr>();
        Inventory inventory = playerObj.GetComponent<Inventory>();

        GameSaveData data = new GameSaveData
        {
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            currentGold = inventory.currentGold,
            level = Mathf.RoundToInt(playerAttr.GetAttrValue(AttributeType.Level)),
            currentEXP = playerAttr.GetAttrValue(AttributeType.CurrentEXP),
            expToLevelUp = playerAttr.GetAttrValue(AttributeType.EXPToLevelUp),
            position = playerObj.transform.position,
            rotation = playerObj.transform.rotation
        };

        // ── 相机 ──
        Camera cam = Camera.main;
        if (cam != null)
        {
            data.cameraPosition = cam.transform.position;
            data.cameraRotation = cam.transform.rotation;
        }

        // ── 属性字典 ──
        if (playerAttr.attrDic != null)
        {
            foreach (var kvp in playerAttr.attrDic)
            {
                data.attrKeys.Add(kvp.Key);
                data.attrValues.Add(kvp.Value);
            }
        }

        // ── 背包物品（所有在背包中的物品，不管类型） ──
        foreach (var item in inventory.itemList)
        {
            data.bagItemDatas.Add(new ItemData(item));
        }

        // ── 已装备物品（单独存储，恢复时需要重新调用 Equip() ） ──
        foreach (var equip in inventory.equipedItemList)
        {
            data.equipedItemDatas.Add(new EquipItemData(equip));
        }

        // ── 所有场景角色（敌人/NPC，排除玩家） ──
        BaseCharacterAttr[] allChars = FindObjectsOfType<BaseCharacterAttr>();
        foreach (var ch in allChars)
        {
            if (ch is PlayerAttr) continue;

            CharacterStateData csd = new CharacterStateData
            {
                characterName = ch.gameObject.name,
                position = ch.transform.position,
                rotation = ch.transform.rotation,
                isDead = ch.isDead
            };
            if (ch.attrDic != null)
            {
                foreach (var kvp in ch.attrDic)
                {
                    csd.attrKeys.Add(kvp.Key);
                    csd.attrValues.Add(kvp.Value);
                }
            }
            data.characters.Add(csd);
        }

        return data;
    }

    /// <summary>
    /// 将存档数据恢复到当前游戏状态（同时清理战斗中残留的UI/状态）
    /// </summary>
    private bool ApplySaveData(GameSaveData data, GameObject playerObj)
    {
        PlayerAttr playerAttr = playerObj.GetComponent<PlayerAttr>();
        Inventory inventory = playerObj.GetComponent<Inventory>();
        if (playerAttr == null || inventory == null) return false;

        // ── 0. 清理活跃的战斗状态（防止读档后战斗UI残留） ──
        if (TurnBattleManager.Instance != null)
        {
            GameStateMgr.Instance?.SwitchGameState(GameStateMgr.GamePlayState.ExploreState);
        }

        // 构建物品缓存（首次读档时扫描所有已加载的ItemBase）
        EnsureItemCache();

        // ── 1. 恢复属性 ──
        playerAttr.attrDic.Clear();
        for (int i = 0; i < data.attrKeys.Count; i++)
        {
            if (i < data.attrValues.Count)
                playerAttr.SetAttrValue(data.attrKeys[i], data.attrValues[i]);
        }

        // 强制重置死亡标记+动画状态（SetAttrValue只改数值，不会改isDead）
        playerAttr.isDead = false;
        Animator anim = playerObj.GetComponent<Animator>();
        if (anim != null)
        {
            anim.ResetTrigger("Die");
            anim.ResetTrigger("Hurt");
            anim.SetBool("BattleMode", false);
            anim.Play("Idle", 0, 0f);
        }

        // 恢复NavMeshAgent（PlayerAttr.Die()会将其禁用）
        UnityEngine.AI.NavMeshAgent navAgent = playerObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.enabled = true;
        }

        // ── 2. 恢复位置、金币 ──
        playerObj.transform.position = data.position;
        playerObj.transform.rotation = data.rotation;
        inventory.currentGold = data.currentGold;

        // ── 3. 恢复相机 ──
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = data.cameraPosition;
            cam.transform.rotation = data.cameraRotation;
        }

        // ── 4. 恢复背包物品 ──
        inventory.itemList.Clear();
        foreach (var itemData in data.bagItemDatas)
        {
            if (_itemCache.TryGetValue(itemData.itemName, out ItemBase item))
            {
                item.itemCount = itemData.itemCount;
                inventory.itemList.Add(item);
            }
            else
            {
                Debug.LogWarning($"读档：找不到物品 '{itemData.itemName}'，物品可能已被删除");
            }
        }

        // ── 5. 恢复装备（先卸后装） ──
        foreach (var equip in inventory.equipedItemList)
        {
            if (equip != null) equip.UnEquip(playerObj);
        }
        inventory.equipedItemList.Clear();

        foreach (var equipData in data.equipedItemDatas)
        {
            if (_equipCache.TryGetValue(equipData.itemName, out EquipItem equip))
            {
                equip.itemCount = equipData.itemCount;
                equip.Equip(playerObj);
                inventory.equipedItemList.Add(equip);
            }
            else
            {
                Debug.LogWarning($"读档：找不到装备 '{equipData.itemName}'，装备可能已被删除");
            }
        }

        // ── 6. 恢复所有其他角色状态（敌人/NPC） ──
        if (data.characters != null && data.characters.Count > 0)
        {
        BaseCharacterAttr[] allChars = FindObjectsOfType<BaseCharacterAttr>();
        foreach (var csd in data.characters)
        {
            BaseCharacterAttr match = null;
            foreach (var ch in allChars)
            {
                if (!(ch is PlayerAttr) && ch.gameObject.name == csd.characterName)
                {
                    match = ch;
                    break;
                }
            }
            if (match == null) continue; // 角色已被销毁，跳过

            // 位置
            match.transform.position = csd.position;
            match.transform.rotation = csd.rotation;

            // 生死状态
            match.isDead = csd.isDead;

            // 属性
            if (match.attrDic != null)
            {
                for (int i = 0; i < csd.attrKeys.Count; i++)
                {
                    if (i < csd.attrValues.Count)
                        match.SetAttrValue(csd.attrKeys[i], csd.attrValues[i]);
                }
            }

            // 动画+移动
            Animator chAnim = match.GetComponent<Animator>();
            if (chAnim != null)
            {
                chAnim.ResetTrigger("Die");
                chAnim.ResetTrigger("Hurt");
                chAnim.SetBool("BattleMode", false);
                if (!match.isDead) chAnim.Play("Idle", 0, 0f);
            }

            UnityEngine.AI.NavMeshAgent chNav = match.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (chNav != null && !match.isDead) chNav.enabled = true;

            // 敌人专属：强制重置行为树到日常分支，恢复巡逻
            if (match is EnemyAttr enemy)
            {
                enemy.isInBattle = false;
                enemy.isMyTurn = false;
                enemy.StopPatrol();

                // 强制行为树重置：禁用→同步变量→启用，确保从根节点重新评估
                if (enemy.behaviorTree != null)
                {
                    enemy.behaviorTree.SetVariableValue("isInCombat", false);
                    enemy.behaviorTree.SetVariableValue("isMyTurn", false);
                    enemy.behaviorTree.SetVariableValue("isDead", match.isDead);
                    enemy.behaviorTree.SetVariableValue("hasAvailableAction", false);
                    enemy.behaviorTree.DisableBehavior();
                    enemy.behaviorTree.EnableBehavior();
                }

                if (!match.isDead) enemy.StartPatrol();
                Debug.Log($"读档：恢复敌人 [{match.name}] 位置→{csd.position}，HP→{match.GetAttrValue(AttributeType.CurrentHP)}");
            }
        }
        } // data.characters != null

        // ── 7. 兜底：关闭所有可能的战斗UI ──
        UIManager uiMgr = FindObjectOfType<UIManager>();
        if (uiMgr != null && uiMgr.overlayBattleUI != null)
            uiMgr.overlayBattleUI.SetActive(false);
        if (uiMgr != null && uiMgr.settlementPanel != null)
            uiMgr.settlementPanel.SetActive(false);

        return true;
    }

    // ============================================================
    //  对外接口
    // ============================================================

    public void SaveGame(int slotIndex, GameObject playerObj)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOT)
        {
            Debug.LogError("存档位非法！");
            return;
        }
        if (playerObj == null)
        {
            Debug.LogError("玩家对象为空！");
            return;
        }

        try
        {
            GameSaveData data = BuildSaveData(playerObj);
            string json = JsonUtility.ToJson(data, true);
            string encrypt = XOREncryptDecrypt(json, ENCRYPT_KEY);
            string filePath = GetSaveFilePath(slotIndex);
            File.WriteAllText(filePath, encrypt);
            Debug.Log($"存档成功！存档位{slotIndex + 1}，路径：{filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"存档失败！{e.Message}");
        }
    }

    public bool LoadGame(int slotIndex, GameObject playerObj)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOT) return false;
        string filePath = GetSaveFilePath(slotIndex);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"存档文件不存在：{filePath}");
            return false;
        }
        if (playerObj == null) return false;

        try
        {
            string encrypt = File.ReadAllText(filePath);
            string json = XOREncryptDecrypt(encrypt, ENCRYPT_KEY);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null)
            {
                Debug.LogError("反序列化失败！");
                return false;
            }

            bool ok = ApplySaveData(data, playerObj);
            Debug.Log(ok ? $"读档成功！存档位：{slotIndex + 1}" : "读档失败：ApplySaveData返回false");
            return ok;
        }
        catch (Exception e)
        {
            Debug.LogError($"读档失败！{e.Message}");
            return false;
        }
    }

    // ── 战前自动存档（独立文件） ──

    public void QuickSaveBeforeBattle(GameObject playerObj)
    {
        if (playerObj == null) return;
        try
        {
            GameSaveData data = BuildSaveData(playerObj);
            string json = JsonUtility.ToJson(data, true);
            string encrypt = XOREncryptDecrypt(json, ENCRYPT_KEY);
            string filePath = Path.Combine(SaveFolderPath, "before_battle.json");
            File.WriteAllText(filePath, encrypt);
            Debug.Log($"战前自动存档完成：{filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"战前自动存档失败：{e.Message}");
        }
    }

    public bool LoadBeforeBattleSave(GameObject playerObj)
    {
        string filePath = Path.Combine(SaveFolderPath, "before_battle.json");
        if (!File.Exists(filePath) || playerObj == null) return false;
        try
        {
            string encrypt = File.ReadAllText(filePath);
            string json = XOREncryptDecrypt(encrypt, ENCRYPT_KEY);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null) return false;

            bool ok = ApplySaveData(data, playerObj);
            Debug.Log(ok ? "战前存档已恢复" : "战前存档恢复失败");
            return ok;
        }
        catch (Exception e)
        {
            Debug.LogError($"战前存档恢复失败：{e.Message}");
            return false;
        }
    }

    // ── 辅助 ──

    public bool HasSaveFile(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOT) return false;
        bool exists = File.Exists(GetSaveFilePath(slotIndex));
        Debug.Log($"检查存档位{slotIndex + 1}：文件存在={exists}");
        return exists;
    }

    public GameSaveData GetSaveInfo(int slotIndex)
    {
        if (!HasSaveFile(slotIndex)) return null;
        try
        {
            string encrypt = File.ReadAllText(GetSaveFilePath(slotIndex));
            string json = XOREncryptDecrypt(encrypt, ENCRYPT_KEY);
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"读取存档信息失败：{e.Message}");
            return null;
        }
    }

    public void DeleteSaveFile(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOT) return;
        string path = GetSaveFilePath(slotIndex);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"已删除存档位{slotIndex + 1}");
        }
    }

    // ── 异或加密 ──

    private string XOREncryptDecrypt(string input, string key)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(key)) return string.Empty;
        char[] inputChars = input.ToCharArray();
        char[] keyChars = key.ToCharArray();
        int keyLen = keyChars.Length;
        for (int i = 0; i < inputChars.Length; i++)
            inputChars[i] = (char)(inputChars[i] ^ keyChars[i % keyLen]);
        return new string(inputChars);
    }
}
