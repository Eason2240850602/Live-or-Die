using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Day 3：撤离点。玩家靠近（距离判断）→ 结算"本次带回 X 物资" → 重载当前场景（重开一局）。
/// </summary>
public class ExtractionPoint : MonoBehaviour
{
    [Tooltip("撤离触发距离")]
    public float extractRange = 1.5f;

    Transform player;
    Inventory inventory;
    bool triggered;

    void Awake()
    {
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            inventory = pm.GetComponent<Inventory>();
        }
    }

    void Update()
    {
        if (triggered || player == null) return;

        if (Vector3.Distance(transform.position, player.position) <= extractRange)
        {
            triggered = true;
            int n = inventory != null ? inventory.Count : 0;
            Debug.Log($"本次带回 {n} 物资");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
