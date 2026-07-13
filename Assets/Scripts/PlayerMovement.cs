using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家移动（A/D 左右、Ctrl 静步）。研究所灰盒新增：
///   - 楼梯：X 在 StairZone 内时按 W/S(或上下键)沿梯匀速上下，纯 transform；
///     上下到一半(不在任一层地面)时不许横着走出楼梯区
///   - 阻挡：X 位移经 Blocker.ClampMove 截停(碎石堆/外墙)，门洞(无阻挡)自然可过
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("移动速度（世界单位/秒）")]
    public float moveSpeed = 5f;

    [Tooltip("静步速度（按住 Ctrl）")]
    public float sneakSpeed = 2.5f;

    [Tooltip("楼梯攀爬速度（单位/秒）")]
    public float stairSpeed = 3.5f;

    /// <summary>朝向（只读）：+1 右 / -1 左。</summary>
    public int Facing { get; private set; } = 1;

    /// <summary>静步中（只读）。</summary>
    public bool IsSneaking { get; private set; }

    float baseScaleY = -1f;
    StairZone climbing;   // 非空 = 攀爬中（吸附路径线）
    float climbT;         // 0=底端点 1=顶端点

    void Awake() { baseScaleY = transform.localScale.y; }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        IsSneaking = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;

        float targetY = IsSneaking ? baseScaleY * 0.9f : baseScaleY;
        if (!Mathf.Approximately(transform.localScale.y, targetY))
            transform.localScale = new Vector3(transform.localScale.x, targetY, transform.localScale.z);

        // —— 攀爬状态：吸附楼梯路径线，只沿梯轴动，横向输入无效；只在两端脱离 ——
        if (climbing != null) { ClimbUpdate(kb); return; }

        float x = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        if (x != 0f) Facing = x > 0f ? 1 : -1;

        float speed = IsSneaking ? sneakSpeed : moveSpeed;
        float newX = transform.position.x + x * speed * Time.deltaTime;
        newX = Blocker.ClampMove(transform.position.x, newX, transform.position.y);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        // —— 进入楼梯：底层按 W(上) / 顶层按 S(下)，吸附到对应端点 ——
        var zone = StairZone.At(transform.position.x);
        if (zone != null)
        {
            float y = transform.position.y;
            bool atBottom = Mathf.Abs(y - (zone.bottomY + 1f)) < 0.1f;
            bool atTop    = Mathf.Abs(y - (zone.topY + 1f)) < 0.1f;
            bool up   = kb.wKey.isPressed || kb.upArrowKey.isPressed;
            bool down = kb.sKey.isPressed || kb.downArrowKey.isPressed;

            if (atBottom && up)   BeginClimb(zone, 0f);
            else if (atTop && down) BeginClimb(zone, 1f);
        }
    }

    void BeginClimb(StairZone zone, float t)
    {
        climbing = zone;
        climbT = t;
        SnapToPath();
    }

    void ClimbUpdate(Keyboard kb)
    {
        float v = 0f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   v += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;

        if (v != 0f)
        {
            float len = Vector2.Distance(climbing.BottomPoint, climbing.TopPoint);
            climbT += v * stairSpeed * Time.deltaTime / Mathf.Max(len, 0.01f);
        }

        if (climbT <= 0f)      { ExitClimb(climbing.BottomPoint); return; }
        if (climbT >= 1f)      { ExitClimb(climbing.TopPoint); return; }
        SnapToPath();
    }

    void SnapToPath()
    {
        Vector2 p = Vector2.Lerp(climbing.BottomPoint, climbing.TopPoint, climbT);
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }

    void ExitClimb(Vector2 at)
    {
        transform.position = new Vector3(at.x, at.y, transform.position.z);
        climbing = null;
    }
}
