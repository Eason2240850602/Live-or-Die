using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 盲盒第 2 块：玩家交互。按 E 时：
///   - 若已有搜刮进行中 → 不触发第二个（全场单一搜刮）
///   - 否则在范围内所有可搜容器中，只让"距离玩家最近"的那个开搜
/// 纯 transform + 距离判断，不引入物理/触发器。挂在玩家身上。
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.eKey.wasPressedThisFrame) return;

        var all = Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None);

        // 已有搜刮进行中 → 不开第二个
        foreach (var p in all)
            if (p.IsSearching) return;

        // 找范围内最近的可搜容器
        Pickup nearest = null;
        float best = float.MaxValue;
        foreach (var p in all)
        {
            if (!p.CanSearch()) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < best) { best = d; nearest = p; }
        }

        if (nearest != null) nearest.BeginSearch();
    }
}
