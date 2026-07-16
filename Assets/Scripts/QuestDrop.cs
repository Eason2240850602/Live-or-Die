using UnityEngine;

/// <summary>
/// 序章：任务掉落物（博士掉的元素资料）。紫色小方块，玩家走近自动入包；背包满则留在原地等。
/// </summary>
public class QuestDrop : MonoBehaviour
{
    public string itemName = "元素资料";
    public float pickupRange = 1.2f;

    Transform player;
    Inventory inventory;

    public static void Spawn(string itemName, Vector3 at)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "QuestDrop_" + itemName;
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.position = new Vector3(at.x, at.y - 0.6f, 0f);
        go.transform.localScale = Vector3.one * 0.4f;
        go.GetComponent<Renderer>().material.SetColor("_BaseColor", new Color(0.6f, 0.25f, 0.9f));
        var qd = go.AddComponent<QuestDrop>();
        qd.itemName = itemName;
    }

    void Start()
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
        if (player == null || inventory == null) return;
        if (Vector3.Distance(transform.position, player.position) > pickupRange) return;
        if (inventory.AcquireItem(itemName, 1) > 0)
        {
            Debug.Log($"拾取:{itemName}");
            HudController.Instance?.ShowMessage($"获得:{itemName}", 2f);
            Debug.Log(inventory.GridView());
            Destroy(gameObject);
        }
    }
}
