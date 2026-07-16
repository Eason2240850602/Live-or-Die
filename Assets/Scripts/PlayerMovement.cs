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
    [Tooltip("走路速度（世界单位/秒）")]
    public float moveSpeed = 5f;

    [Tooltip("蹲行速度（Ctrl 切换蹲/走）")]
    public float sneakSpeed = 2.5f;

    [Tooltip("跑步速度（双击方向键触发，按住维持）")]
    public float runSpeed = 7f;

    [Tooltip("双击判定间隔（秒）")]
    public float doubleTapWindow = 0.3f;

    [Tooltip("楼梯攀爬速度（单位/秒）")]
    public float stairSpeed = 3.5f;

    [Header("声音半径（蹲行=0 不发声）")]
    public float walkNoise = 3f;
    public float runNoise = 8f;
    public float noisePulseInterval = 0.5f;

    /// <summary>朝向（只读）：+1 右 / -1 左。</summary>
    public int Facing { get; private set; } = 1;

    /// <summary>蹲行中（只读）：丧尸视野减半/暗杀条件用。</summary>
    public bool IsSneaking { get; private set; }

    /// <summary>跑步中（只读）：声音分级用。</summary>
    public bool IsRunning { get; private set; }

    /// <summary>移动锁定（处决/挥击定身/抓取/剥夺共用）。</summary>
    public bool Locked { get; set; }

    /// <summary>定身 seconds 秒后自动解锁（玩家侧计时——施加方死亡也不会卡锁）。</summary>
    public void LockFor(float seconds) => StartCoroutine(LockRoutine(seconds));

    System.Collections.IEnumerator LockRoutine(float seconds)
    {
        Locked = true;
        yield return new WaitForSeconds(seconds);
        Locked = false;
    }

    float baseScaleY = -1f;
    StairZone climbing;   // 非空 = 攀爬中（吸附路径线）
    float climbT;         // 0=底端点 1=顶端点
    int lastTapDir;       // 双击跑检测
    float lastTapTime = -10f;

    void Awake() { baseScaleY = transform.localScale.y; }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || Locked) return;

        // —— 键位批：蹲/起 = Ctrl 或 C(双绑定,切换式)；开箱窗口开着时让位给 Ctrl 组合键 ——
        bool crouchTap = kb.leftCtrlKey.wasPressedThisFrame || kb.rightCtrlKey.wasPressedThisFrame
                      || kb.cKey.wasPressedThisFrame;
        if (crouchTap && !LootWindow.AnyOpen)
        {
            IsSneaking = !IsSneaking;
            if (IsSneaking) IsRunning = false;
        }

        bool leftHeld  = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
        bool rightHeld = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
        bool leftTap   = kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;
        bool rightTap  = kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;

        // 跑 = Shift 按住(主) 或 双击方向(保留)；蹲下触发跑 = 先起身
        if (leftTap || rightTap)
        {
            int dir = rightTap ? 1 : -1;
            if (dir == lastTapDir && Time.time - lastTapTime <= doubleTapWindow)
            {
                IsRunning = true;
                IsSneaking = false;
            }
            lastTapDir = dir;
            lastTapTime = Time.time;
        }
        bool shiftHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        if (shiftHeld && (leftHeld || rightHeld))
        {
            IsRunning = true;
            IsSneaking = false;
        }
        if (IsRunning && !shiftHeld && !((lastTapDir > 0 && rightHeld) || (lastTapDir < 0 && leftHeld)))
            IsRunning = false;        // 双击跑：松开方向键回走；Shift 跑：松开 Shift 且无双击维持时回走
        if (IsRunning && !(leftHeld || rightHeld))
            IsRunning = false;

        float targetY = IsSneaking ? baseScaleY * 0.9f : baseScaleY;
        if (!Mathf.Approximately(transform.localScale.y, targetY))
            transform.localScale = new Vector3(transform.localScale.x, targetY, transform.localScale.z);

        // —— 攀爬状态：吸附楼梯路径线，只沿梯轴动，横向输入无效；只在两端脱离 ——
        if (climbing != null) { ClimbUpdate(kb); return; }

        float x = 0f;
        if (leftHeld)  x -= 1f;
        if (rightHeld) x += 1f;
        if (x != 0f) Facing = x > 0f ? 1 : -1;

        float speed = IsSneaking ? sneakSpeed : (IsRunning ? runSpeed : moveSpeed);
        float newX = transform.position.x + x * speed * Time.deltaTime;
        newX = Blocker.ClampMove(transform.position.x, newX, transform.position.y);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        EmitMovePulse(x != 0f);

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
        EmitMovePulse(v != 0f);
    }

    // 移动声音脉冲：蹲行 0 / 走路 3 / 跑步 8
    float noiseTimer;
    void EmitMovePulse(bool moved)
    {
        if (!moved || IsSneaking) { noiseTimer = 0f; return; }
        noiseTimer += Time.deltaTime;
        if (noiseTimer >= noisePulseInterval)
        {
            noiseTimer = 0f;
            NoiseSystem.Emit(transform.position, IsRunning ? runNoise : walkNoise, IsRunning ? "跑步" : "走路");
        }
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
