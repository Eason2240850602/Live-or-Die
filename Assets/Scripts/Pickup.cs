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

    [Header("开箱概率")]
    [Range(0f, 1f)] public float emptyChance = 0.1f;
    [Range(0f, 1f)] public float twoItemChance = 0.3f;

    [Header("搜刮")]
    public float searchRange = 1.5f;
    public float searchDuration = 2.5f;

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
        while (t < searchDuration)
        {
            if (!InRange()) { Interrupt("搜刮被打断(离开范围)"); yield break; }
            if (playerHealth != null && playerHealth.Current < startHealth) { Interrupt("搜刮被打断(受到攻击)"); yield break; }
            HudController.Instance?.SetProgress(t / searchDuration);
            t += Time.deltaTime;
            yield return null;
        }
        HudController.Instance?.HideProgress();
        Debug.Log($"搜刮完成 [{containerName}]");

        // 掷骰一次，存进容器（绝不重掷）
        if (Random.value < emptyChance)
        {
            Debug.Log("什么都没有...");
            Searched = true;
            Destroy(gameObject);
            yield break;
        }

        int count = Random.value < twoItemChance ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            string name = RollLoot();
            int group = ItemDatabase.GroupSize(name);
            AddToContents(name, group);
            Debug.Log(group > 1 ? $"开出:{name} x{group}" : $"开出:{name}");
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
        if (inventory != null) s.count -= inventory.AddItem(s.name, s.count);
        contents.RemoveAll(x => x.count <= 0);
        if (inventory != null) Debug.Log(inventory.GridView());
        CheckConsumed();
    }

    public void TakeAll()
    {
        if (inventory != null)
        {
            foreach (var s in contents) s.count -= inventory.AddItem(s.name, s.count);
            contents.RemoveAll(x => x.count <= 0);
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
