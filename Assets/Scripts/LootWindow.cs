using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 盲盒第 6 块：九宫格开箱窗口（uGUI 运行时生成，纯色面板+文字，零图标零美术）。
/// 由 HudController 创建并 Init。点击用新 Input System 手动命中检测（不建 EventSystem）。
/// 游戏不暂停；受击 / 离开容器 searchRange / Esc / 关闭按钮 → 关窗，未拿内容原样留容器。
/// </summary>
public class LootWindow : MonoBehaviour
{
    Font font;
    Sprite white;

    Pickup container;
    Transform player;
    Health playerHealth;
    float healthAtOpen;
    int selected = -1;

    GameObject root;
    Text titleText;
    Text infoText;
    readonly Text[] cellText = new Text[9];
    readonly Image[] cellBg = new Image[9];
    readonly RectTransform[] cellRect = new RectTransform[9];
    RectTransform takeBtn, takeAllBtn, closeBtn;

    public bool IsOpen => root != null && root.activeSelf;

    public void Init(Font font, Sprite white)
    {
        this.font = font;
        this.white = white;
        BuildUI();
        root.SetActive(false);
    }

    public void Open(Pickup c)
    {
        container = c;
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        player = pm != null ? pm.transform : null;
        playerHealth = pm != null ? pm.GetComponent<Health>() : null;
        healthAtOpen = playerHealth != null ? playerHealth.Current : float.MaxValue;
        selected = -1;
        titleText.text = c.ContainerName;
        Refresh();
        root.SetActive(true);
    }

    public void Close()
    {
        container = null;
        if (root != null) root.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen) return;

        if (container == null) { Close(); return; }

        // 自动关窗：离开范围 / 受击
        if (player == null
            || Vector3.Distance(container.transform.position, player.position) > container.SearchRange
            || (playerHealth != null && playerHealth.Current < healthAtOpen))
        {
            Close();
            return;
        }

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) { Close(); return; }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            HandleClick(mouse.position.ReadValue());
    }

    void HandleClick(Vector2 screenPos)
    {
        if (Contains(takeBtn, screenPos)) { TakeSelected(); return; }
        if (Contains(takeAllBtn, screenPos)) { container.TakeAll(); AfterTake(); return; }
        if (Contains(closeBtn, screenPos)) { Close(); return; }

        for (int i = 0; i < 9; i++)
        {
            if (Contains(cellRect[i], screenPos))
            {
                if (i < container.Contents.Count) { selected = i; RefreshInfo(); }  // 空格无反应
                return;
            }
        }
    }

    void TakeSelected()
    {
        if (selected < 0 || selected >= container.Contents.Count) return;
        container.TakeStackAt(selected);
        AfterTake();
    }

    void AfterTake()
    {
        if (container == null || container.Contents.Count == 0) { Close(); return; }
        if (selected >= container.Contents.Count) selected = -1;
        Refresh();
    }

    void Refresh()
    {
        var contents = container.Contents;
        for (int i = 0; i < 9; i++)
        {
            bool has = i < contents.Count;
            cellText[i].text = has ? (contents[i].count > 1 ? $"{contents[i].name}\nx{contents[i].count}" : contents[i].name) : "";
        }
        RefreshInfo();
    }

    void RefreshInfo()
    {
        var contents = container.Contents;
        for (int i = 0; i < 9; i++)
        {
            bool has = i < contents.Count;
            bool sel = has && i == selected;
            cellBg[i].color = !has ? new Color(0.15f, 0.15f, 0.18f, 0.55f)
                : sel ? new Color(0.40f, 0.52f, 0.72f, 0.98f)
                : new Color(0.25f, 0.28f, 0.35f, 0.95f);
        }

        if (selected >= 0 && selected < contents.Count)
        {
            var s = contents[selected];
            infoText.text = $"{s.name}   x{s.count}\n\n{ItemDatabase.Description(s.name)}";
        }
        else infoText.text = "点击格子查看物品";
    }

    bool Contains(RectTransform rt, Vector2 screenPos)
        => rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null);

    // ———————————————— UI 构建 ————————————————
    void BuildUI()
    {
        root = MakePanel("Root", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -40), new Vector2(820, 500), new Color(0.08f, 0.08f, 0.10f, 0.92f));
        var rootRT = root.GetComponent<RectTransform>();

        titleText = MakeText("Title", rootRT, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -16), new Vector2(780, 44), TextAnchor.MiddleCenter, 30);

        // 3x3 九宫格（左上区域）
        const float cell = 120f, gap = 12f, left = 26f, top = 74f;
        for (int i = 0; i < 9; i++)
        {
            int r = i / 3, c = i % 3;
            var cellGO = MakePanel($"Cell{i}", rootRT, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(left + c * (cell + gap), -(top + r * (cell + gap))),
                new Vector2(cell, cell), new Color(0.25f, 0.28f, 0.35f, 0.95f));
            var crt = cellGO.GetComponent<RectTransform>();
            crt.pivot = new Vector2(0, 1);
            cellRect[i] = crt;
            cellBg[i] = cellGO.GetComponent<Image>();
            cellText[i] = MakeText($"CellTxt{i}", crt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(cell - 8, cell - 8), TextAnchor.MiddleCenter, 22);
        }

        // 信息框（右侧）
        var infoPanel = MakePanel("Info", rootRT, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-24, -74), new Vector2(300, 320), new Color(0.05f, 0.05f, 0.07f, 0.9f));
        var irt = infoPanel.GetComponent<RectTransform>();
        irt.pivot = new Vector2(1, 1);
        infoText = MakeText("InfoTxt", irt, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -14), new Vector2(272, 300), TextAnchor.UpperLeft, 22);

        // 底部按钮：一排，尺寸与文字匹配（高约为九宫格单格的 65%）
        const float btnH = 78f;
        takeBtn    = MakeButton("拿取",     rootRT, new Vector2(0, 0), new Vector2(0, 0), new Vector2(26, 20),   new Vector2(130, btnH));
        takeAllBtn = MakeButton("全部拿取", rootRT, new Vector2(0, 0), new Vector2(0, 0), new Vector2(172, 20),  new Vector2(190, btnH));
        closeBtn   = MakeButton("关闭",     rootRT, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-26, 20),  new Vector2(130, btnH));
    }

    GameObject MakePanel(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color col)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.sprite = white; img.color = col;
        return go;
    }

    Text MakeText(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, TextAnchor align, int fontSize)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var txt = go.AddComponent<Text>();
        txt.font = font; txt.fontSize = fontSize; txt.alignment = align; txt.color = Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    RectTransform MakeButton(string label, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = MakePanel("Btn_" + label, parent, anchor, anchor, pos, size, new Color(0.3f, 0.35f, 0.45f, 0.98f));
        var rt = go.GetComponent<RectTransform>();
        rt.pivot = pivot;                     // 先定好锚点/轴心再放文字，避免文字错位
        var txt = MakeText("Label", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(size.x - 8, size.y - 8), TextAnchor.MiddleCenter, 26);
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;   // 中文不因换行被裁掉
        return rt;
    }
}
