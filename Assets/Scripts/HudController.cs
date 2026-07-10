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

    // 打击手感v1b：玩家受击红边框
    Health playerHealth;
    float lastHealth = -1f;
    float hurtHold;                    // 掉血停止后的保持时间
    readonly Image[] borders = new Image[4];
    const float BorderAlpha = 0.5f;

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
        playerHealth = pm != null ? pm.GetComponent<Health>() : null;
        lastHealth = playerHealth != null ? playerHealth.Current : -1f;
        hurtHold = 0f;
        SetBorderAlpha(0f);
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

        // 受击红边框：掉血期间亮起，停止掉血约 0.5 秒后淡出
        if (playerHealth != null)
        {
            if (playerHealth.Current < lastHealth) hurtHold = 0.5f;
            lastHealth = playerHealth.Current;
        }
        if (hurtHold > 0f)
        {
            hurtHold -= Time.deltaTime;
            SetBorderAlpha(BorderAlpha);
        }
        else if (borders[0] != null && borders[0].color.a > 0f)
        {
            SetBorderAlpha(Mathf.MoveTowards(borders[0].color.a, 0f, Time.deltaTime * (BorderAlpha / 0.5f)));
        }
    }

    void SetBorderAlpha(float a)
    {
        foreach (var b in borders)
            if (b != null) b.color = new Color(0.85f, 0.1f, 0.1f, a);
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

        // 受击红边框：四条纯色 Image 贴屏幕边缘，初始透明
        borders[0] = MakeBorder("BorderTop",    new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -9),  new Vector2(0, 18));
        borders[1] = MakeBorder("BorderBottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 9),   new Vector2(0, 18));
        borders[2] = MakeBorder("BorderLeft",   new Vector2(0, 0), new Vector2(0, 1), new Vector2(9, 0),   new Vector2(18, 0));
        borders[3] = MakeBorder("BorderRight",  new Vector2(1, 0), new Vector2(1, 1), new Vector2(-9, 0),  new Vector2(18, 0));
    }

    Image MakeBorder(string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.sprite = white;
        img.color = new Color(0.85f, 0.1f, 0.1f, 0f);
        img.raycastTarget = false;
        return img;
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
