using UnityEngine;

/// <summary>
/// Day 1：相机平滑跟随目标，并保持你在编辑器里摆好的相对位置/角度。
/// 挂在 Main Camera 上，把 target 设为玩家即可——你摆成俯视还是横版，它都照搬那个视角跟随。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("跟随目标（拖玩家进来）")]
    public Transform target;

    [Tooltip("跟随平滑时间，0 = 瞬间贴住目标")]
    public float smoothTime = 0.15f;

    Vector3 offset;
    Vector3 velocity;

    void Start()
    {
        // 记录开场时相机相对玩家的位置差，之后一直保持这个视角
        if (target != null) offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
