using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 研究所灰盒：通行阻挡区（碎石堆/楼体外墙等）。纯 transform 方案——
/// 移动方在改 X 前调用 ClampMove，穿越阻挡区的位移被截停在边缘。不用物理。
/// 世界坐标范围在 Inspector/构建器里设置。
/// </summary>
public class Blocker : MonoBehaviour
{
    public float xMin, xMax;   // 阻挡的 X 范围
    public float yMin, yMax;   // 生效的 Y 范围（区分楼层）

    static readonly List<Blocker> all = new List<Blocker>();

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    /// <summary>从 fromX 移到 toX（角色中心在 centerY）：被阻挡则截停在阻挡边缘。</summary>
    public static float ClampMove(float fromX, float toX, float centerY)
    {
        foreach (var b in all)
        {
            if (centerY < b.yMin || centerY > b.yMax) continue;
            if (fromX <= b.xMin && toX > b.xMin) toX = Mathf.Min(toX, b.xMin);
            else if (fromX >= b.xMax && toX < b.xMax) toX = Mathf.Max(toX, b.xMax);
        }
        return toX;
    }
}
