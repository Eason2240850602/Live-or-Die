using UnityEngine;

/// <summary>
/// Day 2：丧尸占位体。沿 X 轴朝玩家靠近；足够近时持续对玩家造成接触伤害。
/// 纯 transform 移动 + 距离判断，不使用物理/寻路。
/// 自动找场景里的玩家（带 PlayerMovement 的对象），不需要手动拖引用。
/// </summary>
public class ZombieController : MonoBehaviour
{
    [Tooltip("移动速度（单位/秒）")]
    public float moveSpeed = 2f;

    [Tooltip("接触距离：与玩家的 X 距离小于它，就贴住并开始掉血")]
    public float touchRange = 1.2f;

    [Tooltip("接触伤害（每秒）")]
    public float touchDamagePerSecond = 20f;

    Transform player;
    Health playerHealth;

    void Awake()
    {
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            playerHealth = pm.GetComponent<Health>();
        }
    }

    void Update()
    {
        if (player == null) return;

        float dx = player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);

        if (dist > touchRange)
        {
            // 朝玩家方向沿 X 轴走近
            float dir = Mathf.Sign(dx);
            transform.position += new Vector3(dir, 0f, 0f) * moveSpeed * Time.deltaTime;
        }
        else if (playerHealth != null)
        {
            // 贴住玩家：持续掉血
            playerHealth.TakeDamage(touchDamagePerSecond * Time.deltaTime);
        }
    }
}
