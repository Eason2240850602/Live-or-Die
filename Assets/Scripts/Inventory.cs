using UnityEngine;

/// <summary>
/// Day 3：最简背包 = 计数 + 容量上限。不做格子/图标/任何 UI，靠 Console 打印体现。
/// 挂在玩家身上；撤离点读它的当前数量来结算。
/// </summary>
public class Inventory : MonoBehaviour
{
    [Tooltip("背包容量上限")]
    public int capacity = 8;

    int count;

    public int Count => count;
    public bool IsFull => count >= capacity;

    /// <summary>尝试拾取一个物资；满了返回 false。</summary>
    public bool TryAdd()
    {
        if (IsFull)
        {
            Debug.Log("背包已满");
            return false;
        }
        count++;
        Debug.Log($"拾取物资，当前 {count}/{capacity}");
        return true;
    }
}
