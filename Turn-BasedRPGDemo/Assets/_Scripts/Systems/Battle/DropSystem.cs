using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 掉落系统（无重复定义，DropTable仅定义一次）
/// </summary>
public class DropSystem : MonoBehaviour
{
    public static DropSystem Instance;
    private Dictionary<GameObject, DropResult> spawnedDrops = new Dictionary<GameObject, DropResult>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // 生成掉落物（EnemyAttr调用匹配）
    public void SpawnDrop(Vector3 dropPos, DropTable table)
    {
        if (table == null) return;

        // 生成掉落结果
        DropResult result = table.GenerateRandomDrop();
        if (result.dropGold <= 0 && result.dropItemList.Count == 0) return;

        // 创建掉落物
        GameObject dropObj = new GameObject($"Drop_{Random.Range(1000, 9999)}");
        dropObj.transform.position = dropPos + Vector3.up * 0.6f;

        // 添加碰撞体+拾取组件
        SphereCollider collider = dropObj.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        DropPickup pickup = dropObj.AddComponent<DropPickup>();
        pickup.dropResult = result;
        pickup.dropObj = dropObj;

        // 可视化提示
        TextMesh text = dropObj.AddComponent<TextMesh>();
        text.text = result.dropGold > 0 ? $"{result.dropGold}金币" : result.dropItemList[0].itemName;
        text.color = Color.yellow;
        text.anchor = TextAnchor.MiddleCenter;

        // 缓存+旋转
        spawnedDrops.Add(dropObj, result);
        dropObj.AddComponent<DropRotate>();
    }

    // 拾取掉落物
    public void PickupDrop(GameObject dropObj, Inventory inventory)
    {
        if (!spawnedDrops.ContainsKey(dropObj) || inventory == null) return;

        DropResult result = spawnedDrops[dropObj];
        // 金币入库
        if (result.dropGold > 0) inventory.AddGold(result.dropGold);
        // 物品入库
        foreach (var item in result.dropItemList) inventory.AddItem(item);

        // 清理
        spawnedDrops.Remove(dropObj);
        Destroy(dropObj);
    }
}

// 掉落拾取组件
public class DropPickup : MonoBehaviour
{
    public DropResult dropResult;
    public GameObject dropObj;

    // 点击拾取
    private void OnMouseDown()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Inventory inv = player.GetComponent<Inventory>();
            DropSystem.Instance.PickupDrop(dropObj, inv);
        }
    }

    // 靠近拾取
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Inventory inv = other.GetComponent<Inventory>();
            DropSystem.Instance.PickupDrop(dropObj, inv);
        }
    }
}

// 掉落物旋转
public class DropRotate : MonoBehaviour
{
    public float speed = 60f;
    private void Update() => transform.Rotate(0, speed * Time.deltaTime, 0);
}

// 掉落配置表（仅定义一次，无重复）
[CreateAssetMenu(fileName = "DropTable", menuName = "战斗系统/掉落表")]
public class DropTable : ScriptableObject
{
    [Header("金币掉落")]
    public int minGold = 10;
    public int maxGold = 50;

    [Header("物品掉落")]
    public List<DropItem> dropItems = new List<DropItem>();

    // 生成随机掉落
    public DropResult GenerateRandomDrop()
    {
        DropResult result = new DropResult();
        // 随机金币
        result.dropGold = Random.Range(minGold, maxGold + 1);
        // 随机物品
        foreach (var item in dropItems)
        {
            if (Random.value <= item.dropRate && item.item != null)
                result.dropItemList.Add(item.item);
        }
        return result;
    }
}

// 掉落项
[System.Serializable]
public class DropItem
{
    public ItemBase item;
    [Range(0f, 1f)] public float dropRate = 0.5f;
}

// 掉落结果
public class DropResult
{
    public int dropGold;
    public List<ItemBase> dropItemList = new List<ItemBase>();
}
