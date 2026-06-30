using UnityEngine;

/// <summary>
/// Day 3：物资点（通用物资，不分品类）。玩家靠近（距离判断，不用物理）自动拾取：
/// 背包 +1 → 自身消失；背包满则不拾取（Inventory 会打印"背包已满"）。
/// 进入范围时只触发一次，避免每帧重复打印。
/// </summary>
public class Pickup : MonoBehaviour
{
    [Tooltip("拾取距离")]
    public float pickupRange = 1.5f;

    Transform player;
    Inventory inventory;
    bool inside;

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
        if (player == null || inventory == null) return;

        bool now = Vector3.Distance(transform.position, player.position) <= pickupRange;
        if (now && !inside)   // 刚进入范围：尝试拾取一次
        {
            if (inventory.TryAdd()) Destroy(gameObject);
        }
        inside = now;
    }
}
