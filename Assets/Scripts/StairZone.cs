using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 研究所灰盒：楼梯区域。玩家 X 在区域内时可按 W/S 沿梯匀速上下（PlayerMovement 处理）。
/// bottomY/topY = 两层地面的 Y。丧尸不使用楼梯（规则已记）。
/// </summary>
public class StairZone : MonoBehaviour
{
    public float xMin, xMax;
    public float bottomY, topY;   // 地面 Y（角色中心再 +1）

    static readonly List<StairZone> all = new List<StairZone>();

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    /// <summary>x 处的楼梯区（无则 null）。</summary>
    public static StairZone At(float x)
    {
        foreach (var z in all)
            if (x >= z.xMin && x <= z.xMax) return z;
        return null;
    }
}
