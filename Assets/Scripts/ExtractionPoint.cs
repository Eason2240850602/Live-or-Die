using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 撤离点。玩家靠近(距离判断) → 打印带回明细清单(名称 x 数量 + 总数) → 重开一局。
/// 盲盒第 3 块：结算从"带回 X 物资"升级为逐项明细。
/// </summary>
public class ExtractionPoint : MonoBehaviour
{
    [Tooltip("撤离触发距离")]
    public float extractRange = 1.5f;

    Transform player;
    Inventory inventory;
    bool triggered;

    void Awake()
    {
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            inventory = pm.GetComponent<Inventory>();
        }
    }

    void Update()
    {
        if (triggered || player == null) return;

        if (Vector3.Distance(transform.position, player.position) <= extractRange)
        {
            triggered = true;
            PrintManifest();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void PrintManifest()
    {
        var items = inventory != null ? inventory.Items : null;
        if (items == null || items.Count == 0)
        {
            Debug.Log("本次带回:空手而归，共 0 件");
            return;
        }

        // 按名称聚合计数
        var counts = new Dictionary<string, int>();
        foreach (var it in items)
            counts[it] = counts.TryGetValue(it, out var c) ? c + 1 : 1;

        var parts = new List<string>();
        foreach (var kv in counts)
            parts.Add($"{kv.Key} x{kv.Value}");

        Debug.Log("本次带回:" + string.Join(" / ", parts) + "，共 " + items.Count + " 件");
    }
}
