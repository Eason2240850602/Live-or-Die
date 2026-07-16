using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家攻击（J / 鼠标左键）。攻击档案批：
///   - 档案分武器：距离/击退/噪音/伤害读 ItemDatabase（空手 1.2距离/0击退/噪4/伤25）
///   - 挥击定身（攻击即承诺）：按下后定身至伤害出手——空手0.35s/持械0.3s；定身期间可被丧尸正常攻击
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Tooltip("空手攻击距离")]
    public float unarmedRange = 1.2f;

    [Tooltip("空手挥击定身时长（秒），到时伤害出手")]
    public float unarmedSwingLock = 0.35f;

    [Tooltip("持械挥击定身时长（秒）")]
    public float armedSwingLock = 0.3f;

    Inventory inventory;
    PlayerMovement pm;
    bool swinging;

    void Awake() { inventory = GetComponent<Inventory>(); pm = GetComponent<PlayerMovement>(); }

    void Update()
    {
        if (Time.timeScale == 0f || swinging) return;      // 暂停/挥击中
        if (pm != null && pm.Locked) return;               // 处决动作中

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool pressed = (kb != null && kb.jKey.wasPressedThisFrame)
                    || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (pressed) { StartCoroutine(Swing()); return; }

        // 序章：左手手枪 → 右键射击（命中面朝方向最近丧尸，无瞄准；序章期间无限弹）
        bool shoot = mouse != null && mouse.rightButton.wasPressedThisFrame;
        if (shoot && inventory != null && inventory.LeftHand == "手枪")
            StartCoroutine(Shoot());
    }

    IEnumerator Shoot()
    {
        swinging = true;
        var def = ItemDatabase.Get("手枪");
        float lockTime = def.swingLock > 0f ? def.swingLock : 0.2f;

        if (pm != null) pm.Locked = true;      // 射击定身
        yield return new WaitForSeconds(lockTime);
        if (pm != null) pm.Locked = false;
        swinging = false;

        NoiseSystem.Emit(transform.position, def.noiseRadius, "枪声");   // r=15 全楼警报

        // 面朝方向最近、同层、视线无遮挡的一只
        int facing = pm != null ? pm.Facing : 1;
        ZombieController best = null;
        float bestDist = float.MaxValue;
        foreach (var z in Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
        {
            float zdx = z.transform.position.x - transform.position.x;
            if (Mathf.Sign(zdx) != facing) continue;
            if (Mathf.Abs(z.transform.position.y - transform.position.y) > 3f) continue;
            if (Blocker.BlocksLine(transform.position.x, z.transform.position.x, transform.position.y)) continue;
            float d = Mathf.Abs(zdx);
            if (d < bestDist) { bestDist = d; best = z; }
        }
        if (best != null)
        {
            best.OnHit(transform.position, 0f);
            var h = best.GetComponent<Health>();
            if (h != null) h.TakeDamage(def.damage);
        }
    }

    IEnumerator Swing()
    {
        swinging = true;

        bool armed = inventory != null && inventory.RightHand != null;
        var def = armed ? ItemDatabase.Get(inventory.RightHand) : default;
        float range = armed && def.attackRange > 0f ? def.attackRange : unarmedRange;
        float knock = armed ? def.knockback : 0f;
        float noise = armed && def.noiseRadius > 0f ? def.noiseRadius : 4f;
        int damage = inventory != null ? inventory.AttackDamage : 25;
        float lockTime = armed ? (def.swingLock > 0f ? def.swingLock : armedSwingLock) : unarmedSwingLock;

        if (pm != null) pm.Locked = true;      // 定身至出手（期间可被打，换血是玩家自己的决定）
        yield return new WaitForSeconds(lockTime);
        if (pm != null) pm.Locked = false;
        swinging = false;

        // 出手：噪音 + 范围内命中
        NoiseSystem.Emit(transform.position, noise, "挥击");
        foreach (var z in Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
        {
            if (Vector3.Distance(transform.position, z.transform.position) <= range)
            {
                z.OnHit(transform.position, knock);
                var h = z.GetComponent<Health>();
                if (h != null) h.TakeDamage(damage);
            }
        }
    }
}
