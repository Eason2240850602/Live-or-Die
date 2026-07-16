using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 格子制背包 + 闭环v1 装备架构。
/// 5 格背包；双手槽（v1 左手禁用、只用右手）；医疗快捷栏（只收医疗品，容纳一个堆）。
/// 无任何自动装备/自动使用——一切经背包页手动操作；斩杀线保命是唯一例外（玩家提前配置）。
/// </summary>
public class Inventory : MonoBehaviour
{
    [Tooltip("背包格数上限")]
    public int capacity = 6;

    readonly List<ItemStack> slots = new List<ItemStack>();

    // —— 装备（按双手架构；左手常态禁用，序章临时解锁）——
    public string RightHand { get; private set; }          // null = 空手
    public string LeftHand { get; private set; }           // 常态 null；序章=手枪
    public ItemStack MedSlot { get; private set; }         // 医疗快捷栏（一个堆）

    /// <summary>序章装备锁：双武器不可卸下不可丢。</summary>
    public bool LoadoutLocked { get; private set; }

    /// <summary>序章：发放双武器并上锁（右手武士刀/左手手枪）。</summary>
    public void PrologueEquip(string right, string left)
    {
        RightHand = right;
        LeftHand = left;
        LoadoutLocked = true;
    }

    /// <summary>剥夺：双武器消失，左手槽回禁用。</summary>
    public void PrologueClear()
    {
        RightHand = null;
        LeftHand = null;
        LoadoutLocked = false;
    }

    public int Count => slots.Count;
    public bool IsFull => slots.Count >= capacity;
    public IReadOnlyList<ItemStack> Slots => slots;

    /// <summary>当前攻击伤害：空手25，持武器用武器伤害。</summary>
    public int AttackDamage => RightHand == null ? 25 : ItemDatabase.Get(RightHand).damage;

    /// <summary>
    /// UX1 获取物品（开箱拿取路径）：武器且右手空 → 直接上手（背包满也生效，修"满包捡不了撬棍"死局）；
    /// 右手已占 → 走背包；其余物品照旧 AddItem。已装备武器永不被自动替换。返回实际收下数量。
    /// </summary>
    public int AcquireItem(string name, int qty)
    {
        int taken = 0;
        if (qty > 0 && ItemDatabase.Get(name).weapon && RightHand == null)
        {
            RightHand = name;
            qty--; taken++;
        }
        return taken + AddItem(name, qty);
    }

    /// <summary>装入 qty 个 name；返回实际装入数量。</summary>
    public int AddItem(string name, int qty)
    {
        int stackMax = ItemDatabase.StackMax(name);
        int added = 0;

        foreach (var s in slots)
        {
            if (qty <= 0) break;
            if (s.name == name && s.count < stackMax)
            {
                int put = Mathf.Min(stackMax - s.count, qty);
                s.count += put; qty -= put; added += put;
            }
        }
        while (qty > 0 && slots.Count < capacity)
        {
            int put = Mathf.Min(stackMax, qty);
            slots.Add(new ItemStack(name, put));
            qty -= put; added += put;
        }
        return added;
    }

    /// <summary>背包页"使用"：消耗品回血（上限100截断），堆减1。返回是否成功。</summary>
    public bool UseAt(int index)
    {
        if (index < 0 || index >= slots.Count) return false;
        var s = slots[index];
        var def = ItemDatabase.Get(s.name);
        if (def.heal <= 0) return false;

        var h = GetComponent<Health>();
        if (h != null) h.HealPlayer(def.heal);
        ConsumeAt(index);
        return true;
    }

    /// <summary>背包页"装备到右手"：武器从格子移入槽位（不占格）。右手已有则先要求卸下。</summary>
    public bool EquipRightHandFrom(int index)
    {
        if (index < 0 || index >= slots.Count) return false;
        var s = slots[index];
        if (!ItemDatabase.Get(s.name).weapon || RightHand != null) return false;
        RightHand = s.name;
        ConsumeAt(index);
        return true;
    }

    /// <summary>背包页"卸下回背包"：需要能放下，背包满则拒绝；序章锁定期间不可卸。</summary>
    public bool UnequipRightHand()
    {
        if (RightHand == null || LoadoutLocked) return false;
        if (AddItem(RightHand, 1) < 1) return false;   // 放不下（武器不可叠=需空格）
        RightHand = null;
        return true;
    }

    /// <summary>背包页"放入医疗栏"：只收医疗品，快捷栏须为空，整堆移入。</summary>
    public bool MoveToMedSlotFrom(int index)
    {
        if (index < 0 || index >= slots.Count || MedSlot != null) return false;
        var s = slots[index];
        if (!ItemDatabase.Get(s.name).medical) return false;
        MedSlot = s;
        slots.RemoveAt(index);
        return true;
    }

    /// <summary>医疗栏"取回背包"。</summary>
    public bool TakeMedSlotBack()
    {
        if (MedSlot == null) return false;
        int put = AddItem(MedSlot.name, MedSlot.count);
        if (put <= 0) return false;
        MedSlot.count -= put;
        if (MedSlot.count <= 0) MedSlot = null;
        return true;
    }

    /// <summary>斩杀线保命（由 Health 调用）：快捷栏非空 → 消耗1个，给出恢复值与名称。</summary>
    public bool TryKillLineSave(out int restoreTo, out string itemName)
    {
        restoreTo = 0; itemName = null;
        if (MedSlot == null) return false;
        itemName = MedSlot.name;
        restoreTo = ItemDatabase.Get(MedSlot.name).heal;
        MedSlot.count--;
        if (MedSlot.count <= 0) MedSlot = null;
        return true;
    }

    void ConsumeAt(int index)
    {
        var s = slots[index];
        s.count--;
        if (s.count <= 0) slots.RemoveAt(index);
    }

    /// <summary>Console 格子视图。</summary>
    public string GridView()
    {
        var cells = new List<string>();
        foreach (var s in slots) cells.Add(s.count > 1 ? $"{s.name}x{s.count}" : s.name);
        for (int i = slots.Count; i < capacity; i++) cells.Add("-");
        return $"背包[{slots.Count}/{capacity}]: " + string.Join(" | ", cells);
    }
}
