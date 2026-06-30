using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Day 1：玩家占位胶囊的左右移动。
/// 只沿 X 轴左右走：A / 左方向键 = 左，D / 右方向键 = 右。去掉了前后(W/S)移动。
/// 移动速度在 Inspector 可调。用新 Input System 直接读键盘。
/// 用最简单的 transform 移动，不引入物理。
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("移动速度（世界单位/秒）")]
    public float moveSpeed = 5f;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;   // 没有键盘设备时直接跳过

        float x = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;

        // 只在 X 轴上左右移动
        transform.position += new Vector3(x, 0f, 0f) * moveSpeed * Time.deltaTime;
    }
}
