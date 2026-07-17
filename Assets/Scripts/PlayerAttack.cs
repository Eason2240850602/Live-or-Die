using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家攻击。挥击(J/左键)：档案分武器 + 挥击定身(攻击即承诺,LockFor 时间戳制)。
/// 序章修③ 左轮(右键)：弹巢9;备弹=背包里的手枪弹药;打空自动装填、R 手动装填;
/// 装填 2 秒**可移动不定身**;射击定身 0.2;声 r=15。
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Tooltip("空手攻击距离")]
    public float unarmedRange = 1.2f;

    [Tooltip("空手挥击定身时长（秒）")]
    public float unarmedSwingLock = 0.35f;

    [Tooltip("持械挥击定身时长（秒，武器档案未指定时用）")]
    public float armedSwingLock = 0.3f;

    [Header("左轮")]
    [Tooltip("弹巢容量")]
    public int cylinderSize = 9;
    [Tooltip("装填时长（秒，期间可移动、不可射击）")]
    public float reloadTime = 2f;

    /// <summary>弹巢内当前发数（HUD 用）。</summary>
    public int Loaded { get; private set; }
    /// <summary>装填中（HUD 用）。</summary>
    public bool Reloading { get; private set; }
    /// <summary>备弹=背包里的手枪弹药总数（HUD 用）。</summary>
    public int Reserve => inventory != null ? inventory.CountItem("手枪弹药") : 0;

    Inventory inventory;
    PlayerMovement pm;
    bool swinging;

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        pm = GetComponent<PlayerMovement>();
        Loaded = cylinderSize;   // 开局满巢
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool hasPistol = inventory != null && inventory.LeftHand == "手枪";

        // R 手动装填（装填可与移动并行，不吃 swinging/Locked）
        if (hasPistol && !Reloading && kb != null && kb.rKey.wasPressedThisFrame)
            TryReload();

        if (swinging) return;
        if (pm != null && pm.Locked) return;   // 处决/定身中

        bool pressed = (kb != null && kb.jKey.wasPressedThisFrame)
                    || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (pressed) { StartCoroutine(Swing()); return; }

        bool shoot = mouse != null && mouse.rightButton.wasPressedThisFrame;
        if (shoot && hasPistol) TryShoot();
    }

    // ———— 左轮（左轮修：瞬发时序——当帧结算目标+伤害+枪声+曳光，定身(后坐力)垫在其后） ————
    void TryShoot()
    {
        if (Reloading) return;
        if (Loaded <= 0) { TryReload(); return; }   // 打空点射键 → 自动装填
        FireRevolver();
    }

    // 左轮修D：攻击即转身——触发帧比较鼠标屏幕X与角色屏幕X，攻击方向=鼠标侧，与移动方向无关
    int MouseSide()
    {
        var mouse = Mouse.current;
        if (mouse == null) return pm != null ? pm.Facing : 1;
        float mx = mouse.position.ReadValue().x;
        float px = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position).x : Screen.width * 0.5f;
        return mx >= px ? 1 : -1;
    }

    // 鼠标指针的世界坐标（X-Y 平面，z=0）
    Vector2 MouseWorld()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null)
            return (Vector2)transform.position + Vector2.right * (pm != null ? pm.Facing : 1);
        Vector3 sp = mouse.position.ReadValue();
        sp.z = -cam.transform.position.z;   // 相机 z=-10 → 求交到 z=0 平面
        return cam.ScreenToWorldPoint(sp);
    }

    // 枪械判定v2：朝鼠标指针方向的真实射线弹道，先撞谁算谁（墙/关门/丧尸矩形）
    void FireRevolver()
    {
        Loaded--;
        var def = ItemDatabase.Get("手枪");
        NoiseSystem.Emit(transform.position, def.noiseRadius, "枪声");   // r=15 全楼警报

        // 鼠标侧转身在先（管朝向），同帧出枪；弹道随即朝新面向侧
        int facing = MouseSide();
        if (pm != null) pm.SetFacing(facing);

        Vector2 muzzle = (Vector2)transform.position + new Vector2(0f, 0.4f);   // 胸口高度
        Vector2 dir = (MouseWorld() - muzzle).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = new Vector2(facing, 0f);
        float range = def.attackRange > 0f ? def.attackRange : 25f;

        // 1) 墙/关门/碎石（斜线求交）
        float stopDist = range;
        bool hitWall = Blocker.RaycastNearest(muzzle, dir, range, out float wallDist);
        if (hitWall) stopDist = wallDist;

        // 2) 丧尸受击矩形：取阻挡前最近的一个（IsDying 跳过；限同层，斜穿楼板不判中）
        ZombieController hitZ = null;
        float zDist = stopDist;
        foreach (var z in Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
        {
            if (z.IsDying) continue;
            if (Mathf.Abs(z.transform.position.y - transform.position.y) > 3f) continue;
            if (Blocker.RayVsRect(muzzle, dir, z.HitRect(), zDist, out float t) && t < zDist)
            {
                zDist = t;
                hitZ = z;
            }
        }

        // 3) 结算 + 真实曳光（画到实际命中点）
        Vector3 m3 = new Vector3(muzzle.x, muzzle.y, 0f);
        if (hitZ != null)
        {
            hitZ.OnHit(transform.position, 0f);
            var h = hitZ.GetComponent<Health>();
            if (h != null) h.TakeDamage(def.damage);
            Vector2 hp = muzzle + dir * zDist;
            Tracer(m3, new Vector3(hp.x, hp.y, 0f));
            Debug.Log($"枪击命中: {hitZ.name}");
        }
        else if (hitWall)
        {
            Vector2 wp = muzzle + dir * wallDist;
            Tracer(m3, new Vector3(wp.x, wp.y, 0f));
            Debug.Log("枪击打在墙上");
        }
        else
        {
            Vector2 ep = muzzle + dir * range;
            Tracer(m3, new Vector3(ep.x, ep.y, 0f));
            Debug.Log("枪击落空");
        }

        float lockTime = def.swingLock > 0f ? def.swingLock : 0.2f;
        if (pm != null) pm.LockFor(lockTime);      // 后坐力定身（在出弹之后）
        StartCoroutine(RecoilGate(lockTime));

        if (Loaded <= 0 && Reserve > 0) TryReload();   // 打空自动装填
    }

    IEnumerator RecoilGate(float t)
    {
        swinging = true;
        yield return new WaitForSeconds(t);
        swinging = false;
    }

    // 占位曳光：白色细线 0.05s（调试与手感两用，正式美术阶段替换）
    void Tracer(Vector3 from, Vector3 to)
    {
        var go = new GameObject("Tracer");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.startWidth = lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = Color.white;
        Destroy(go, 0.05f);
    }

    void TryReload()
    {
        if (Reloading || Loaded >= cylinderSize || Reserve <= 0) return;
        StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        Reloading = true;
        Debug.Log("装填中...");
        yield return new WaitForSeconds(reloadTime);   // 期间可移动，不定身
        int need = cylinderSize - Loaded;
        int got = inventory != null ? inventory.ConsumeItem("手枪弹药", need) : 0;
        Loaded += got;
        Reloading = false;
        Debug.Log($"装填完成 {Loaded}/{Reserve}");
    }

    // ———— 挥击 ————
    IEnumerator Swing()
    {
        swinging = true;
        if (pm != null) pm.SetFacing(MouseSide());   // 左轮修D：挥击同样转向鼠标侧

        bool armed = inventory != null && inventory.RightHand != null;
        var def = armed ? ItemDatabase.Get(inventory.RightHand) : default;
        float range = armed && def.attackRange > 0f ? def.attackRange : unarmedRange;
        float knock = armed ? def.knockback : 0f;
        float noise = armed && def.noiseRadius > 0f ? def.noiseRadius : 4f;
        int damage = inventory != null ? inventory.AttackDamage : 25;
        float lockTime = armed ? (def.swingLock > 0f ? def.swingLock : armedSwingLock) : unarmedSwingLock;

        if (pm != null) pm.LockFor(lockTime);   // 定身至出手（时间戳制）
        yield return new WaitForSeconds(lockTime);
        swinging = false;

        NoiseSystem.Emit(transform.position, noise, "挥击");
        foreach (var z in Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
        {
            if (z.IsDying) continue;   // 左轮修A：近战同修——不砍尸体
            if (Vector3.Distance(transform.position, z.transform.position) <= range)
            {
                z.OnHit(transform.position, knock);
                var h = z.GetComponent<Health>();
                if (h != null) h.TakeDamage(damage);
            }
        }
    }
}
