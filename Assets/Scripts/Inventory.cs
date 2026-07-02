using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 盲盒第 5 块：格子制背包。capacity 格，一格 = 一堆(同名，≤堆叠上限) 或 一件武器。
/// 入包：先并入未满的同名堆，再开新格；无格可用且无堆可并 = 装不下。仍无 UI，靠 Console。
/// </summary>
public class Inventory : MonoBehaviour
{
    [Tooltip("背包格数上限")]
    public int capacity = 5;

    readonly List<ItemStack> slots = new List<ItemStack>();

    public int Count => slots.Count;              // 已用格数
    public bool IsFull => slots.Count >= capacity;
    public IReadOnlyList<ItemStack> Slots => slots;

    /// <summary>装入 qty 个 name；返回实际装入数量（先并同名未满堆，再开新格）。</summary>
    public int AddItem(string name, int qty)
    {
        int stackMax = ItemDatabase.StackMax(name);
        int added = 0;

        foreach (var s in slots)
        {
            if (qty <= 0) break;
            if (s.name == name && s.count < stackMax)
            {
                int put = Mathf.Min(stackMax - s.count, qty);
                s.count += put; qty -= put; added += put;
            }
        }
        while (qty > 0 && slots.Count < capacity)
        {
            int put = Mathf.Min(stackMax, qty);
            slots.Add(new ItemStack(name, put));
            qty -= put; added += put;
        }
        return added;
    }

    /// <summary>Console 格子视图：背包[1/5]: 木材x5 | 罐头x2 | 撬棍 | - | -</summary>
    public string GridView()
    {
        var cells = new List<string>();
        foreach (var s in slots) cells.Add(s.count > 1 ? $"{s.name}x{s.count}" : s.name);
        for (int i = slots.Count; i < capacity; i++) cells.Add("-");
        return $"背包[{slots.Count}/{capacity}]: " + string.Join(" | ", cells);
    }
}
