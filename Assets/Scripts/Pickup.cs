using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 盲盒第 1 块：容器交互 + 搜刮等待（不碰 UI，全部用 Console）。
/// 玩家在范围内按 E → 进入几秒"搜刮中" → 完成后才把物资给进背包、容器消失。
/// 背包满则不给且容器保留（可稍后重搜）；已搜刮的容器不再响应。
/// 沿用一贯做法：纯 transform + 距离判断，不引入物理；用协程做计时等待。
/// </summary>
public class Pickup : MonoBehaviour
{
    [Tooltip("可搜刮距离：玩家在此范围内按 E 才能搜")]
    public float searchRange = 1.5f;

    [Tooltip("搜刮等待时长（秒）")]
    public float searchDuration = 2.5f;

    Transform player;
    Inventory inventory;
    bool searching;
    bool searched;

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
        if (searching || searched || player == null) return;

        var kb = Keyboard.current;
        if (kb == null || !kb.eKey.wasPressedThisFrame) return;

        // 只有玩家在范围内按 E 才触发搜刮（不再自动拾取）
        if (Vector3.Distance(transform.position, player.position) <= searchRange)
            StartCoroutine(Search());
    }

    IEnumerator Search()
    {
        searching = true;
        Debug.Log("开始搜刮...");
        yield return new WaitForSeconds(searchDuration);
        Debug.Log("搜刮完成");

        if (inventory != null && inventory.TryAdd())
        {
            // 成功入包 → 容器消失，标记已搜
            searched = true;
            Destroy(gameObject);
        }
        else
        {
            // 背包满（Inventory 已打印"背包已满"）：不给、不标记已搜，可稍后重搜
            searching = false;
        }
    }
}
