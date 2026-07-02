using System.Collections;
using UnityEngine;

/// <summary>
/// 盲盒第 2 块：容器"被搜"的状态与协程（按 E 的目标查找已挪到 PlayerInteraction）。
/// 搜刮改为逐帧计时循环，期间可被打断：
///   - 玩家离开 searchRange → 中断（离开范围）
///   - 玩家血量低于开搜时的值（被咬）→ 中断（受到攻击）
/// 中断：不入包、不标记已搜、状态复位，可回来重搜。
/// 沿用纯 transform + 距离判断，不引入物理。
/// </summary>
public class Pickup : MonoBehaviour
{
    [Tooltip("可搜刮距离：玩家在此范围内才能对它开搜")]
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

    /// <summary>玩家在范围内、且未在搜/未搜过时，可以开搜。</summary>
    public bool CanSearch()
    {
        return !IsSearching && !Searched && player != null
            && Vector3.Distance(transform.position, player.position) <= searchRange;
    }

    /// <summary>由 PlayerInteraction 调用开始搜刮。</summary>
    public void BeginSearch()
    {
        if (CanSearch()) StartCoroutine(SearchRoutine());
    }

    IEnumerator SearchRoutine()
    {
        IsSearching = true;
        Debug.Log("开始搜刮...");

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

        Debug.Log("搜刮完成");
        if (inventory != null && inventory.TryAdd())
        {
            Searched = true;
            Destroy(gameObject);   // 成功入包 → 容器消失
        }
        // 背包满：TryAdd 已打印"背包已满"，不标记已搜，可稍后重搜
        IsSearching = false;
    }
}
