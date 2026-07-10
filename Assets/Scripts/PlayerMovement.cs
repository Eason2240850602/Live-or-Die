using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家左右移动（A/D、方向键）。闭环v1：按住 Ctrl 静步——移速减半，丧尸正面视野减半（由 ZombieController 读 IsSneaking）。
/// 静步视觉提示：胶囊压扁 10%。纯 transform，不引入物理。
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("移动速度（世界单位/秒）")]
    public float moveSpeed = 5f;

    [Tooltip("静步速度（按住 Ctrl）")]
    public float sneakSpeed = 2.5f;

    /// <summary>朝向（只读）：+1 右 / -1 左。相机前瞻用。</summary>
    public int Facing { get; private set; } = 1;

    /// <summary>静步中（只读）：丧尸正面视野减半用。</summary>
    public bool IsSneaking { get; private set; }

    float baseScaleY = -1f;

    void Awake() { baseScaleY = transform.localScale.y; }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        IsSneaking = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;

        // 静步视觉：压扁 10%
        float targetY = IsSneaking ? baseScaleY * 0.9f : baseScaleY;
        if (!Mathf.Approximately(transform.localScale.y, targetY))
            transform.localScale = new Vector3(transform.localScale.x, targetY, transform.localScale.z);

        float x = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;

        if (x != 0f) Facing = x > 0f ? 1 : -1;

        float speed = IsSneaking ? sneakSpeed : moveSpeed;
        transform.position += new Vector3(x, 0f, 0f) * speed * Time.deltaTime;
    }
}
