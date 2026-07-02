using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 撤离点。玩家靠近(距离判断) → 按堆打印带回明细(名称 x 数量 + 总件数) → 重开一局。
/// 第 5 块：读格子制背包的每一堆。撤离结算不做 UI，维持 Console。
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
        var slots = inventory != null ? inventory.Slots : null;
        if (slots == null || slots.Count == 0)
        {
            Debug.Log("本次带回:空手而归，共 0 件");
            return;
        }

        var parts = new List<string>();
        int total = 0;
        foreach (var s in slots)
        {
            parts.Add(s.count > 1 ? $"{s.name} x{s.count}" : s.name);
            total += s.count;
        }
        Debug.Log("本次带回:" + string.Join(" / ", parts) + "，共 " + total + " 件");
    }
}
