using System.Collections.Generic;

/// <summary>
/// 盲盒第 5/6 块：物品定义表（写死）。名称 → 堆叠上限 + 开出组大小 + 描述。
/// 不做物品类继承 / ScriptableObject / 稀有度品质。名称就是 string。
/// </summary>
public static class ItemDatabase
{
    public struct Def { public int stackMax; public int groupSize; public string desc; }

    static readonly Dictionary<string, Def> table = new Dictionary<string, Def>
    {
        { "罐头",   new Def { stackMax = 5,  groupSize = 1, desc = "保质期漫长的午餐肉。末日里的硬通货。" } },
        { "瓶装水", new Def { stackMax = 5,  groupSize = 1, desc = "未开封的饮用水。比黄金更让人安心。" } },
        { "巧克力", new Def { stackMax = 5,  groupSize = 1, desc = "高热量的小奢侈，能提一口气。" } },
        { "绷带",   new Def { stackMax = 5,  groupSize = 1, desc = "处理外伤的基本款。" } },
        { "止痛药", new Def { stackMax = 5,  groupSize = 1, desc = "让你撑过疼痛的小药片。" } },
        { "急救包", new Def { stackMax = 5,  groupSize = 1, desc = "正经的医疗组合，关键时刻救命。" } },
        { "布料",   new Def { stackMax = 5,  groupSize = 1, desc = "撕好的布条，修补或包扎的原料。" } },
        { "胶带",   new Def { stackMax = 5,  groupSize = 1, desc = "万能修补神器，末日工程学的核心。" } },
        { "电池",   new Def { stackMax = 5,  groupSize = 1, desc = "还有电。总会有东西用得上它。" } },
        { "零件",   new Def { stackMax = 5,  groupSize = 1, desc = "螺丝、弹簧和叫不上名字的金属件。" } },
        { "木材",   new Def { stackMax = 5,  groupSize = 5, desc = "一捆木板，加固或生火都用得上。" } },
        { "手枪弹药", new Def { stackMax = 20, groupSize = 5, desc = "9mm，每一发都要省着用。" } },
        { "撬棍",   new Def { stackMax = 1,  groupSize = 1, desc = "能开门，也能开颅。可靠的老朋友。" } },
    };

    public static int StackMax(string name) => table.TryGetValue(name, out var d) ? d.stackMax : 1;
    public static int GroupSize(string name) => table.TryGetValue(name, out var d) ? d.groupSize : 1;
    public static string Description(string name) => table.TryGetValue(name, out var d) ? d.desc : "";
}

/// <summary>一堆物品：名称 + 数量。背包格子与容器内容物共用。</summary>
public class ItemStack
{
    public string name;
    public int count;
    public ItemStack(string name, int count) { this.name = name; this.count = count; }
}
