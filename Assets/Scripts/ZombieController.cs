using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 丧尸。追击v2：视觉与听觉同构——追击目标 = 最后已知位置(LKP)：
///   看得见时每帧刷新；声音给坐标。到达 LKP 看不见 → 张望 2.5s → 脱战回岗(patrolSpeed)。
///   追击态可用右梯跨层(速度=追击×0.8，可被攻击硬直"卡"一下)；巡逻/驻足/回岗永远锁本层。
///   追击撞关门 → 砸门 3s(每秒声音 r=6，会惊动别人) → 门开立即扑；砸满未见 → 按丢失脱战。不做破门。
/// 保留：前摇攻击/硬直/巡逻驻足/朝向视野(蹲行减半,无背后感知)/遮挡/偷窥豁免/处决/闪白击退/朝向标记。
/// 纯 transform + 距离判断。
/// </summary>
public class ZombieController : MonoBehaviour
{
    public enum Mode { Patrol, Standing }

    [Header("行为")]
    public Mode mode = Mode.Patrol;
    [Tooltip("巡逻范围（以出生点为中心的总宽度）")]
    public float patrolRange = 5f;
    [Tooltip("站桩尸初始朝向（勾=朝右）")]
    public bool facingRight = true;

    [Header("视野")]
    [Tooltip("面朝方向的发现距离（玩家蹲行时减半）")]
    public float sightRange = 7f;
    [Tooltip("(已退役·fix3) 背后感知——丧尸只有视觉和听觉,无第六感")]
    public float backSenseRange = 1.5f;

    [Header("移动")]
    [Tooltip("追击速度（视觉锁定/声音扑向）")]
    public float moveSpeed = 2.2f;
    [Tooltip("巡逻/回岗速度")]
    public float patrolSpeed = 1.2f;
    [Tooltip("端点驻足时长范围（秒）")]
    public float dwellMin = 5f;
    public float dwellMax = 8f;

    [Header("追击v2")]
    [Tooltip("到达最后已知位置后的张望时长（秒），到时脱战")]
    public float lookoutTime = 2.5f;
    [Tooltip("砸门时长（秒），砸满未见玩家按丢失处理")]
    public float bashTime = 3f;
    [Tooltip("砸门声音半径（每秒一次，会惊动其他丧尸）")]
    public float bashNoiseRadius = 6f;
    [Tooltip("爬梯速度 = 追击速度 × 该系数")]
    public float climbSpeedFactor = 0.8f;

    [Header("攻击（前摇制）")]
    public float touchRange = 1.2f;
    [Tooltip("前摇时长（行走尸0.7 / 跑尸0.5）")]
    public float windupTime = 0.7f;
    public float swingDamage = 25f;
    public float swingRecovery = 0.5f;
    public float staggerTime = 0.25f;

    [Tooltip("可被无声处决（精英/防弹尸设 false）")]
    public bool canBeExecuted = true;

    // —— runtime ——
    Transform player;
    Health playerHealth;
    PlayerMovement playerMove;
    float originX;
    int facing;
    int patrolDir = 1;
    bool dying;
    Renderer rend;
    Color baseColor;

    bool engaged;            // 追击态（视觉/听觉共用）
    Vector3 targetPos;       // 最后已知位置
    float lookout;
    bool returning;
    float dwellTimer;

    enum AtkState { None, Windup, Recovery }
    AtkState atk = AtkState.None;
    float atkTimer;
    float staggerTimer;

    StairZone climbZone;     // 非空=爬梯中
    float climbT;
    int climbDir;            // +1 上 / -1 下

    Door bashDoor;           // 非空=砸门中
    float bashTimer, bashNoiseTimer;

    GameObject barRoot;
    Image barFill;
    Transform faceMarker;

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
        BuildFaceMarker();
    }

    // —— 处决（感知v1段3） ——
    public int FacingDir => facing;
    public bool IsDying => dying;
    public bool BeingExecuted { get; private set; }

    public void StartExecuted() { BeingExecuted = true; CancelWindup(); staggerTimer = 0f; }
    public void CancelExecuted() { BeingExecuted = false; }
    public void ExecuteKill() { BeingExecuted = false; DieShrink(); }

    /// <summary>听到声音：取得声源坐标。已在追击 → 刷新 LKP 并放弃砸门；否则进入追击。垂死/被处决不响应。</summary>
    public bool HearNoise(Vector3 pos)
    {
        if (dying || BeingExecuted) return false;
        targetPos = pos;
        lookout = 0f;
        returning = false;
        bashDoor = null;         // 有新声源 → 不砸了，扑声音
        engaged = true;
        return true;
    }

    /// <summary>被玩家命中：闪白+击退+硬直（打断前摇）。</summary>
    public void OnHit(Vector3 attackerPos)
    {
        if (dying) return;
        int away = transform.position.x >= attackerPos.x ? 1 : -1;
        if (climbZone == null)   // 爬梯中不位移(防被打下楼梯路径)，只硬直"卡"一下
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

        if (BeingExecuted)
        {
            float jx = Mathf.Sin(Time.time * 40f) * 0.03f;
            transform.position = new Vector3(transform.position.x + jx, transform.position.y, transform.position.z);
            return;
        }

        if (staggerTimer > 0f) { staggerTimer -= Time.deltaTime; return; }   // 硬直（含爬梯中"卡"）

        float dx = player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);
        bool sameFloor = Mathf.Abs(player.position.y - transform.position.y) < 3f;

        // 追击中的视觉刷新（全向）：看得见 → 每帧刷新 LKP
        bool canSeeNow = sameFloor && dist <= sightRange && !PlayerInteraction.IsPeeking
            && !Blocker.BlocksLine(transform.position.x, player.position.x, transform.position.y);
        if (engaged && canSeeNow) { targetPos = player.position; lookout = 0f; }

        if (!engaged)
        {
            // 初次发现（视觉）：同层+正面朝向+视距(蹲行减半)+无遮挡+非偷窥（fix3：无背后感知）
            float eff = (playerMove != null && playerMove.IsSneaking) ? sightRange * 0.5f : sightRange;
            bool inFront = Mathf.Sign(dx) == facing;
            if (canSeeNow && inFront && dist <= eff)
            {
                engaged = true;
                targetPos = player.position;
                lookout = 0f; returning = false;
                Debug.Log("丧尸发现了你!");
            }
            else if (returning) ReturnStep();
            else if (mode == Mode.Patrol) PatrolStep();
            return;
        }

        // —— 追击态 ——
        if (climbZone != null) { ClimbStep(); return; }
        if (bashDoor != null) { BashStep(canSeeNow); return; }

        if (atk != AtkState.None) { AttackStep(dist, sameFloor); return; }
        if (sameFloor && dist <= touchRange)
        {
            atk = AtkState.Windup;
            atkTimer = 0f;
            facing = (int)Mathf.Sign(dx);
            ShowBar();
            return;
        }

        bool targetOtherFloor = Mathf.Abs(targetPos.y - transform.position.y) >= 3f;
        if (targetOtherFloor)
        {
            // 目标在另一层 → 走向右梯入口 → 爬
            var zone = StairZone.Nearest(transform.position.x);
            if (zone == null) { LookoutTick(canSeeNow); return; }
            bool iAmBottom = Mathf.Abs(transform.position.y - (zone.bottomY + 1f))
                           < Mathf.Abs(transform.position.y - (zone.topY + 1f));
            Vector2 entry = iAmBottom ? zone.BottomPoint : zone.TopPoint;
            if (Mathf.Abs(transform.position.x - entry.x) <= 0.3f)
            {
                climbZone = zone;
                climbDir = iAmBottom ? 1 : -1;
                climbT = iAmBottom ? 0f : 1f;
            }
            else MoveEngagedTowards(entry.x, canSeeNow);
        }
        else if (Mathf.Abs(targetPos.x - transform.position.x) > 0.5f)
        {
            MoveEngagedTowards(targetPos.x, canSeeNow);
        }
        else if (!canSeeNow)
        {
            LookoutTick(canSeeNow);   // 到达 LKP 且看不见 → 张望
        }
    }

    // ———— 追击移动（撞门→砸；撞墙→按丢失计时） ————
    void MoveEngagedTowards(float tx, bool canSeeNow)
    {
        int dir = (int)Mathf.Sign(tx - transform.position.x);
        facing = dir;
        float nx = transform.position.x + dir * moveSpeed * Time.deltaTime;
        float clamped = Blocker.ClampMove(transform.position.x, nx, transform.position.y);

        if (Mathf.Approximately(clamped, transform.position.x))
        {
            var b = Blocker.FirstBlocking(transform.position.x, nx, transform.position.y);
            var door = b != null ? b.GetComponent<Door>() : null;
            if (door != null && !door.open)
            {
                bashDoor = door;      // 关门挡路 → 砸门
                bashTimer = 0f; bashNoiseTimer = 0f;
                CancelWindup();
            }
            else LookoutTick(canSeeNow);   // 墙/碎石挡死 → 按到达处理
            return;
        }
        transform.position = new Vector3(clamped, transform.position.y, transform.position.z);
    }

    void LookoutTick(bool canSeeNow)
    {
        if (canSeeNow) { lookout = 0f; return; }
        lookout += Time.deltaTime;
        if (lookout >= lookoutTime) Disengage();
    }

    void Disengage()
    {
        engaged = false;
        lookout = 0f;
        bashDoor = null;
        CancelWindup();
        returning = true;   // 回岗（patrolSpeed，锁本层）
    }

    void ClimbStep()
    {
        float len = Vector2.Distance(climbZone.BottomPoint, climbZone.TopPoint);
        climbT += climbDir * moveSpeed * climbSpeedFactor * Time.deltaTime / Mathf.Max(len, 0.01f);
        facing = (int)Mathf.Sign((climbZone.TopPoint.x - climbZone.BottomPoint.x) * climbDir);

        if (climbT >= 1f) { ExitClimb(climbZone.TopPoint); return; }
        if (climbT <= 0f) { ExitClimb(climbZone.BottomPoint); return; }
        Vector2 p = Vector2.Lerp(climbZone.BottomPoint, climbZone.TopPoint, climbT);
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }

    void ExitClimb(Vector2 at)
    {
        transform.position = new Vector3(at.x, at.y, transform.position.z);
        climbZone = null;
    }

    void BashStep(bool canSeeNow)
    {
        if (bashDoor == null || bashDoor.open) { bashDoor = null; return; }   // 门开了 → 立即恢复追击

        facing = (int)Mathf.Sign(bashDoor.doorX - transform.position.x);
        bashTimer += Time.deltaTime;
        bashNoiseTimer += Time.deltaTime;
        if (bashNoiseTimer >= 1f)
        {
            bashNoiseTimer = 0f;
            NoiseSystem.Emit(bashDoor.transform.position, bashNoiseRadius, "砸门", this);
        }
        if (bashTimer >= bashTime)
        {
            bashDoor = null;
            if (!canSeeNow) Disengage();   // 砸满未见 → 按丢失处理
        }
    }

    void AttackStep(float dist, bool sameFloor)
    {
        switch (atk)
        {
            case AtkState.Windup:
                atkTimer += Time.deltaTime;
                if (barFill != null) barFill.fillAmount = Mathf.Clamp01(atkTimer / windupTime);
                if (atkTimer >= windupTime)
                {
                    HideBar();
                    if (sameFloor && dist <= touchRange && playerHealth != null)
                        playerHealth.TakeDamage(swingDamage);
                    atk = AtkState.Recovery;
                    atkTimer = 0f;
                }
                break;
            case AtkState.Recovery:
                atkTimer += Time.deltaTime;
                if (atkTimer >= swingRecovery) { atk = AtkState.None; atkTimer = 0f; }
                break;
        }
    }

    void PatrolStep()
    {
        if (dwellTimer > 0f)
        {
            dwellTimer -= Time.deltaTime;
            if (dwellTimer <= 0f) patrolDir = -patrolDir;
        }
        else
        {
            float half = patrolRange * 0.5f;
            float x = transform.position.x + patrolDir * patrolSpeed * Time.deltaTime;
            bool hitEnd = false;
            if (x >= originX + half) { x = originX + half; hitEnd = true; }
            else if (x <= originX - half) { x = originX - half; hitEnd = true; }
            x = Blocker.ClampMove(transform.position.x, x, transform.position.y);
            facing = patrolDir;
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
            if (hitEnd) dwellTimer = Random.Range(dwellMin, dwellMax);
        }
    }

    void ReturnStep()
    {
        // 回岗锁本层：走向 originX（若被追到另一层，就在当前层 originX 处重新驻防）
        float rdx = originX - transform.position.x;
        if (Mathf.Abs(rdx) > 0.3f)
        {
            int dir = (int)Mathf.Sign(rdx);
            facing = dir;
            float nx = transform.position.x + dir * patrolSpeed * Time.deltaTime;
            nx = Blocker.ClampMove(transform.position.x, nx, transform.position.y);
            if (Mathf.Approximately(nx, transform.position.x)) { returning = false; return; }   // 被挡死就地驻防
            transform.position = new Vector3(nx, transform.position.y, transform.position.z);
        }
        else
        {
            returning = false;
            if (mode == Mode.Standing) facing = facingRight ? 1 : -1;
        }
    }

    void CancelWindup()
    {
        if (atk == AtkState.Windup) HideBar();
        atk = AtkState.None;
        atkTimer = 0f;
    }

    // —— 朝向标记（fix3 占位） ——
    void BuildFaceMarker()
    {
        var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
        m.name = "FaceMarker";
        Object.Destroy(m.GetComponent<Collider>());
        m.transform.SetParent(transform, false);
        m.transform.localScale = new Vector3(0.18f, 0.35f, 0.35f);
        m.GetComponent<Renderer>().material.SetColor("_BaseColor", new Color(0.45f, 0.05f, 0.05f));
        faceMarker = m.transform;
    }

    void LateUpdate()
    {
        if (faceMarker != null)
            faceMarker.localPosition = new Vector3(facing * 0.55f, 0.35f, 0f);
    }

    // —— 头顶红色充能条 ——
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
