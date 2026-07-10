using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 闭环v1：背包页。按 I 开/关(Esc 也可关)，打开时 Time.timeScale=0 全局暂停——
/// 与开箱窗口(不暂停)是有意的镜像：开箱冒险，背包喘息。
/// 内容：5 背包格 + 左手槽(v1 禁用) + 右手槽 + 医疗快捷栏 + 信息区 + 情景按钮。
/// 占位纯色+文字，复用 LootWindow 的构 UI 手法；手动点击命中；计时用 unscaled time。
/// </summary>
public class InventoryWindow : MonoBehaviour
{
    Font font;
    Sprite white;

    Inventory inventory;

    GameObject root;
    Text infoText, statusText;
    readonly Text[] bagText = new Text[5];
    readonly Image[] bagBg = new Image[5];
    readonly RectTransform[] bagRect = new RectTransform[5];
    Text rightHandText, medSlotText, leftHandText;
    Image rightHandBg, medSlotBg;
    RectTransform rightHandRect, medSlotRect;
    RectTransform btnARect, btnBRect, closeRect;
    Text btnAText, btnBText;

    enum Sel { None, Bag, RightHand, MedSlot }
    Sel selKind = Sel.None;
    int selIndex = -1;

    public bool IsOpen => root != null && root.activeSelf;

    public void Init(Font font, Sprite white)
    {
        this.font = font;
        this.white = white;
        BuildUI();
        root.SetActive(false);
    }

    public void CloseIfOpen()
    {
        if (IsOpen) Close();
    }

    void AcquireInventory()
    {
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        inventory = pm != null ? pm.GetComponent<Inventory>() : null;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.iKey.wasPressedThisFrame)
        {
            if (IsOpen) Close(); else Open();
            return;
        }
        if (!IsOpen) return;

        if (kb.escapeKey.wasPressedThisFrame) { Close(); return; }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            HandleClick(mouse.position.ReadValue());
    }

    void Open()
    {
        AcquireInventory();
        if (inventory == null) return;
        selKind = Sel.None; selIndex = -1;
        statusText.text = "";
        Time.timeScale = 0f;          // 全局暂停：世界完全静止
        Refresh();
        root.SetActive(true);
    }

    void Close()
    {
        Time.timeScale = 1f;
        root.SetActive(false);
    }

    void HandleClick(Vector2 pos)
    {
        if (Contains(closeRect, pos)) { Close(); return; }
        if (btnARect.gameObject.activeSelf && Contains(btnARect, pos)) { DoAction(btnAText.text); return; }
        if (btnBRect.gameObject.activeSelf && Contains(btnBRect, pos)) { DoAction(btnBText.text); return; }

        for (int i = 0; i < 5; i++)
            if (Contains(bagRect[i], pos))
            {
                if (i < inventory.Slots.Count) { selKind = Sel.Bag; selIndex = i; Refresh(); }
                return;
            }
        if (Contains(rightHandRect, pos)) { if (inventory.RightHand != null) { selKind = Sel.RightHand; Refresh(); } return; }
        if (Contains(medSlotRect, pos))   { if (inventory.MedSlot != null)   { selKind = Sel.MedSlot;   Refresh(); } return; }
    }

    void DoAction(string action)
    {
        statusText.text = "";
        switch (action)
        {
            case "使用":
                if (selKind == Sel.Bag && !inventory.UseAt(selIndex)) statusText.text = "无法使用";
                break;
            case "装备到右手":
                if (selKind == Sel.Bag)
                {
                    if (inventory.RightHand != null) statusText.text = "右手已有武器，先卸下";
                    else if (!inventory.EquipRightHandFrom(selIndex)) statusText.text = "无法装备";
                }
                break;
            case "放入医疗栏":
                if (selKind == Sel.Bag)
                {
                    if (inventory.MedSlot != null) statusText.text = "医疗栏已占用";
                    else if (!inventory.MoveToMedSlotFrom(selIndex)) statusText.text = "只能放医疗品";
                }
                break;
            case "卸下回背包":
                if (!inventory.UnequipRightHand()) statusText.text = "背包已满，无法卸下";
                break;
            case "取回背包":
                if (!inventory.TakeMedSlotBack()) statusText.text = "背包已满";
                break;
        }
        // 操作后选中项可能已消失/移动，回到未选中
        selKind = Sel.None; selIndex = -1;
        Refresh();
    }

    void Refresh()
    {
        var slots = inventory.Slots;
        for (int i = 0; i < 5; i++)
        {
            bool has = i < slots.Count;
            bagText[i].text = has ? (slots[i].count > 1 ? $"{slots[i].name}\nx{slots[i].count}" : slots[i].name) : "";
            bagBg[i].color = CellColor(has, selKind == Sel.Bag && selIndex == i);
        }

        leftHandText.text = "左手\n(禁用)";
        rightHandText.text = inventory.RightHand != null ? $"右手\n{inventory.RightHand}" : "右手\n空";
        rightHandBg.color = CellColor(inventory.RightHand != null, selKind == Sel.RightHand);
        medSlotText.text = inventory.MedSlot != null
            ? $"医疗栏\n{inventory.MedSlot.name}" + (inventory.MedSlot.count > 1 ? $" x{inventory.MedSlot.count}" : "")
            : "医疗栏\n空";
        medSlotBg.color = CellColor(inventory.MedSlot != null, selKind == Sel.MedSlot);

        // 信息 + 情景按钮
        btnARect.gameObject.SetActive(false);
        btnBRect.gameObject.SetActive(false);
        if (selKind == Sel.Bag && selIndex < slots.Count)
        {
            var s = slots[selIndex];
            var def = ItemDatabase.Get(s.name);
            infoText.text = $"{s.name}   x{s.count}\n\n{def.desc}";
            if (def.heal > 0)  ShowBtn(btnARect, btnAText, "使用");
            if (def.weapon)    ShowBtn(btnARect, btnAText, "装备到右手");
            if (def.medical)   ShowBtn(btnBRect, btnBText, "放入医疗栏");
        }
        else if (selKind == Sel.RightHand)
        {
            infoText.text = $"{inventory.RightHand}\n\n{ItemDatabase.Description(inventory.RightHand)}";
            ShowBtn(btnARect, btnAText, "卸下回背包");
        }
        else if (selKind == Sel.MedSlot)
        {
            infoText.text = $"{inventory.MedSlot.name}   x{inventory.MedSlot.count}\n\n致死一击时自动消耗保命";
            ShowBtn(btnARect, btnAText, "取回背包");
        }
        else infoText.text = "点击格子查看物品";
    }

    void ShowBtn(RectTransform rt, Text label, string text)
    {
        rt.gameObject.SetActive(true);
        label.text = text;
    }

    Color CellColor(bool has, bool sel)
        => !has ? new Color(0.15f, 0.15f, 0.18f, 0.55f)
         : sel ? new Color(0.40f, 0.52f, 0.72f, 0.98f)
         : new Color(0.25f, 0.28f, 0.35f, 0.95f);

    bool Contains(RectTransform rt, Vector2 pos)
        => rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, pos, null);

    // ———————— UI 构建（占位纯色，复用 LootWindow 手法） ————————
    void BuildUI()
    {
        root = Panel("Root", transform, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(920, 560),
            new Color(0.08f, 0.08f, 0.10f, 0.94f), new Vector2(0.5f, 0.5f));
        var rootRT = root.GetComponent<RectTransform>();

        TextAt("Title", rootRT, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(880, 44),
            TextAnchor.MiddleCenter, 30).text = "背包（游戏已暂停）";

        // 5 背包格（上排）
        const float cell = 120f, gap = 14f;
        float startX = 40f;
        for (int i = 0; i < 5; i++)
        {
            var c = Panel($"Bag{i}", rootRT, new Vector2(0, 1), new Vector2(startX + i * (cell + gap), -80f),
                new Vector2(cell, cell), new Color(0.25f, 0.28f, 0.35f, 0.95f), new Vector2(0, 1));
            bagRect[i] = c.GetComponent<RectTransform>();
            bagBg[i] = c.GetComponent<Image>();
            bagText[i] = TextAt($"BagTxt{i}", bagRect[i], new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(cell - 8, cell - 8), TextAnchor.MiddleCenter, 21);
        }

        // 装备排（下排）：左手(禁用) / 右手 / 医疗栏
        float eqY = -240f;
        var lh = Panel("LeftHand", rootRT, new Vector2(0, 1), new Vector2(40f, eqY),
            new Vector2(cell, cell), new Color(0.13f, 0.13f, 0.16f, 0.8f), new Vector2(0, 1));
        leftHandText = TextAt("LeftTxt", lh.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(cell - 8, cell - 8), TextAnchor.MiddleCenter, 21);
        leftHandText.color = new Color(1f, 1f, 1f, 0.35f);

        var rh = Panel("RightHand", rootRT, new Vector2(0, 1), new Vector2(40f + cell + gap, eqY),
            new Vector2(cell, cell), new Color(0.25f, 0.28f, 0.35f, 0.95f), new Vector2(0, 1));
        rightHandRect = rh.GetComponent<RectTransform>();
        rightHandBg = rh.GetComponent<Image>();
        rightHandText = TextAt("RightTxt", rightHandRect, new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(cell - 8, cell - 8), TextAnchor.MiddleCenter, 21);

        var ms = Panel("MedSlot", rootRT, new Vector2(0, 1), new Vector2(40f + 2 * (cell + gap), eqY),
            new Vector2(cell, cell), new Color(0.30f, 0.22f, 0.24f, 0.95f), new Vector2(0, 1));
        medSlotRect = ms.GetComponent<RectTransform>();
        medSlotBg = ms.GetComponent<Image>();
        medSlotText = TextAt("MedTxt", medSlotRect, new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(cell - 8, cell - 8), TextAnchor.MiddleCenter, 21);

        // 信息区（右侧）
        var info = Panel("Info", rootRT, new Vector2(1, 1), new Vector2(-30f, -80f),
            new Vector2(210, 280), new Color(0.05f, 0.05f, 0.07f, 0.9f), new Vector2(1, 1));
        infoText = TextAt("InfoTxt", info.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(0, -12),
            new Vector2(186, 260), TextAnchor.UpperLeft, 20);

        // 状态提示行
        statusText = TextAt("Status", rootRT, new Vector2(0.5f, 0), new Vector2(0, 96), new Vector2(860, 34),
            TextAnchor.MiddleCenter, 22);
        statusText.color = new Color(1f, 0.75f, 0.4f, 1f);

        // 情景按钮 ×2 + 关闭
        btnARect = Button("BtnA", rootRT, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 20), new Vector2(210, 64), out btnAText);
        btnBRect = Button("BtnB", rootRT, new Vector2(0, 0), new Vector2(0, 0), new Vector2(266, 20), new Vector2(210, 64), out btnBText);
        Text closeTxt;
        closeRect = Button("BtnClose", rootRT, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 20), new Vector2(160, 64), out closeTxt);
        closeTxt.text = "关闭(I)";
    }

    GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color col, Vector2 pivot)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.sprite = white; img.color = col;
        return go;
    }

    Text TextAt(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, TextAnchor align, int fontSize)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, anchor.y);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var txt = go.AddComponent<Text>();
        txt.font = font; txt.fontSize = fontSize; txt.alignment = align; txt.color = Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    RectTransform Button(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, out Text label)
    {
        var go = Panel(name, parent, anchor, pos, size, new Color(0.3f, 0.35f, 0.45f, 0.98f), pivot);
        var rt = go.GetComponent<RectTransform>();
        label = TextAt("Label", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 8, size.y - 8),
            TextAnchor.MiddleCenter, 24);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        return rt;
    }
}
