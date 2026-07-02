using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 盲盒容器。第 5 块：内容物本局内持久化。
/// 首次搜刮：2.5s 等待(可中断，第2块) → 掷骰一次(空箱/件数/权重，第3块) → 内容物存进容器 → 自动"能装多少装多少"。
/// 有剩余 → 容器保留=已开封；再次交互不等待，直接转移剩余（绝不重掷）。拿光或空箱 → 容器消失。
/// 战利品表挂 Inspector（第3块）；堆叠/组大小查 ItemDatabase（第5块）。纯 transform + 距离判断。
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

    Transform player;
    Inventory inventory;
    Health playerHealth;

    bool opened;                                                // 已开封（已掷骰）
    readonly List<ItemStack> contents = new List<ItemStack>();  // 容器内剩余内容物（本局持久）

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
        if (opened) TakeRemaining();            // 已开封 → 不等待，直接拿剩余
        else StartCoroutine(SearchRoutine());   // 首次 → 2.5s 等待
    }

    IEnumerator SearchRoutine()
    {
        IsSearching = true;
        Debug.Log($"开始搜刮 [{containerName}]...");
        HudController.Instance?.ShowProgress();

        // —— 第 2 块：逐帧计时，可被打断 ——
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

        // —— 掷骰一次，结果存进容器（绝不重掷）——
        if (Random.value < emptyChance)
        {
            Debug.Log("什么都没有...");
            HudController.Instance?.ShowMessage("什么都没有...", 2f);
            Searched = true;
            Destroy(gameObject);
            yield break;
        }

        int count = Random.value < twoItemChance ? 2 : 1;
        var ui = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++)
        {
            string name = RollLoot();
            int group = ItemDatabase.GroupSize(name);
            AddToContents(name, group);
            string line = group > 1 ? $"开出:{name} x{group}" : $"开出:{name}";
            Debug.Log(line);
            ui.AppendLine(line);
        }
        opened = true;
        HudController.Instance?.ShowMessage(ui.ToString().TrimEnd(), 2f);

        IsSearching = false;
        TransferToInventory();
    }

    void Interrupt(string reason)
    {
        Debug.Log(reason);
        HudController.Instance?.HideProgress();
        HudController.Instance?.ShowMessage(reason, 1.5f);
        IsSearching = false;   // 未掷骰、未开封 → 下次重新等待
    }

    void TakeRemaining()
    {
        Debug.Log($"再翻 [{containerName}]...");
        TransferToInventory();
    }

    /// <summary>把容器内容物"能装多少装多少"转移进背包；拿光则容器消失，有剩余则保留。</summary>
    void TransferToInventory()
    {
        if (inventory != null)
        {
            foreach (var s in contents)
                s.count -= inventory.AddItem(s.name, s.count);
            contents.RemoveAll(s => s.count <= 0);
            Debug.Log(inventory.GridView());
        }

        if (contents.Count == 0)
        {
            Searched = true;
            Destroy(gameObject);            // 拿光 → 容器消失
        }
        else
        {
            var left = new List<string>();
            foreach (var s in contents) left.Add(s.count > 1 ? $"{s.name}x{s.count}" : s.name);
            Debug.Log("背包装不下，留在容器: " + string.Join(" | ", left));
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
