using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 感知v1段3：无声暗杀。条件：蹲行 + 位于丧尸背后(其朝向背离玩家) + 距离≤1.2 + 按 F。
/// 流程：约1秒处决动作(玩家吸附到目标背后、目标挣扎抖动)，期间玩家不可移动；
/// 受到任何伤害 → 中断取消。完成 → 目标无声即死(零声音零红条)。
/// 条件满足时 HUD 显示"F 处决"提示。白名单看 ZombieController.canBeExecuted。
/// </summary>
public class PlayerExecution : MonoBehaviour
{
    [Tooltip("处决触发距离")]
    public float executeRange = 1.2f;

    [Tooltip("处决动作时长（秒）")]
    public float executeDuration = 1f;

    PlayerMovement pm;
    Health health;

    bool executing;
    float t;
    float healthAtStart;
    ZombieController target;

    public bool IsExecuting => executing;

    void Awake()
    {
        pm = GetComponent<PlayerMovement>();
        health = GetComponent<Health>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (LootWindow.AnyOpen) { HudController.Instance?.SetExecuteHint(false); return; }   // 开箱时 F 让位给 Ctrl+F 全拿

        if (executing)
        {
            if (target == null || target.IsDying) { Finish(false); return; }
            if (health != null && health.Current < healthAtStart)   // 受伤 → 处决中断取消
            {
                target.CancelExecuted();
                Finish(false);
                return;
            }
            t += Time.deltaTime;
            if (t >= executeDuration)
            {
                target.ExecuteKill();   // 无声即死
                Finish(true);
            }
            return;
        }

        var cand = FindTarget();
        HudController.Instance?.SetExecuteHint(cand != null);

        var kb = Keyboard.current;
        if (cand != null && kb != null && kb.fKey.wasPressedThisFrame)
            Begin(cand);
    }

    ZombieController FindTarget()
    {
        if (pm == null || !pm.IsSneaking || pm.Locked) return null;   // 必须蹲行；挥击定身中不可处决

        ZombieController best = null;
        float bestDist = float.MaxValue;
        foreach (var z in Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
        {
            if (!z.canBeExecuted || z.IsDying || z.BeingExecuted) continue;
            float d = Vector3.Distance(transform.position, z.transform.position);
            if (d > executeRange || d >= bestDist) continue;
            if (Mathf.Abs(z.transform.position.y - transform.position.y) > 3f) continue;   // 同层

            // 背后判定：丧尸朝向背离玩家
            int towardPlayer = transform.position.x >= z.transform.position.x ? 1 : -1;
            if (z.FacingDir == towardPlayer) continue;   // 它面对着你 → 不满足

            best = z; bestDist = d;
        }
        return best;
    }

    void Begin(ZombieController z)
    {
        executing = true;
        t = 0f;
        target = z;
        healthAtStart = health != null ? health.Current : 0f;
        pm.Locked = true;
        HudController.Instance?.SetExecuteHint(false);

        // 玩家吸附到目标背后
        float behind = z.transform.position.x - z.FacingDir * 0.8f;
        transform.position = new Vector3(behind, transform.position.y, transform.position.z);

        z.StartExecuted();
    }

    void Finish(bool _)
    {
        executing = false;
        target = null;
        pm.Locked = false;
    }
}
