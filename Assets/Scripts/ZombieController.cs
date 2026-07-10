using UnityEngine;

/// <summary>
/// 环境v1：丧尸获得朝向与视野。
/// 未发现玩家时：巡逻尸在出生点小范围左右来回(朝向=移动方向)；站桩尸静止(朝向 Inspector 指定)。
/// 发现规则：玩家在面朝方向且距离 ≤ sightRange → 追击；在背后须距离 ≤ backSenseRange(贴脸)才发现。
/// 一旦发现追击不解除(v1 不做脱战)。追上后贴住持续掉血。
/// 纯 transform + 距离判断，不用物理/寻路。
/// </summary>
public class ZombieController : MonoBehaviour
{
    public enum Mode { Patrol, Standing }

    [Header("行为")]
    [Tooltip("Patrol=小范围巡逻；Standing=原地站桩")]
    public Mode mode = Mode.Patrol;

    [Tooltip("巡逻范围（以出生点为中心的总宽度，单位）")]
    public float patrolRange = 5f;

    [Tooltip("站桩尸初始朝向（勾=朝右，不勾=朝左）；巡逻尸朝向跟随移动方向")]
    public bool facingRight = true;

    [Header("视野")]
    [Tooltip("面朝方向的发现距离")]
    public float sightRange = 7f;

    [Tooltip("背后感知距离（贴脸才被发现）")]
    public float backSenseRange = 1.5f;

    [Header("移动与伤害")]
    [Tooltip("移动速度（单位/秒）")]
    public float moveSpeed = 2.2f;

    [Tooltip("接触距离：与玩家的 X 距离小于它，就贴住并开始掉血")]
    public float touchRange = 1.2f;

    [Tooltip("接触伤害（每秒）")]
    public float touchDamagePerSecond = 35f;

    Transform player;
    Health playerHealth;
    PlayerMovement playerMove;
    float originX;          // 出生点（巡逻中心）
    int facing;             // +1 朝右 / -1 朝左
    int patrolDir = 1;
    bool alerted;
    bool dying;
    Renderer rend;
    Color baseColor;

    void Awake()
    {
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            playerHealth = pm.GetComponent<Health>();
            playerMove = pm;
        }
        originX = transform.position.x;
        facing = facingRight ? 1 : -1;
        patrolDir = facing;
        rend = GetComponent<Renderer>();
        if (rend != null) baseColor = rend.material.GetColor("_BaseColor");
    }

    /// <summary>打击手感v1b：被玩家命中——闪白约0.1秒 + 沿远离玩家方向微击退0.3单位。</summary>
    public void OnHit(Vector3 attackerPos)
    {
        if (dying) return;
        int away = transform.position.x >= attackerPos.x ? 1 : -1;
        transform.position += new Vector3(away * 0.3f, 0f, 0f);
        StopCoroutine(nameof(FlashWhite));
        StartCoroutine(nameof(FlashWhite));
    }

    System.Collections.IEnumerator FlashWhite()
    {
        if (rend == null) yield break;
        rend.material.SetColor("_BaseColor", Color.white);
        yield return new WaitForSeconds(0.1f);
        if (rend != null) rend.material.SetColor("_BaseColor", baseColor);
    }

    /// <summary>打击手感v1b：死亡反馈——0.25秒缩小至消失，期间不伤害不移动。由 Health 调用。</summary>
    public void DieShrink()
    {
        if (dying) return;
        dying = true;
        StartCoroutine(ShrinkAndDestroy());
    }

    System.Collections.IEnumerator ShrinkAndDestroy()
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < 0.25f)
        {
            transform.localScale = Vector3.Lerp(start, Vector3.zero, t / 0.25f);
            t += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    void Update()
    {
        if (dying || player == null) return;

        float dx = player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);

        if (!alerted)
        {
            // 发现判定：面前 sightRange 内，或背后贴脸 backSenseRange 内；静步时正面视野减半(背后不变)
            float effectiveSight = (playerMove != null && playerMove.IsSneaking) ? sightRange * 0.5f : sightRange;
            bool inFront = Mathf.Sign(dx) == facing;
            if ((inFront && dist <= effectiveSight) || dist <= backSenseRange)
            {
                alerted = true;
                Debug.Log("丧尸发现了你!");
            }
            else if (mode == Mode.Patrol)
            {
                // 小范围来回巡逻，朝向=移动方向
                float half = patrolRange * 0.5f;
                float x = transform.position.x + patrolDir * moveSpeed * Time.deltaTime;
                if (x > originX + half) { x = originX + half; patrolDir = -1; }
                else if (x < originX - half) { x = originX - half; patrolDir = 1; }
                facing = patrolDir;
                transform.position = new Vector3(x, transform.position.y, transform.position.z);
            }
            // Standing：静止，朝向固定
            return;
        }

        // 已发现：追击不解除
        if (dist > touchRange)
        {
            int dir = (int)Mathf.Sign(dx);
            facing = dir;
            transform.position += new Vector3(dir, 0f, 0f) * moveSpeed * Time.deltaTime;
        }
        else if (playerHealth != null)
        {
            playerHealth.TakeDamage(touchDamagePerSecond * Time.deltaTime);
        }
    }
}
