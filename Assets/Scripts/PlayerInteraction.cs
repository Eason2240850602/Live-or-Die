using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家交互（E）。感知v1段4：门加入最近目标集（与容器同权，取最近）。
///   - 最近是容器 → 按下即开搜（原行为）
///   - 最近是门 → 短按(<0.5s)=开/关门；按住(≥0.5s)对着关门=偷窥：
///     门后房间临时揭开、期间丧尸无论朝向/距离都无法发现玩家；松开复原
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Tooltip("门交互距离")]
    public float doorRange = 1.5f;

    [Tooltip("按住多久算偷窥（秒）")]
    public float peekHoldTime = 0.5f;

    /// <summary>偷窥中（全局）：丧尸检测直接跳过。</summary>
    public static bool IsPeeking { get; private set; }

    Door heldDoor;
    float holdTime;
    bool peeking;
    Room peekedRoom;

    void OnDisable() { EndPeek(); }

    void Update()
    {
        if (Time.timeScale == 0f) { EndHold(); return; }

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.eKey.wasPressedThisFrame) OnPress();
        else if (heldDoor != null && kb.eKey.isPressed) OnHold();
        else if (heldDoor != null && kb.eKey.wasReleasedThisFrame) OnRelease();
    }

    void OnPress()
    {
        // 已有搜刮进行中 → 不开第二个
        var pickups = Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None);
        foreach (var p in pickups)
            if (p.IsSearching) return;

        // 最近可搜容器
        Pickup nearestPickup = null;
        float pickupDist = float.MaxValue;
        foreach (var p in pickups)
        {
            if (!p.CanSearch()) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < pickupDist) { pickupDist = d; nearestPickup = p; }
        }

        // 最近门
        Door nearestDoor = null;
        float doorDist = float.MaxValue;
        foreach (var d in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            float dd = Vector3.Distance(transform.position, d.transform.position);
            if (dd <= doorRange && dd < doorDist) { doorDist = dd; nearestDoor = d; }
        }

        // 同权取最近
        if (nearestDoor != null && (nearestPickup == null || doorDist < pickupDist))
        {
            heldDoor = nearestDoor;   // 短按/按住在释放/持续时判定
            holdTime = 0f;
        }
        else if (nearestPickup != null)
        {
            nearestPickup.BeginSearch();
        }
    }

    void OnHold()
    {
        holdTime += Time.deltaTime;
        if (!peeking && holdTime >= peekHoldTime && !heldDoor.open)
        {
            // 偷窥开始：门后房间临时揭开；期间丧尸无法发现玩家
            peeking = true;
            IsPeeking = true;
            peekedRoom = heldDoor.RoomBehind(transform.position.x);
            peekedRoom?.SetPeek(true);
            Debug.Log("偷窥中...");
        }
    }

    void OnRelease()
    {
        if (peeking) EndPeek();
        else if (holdTime < peekHoldTime) heldDoor.Toggle();   // 短按 = 开/关门
        heldDoor = null;
        holdTime = 0f;
    }

    void EndHold()
    {
        EndPeek();
        heldDoor = null;
        holdTime = 0f;
    }

    void EndPeek()
    {
        if (!peeking) return;
        peeking = false;
        IsPeeking = false;
        peekedRoom?.SetPeek(false);
        peekedRoom = null;
    }
}
