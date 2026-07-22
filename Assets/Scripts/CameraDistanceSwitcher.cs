using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 镜头基准候选档切换器（Play 调试用，选定后固化正式值并删除本脚本）。
/// 只调**距离**（CameraFollow.Offset 的 Z 分量），FOV 保持现值不动——变量单一化。
/// 数字键 1/2/3/4 切换，0.3s 平滑插值；与 F1/F2/F3 风格档共存互不干扰。
///
/// 档位（相对现状百分比，实际数值 Start 时按现状换算并打进 Console/HUD）：
///   档1 现状(远) 100% ／ 档2 中 75% ／ 档3 近 55%(目标:角色占屏高约1/3,对标参考游戏) ／ 档4 特写 40%
/// </summary>
public class CameraDistanceSwitcher : MonoBehaviour
{
    [Tooltip("四档距离系数（档1=现状基准）")]
    public float[] presets = { 1f, 0.75f, 0.55f, 0.40f };

    [Tooltip("切换插值时长（秒）")]
    public float lerpTime = 0.3f;

    CameraFollow follow;
    float baseZ;          // 现状 Z 偏移（负值，|z| = 侧视距离）
    float baseY;          // 现状 Y 偏移
    int current = 1;      // 1..4
    float fromZ, toZ, fromY, toY, t = 1f;

    void Start()
    {
        follow = Object.FindFirstObjectByType<CameraFollow>();
        if (follow == null) { enabled = false; return; }

        baseZ = follow.Offset.z;
        baseY = follow.Offset.y;
        fromZ = toZ = baseZ;
        fromY = toY = baseY;

        Debug.Log($"[镜头] 现状距离 {Mathf.Abs(baseZ):F2} → 候选: " +
                  $"档2 {Mathf.Abs(baseZ * presets[1]):F2} / 档3 {Mathf.Abs(baseZ * presets[2]):F2} / 档4 {Mathf.Abs(baseZ * presets[3]):F2}；数字键 1-4 切换");
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) Apply(1);
            else if (kb.digit2Key.wasPressedThisFrame) Apply(2);
            else if (kb.digit3Key.wasPressedThisFrame) Apply(3);
            else if (kb.digit4Key.wasPressedThisFrame) Apply(4);
        }

        if (t < 1f)   // 平滑过渡（unscaled：背包页暂停时也能调）
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / Mathf.Max(lerpTime, 0.01f));
            float e = Mathf.SmoothStep(0f, 1f, t);
            var o = follow.Offset;
            o.z = Mathf.Lerp(fromZ, toZ, e);
            o.y = Mathf.Lerp(fromY, toY, e);
            follow.Offset = o;
        }
    }

    void Apply(int mode)
    {
        current = Mathf.Clamp(mode, 1, presets.Length);
        float k = presets[current - 1];

        fromZ = follow.Offset.z; toZ = baseZ * k;
        fromY = follow.Offset.y; toY = baseY * k;   // 高度同比收，保持侧视俯角不变
        t = 0f;

        float dist = Mathf.Abs(toZ);
        HudController.Instance?.ShowMessage($"镜头档 {current}　距离 {dist:F2}", 1.5f);
        Debug.Log($"[镜头] 档{current} 距离={dist:F2} (Y偏移={toY:F2})");
    }
}
