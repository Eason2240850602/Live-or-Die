using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 盲盒第 3 块：背包从纯计数升级为记录物品名称列表；容量逻辑不变(每件占 1 格)。
/// 仍不做任何 UI，靠 Console。数据持有者，narration 交给调用方。
/// </summary>
public class Inventory : MonoBehaviour
{
    [Tooltip("背包容量上限")]
    public int capacity = 5;

    readonly List<string> items = new List<string>();

    public int Count => items.Count;
    public bool IsFull => items.Count >= capacity;
    public IReadOnlyList<string> Items => items;

    /// <summary>装入一件（按名字）；满了返回 false（不打印，由调用方叙述）。</summary>
    public bool TryAdd(string itemName)
    {
        if (IsFull) return false;
        items.Add(itemName);
        return true;
    }
}
