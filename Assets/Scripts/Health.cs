using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 通用血量组件。闭环v1：
///   - HealPlayer：消耗品回血，上限截断
///   - 斩杀线保命（一条 if，无算术）：致死伤害时医疗快捷栏非空 → 消耗1 → 血量=道具恢复值 → 1秒无敌帧(闪烁)
///   - 无敌帧期间不受任何伤害
/// 丧尸死亡走 DieShrink，玩家死亡 ReloadScene（不变）。
/// </summary>
public class Health : MonoBehaviour
{
    public enum DeathAction { Destroy, ReloadScene }

    [Tooltip("最大血量")]
    public float maxHealth = 100f;

    [Tooltip("死亡处理：Destroy=丧尸；ReloadScene=玩家")]
    public DeathAction onDeath = DeathAction.Destroy;

    [Tooltip("斩杀线保命后的无敌帧时长（秒）")]
    public float invulnDuration = 1f;

    float current;
    bool dead;
    bool invulnerable;

    /// <summary>当前血量（只读）。</summary>
    public float Current => current;

    void Awake() { current = maxHealth; }

    /// <summary>序章导演用：直接设置上限与当前值（buff 1200 / 剥夺流回 100）。</summary>
    public void SetMaxAndCurrent(float max, float cur)
    {
        maxHealth = max;
        current = Mathf.Clamp(cur, 1f, max);
    }

    /// <summary>消耗品回血，上限截断。</summary>
    public void HealPlayer(float amount)
    {
        if (dead || amount <= 0f) return;
        current = Mathf.Min(current + amount, maxHealth);
    }

    /// <summary>扣血；无敌帧免疫；致死时先走斩杀线保命；归零死亡。</summary>
    public void TakeDamage(float amount)
    {
        if (dead || invulnerable || amount <= 0f) return;

        // 斩杀线保命：会把血量打到 ≤0 且快捷栏非空 → 消耗1 → 血量=恢复值 → 无敌帧
        if (current - amount <= 0f)
        {
            var inv = GetComponent<Inventory>();
            if (inv != null && inv.TryKillLineSave(out int restoreTo, out string itemName))
            {
                current = restoreTo;
                Debug.Log($"{itemName}救了你!");
                HudController.Instance?.ShowMessage($"{itemName}救了你!", 2f);
                HudController.Instance?.FlashHpHighlight();   // 血条跳到恢复值并高亮
                StartCoroutine(InvulnBlink());
                return;
            }
        }

        current -= amount;
        if (current <= 0f)
        {
            dead = true;
            Die();
        }
    }

    IEnumerator InvulnBlink()
    {
        invulnerable = true;
        var rend = GetComponent<Renderer>();
        Color baseCol = rend != null ? rend.material.GetColor("_BaseColor") : Color.white;
        float t = 0f;
        while (t < invulnDuration)
        {
            if (rend != null)
                rend.material.SetColor("_BaseColor", (int)(t * 10f) % 2 == 0 ? new Color(1f, 1f, 1f, 0.4f) : baseCol);
            t += Time.deltaTime;
            yield return null;
        }
        if (rend != null) rend.material.SetColor("_BaseColor", baseCol);
        invulnerable = false;
    }

    void Die()
    {
        if (onDeath == DeathAction.ReloadScene)
        {
            Debug.Log("You died");
            Time.timeScale = 1f;   // 防止暂停状态下死亡导致重开后世界冻结
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            var zc = GetComponent<ZombieController>();
            if (zc != null) zc.DieShrink();
            else Destroy(gameObject);
        }
    }
}
