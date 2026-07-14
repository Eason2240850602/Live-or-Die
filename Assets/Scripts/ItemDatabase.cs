using System.Collections.Generic;

/// <summary>
/// 物品定义表（写死）。物品表 v2：现役 + 预留全部先写定义——预留件不进任何掉落表，
/// 各系统块上线时零改动直接激活。堆叠：材料/耗材 5(标注除外)、弹药 20、武器/工具/可拆解/任务 1。
/// </summary>
public static class ItemDatabase
{
    public struct Def
    {
        public int stackMax;
        public int groupSize;
        public string desc;
        public int heal;        // >0 = 可在背包页使用，回复量（上限截断）
        public bool medical;    // 可放入医疗快捷栏
        public bool weapon;     // 可装备到手槽
        public int damage;      // 武器伤害
        public float noiseRadius;   // 挥击声音半径（空手默认4）
    }

    static readonly Dictionary<string, Def> table = new Dictionary<string, Def>
    {
        // —— 一、食物（现役） ——
        { "罐头",     new Def { stackMax = 5, groupSize = 1, heal = 15, desc = "保质期漫长的午餐肉。末日里的硬通货。" } },
        { "瓶装水",   new Def { stackMax = 5, groupSize = 1, heal = 10, desc = "未开封的饮用水。比黄金更让人安心。" } },
        { "巧克力",   new Def { stackMax = 5, groupSize = 1, heal = 8,  desc = "高热量的小奢侈，能提一口气。" } },
        { "军用口粮", new Def { stackMax = 3, groupSize = 1, heal = 30, desc = "压缩饼干加脱水肉块。难吃，但顶饿。" } },
        // 预留 · 7.24 疲劳
        { "咖啡豆",   new Def { stackMax = 5, groupSize = 1, desc = "烘焙过的豆子。世界塌了，瘾还在。" } },

        // —— 二、医疗（现役） ——
        { "绷带",     new Def { stackMax = 5, groupSize = 1, heal = 25,  medical = true, desc = "处理外伤的基本款。" } },
        { "急救包",   new Def { stackMax = 3, groupSize = 1, heal = 100, medical = true, desc = "正经的医疗组合，关键时刻救命。" } },
        // 预留 · 7.2 伤病 / 7.22 制造
        { "止痛药",   new Def { stackMax = 5, groupSize = 1, desc = "让你撑过疼痛的小药片。" } },
        { "消毒酒精", new Def { stackMax = 5, groupSize = 1, desc = "刺鼻但可靠。伤口和器械都用得上。" } },
        { "缝合包",   new Def { stackMax = 2, groupSize = 1, desc = "针线和止血钳。深可见骨的伤才用它。" } },

        // —— 三、材料（现役） ——
        { "木材",   new Def { stackMax = 5, groupSize = 5, desc = "一捆木板，加固或生火都用得上。" } },
        { "布料",   new Def { stackMax = 5, groupSize = 1, desc = "撕好的布条，修补或包扎的原料。" } },
        { "胶带",   new Def { stackMax = 5, groupSize = 1, desc = "万能修补神器，末日工程学的核心。" } },
        { "零件",   new Def { stackMax = 5, groupSize = 1, desc = "螺丝、弹簧和叫不上名字的金属件。" } },
        { "钉子",   new Def { stackMax = 5, groupSize = 1, desc = "一把锈钉子。钉住木板，也钉住希望。" } },
        { "金属片", new Def { stackMax = 5, groupSize = 1, desc = "边缘锋利的铁皮，武器台的好原料。" } },
        { "电线",   new Def { stackMax = 5, groupSize = 1, desc = "几段还带绝缘皮的铜线。" } },
        { "电池",   new Def { stackMax = 5, groupSize = 1, desc = "还有电。总会有东西用得上它。" } },
        // 预留 · 枪械块
        { "火药",   new Def { stackMax = 5, groupSize = 1, desc = "干燥的黑色粉末。离火远点。" } },

        // —— 四、武器（现役：撬棍/弹药） ——
        { "撬棍",     new Def { stackMax = 1, groupSize = 1, weapon = true, damage = 50, noiseRadius = 6f, desc = "能开门，也能开颅。可靠的老朋友。" } },
        { "手枪弹药", new Def { stackMax = 20, groupSize = 5, desc = "9mm，每一发都要省着用。" } },
        // 预留 · 战斗扩展块 / 7.16 / 序章（数值待各自块最终定）
        { "菜刀",   new Def { stackMax = 1, groupSize = 1, weapon = true, damage = 35, noiseRadius = 4f, desc = "厨房里最锋利的东西。挥得快，声音小。" } },
        { "匕首",   new Def { stackMax = 1, groupSize = 1, weapon = true, damage = 30, noiseRadius = 3f, desc = "贴身的最后手段，也是最安静的开场。" } },
        { "三叉戟", new Def { stackMax = 1, groupSize = 1, weapon = true, desc = "鱼叉改的长兵。捅进去，就别想立刻拔出来。" } },
        { "大锤",   new Def { stackMax = 1, groupSize = 1, weapon = true, desc = "抡圆了能砸塌一面墙。前提是你抡得动。" } },
        { "手枪",   new Def { stackMax = 1, groupSize = 1, weapon = true, noiseRadius = 15f, desc = "最后的发言权。整栋楼都会听见。" } },

        // —— 五、工具（预留 · 7.15 / 时段 / 7.21） ——
        { "撬锁器", new Def { stackMax = 1, groupSize = 1, desc = "细钩与扭杆。上锁的容器挡不住它。" } },
        { "断线钳", new Def { stackMax = 1, groupSize = 1, desc = "剪断铁链和挂锁的粗家伙。" } },
        { "手电筒", new Def { stackMax = 1, groupSize = 1, desc = "一束光。黑暗里它就是命。" } },
        { "空瓶",   new Def { stackMax = 1, groupSize = 1, desc = "扔出去会响。有时候响比刀有用。" } },

        // —— 六、可拆解（预留 · 枢纽拆解台;占格但回家血赚） ——
        { "木箱",       new Def { stackMax = 1, groupSize = 1, desc = "钉死的板条箱。拆开全是好木料。" } },
        { "废旧收音机", new Def { stackMax = 1, groupSize = 1, desc = "不响了，但里面的零件还活着。" } },
        { "旧工具箱",   new Def { stackMax = 1, groupSize = 1, desc = "生锈的铁盒，里面比外面值钱。" } },

        // —— 七、任务/特殊（预留 · 序章 / 7.25 / 7.26） ——
        { "元素资料", new Def { stackMax = 1, groupSize = 1, desc = "保护网公司的加密档案。一切的答案在里面。" } },
        { "兑换券",   new Def { stackMax = 1, groupSize = 1, desc = "流浪商人认的硬通货。别问是谁印的。" } },
        { "蓝图",     new Def { stackMax = 1, groupSize = 1, desc = "手绘的制造图纸。看得懂的人才配拥有。" } },
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
