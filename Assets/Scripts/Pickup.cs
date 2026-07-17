using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 盲盒容器。第 6 块：搜刮完成不再自动转移，改为弹出九宫格窗口由玩家选择拿取。
/// 首次搜刮：2.5s 等待(可中断) → 掷骰入箱(第5块，绝不重掷) → 开窗。
/// 已开封再交互：无等待，秒开窗，内容一致。拿光/空箱 → 容器消失。
/// 拿取动作由 LootWindow 调用 TakeStackAt / TakeAll。纯 transform + 距离判断。
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
    public string containerName = "容器";
    public LootEntry[] lootTable;

    [Header("开箱概率（v3：1件15%/2件50%/3件35%，无空箱）")]
    [Range(0f, 1f)] public float emptyChance = 0f;      // 保留字段，v3 置 0
    [Range(0f, 1f)] public float chanceOne = 0.15f;
    [Range(0f, 1f)] public float chanceTwo = 0.50f;     // 三件 = 剩余 35%

    [Header("保底模式（弹药箱）：设了名字则必出该物 min~max 个，无其他掉落")]
    public string guaranteedItem = "";
    public int guaranteedMin = 8;
    public int guaranteedMax = 15;

    [Header("搜刮")]
    public float searchRange = 1.5f;
    public float searchDuration = 2.5f;

    [Tooltip("搜刮声音半径（等待期每秒脉冲一次）")]
    public float searchNoiseRadius = 4f;

    public bool IsSearching { get; private set; }
    public bool Searched { get; private set; }

    public string ContainerName => containerName;
    public float SearchRange => searchRange;
    public IReadOnlyList<ItemStack> Contents => contents;

    Transform player;
    Inventory inventory;
    Health playerHealth;

    bool opened;
    readonly List<ItemStack> contents = new List<ItemStack>();

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

    bool InRange() => player != null && Vector3.Distance(transform.position, player.position) <= searchRange;

    public bool CanSearch() => !IsSearching && !Searched && InRange();

    public void BeginSearch()
    {
        if (!CanSearch()) return;
        if (opened) HudController.Instance?.OpenLootWindow(this);   // 已开封 → 秒开窗
        else StartCoroutine(SearchRoutine());                       // 首次 → 等待
    }

    IEnumerator SearchRoutine()
    {
        IsSearching = true;
        Debug.Log($"开始搜刮 [{containerName}]...");
        HudController.Instance?.ShowProgress();

        float startHealth = playerHealth != null ? playerHealth.Current : float.MaxValue;
        float t = 0f;
        float noiseT = 0f;
        while (t < searchDuration)
        {
            if (!InRange()) { Interrupt("搜刮被打断(离开范围)"); yield break; }
            if (playerHealth != null && playerHealth.Current < startHealth) { Interrupt("搜刮被打断(受到攻击)"); yield break; }
            HudController.Instance?.SetProgress(t / searchDuration);
            noiseT += Time.deltaTime;                       // 搜刮有声音：每秒脉冲
            if (noiseT >= 1f) { noiseT = 0f; NoiseSystem.Emit(transform.position, searchNoiseRadius, "搜刮"); }
            t += Time.deltaTime;
            yield return null;
        }
        HudController.Instance?.HideProgress();
        Debug.Log($"搜刮完成 [{containerName}]");

        // 掷骰一次，存进容器（绝不重掷）
        if (!string.IsNullOrEmpty(guaranteedItem))
        {
            // 保底模式（弹药箱）：必出 min~max 个
            int qty = Random.Range(guaranteedMin, guaranteedMax + 1);
            AddToContents(guaranteedItem, qty);
            Debug.Log($"开出:{guaranteedItem} x{qty}");
        }
        else if (Random.value < emptyChance)   // v3 = 0，字段保留
        {
            Debug.Log("什么都没有...");
            Searched = true;
            Destroy(gameObject);
            yield break;
        }
        else
        {
            // v3：件数 1件15%/2件50%/3件35%；同次开箱不放回，池子抽干才允许重复
            float r = Random.value;
            int count = r < chanceOne ? 1 : (r < chanceOne + chanceTwo ? 2 : 3);

            var pool = new List<string>();
            void Refill() { pool.Clear(); if (lootTable != null) foreach (var e in lootTable) pool.Add(e.itemName); }
            Refill();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                string name = RollFromPool(pool);
                pool.Remove(name);
                if (pool.Count == 0) Refill();   // 抽干才允许重复
                int group = ItemDatabase.GroupSize(name);
                AddToContents(name, group);
                Debug.Log(group > 1 ? $"开出:{name} x{group}" : $"开出:{name}");
            }
        }
        opened = true;
        IsSearching = false;
        HudController.Instance?.OpenLootWindow(this);   // 弹窗展示，玩家选择拿取
    }

    void Interrupt(string reason)
    {
        Debug.Log(reason);
        HudController.Instance?.HideProgress();
        HudController.Instance?.ShowMessage(reason, 1.5f);   // 打断提示保留（第4块）
        IsSearching = false;
    }

    // —— 供 LootWindow 调用 ——
    public void TakeStackAt(int index)
    {
        if (index < 0 || index >= contents.Count) return;
        var s = contents[index];
        if (inventory != null)
        {
            int got = inventory.AcquireItem(s.name, s.count);   // UX1：武器右手空则直接上手
            s.count -= got;
            if (got == 0) HudController.Instance?.ShowMessage("背包已满", 1.5f);
        }
        contents.RemoveAll(x => x.count <= 0);
        if (inventory != null) Debug.Log(inventory.GridView());
        CheckConsumed();
    }

    public void TakeAll()
    {
        if (inventory != null)
        {
            int gotTotal = 0;
            foreach (var s in contents)
            {
                int got = inventory.AcquireItem(s.name, s.count);   // UX1
                s.count -= got;
                gotTotal += got;
            }
            contents.RemoveAll(x => x.count <= 0);
            if (gotTotal == 0 && contents.Count > 0) HudController.Instance?.ShowMessage("背包已满", 1.5f);
            Debug.Log(inventory.GridView());
        }
        CheckConsumed();
    }

    void CheckConsumed()
    {
        if (contents.Count == 0)
        {
            Searched = true;
            Destroy(gameObject);   // 拿光 → 容器消失
        }
    }

    void AddToContents(string name, int qty)
    {
        foreach (var s in contents) if (s.name == name) { s.count += qty; return; }
        contents.Add(new ItemStack(name, qty));
    }

    // v3：按稀有度三档权重(常见60/少见30/稀有10)从候选池抽一件——裸权重(entry.weight)退役
    string RollFromPool(List<string> pool)
    {
        int total = 0;
        foreach (var n in pool) total += ItemDatabase.RarityWeight(n);
        int r = Random.Range(0, total);
        foreach (var n in pool)
        {
            r -= ItemDatabase.RarityWeight(n);
            if (r < 0) return n;
        }
        return pool[pool.Count - 1];
    }
}
