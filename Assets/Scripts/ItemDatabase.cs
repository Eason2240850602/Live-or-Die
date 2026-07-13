using System.Collections.Generic;

/// <summary>
/// 物品定义表（写死）。闭环v1 扩展：恢复量(heal>0=可使用消耗品)、医疗品(可入快捷栏)、武器(可装备+伤害)。
/// 不做物品类继承 / ScriptableObject / 稀有度。名称就是 string。
/// </summary>
public static class ItemDatabase
{
    public struct Def
    {
        public int stackMax;
        public int groupSize;
        public string desc;
        public int heal;        // >0 = 可在背包页使用，回复量（上限100截断）
        public bool medical;    // 可放入医疗快捷栏（绷带/急救包）
        public bool weapon;     // 可装备到手槽
        public int damage;      // 武器伤害（装备后替代空手25）
        public float noiseRadius;   // 挥击声音半径（感知v1；空手默认4）
    }

    static readonly Dictionary<string, Def> table = new Dictionary<string, Def>
    {
        { "罐头",   new Def { stackMax = 5,  groupSize = 1, heal = 15, desc = "保质期漫长的午餐肉。末日里的硬通货。" } },
        { "瓶装水", new Def { stackMax = 5,  groupSize = 1, heal = 10, desc = "未开封的饮用水。比黄金更让人安心。" } },
        { "巧克力", new Def { stackMax = 5,  groupSize = 1, desc = "高热量的小奢侈，能提一口气。" } },
        { "绷带",   new Def { stackMax = 5,  groupSize = 1, heal = 25, medical = true, desc = "处理外伤的基本款。" } },
        { "止痛药", new Def { stackMax = 5,  groupSize = 1, desc = "让你撑过疼痛的小药片。" } },
        { "急救包", new Def { stackMax = 5,  groupSize = 1, heal = 100, medical = true, desc = "正经的医疗组合，关键时刻救命。" } },
        { "布料",   new Def { stackMax = 5,  groupSize = 1, desc = "撕好的布条，修补或包扎的原料。" } },
        { "胶带",   new Def { stackMax = 5,  groupSize = 1, desc = "万能修补神器，末日工程学的核心。" } },
        { "电池",   new Def { stackMax = 5,  groupSize = 1, desc = "还有电。总会有东西用得上它。" } },
        { "零件",   new Def { stackMax = 5,  groupSize = 1, desc = "螺丝、弹簧和叫不上名字的金属件。" } },
        { "木材",   new Def { stackMax = 5,  groupSize = 5, desc = "一捆木板，加固或生火都用得上。" } },
        { "手枪弹药", new Def { stackMax = 20, groupSize = 5, desc = "9mm，每一发都要省着用。" } },
        { "撬棍",   new Def { stackMax = 1,  groupSize = 1, weapon = true, damage = 50, noiseRadius = 6f, desc = "能开门，也能开颅。可靠的老朋友。" } },
    };

    public static Def Get(string name) { table.TryGetValue(name, out var d); return d; }
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
