using System.Collections;
using UnityEngine;

/// <summary>
/// 盲盒容器：品类锁定 + 随机战利品表（第 3 块）。搜刮流程与中断（第 2 块）不变，
/// 只在"搜刮完成"处接入开箱规则：空箱判定 → 件数判定 → 按权重从本容器品类池抽取。
/// 战利品表用 Serializable 数组挂在 Inspector（不搞 ScriptableObject/JSON/物品类体系）。
/// 纯 transform + 距离判断，不引入物理。按 E 的目标查找在 PlayerInteraction 里。
/// </summary>
public class Pickup : MonoBehaviour
{
    [System.Serializable]
    public class LootEntry
    {
        public string itemName;
        public int weight = 1;
    }

    [Header("容器")]
    [Tooltip("容器类型名（仅用于 Console 打印区分）")]
    public string containerName = "容器";
    [Tooltip("本容器的品类池：名称 + 权重")]
    public LootEntry[] lootTable;

    [Header("开箱概率")]
    [Range(0f, 1f)] public float emptyChance = 0.1f;   // 空箱
    [Range(0f, 1f)] public float twoItemChance = 0.3f; // 出 2 件（否则 1 件）

    [Header("搜刮")]
    [Tooltip("可搜刮距离")]
    public float searchRange = 1.5f;
    [Tooltip("搜刮等待时长（秒）")]
    public float searchDuration = 2.5f;

    public bool IsSearching { get; private set; }
    public bool Searched { get; private set; }

    Transform player;
    Inventory inventory;
    Health playerHealth;

    void Awake()
    {
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            inventory = pm.GetComponent<Inventory>();
            playerHealth = pm.GetComponent<Health>();
        }
    }

    public bool CanSearch()
    {
        return !IsSearching && !Searched && player != null
            && Vector3.Distance(transform.position, player.position) <= searchRange;
    }

    public void BeginSearch()
    {
        if (CanSearch()) StartCoroutine(SearchRoutine());
    }

    IEnumerator SearchRoutine()
    {
        IsSearching = true;
        Debug.Log($"开始搜刮 [{containerName}]...");

        // —— 第 2 块：逐帧计时，可被打断 ——
        float startHealth = playerHealth != null ? playerHealth.Current : float.MaxValue;
        float t = 0f;
        while (t < searchDuration)
        {
            if (player == null || Vector3.Distance(transform.position, player.position) > searchRange)
            {
                Debug.Log("搜刮被打断(离开范围)");
                IsSearching = false;
                yield break;
            }
            if (playerHealth != null && playerHealth.Current < startHealth)
            {
                Debug.Log("搜刮被打断(受到攻击)");
                IsSearching = false;
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        // —— 第 3 块：开箱结算 ——
        Debug.Log($"搜刮完成 [{containerName}]");

        // 1) 空箱 10%（空也算搜过）
        if (Random.value < emptyChance)
        {
            Debug.Log("什么都没有...");
            Searched = true;
            Destroy(gameObject);
            yield break;
        }

        // 2) 件数：twoItemChance 出 2 件，否则 1 件（2 件允许重复）
        int count = Random.value < twoItemChance ? 2 : 1;
        bool allGiven = true;
        for (int i = 0; i < count; i++)
        {
            string item = RollLoot();
            if (inventory != null && inventory.TryAdd(item))
                Debug.Log($"开出:{item}（背包 {inventory.Count}/{inventory.capacity}）");
            else
            {
                Debug.Log($"开出:{item} → 背包已满，没带走");
                allGiven = false;
            }
        }

        if (allGiven)
        {
            Searched = true;
            Destroy(gameObject);      // 全部带走 → 容器消失
        }
        else
        {
            IsSearching = false;      // 有东西没带走 → 容器保留可重搜
        }
    }

    /// <summary>按权重从本容器品类池抽一件。</summary>
    string RollLoot()
    {
        if (lootTable == null || lootTable.Length == 0) return "空";
        int total = 0;
        foreach (var e in lootTable) total += Mathf.Max(0, e.weight);
        if (total <= 0) return lootTable[0].itemName;

        int r = Random.Range(0, total);
        foreach (var e in lootTable)
        {
            r -= Mathf.Max(0, e.weight);
            if (r < 0) return e.itemName;
        }
        return lootTable[lootTable.Length - 1].itemName;
    }
}
