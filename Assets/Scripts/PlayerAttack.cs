using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Day 2：玩家攻击。按 J 或 鼠标左键，对范围内的丧尸造成一次伤害。
/// 用距离判断命中（不使用物理碰撞器）。新 Input System 读输入。
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Tooltip("攻击范围：与丧尸的距离小于它就打到")]
    public float attackRange = 2f;

    [Tooltip("每次攻击造成的伤害")]
    public float attackDamage = 50f;

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool pressed = (kb != null && kb.jKey.wasPressedThisFrame)
                    || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (!pressed) return;

        // 找出场景里所有丧尸，范围内的扣血
        var zombies = Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None);
        foreach (var z in zombies)
        {
            if (Vector3.Distance(transform.position, z.transform.position) <= attackRange)
            {
                z.OnHit(transform.position);   // 打击手感v1b：闪白+微击退
                var h = z.GetComponent<Health>();
                if (h != null) h.TakeDamage(attackDamage);
            }
        }
    }
}
