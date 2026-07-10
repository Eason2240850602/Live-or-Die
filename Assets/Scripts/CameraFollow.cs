using UnityEngine;

/// <summary>
/// 相机v2：朝向前瞻 + 地图边界钳制。
/// X = 玩家X + 朝向×lookAhead，再钳制进 [minX, maxX]；Y/Z 维持编辑器摆放的偏移不动。
/// 转身靠 SmoothDamp 自然过渡，不做额外动画/死区/缩放。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("跟随目标（拖玩家进来）")]
    public Transform target;

    [Tooltip("跟随平滑时间，0 = 瞬间贴住目标")]
    public float smoothTime = 0.15f;

    [Tooltip("朝向前瞻距离：镜头向玩家面朝方向偏移")]
    public float lookAhead = 3f;

    [Header("边界钳制（相机 X 可到达的范围）")]
    public float minX = 10f;
    public float maxX = 140f;

    Vector3 offset;
    Vector3 velocity;
    PlayerMovement pm;

    void Start()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
            pm = target.GetComponent<PlayerMovement>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        int facing = pm != null ? pm.Facing : 1;
        float goalX = Mathf.Clamp(target.position.x + offset.x + facing * lookAhead, minX, maxX);
        Vector3 goal = new Vector3(goalX, target.position.y + offset.y, target.position.z + offset.z);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
