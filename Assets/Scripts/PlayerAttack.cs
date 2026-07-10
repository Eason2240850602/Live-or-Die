using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家攻击。按 J 或 鼠标左键，对范围内丧尸造成一次伤害。
/// 闭环v1：伤害由装备决定——空手25，撬棍50（读 Inventory.AttackDamage）；暂停(背包页)时不挥击。
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Tooltip("攻击范围")]
    public float attackRange = 2f;

    Inventory inventory;

    void Awake() { inventory = GetComponent<Inventory>(); }

    void Update()
    {
        if (Time.timeScale == 0f) return;   // 背包页暂停中：点 UI 不挥击

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool pressed = (kb != null && kb.jKey.wasPressedThisFrame)
                    || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (!pressed) return;

        int damage = inventory != null ? inventory.AttackDamage : 25;
        var zombies = Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None);
        foreach (var z in zombies)
        {
            if (Vector3.Distance(transform.position, z.transform.position) <= attackRange)
            {
                z.OnHit(transform.position);   // 闪白+击退+硬直
                var h = z.GetComponent<Health>();
                if (h != null) h.TakeDamage(damage);
            }
        }
    }
}
