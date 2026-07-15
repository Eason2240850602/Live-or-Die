using UnityEngine;

/// <summary>
/// 相机：居中跟随 + 边界钳制（前瞻已回滚）。
/// 键位手感批：蹲下镜头——进入蹲行 Y 下沉 crouchCamDrop、FOV 收窄到 crouchFov，camLerpTime 平滑过渡；起身反向恢复。
/// 不做投影矩阵压扁。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("跟随目标（拖玩家进来）")]
    public Transform target;

    [Tooltip("跟随平滑时间，0 = 瞬间贴住目标")]
    public float smoothTime = 0.15f;

    [Tooltip("朝向前瞻距离（已回滚默认0，正确形态见构想库7.19）")]
    public float lookAhead = 0f;

    [Header("边界钳制（相机 X 可到达的范围）")]
    public float minX = 8f;
    public float maxX = 140f;

    [Header("蹲下镜头")]
    [Tooltip("蹲行时相机 Y 下沉量")]
    public float crouchCamDrop = 0.8f;
    [Tooltip("蹲行时 FOV（常态用相机初始值）")]
    public float crouchFov = 42f;
    [Tooltip("蹲/起过渡时长（秒）")]
    public float camLerpTime = 0.3f;

    Vector3 offset;
    Vector3 velocity;
    PlayerMovement pm;
    Camera cam;
    float baseFov;
    float crouchBlend;   // 0=站 1=蹲

    void Start()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
            pm = target.GetComponent<PlayerMovement>();
        }
        cam = GetComponent<Camera>();
        if (cam != null) baseFov = cam.fieldOfView;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 蹲下镜头：0.3s 平滑过渡
        float blendTarget = (pm != null && pm.IsSneaking) ? 1f : 0f;
        crouchBlend = Mathf.MoveTowards(crouchBlend, blendTarget, Time.deltaTime / Mathf.Max(camLerpTime, 0.01f));
        if (cam != null) cam.fieldOfView = Mathf.Lerp(baseFov, crouchFov, crouchBlend);

        int facing = pm != null ? pm.Facing : 1;
        float goalX = Mathf.Clamp(target.position.x + offset.x + facing * lookAhead, minX, maxX);
        float goalY = target.position.y + offset.y - crouchCamDrop * crouchBlend;
        Vector3 goal = new Vector3(goalX, goalY, target.position.z + offset.z);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
