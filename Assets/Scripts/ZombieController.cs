using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 丧尸。闭环v1：贴身持续掉血退役，改为可读招的前摇攻击制——
/// 进入攻击范围 → 抬手前摇(头顶红色充能条,world-space,充满即挥) → 挥击一次25伤 → 后摇 → 仍在范围则重复。
/// 受击硬直 0.25s：停止移动与攻击流程，前摇被打断则重新抬手（单只可被连击压制是 v1 允许，多只自然破解）。
/// 朝向视野(正面/背后)与静步减半、巡逻/站桩、闪白/击退/死亡缩小 均保留。
/// 充能条只做红色，不做绿色弹反段（弹反块未开）。纯 transform + 距离判断。
/// </summary>
public class ZombieController : MonoBehaviour
{
    public enum Mode { Patrol, Standing }

    [Header("行为")]
    public Mode mode = Mode.Patrol;
    [Tooltip("巡逻范围（以出生点为中心的总宽度）")]
    public float patrolRange = 5f;
    [Tooltip("站桩尸初始朝向（勾=朝右）；巡逻尸朝向跟随移动方向")]
    public bool facingRight = true;

    [Header("视野")]
    [Tooltip("面朝方向的发现距离（玩家静步时减半）")]
    public float sightRange = 7f;
    [Tooltip("背后感知距离（贴脸才被发现，静步不影响）")]
    public float backSenseRange = 1.5f;

    [Header("移动")]
    public float moveSpeed = 2.2f;

    [Header("攻击（前摇制）")]
    [Tooltip("攻击范围：与玩家的 X 距离小于它就开始抬手")]
    public float touchRange = 1.2f;
    [Tooltip("前摇时长（行走尸0.7 / 跑尸0.5）")]
    public float windupTime = 0.7f;
    [Tooltip("挥击伤害")]
    public float swingDamage = 25f;
    [Tooltip("后摇时长")]
    public float swingRecovery = 0.5f;
    [Tooltip("受击硬直时长")]
    public float staggerTime = 0.25f;

    Transform player;
    Health playerHealth;
    PlayerMovement playerMove;
    float originX;
    int facing;
    int patrolDir = 1;
    bool alerted;
    bool dying;
    Renderer rend;
    Color baseColor;

    enum AtkState { None, Windup, Recovery }
    AtkState atk = AtkState.None;
    float atkTimer;
    float staggerTimer;

    GameObject barRoot;
    Image barFill;

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

    /// <summary>被玩家命中：闪白+击退（打击手感v1b）+ 硬直（闭环v1，打断前摇）。</summary>
    public void OnHit(Vector3 attackerPos)
    {
        if (dying) return;
        int away = transform.position.x >= attackerPos.x ? 1 : -1;
        transform.position += new Vector3(away * 0.3f, 0f, 0f);
        StopCoroutine(nameof(FlashWhite));
        StartCoroutine(nameof(FlashWhite));

        staggerTimer = staggerTime;
        CancelWindup();
    }

    System.Collections.IEnumerator FlashWhite()
    {
        if (rend == null) yield break;
        rend.material.SetColor("_BaseColor", Color.white);
        yield return new WaitForSeconds(0.1f);
        if (rend != null) rend.material.SetColor("_BaseColor", baseColor);
    }

    public void DieShrink()
    {
        if (dying) return;
        dying = true;
        CancelWindup();
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
            float effectiveSight = (playerMove != null && playerMove.IsSneaking) ? sightRange * 0.5f : sightRange;
            bool inFront = Mathf.Sign(dx) == facing;
            if ((inFront && dist <= effectiveSight) || dist <= backSenseRange)
            {
                alerted = true;
                Debug.Log("丧尸发现了你!");
            }
            else if (mode == Mode.Patrol)
            {
                float half = patrolRange * 0.5f;
                float x = transform.position.x + patrolDir * moveSpeed * Time.deltaTime;
                if (x > originX + half) { x = originX + half; patrolDir = -1; }
                else if (x < originX - half) { x = originX - half; patrolDir = 1; }
                facing = patrolDir;
                transform.position = new Vector3(x, transform.position.y, transform.position.z);
            }
            return;
        }

        // —— 已发现 ——
        if (staggerTimer > 0f)                     // 受击硬直：停移动停攻击
        {
            staggerTimer -= Time.deltaTime;
            return;
        }

        switch (atk)
        {
            case AtkState.Windup:                  // 前摇：红条充能，充满即挥
                atkTimer += Time.deltaTime;
                if (barFill != null) barFill.fillAmount = Mathf.Clamp01(atkTimer / windupTime);
                if (atkTimer >= windupTime)
                {
                    HideBar();
                    if (dist <= touchRange && playerHealth != null)
                        playerHealth.TakeDamage(swingDamage);   // 挥击一次；无敌帧由 Health 拦
                    atk = AtkState.Recovery;
                    atkTimer = 0f;
                }
                break;

            case AtkState.Recovery:                // 后摇
                atkTimer += Time.deltaTime;
                if (atkTimer >= swingRecovery) { atk = AtkState.None; atkTimer = 0f; }
                break;

            default:                               // 追击 / 进入范围抬手
                if (dist <= touchRange)
                {
                    atk = AtkState.Windup;
                    atkTimer = 0f;
                    facing = (int)Mathf.Sign(dx);
                    ShowBar();
                }
                else
                {
                    int dir = (int)Mathf.Sign(dx);
                    facing = dir;
                    transform.position += new Vector3(dir, 0f, 0f) * moveSpeed * Time.deltaTime;
                }
                break;
        }
    }

    void CancelWindup()
    {
        if (atk == AtkState.Windup) HideBar();
        atk = AtkState.None;
        atkTimer = 0f;
    }

    // —— 头顶红色充能条（world-space，占位纯色，只红不绿） ——
    void ShowBar()
    {
        if (barRoot == null) BuildBar();
        barFill.fillAmount = 0f;
        barRoot.SetActive(true);
    }

    void HideBar()
    {
        if (barRoot != null) barRoot.SetActive(false);
    }

    void BuildBar()
    {
        barRoot = new GameObject("ChargeBar");
        barRoot.transform.SetParent(transform, false);
        barRoot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        var canvas = barRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = barRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 16f);
        rt.localScale = Vector3.one * 0.01f;

        var white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        var bgGO = new GameObject("Bg");
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.SetParent(barRoot.transform, false);
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        var bg = bgGO.AddComponent<Image>();
        bg.sprite = white; bg.color = new Color(0f, 0f, 0f, 0.6f);

        var fillGO = new GameObject("Fill");
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.SetParent(barRoot.transform, false);
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(2, 2); fillRT.offsetMax = new Vector2(-2, -2);
        barFill = fillGO.AddComponent<Image>();
        barFill.sprite = white;
        barFill.color = new Color(0.95f, 0.15f, 0.1f, 0.95f);
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFill.fillAmount = 0f;

        barRoot.SetActive(false);
    }
}
