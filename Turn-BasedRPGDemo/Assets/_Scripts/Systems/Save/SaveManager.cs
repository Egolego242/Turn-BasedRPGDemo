using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

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

    private const string ENCRYPT_KEY = "RPG_Save_2026_WangDingLi"; // 密钥改得更独特
    private const int MAX_SAVE_SLOT = 3;

    // 【修复】确保存档文件夹存在
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

    // 【修复】获取存档文件完整路径
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

    // ========== 核心保存方法 ==========
    public void SaveGame(int slotIndex, GameObject playerObj)
    {
        Debug.Log($"开始保存存档位{slotIndex + 1}...");

        // 1. 基础校验
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

        PlayerAttr playerAttr = playerObj.GetComponent<PlayerAttr>();
        Inventory inventory = playerObj.GetComponent<Inventory>();
        if (playerAttr == null || inventory == null)
        {
            Debug.LogError("玩家缺少PlayerAttr或Inventory组件！");
            return;
        }

        // 2. 构建存档数据
        GameSaveData saveData = new GameSaveData
        {
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            currentGold = inventory.currentGold,
            level = Mathf.RoundToInt(playerAttr.GetAttrValue(AttributeType.Level)),
            currentEXP = playerAttr.GetAttrValue(AttributeType.CurrentEXP),
            expToLevelUp = playerAttr.GetAttrValue(AttributeType.EXPToLevelUp),
            position = playerObj.transform.position,
            rotation = playerObj.transform.rotation
        };

        // 3. 保存属性字典（增加空值保护）
        if (playerAttr.attrDic != null)
        {
            foreach (var kvp in playerAttr.attrDic)
            {
                saveData.attrKeys.Add(kvp.Key);
                saveData.attrValues.Add(kvp.Value);
            }
        }

        // 4. 序列化+加密+写入
        try
        {
            // 序列化
            string json = JsonUtility.ToJson(saveData, true);
            Debug.Log($"存档JSON序列化成功：\n{json}");

            // 加密
            string encrypt = XOREncryptDecrypt(json, ENCRYPT_KEY);

            // 写入文件
            string filePath = GetSaveFilePath(slotIndex);
            File.WriteAllText(filePath, encrypt);
            Debug.Log($"存档成功！路径：{filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"存档失败！错误信息：{e.Message}\n堆栈跟踪：{e.StackTrace}");
        }
    }

    // ========== 核心加载方法 ==========
    public bool LoadGame(int slotIndex, GameObject playerObj)
    {
        Debug.Log($"开始加载存档位{slotIndex + 1}...");

        // 1. 基础校验
        if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOT) return false;
        string filePath = GetSaveFilePath(slotIndex);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"存档文件不存在：{filePath}");
            return false;
        }
        if (playerObj == null) return false;

        PlayerAttr playerAttr = playerObj.GetComponent<PlayerAttr>();
        Inventory inventory = playerObj.GetComponent<Inventory>();
        if (playerAttr == null || inventory == null) return false;

        // 2. 读取+解密+反序列化
        try
        {
            string encrypt = File.ReadAllText(filePath);
            string json = XOREncryptDecrypt(encrypt, ENCRYPT_KEY);
            Debug.Log($"读档JSON解密成功：\n{json}");

            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            if (saveData == null)
            {
                Debug.LogError("反序列化失败，saveData为空！");
                return false;
            }

            // 3. 恢复属性
            playerAttr.attrDic.Clear();
            for (int i = 0; i < saveData.attrKeys.Count; i++)
            {
                if (i < saveData.attrValues.Count)
                {
                    playerAttr.SetAttrValue(saveData.attrKeys[i], saveData.attrValues[i]);
                }
            }

            // 4. 恢复位置、金币
            playerObj.transform.position = saveData.position;
            playerObj.transform.rotation = saveData.rotation;
            inventory.currentGold = saveData.currentGold;

            Debug.Log($"读档成功！存档位：{slotIndex + 1}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"读档失败！错误信息：{e.Message}\n堆栈跟踪：{e.StackTrace}");
            return false;
        }
    }

    // ========== 辅助方法 ==========
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

    // 【修复】异或加密/解密（统一成一个方法，对称加密）
    private string XOREncryptDecrypt(string input, string key)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(key)) return string.Empty;

        char[] inputChars = input.ToCharArray();
        char[] keyChars = key.ToCharArray();
        int keyLen = keyChars.Length;

        for (int i = 0; i < inputChars.Length; i++)
        {
            inputChars[i] = (char)(inputChars[i] ^ keyChars[i % keyLen]);
        }

        return new string(inputChars);
    }
}