using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 序章导演（占位铁律：胶囊+色块+字卡，零美术零动画）。
/// 流程：开场字卡 → 休息室醒来 → 莉莉娅对话 → 上buff(1200血/移速×1.15/双武器/金血条/肃清进度条)
///      → 肃清全楼 → 回休息室 → 剥夺(2秒血条流光,不暂停) → 收场字卡 → 双任务常驻,自由行动。
/// 字卡/对话 timeScale=0；剥夺不暂停。序章期间撤离点停用。撤离重开=重玩序章(当前正确行为)。
/// </summary>
public class PrologueDirector : MonoBehaviour
{
    public GameObject lilia;   // 粉色小胶囊（构建器接线）

    [Tooltip("buff 移速倍率")]
    public float speedMult = 1.15f;
    [Tooltip("剥夺血条流光时长（秒）")]
    public float drainTime = 2f;

    /// <summary>字卡/对话持有暂停中（背包页等让位）。</summary>
    public static bool PauseHeld { get; private set; }

    enum Stage { Intro, Wake, Dialog, Cleanse, ReturnHome, Depriving, Outro, Done }
    Stage stage = Stage.Intro;

    static readonly string[] IntroLines =
    {
        "导弹撕开了研究所。",
        "你本该死在那里。",
        "有人用她的一切,换了你的命。",
    };
    static readonly string[] DialogLines =
    {
        "莉莉娅:「我姐姐还活着,我能感觉到。」",
        "莉莉娅:「现在,借你一点她留给我的东西。」",
        "莉莉娅:「把这栋楼清干净——然后我们谈以后。」",
    };
    static readonly string[] OutroLines =
    {
        "她没有醒来。",
        "但这栋楼,现在是你们的了。",
        "达拉斯的某处,'天使'还活着。",
    };

    int lineIndex;
    string[] activeLines;
    int quipIndex;

    static readonly string[] Quips =
    {
        "莉莉娅:「还撑得住…别停。」",
        "莉莉娅:「感觉到了吗,她们都在害怕你。」",
        "莉莉娅:「快了…我有点冷。」",
    };

    PlayerMovement pm;
    Health health;
    Inventory inventory;
    ExtractionPoint extraction;
    float origMoveSpeed, origRunSpeed;
    float pollTimer;

    Font font;
    Sprite white;
    GameObject cardPanel;
    Text cardText, cardHint;
    Image flash;
    Text progressText, arrowText, questText;
    GameObject dialogPanel;
    Text dialogText;

    void Awake()
    {
        ZombieController.ResetKillCount();
        var p = Object.FindFirstObjectByType<PlayerMovement>();
        if (p != null)
        {
            pm = p;
            health = p.GetComponent<Health>();
            inventory = p.GetComponent<Inventory>();
        }
        extraction = Object.FindFirstObjectByType<ExtractionPoint>();
        if (extraction != null) extraction.enabled = false;   // 序章期间撤离停用

        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "SimSun", "Arial" }, 24);
        white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        BuildUI();
    }

    void Start()
    {
        ShowCards(IntroLines, Stage.Wake);   // 开场字卡
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool advance = (kb != null && kb.eKey.wasPressedThisFrame)
                    || (mouse != null && mouse.leftButton.wasPressedThisFrame);

        switch (stage)
        {
            case Stage.Intro:
            case Stage.Outro:
                if (advance) NextCard();
                break;

            case Stage.Wake:
                if (advance && lilia != null && pm != null
                    && Vector3.Distance(pm.transform.position, lilia.transform.position) <= 1.5f)
                {
                    stage = Stage.Dialog;
                    lineIndex = 0;
                    Time.timeScale = 0f;
                    PauseHeld = true;
                    dialogPanel.SetActive(true);
                    dialogText.text = DialogLines[0];
                }
                break;

            case Stage.Dialog:
                if (advance)
                {
                    lineIndex++;
                    if (lineIndex < DialogLines.Length) dialogText.text = DialogLines[lineIndex];
                    else
                    {
                        dialogPanel.SetActive(false);
                        Time.timeScale = 1f;
                        PauseHeld = false;
                        ApplyBuff();
                    }
                }
                break;

            case Stage.Cleanse:
                pollTimer -= Time.deltaTime;
                if (pollTimer <= 0f)
                {
                    pollTimer = 0.25f;
                    int alive = Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None).Length;
                    int killed = ZombieController.KillCount;
                    int total = killed + alive;
                    int pct = total > 0 ? Mathf.RoundToInt(killed * 100f / total) : 100;
                    progressText.text = $"肃清进度: 已消灭 {killed}/{total} ({pct}%)";

                    // 莉莉娅台词：每过 25% 一句
                    if (quipIndex < Quips.Length && pct >= (quipIndex + 1) * 25 && pct < 100)
                    {
                        HudController.Instance?.ShowMessage(Quips[quipIndex], 3f);
                        quipIndex++;
                    }

                    if (alive == 0)
                    {
                        stage = Stage.ReturnHome;
                        progressText.text = "回去看看她";
                    }
                }
                break;

            case Stage.ReturnHome:
                if (pm == null) break;
                if (pm.transform.position.y < 4f) arrowText.text = "▲ 楼上";
                else if (pm.transform.position.x > 18f) arrowText.text = "◀ 休息室";
                else
                {
                    arrowText.text = "";
                    StartCoroutine(Deprive());   // 进入休息室 → 剥夺
                }
                break;
        }
    }

    void ApplyBuff()
    {
        if (health != null) health.SetMaxAndCurrent(1200f, 1200f);
        if (pm != null)
        {
            origMoveSpeed = pm.moveSpeed; origRunSpeed = pm.runSpeed;
            pm.moveSpeed *= speedMult;
            pm.runSpeed *= speedMult;
        }
        inventory?.PrologueEquip("武士刀", "手枪");
        inventory?.AddItem("手枪弹药", 6);                                // 左轮开局备弹 6(弹巢9另计)
        HudController.Instance?.SetHpGold(true);
        StartCoroutine(Flash(new Color(1f, 0.85f, 0.3f, 0.8f), 0.5f));   // 金闪

        // 补账：床边粉胶囊在 buff 触发帧消失——背后粉块才是"她"（剥夺时按原位置生成躺倒灰胶囊）
        if (lilia != null)
        {
            liliaRestPos = lilia.transform.position;
            Destroy(lilia);
            lilia = null;
        }

        // 莉莉娅在背上：粉色小块贴玩家背后（剥夺时消失）
        if (pm != null)
        {
            backCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backCube.name = "LiliaOnBack";
            Object.Destroy(backCube.GetComponent<Collider>());
            backCube.transform.SetParent(pm.transform, false);
            backCube.transform.localScale = new Vector3(0.3f, 0.45f, 0.3f);
            backCube.GetComponent<Renderer>().material.SetColor("_BaseColor", new Color(1f, 0.6f, 0.75f));
        }
        liliaStatus.text = "莉莉娅: 燃烧中";
        liliaStatus.color = new Color(1f, 0.82f, 0.3f, 1f);
        liliaStatus.gameObject.SetActive(true);

        progressText.gameObject.SetActive(true);
        arrowText.gameObject.SetActive(true);
        stage = Stage.Cleanse;
        pollTimer = 0f;
        Debug.Log("[序章] buff 就位:1200血/武士刀+手枪(9/6)/肃清开始");
    }

    GameObject backCube;
    Vector3 liliaRestPos;   // 床边位置（剥夺时在此生成躺倒灰胶囊）
    void LateUpdate()
    {
        if (backCube != null && pm != null)
            backCube.transform.localPosition = new Vector3(-pm.Facing * 0.45f, 0.3f, 0f);   // 始终在背后
    }

    IEnumerator Deprive()
    {
        stage = Stage.Depriving;
        progressText.gameObject.SetActive(false);
        arrowText.gameObject.SetActive(false);

        StartCoroutine(Flash(new Color(1f, 1f, 1f, 0.9f), 0.3f));   // 白闪
        if (pm != null) pm.LockFor(drainTime + 0.4f);               // 不可操作;不暂停(死寂衬血条流光)

        float t = 0f;
        while (t < drainTime)
        {
            if (health != null) health.SetMaxAndCurrent(1200f, Mathf.Lerp(1200f, 100f, t / drainTime));
            t += Time.deltaTime;
            yield return null;
        }
        if (health != null) health.SetMaxAndCurrent(100f, 100f);

        inventory?.PrologueClear();                                  // 双武器消失,左手回禁用
        if (pm != null) { pm.moveSpeed = origMoveSpeed; pm.runSpeed = origRunSpeed; }
        HudController.Instance?.SetHpGold(false);
        if (backCube != null) Destroy(backCube);                     // 她不在背上了
        liliaStatus.text = "莉莉娅: 沉睡";
        liliaStatus.color = new Color(0.6f, 0.6f, 0.62f, 1f);

        // 补账：生成式——在休息室原位置生成躺倒(旋转90°)的灰胶囊
        var lying = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        lying.name = "Lilia_Sleeping";
        Destroy(lying.GetComponent<Collider>());
        lying.transform.position = new Vector3(liliaRestPos.x, liliaRestPos.y - 0.35f, 0f);   // 躺平贴地
        lying.transform.localScale = Vector3.one * 0.7f;
        lying.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        lying.GetComponent<Renderer>().material.SetColor("_BaseColor", new Color(0.45f, 0.45f, 0.48f));
        if (extraction != null) extraction.enabled = true;           // 撤离恢复
        Debug.Log("[序章] 剥夺完成");

        ShowCards(OutroLines, Stage.Done);
    }

    void ShowCards(string[] lines, Stage after)
    {
        activeLines = lines;
        lineIndex = 0;
        afterCards = after;
        stage = lines == IntroLines ? Stage.Intro : Stage.Outro;
        Time.timeScale = 0f;
        PauseHeld = true;
        cardPanel.SetActive(true);
        cardText.text = lines[0];
    }

    Stage afterCards;

    void NextCard()
    {
        lineIndex++;
        if (lineIndex < activeLines.Length)
        {
            cardText.text += "\n" + activeLines[lineIndex];   // 逐行显示
            return;
        }
        cardPanel.SetActive(false);
        Time.timeScale = 1f;
        PauseHeld = false;
        stage = afterCards;
        if (stage == Stage.Done)
        {
            questText.gameObject.SetActive(true);             // 双任务常驻,自由行动
            Debug.Log("[序章] 结束,自由行动");
        }
    }

    /// <summary>全屏闪光（博士消失等外部调用）。</summary>
    public void ScreenFlash(Color col, float dur) => StartCoroutine(Flash(col, dur));

    IEnumerator Flash(Color col, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            flash.color = new Color(col.r, col.g, col.b, col.a * (1f - t / dur));
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        flash.color = Color.clear;
    }

    // ———————— UI（独立 Canvas，压在 HUD 之上） ————————
    void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 全屏闪光
        flash = Img("Flash", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
        flash.raycastTarget = false;

        // 字卡（全屏黑）
        cardPanel = Img("CardPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.96f)).gameObject;
        cardText = Txt("CardText", cardPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 40),
            new Vector2(1400, 400), TextAnchor.MiddleCenter, 40);
        cardHint = Txt("CardHint", cardPanel.transform, new Vector2(0.5f, 0f), new Vector2(0, 60),
            new Vector2(600, 40), TextAnchor.MiddleCenter, 22);
        cardHint.text = "E / 点击 继续";
        cardHint.color = new Color(1f, 1f, 1f, 0.5f);
        cardPanel.SetActive(false);

        // 对话框（下方面板）
        dialogPanel = Img("DialogPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 120),
            new Vector2(1100, 170), new Color(0.06f, 0.06f, 0.09f, 0.95f)).gameObject;
        dialogText = Txt("DialogText", dialogPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 12),
            new Vector2(1040, 120), TextAnchor.MiddleCenter, 30);
        var dh = Txt("DialogHint", dialogPanel.transform, new Vector2(0.5f, 0f), new Vector2(0, 8),
            new Vector2(400, 30), TextAnchor.MiddleCenter, 20);
        dh.text = "E / 点击 继续";
        dh.color = new Color(1f, 1f, 1f, 0.5f);
        dialogPanel.SetActive(false);

        // 肃清进度（顶部中央）
        progressText = Txt("Progress", transform, new Vector2(0.5f, 1f), new Vector2(0, -24),
            new Vector2(700, 44), TextAnchor.MiddleCenter, 30);
        progressText.color = new Color(1f, 0.85f, 0.4f, 1f);
        AddOutline(progressText);
        progressText.gameObject.SetActive(false);

        // 方向箭头（进度条下）
        arrowText = Txt("Arrow", transform, new Vector2(0.5f, 1f), new Vector2(0, -70),
            new Vector2(400, 40), TextAnchor.MiddleCenter, 28);
        arrowText.color = new Color(1f, 0.85f, 0.4f, 1f);
        AddOutline(arrowText);
        arrowText.gameObject.SetActive(false);

        // 双任务（左上,血条下方）
        questText = Txt("Quests", transform, new Vector2(0f, 1f), new Vector2(22, -64),
            new Vector2(400, 80), TextAnchor.UpperLeft, 22);
        questText.text = "任务: 唤醒莉莉娅\n任务: 寻找天使";
        AddOutline(questText);
        questText.gameObject.SetActive(false);

        // 莉莉娅状态（血条右侧小字）
        liliaStatus = Txt("LiliaStatus", transform, new Vector2(0f, 1f), new Vector2(340, -22),
            new Vector2(300, 32), TextAnchor.MiddleLeft, 22);
        AddOutline(liliaStatus);
        liliaStatus.gameObject.SetActive(false);
    }

    Text liliaStatus;

    Image Img(string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color col)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        if (aMin == aMax) { rt.pivot = new Vector2(0.5f, aMin.y); rt.anchoredPosition = pos; rt.sizeDelta = size; }
        else { rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        var img = go.AddComponent<Image>();
        img.sprite = white; img.color = col;
        return img;
    }

    Text Txt(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, TextAnchor align, int fontSize)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(anchor.x, anchor.y);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = fontSize; t.alignment = align; t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void AddOutline(Text t)
    {
        var o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.9f);
        o.effectDistance = new Vector2(2, -2);
    }
}
