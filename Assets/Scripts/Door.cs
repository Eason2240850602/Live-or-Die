using UnityEngine;

/// <summary>
/// 感知v1段4：门。关闭 = 挡通行 + 挡视线(通过启用自身 Blocker，计入段2遮挡)；开 = 都不挡。
/// E 短按开关(发小声 r=2)；E 按住≥0.5秒对关门 = 偷窥(PlayerInteraction 处理)。
/// </summary>
public class Door : MonoBehaviour
{
    public float doorX;
    public float floorY;
    public bool open;

    public GameObject visual;   // 门板
    public Blocker blocker;     // 关=启用(挡走+挡视线)

    [Tooltip("开关门声音半径")]
    public float toggleNoise = 2f;

    void Start() { Apply(); }

    public void Toggle()
    {
        open = !open;
        Apply();
        NoiseSystem.Emit(transform.position, toggleNoise, "开关门");
        Debug.Log(open ? $"门开了 (x={doorX})" : $"门关了 (x={doorX})");
    }

    void Apply()
    {
        if (visual != null) visual.SetActive(!open);
        if (blocker != null) blocker.enabled = !open;
    }

    /// <summary>门后房间：玩家在哪侧，另一侧的房间就是偷窥目标（可能为 null=通行区）。</summary>
    public Room RoomBehind(float playerX)
    {
        float side = playerX < doorX ? 1f : -1f;
        return Room.At(doorX + side * 1.5f, floorY);
    }
}
