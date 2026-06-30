using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Day 2：通用血量组件。HP 归零后按 onDeath 处理：
/// 玩家用 ReloadScene（打印 "You died" 并重开一局），丧尸用 Destroy（从场景消失）。
/// 血量是内部变量，不做血条/UI。
/// </summary>
public class Health : MonoBehaviour
{
    public enum DeathAction { Destroy, ReloadScene }

    [Tooltip("最大血量")]
    public float maxHealth = 100f;

    [Tooltip("死亡处理：Destroy=从场景消失(丧尸)；ReloadScene=重开一局(玩家)")]
    public DeathAction onDeath = DeathAction.Destroy;

    float current;
    bool dead;

    void Awake() { current = maxHealth; }

    /// <summary>扣血；归零触发死亡。</summary>
    public void TakeDamage(float amount)
    {
        if (dead || amount <= 0f) return;
        current -= amount;
        if (current <= 0f)
        {
            dead = true;
            Die();
        }
    }

    void Die()
    {
        if (onDeath == DeathAction.ReloadScene)
        {
            Debug.Log("You died");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
