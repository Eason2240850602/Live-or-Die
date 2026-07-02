using System.Collections.Generic;

/// <summary>
/// 盲盒第 5 块：物品定义表（写死）。名称 → 堆叠上限 + 开出时组大小。
/// 不做物品类继承 / ScriptableObject / 稀有度品质。名称就是 string。
/// </summary>
public static class ItemDatabase
{
    public struct Def { public int stackMax; public int groupSize; }

    static readonly Dictionary<string, Def> table = new Dictionary<string, Def>
    {
        // 材料 / 耗材：上限 5，开出 1
        { "罐头",   new Def { stackMax = 5, groupSize = 1 } },
        { "瓶装水", new Def { stackMax = 5, groupSize = 1 } },
        { "巧克力", new Def { stackMax = 5, groupSize = 1 } },
        { "绷带",   new Def { stackMax = 5, groupSize = 1 } },
        { "止痛药", new Def { stackMax = 5, groupSize = 1 } },
        { "急救包", new Def { stackMax = 5, groupSize = 1 } },
        { "布料",   new Def { stackMax = 5, groupSize = 1 } },
        { "胶带",   new Def { stackMax = 5, groupSize = 1 } },
        { "电池",   new Def { stackMax = 5, groupSize = 1 } },
        { "零件",   new Def { stackMax = 5, groupSize = 1 } },
        // 木材：上限 5，开出即 x5 一堆
        { "木材",   new Def { stackMax = 5, groupSize = 5 } },
        // 弹药：上限 20，开出即 x5 一组
        { "手枪弹药", new Def { stackMax = 20, groupSize = 5 } },
        // 武器：不可叠
        { "撬棍",   new Def { stackMax = 1, groupSize = 1 } },
    };

    public static int StackMax(string name) => table.TryGetValue(name, out var d) ? d.stackMax : 1;
    public static int GroupSize(string name) => table.TryGetValue(name, out var d) ? d.groupSize : 1;
}

/// <summary>一堆物品：名称 + 数量。背包格子与容器内容物共用。</summary>
public class ItemStack
{
    public string name;
    public int count;
    public ItemStack(string name, int count) { this.name = name; this.count = count; }
}
