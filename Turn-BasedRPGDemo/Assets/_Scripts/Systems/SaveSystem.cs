using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// 存档系统
/// </summary>
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;
    private string savePath;

    private void Awake()
    {
        Instance = this;
        savePath = Path.Combine(Application.persistentDataPath, "saveData.dat");
    }

    // 保存存档
    public void SaveGame(PlayerAttr player)
    {
        if (player == null) return;

        SaveData data = new SaveData();
        data.playerHP = player.GetAttrValue(AttributeType.CurrentHP);
        data.playerMP = player.GetAttrValue(AttributeType.CurrentMP);
        data.playerLevel = player.GetAttrValue(AttributeType.Level);
        data.playerEXP = player.GetAttrValue(AttributeType.CurrentEXP);
        data.playerPos = player.transform.position;

        // 序列化
        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(savePath, FileMode.Create))
        {
            formatter.Serialize(stream, data);
        }
        Debug.Log("存档成功：" + savePath);
    }

    // 读取存档
    public bool LoadGame(PlayerAttr player)
    {
        if (!File.Exists(savePath) || player == null) return false;

        // 反序列化
        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(savePath, FileMode.Open))
        {
            SaveData data = formatter.Deserialize(stream) as SaveData;

            // 恢复属性
            player.SetAttrValue(AttributeType.CurrentHP, data.playerHP);
            player.SetAttrValue(AttributeType.CurrentMP, data.playerMP);
            player.SetAttrValue(AttributeType.Level, data.playerLevel);
            player.SetAttrValue(AttributeType.CurrentEXP, data.playerEXP);
            player.transform.position = data.playerPos;
            player.RecoverFullAP();
        }
        Debug.Log("读档成功");
        return true;
    }

    // 存档数据类
    [System.Serializable]
    public class SaveData
    {
        public float playerHP;
        public float playerMP;
        public float playerLevel;
        public float playerEXP;
        public Vector3 playerPos;
    }
}