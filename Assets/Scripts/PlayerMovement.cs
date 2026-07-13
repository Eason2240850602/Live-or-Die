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

    void Awake() { baseScaleY = transform.localScale.y; }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        IsSneaking = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;

        float targetY = IsSneaking ? baseScaleY * 0.9f : baseScaleY;
        if (!Mathf.Approximately(transform.localScale.y, targetY))
            transform.localScale = new Vector3(transform.localScale.x, targetY, transform.localScale.z);

        float x = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        if (x != 0f) Facing = x > 0f ? 1 : -1;

        // —— 楼梯：区域内 W/S 上下 ——
        float y = transform.position.y;
        var zone = StairZone.At(transform.position.x);
        if (zone != null)
        {
            float vy = 0f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   vy += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vy -= 1f;
            if (vy != 0f)
                y = Mathf.Clamp(y + vy * stairSpeed * Time.deltaTime, zone.bottomY + 1f, zone.topY + 1f);
        }

        // —— 水平移动：阻挡截停；楼梯中段不许横出 ——
        float speed = IsSneaking ? sneakSpeed : moveSpeed;
        float newX = transform.position.x + x * speed * Time.deltaTime;
        newX = Blocker.ClampMove(transform.position.x, newX, y);

        bool midStairs = zone != null
            && Mathf.Abs(y - (zone.bottomY + 1f)) > 0.05f
            && Mathf.Abs(y - (zone.topY + 1f)) > 0.05f;
        if (midStairs) newX = Mathf.Clamp(newX, zone.xMin, zone.xMax);

        transform.position = new Vector3(newX, y, transform.position.z);
    }
}
