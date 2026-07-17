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

    /// <summary>视线遮挡（感知v1）：x1↔x2 的视线在眼高 eyeY 处是否被任一阻挡体截断。纯数学，不用物理。</summary>
    public static bool BlocksLine(float x1, float x2, float eyeY)
    {
        float lo = Mathf.Min(x1, x2), hi = Mathf.Max(x1, x2);
        foreach (var b in all)
        {
            if (eyeY < b.yMin || eyeY > b.yMax) continue;
            if (b.xMax > lo && b.xMin < hi) return true;   // 阻挡体横在两者之间
        }
        return false;
    }

    /// <summary>枪械判定v2：2D 射线 vs 矩形（slab 法），t=命中距离。通用几何工具。</summary>
    public static bool RayVsRect(Vector2 o, Vector2 d, Rect r, float maxDist, out float t)
    {
        t = 0f;
        float tmin = 0f, tmax = maxDist;

        if (Mathf.Abs(d.x) < 1e-6f) { if (o.x < r.xMin || o.x > r.xMax) return false; }
        else
        {
            float t1 = (r.xMin - o.x) / d.x, t2 = (r.xMax - o.x) / d.x;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tmin = Mathf.Max(tmin, t1); tmax = Mathf.Min(tmax, t2);
        }
        if (Mathf.Abs(d.y) < 1e-6f) { if (o.y < r.yMin || o.y > r.yMax) return false; }
        else
        {
            float t1 = (r.yMin - o.y) / d.y, t2 = (r.yMax - o.y) / d.y;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tmin = Mathf.Max(tmin, t1); tmax = Mathf.Min(tmax, t2);
        }
        if (tmin > tmax) return false;
        t = tmin;
        return t <= maxDist;
    }

    /// <summary>枪械判定v2：射线打到的最近阻挡体（墙/关门/碎石，斜线通用），返回是否命中及距离。</summary>
    public static bool RaycastNearest(Vector2 origin, Vector2 dir, float maxDist, out float dist)
    {
        dist = float.MaxValue;
        bool hit = false;
        foreach (var b in all)
        {
            var r = new Rect(b.xMin, b.yMin, b.xMax - b.xMin, b.yMax - b.yMin);
            if (RayVsRect(origin, dir, r, maxDist, out float t) && t < dist) { dist = t; hit = true; }
        }
        return hit;
    }

    /// <summary>追击v2：找出会截停这次移动的阻挡体（无则 null）——用于区分"撞门(砸)"和"撞墙(丢失)"。</summary>
    public static Blocker FirstBlocking(float fromX, float toX, float centerY)
    {
        foreach (var b in all)
        {
            if (centerY < b.yMin || centerY > b.yMax) continue;
            if (fromX <= b.xMin && toX > b.xMin) return b;
            if (fromX >= b.xMax && toX < b.xMax) return b;
        }
        return null;
    }

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
