using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 感知v1段5：房间迷雾单元。未到访 = 深色半透明遮罩盖住房间(含内部容器/丧尸)。
/// 玩家进入 → 本局永久揭开；偷窥 → 按住期间临时揭开。撤离重开 = 场景重载 = 迷雾自然重置。
/// 通行区(大厅/走廊/阳台/楼梯)不建 Room 即默认可见。
/// </summary>
public class Room : MonoBehaviour
{
    public string roomName;
    public float xMin, xMax;
    public float floorY;
    public GameObject fog;   // 遮罩面片

    bool visited;
    bool peeked;

    static readonly List<Room> all = new List<Room>();

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    Transform player;

    void Start()
    {
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        player = pm != null ? pm.transform : null;
        Apply();
    }

    void Update()
    {
        if (visited || player == null) return;
        if (player.position.x >= xMin && player.position.x <= xMax
            && Mathf.Abs(player.position.y - (floorY + 1f)) < 3f)
        {
            visited = true;   // 进入即永久揭开(本局)
            Apply();
        }
    }

    /// <summary>偷窥临时揭开/复原。</summary>
    public void SetPeek(bool on)
    {
        peeked = on;
        Apply();
    }

    void Apply()
    {
        if (fog != null) fog.SetActive(!visited && !peeked);
    }

    /// <summary>某坐标所在的房间（无则 null）。</summary>
    public static Room At(float x, float floorY)
    {
        foreach (var r in all)
            if (x >= r.xMin && x <= r.xMax && Mathf.Abs(r.floorY - floorY) < 0.5f) return r;
        return null;
    }
}
