using UnityEngine;

/// <summary>
/// 感知v1：声音系统。声音事件 = 位置 + 半径，纯 3D 距离判定，穿墙穿楼层（不做遮挡衰减）。
/// 半径内丧尸立刻取得声源坐标并追击（听到=直接扑，无调查状态）。
/// 追击v2：exclude = 发出声音的丧尸自身（砸门声不惊动自己）。
/// 不做图形化显示，Console 打日志（仅在有丧尸被惊动时）。
/// </summary>
public static class NoiseSystem
{
    public static void Emit(Vector3 pos, float radius, string label, ZombieController exclude = null)
    {
        int woke = 0;
        foreach (var z in Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
        {
            if (z == exclude) continue;
            if (Vector3.Distance(z.transform.position, pos) <= radius && z.HearNoise(pos))
                woke++;
        }
        if (woke > 0) Debug.Log($"[声音] {label} r={radius} 惊动 {woke} 只");
    }
}
