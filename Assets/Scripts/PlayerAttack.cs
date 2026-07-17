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

    void FireRevolver()
    {
        Loaded--;
        var def = ItemDatabase.Get("手枪");
        NoiseSystem.Emit(transform.position, def.noiseRadius, "枪声");   // r=15 全楼警报

        int facing = MouseSide();
        if (pm != null) pm.SetFacing(facing);   // 翻转朝向，定身结束后由移动输入自然接管
        ZombieController best = null;
        float bestDist = float.MaxValue;
        foreach (var z in Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
        {
            if (z.IsDying) continue;   // 左轮修A：尸体不挡枪
            float zdx = z.transform.position.x - transform.position.x;
            if (Mathf.Sign(zdx) != facing) continue;
            if (Mathf.Abs(z.transform.position.y - transform.position.y) > 3f) continue;
            if (Blocker.BlocksLine(transform.position.x, z.transform.position.x, transform.position.y)) continue;
            float d = Mathf.Abs(zdx);
            if (d < bestDist) { bestDist = d; best = z; }
        }

        Vector3 muzzle = transform.position + new Vector3(facing * 0.4f, 0.4f, 0f);
        if (best != null)
        {
            best.OnHit(transform.position, 0f);
            var h = best.GetComponent<Health>();
            if (h != null) h.TakeDamage(def.damage);
            Tracer(muzzle, best.transform.position);
            Debug.Log($"枪击命中: {best.name}");
        }
        else
        {
            Tracer(muzzle, muzzle + new Vector3(facing * 12f, 0f, 0f));   // 落空:面朝12单位
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
