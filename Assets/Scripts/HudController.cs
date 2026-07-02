using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 盲盒第 4 块：最简 HUD（uGUI，运行时代码生成，零美术零外部资产）。
/// 三件：A 搜刮进度条（中下方）B 开箱/打断结果弹显（中部，限时）C 背包计数角标（右上，常驻）。
/// 松耦合：背包计数自轮询 Inventory；进度条/弹显由 Pickup 调用。Console 打印全保留，UI 只叠加。
/// 撤离明细清单不做 UI（维持 Console）。
/// 自举：RuntimeInitializeOnLoadMethod 自动挂载，DontDestroyOnLoad 跨重开一局存活。
/// </summary>
public class HudController : MonoBehaviour
{
    public static HudController Instance { get; private set; }

    Inventory inventory;
    Font font;
    Sprite white;
    LootWindow lootWindow;

    Text counterText;
    Text messageText;
    float messageTimer;
    GameObject progressRoot;
    Image progressFill;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("HudController").AddComponent<HudController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "SimSun", "Arial" }, 24);
        white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        BuildUI();

        var lwGO = new GameObject("LootWindow");
        lwGO.transform.SetParent(transform, false);
        lootWindow = lwGO.AddComponent<LootWindow>();
        lootWindow.Init(font, white);

        SceneManager.sceneLoaded += OnSceneLoaded;
        AcquireInventory();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        AcquireInventory();
        HideProgress();
        if (messageText != null) messageText.text = "";
        messageTimer = 0f;
        lootWindow?.Close();
    }

    void AcquireInventory()
    {
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        inventory = pm != null ? pm.GetComponent<Inventory>() : null;
    }

    void Update()
    {
        if (counterText != null)
            counterText.text = inventory != null ? $"背包 {inventory.Count}/{inventory.capacity} 格" : "背包 0/0 格";

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f && messageText != null) messageText.text = "";
        }
    }

    // —— 供 Pickup 调用 ——
    public void ShowProgress() { if (progressRoot != null) progressRoot.SetActive(true); SetProgress(0f); }
    public void SetProgress(float t01) { if (progressFill != null) progressFill.fillAmount = Mathf.Clamp01(t01); }
    public void HideProgress() { if (progressRoot != null) progressRoot.SetActive(false); }

    public void ShowMessage(string msg, float seconds)
    {
        if (messageText == null) return;
        messageText.text = msg;
        messageTimer = seconds;
    }

    public void OpenLootWindow(Pickup container) => lootWindow?.Open(container);

    void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        // C 背包计数（右上角常驻）
        counterText = MakeText("Counter", new Vector2(1, 1), new Vector2(-20, -20), new Vector2(320, 50),
            new Vector2(1, 1), TextAnchor.UpperRight, 30);

        // B 结果/打断弹显（中部偏上，限时）
        messageText = MakeText("Message", new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1000, 220),
            new Vector2(0.5f, 0.5f), TextAnchor.MiddleCenter, 36);
        messageText.text = "";

        // A 进度条（中下方，默认隐藏）
        progressRoot = new GameObject("Progress");
        var prt = progressRoot.AddComponent<RectTransform>();
        prt.SetParent(transform, false);
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0, 90);
        prt.sizeDelta = new Vector2(500, 28);

        var bg = progressRoot.AddComponent<Image>();
        bg.sprite = white;
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        var fillGO = new GameObject("Fill");
        var frt = fillGO.AddComponent<RectTransform>();
        frt.SetParent(progressRoot.transform, false);
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(3, 3); frt.offsetMax = new Vector2(-3, -3);
        progressFill = fillGO.AddComponent<Image>();
        progressFill.sprite = white;
        progressFill.color = new Color(0.3f, 0.9f, 0.4f, 0.95f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0f;

        progressRoot.SetActive(false);
    }

    Text MakeText(string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Vector2 pivot,
        TextAnchor align, int fontSize)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var txt = go.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = fontSize;
        txt.alignment = align;
        txt.color = Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2, -2);
        return txt;
    }
}
