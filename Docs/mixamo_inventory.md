# Mixamo FBX 盘点与分诊报告

> 段 0 交付物。范围：`Assets/ExternalAssets/Mixamo/` 全量 **48 个 FBX**。
> **只报告，不删除**（删除权在 Zy）。生成日期：2026-07-22（含 Idle/Running 补件后复盘）。

---

## ① 本单必用（11 槽位选定）

### 玩家 7 槽

| 槽位 | 选定文件 | 理由 |
|---|---|---|
| Idle | `Idle` | ✅ 已补件（1892KB）。通用站立待机，无持械/蹲/躺前缀污染 |
| Walking | `Walking` | 唯一通用步行，无武器姿态污染 |
| Running | `Running` | ✅ 已补件（1765KB）。通用跑步；`Great Sword Run`/`Rifle Run` 带持械姿态归 ②，`Running Turn 180` 是转身过渡归 ② |
| Crouched Walking | `Crouched Walking` | 名称与用途完全对应，唯一候选 |
| Crouch Idle | `Crouching Idle` | 与 `Idle Crouching` 二选一；取前者（504KB 单动作，后者 1857KB 疑似带 T-pose 长尾） |
| 近战挥击 | `Standing 右横劈Melee Attack Horizontal` | 工单指定"优先横劈"；备选 `上劈Downward` / `360 High` 归 ③ |
| Death | `Death From The Back` | 工单指定"优先向后倒类"；`Walking To Dying` 是行走中倒地，归 ② |

### 丧尸 4 槽

| 槽位 | 选定文件 | 理由 |
|---|---|---|
| zombie idle | `zombie idle` | 唯一候选 |
| zombie walk | `Zombie slow Walking` | 工单指定"优先 slow"，贴巡逻速度 1.2 的慢速设定 |
| zombie attack | `zombie attack` | 唯一明确攻击动作（`biting` 系列归 ②，属特殊咬击） |
| zombie death | `zombie death` | 与 `zombie dying` 二选一；取 `death`（完成态倒地，`dying` 偏过程） |

### ✅ 缺口已闭合

初次盘点时缺 `Idle` / `Running`，Zy 于 2026-07-22 11:03/11:05 补齐。**11 槽位全部有主，段 2 无阻塞。**

---

## ② 库存备用（留名不接入）

| 文件 | 未来用途 |
|---|---|
| `zombie crawl`、`Zombie Running Crawl` | 爬尸变体（已有 Grab 型爬尸，动画现成） |
| `zombie scream`、`zombie neck bite`、`zombie biting` | 特殊感染者 / 处决被杀动作 |
| `zombie run` | 快速丧尸（跑尸 4.3 速现成动画） |
| `Rifle Idle`、`Rifle Run`、`Rifle Crouch Walk To Idle`、`Pistol Walk`、`holding Weapon Walk Forward` | 枪械动画升级（左轮已上线，持械姿态迟早要） |
| `Hit Reaction`、`Walking Hit Reaction` | 受击反馈（现为闪白+击退，动画版备料） |
| `Crouch To Stand`、`Stop Walking`、`Running Turn 180`、`Push Up To Idle`、`Situp To Idle` | 过渡动作，动画打磨期 |
| `Fallen Idle`、`Kneeling Idle`、`Hanging Idle`、`Male Crouch Pose` | 演出/剧情姿态（莉莉娅躺倒、序章醒来） |
| `女Female Crouch Pose` | 同伴角色（女性姿态） |
| `Falling Back Death被扑倒` | 被扑倒专用死亡（抓取系统 7.29 上线时） |
| `大锤run Jump Attack` | 大锤武器块（7.16） |
| `Great Sword Run`、`Standing Melee Attack 360 High`、`Standing 上劈Melee Attack Downward` | 武士刀/其他近战变体 |
| **`X Bot`（5MB，带皮模型）** | **段 2 玩家换模候选**，本单不用 |

---

## ③ 重复 / 冗余（只报告不删除）

| 冗余件 | 保留件 | 说明 |
|---|---|---|
| `Rifle Crouch Walk To Idle (1)` | `Rifle Crouch Walk To Idle` | 同尺寸 578KB，纯重复下载 |
| `zombie biting (2)` | `zombie biting` | 512KB vs 859KB，疑似同动作不同长度 |
| `zombie walk`(575KB) / `zombie Walking`(1816KB) | `Zombie slow Walking` | 三个走路系，本单只用 slow |
| `zombie dying` | `zombie death` | 死亡系二选一 |
| `Idle Crouching`(1857KB) | `Crouching Idle`(504KB) | 蹲待机二选一 |
| `Standing 上劈…Downward`、`…360 High` | `…Horizontal`（横劈） | 近战三选一，另两个转 ② |

> 冗余合计约 6-8 件，占用不大；建议保留待段 2 对比手感后再清。

---

## ④ 异常件

**盘点阶段（文件层面）未见异常**：46 个 FBX 大小均在 262KB–5MB 合理区间，无 0 字节、无重复扩展名。

⚠️ 注意：Humanoid Avatar 配置与红骨检查需在 Unity 导入设置中逐个确认，**段 2 建状态机时才逐个过**。本段未做该检查（工单段 0 只要求文件级分诊）。已知风险点：
- 中文命名文件（`Standing 上劈…`、`女Female…`、`大锤run…`、`Falling Back Death被扑倒`）— Unity 支持，但脚本引用时注意编码
- `X Bot` 是**带皮模型**而非纯动作，导入类型需设 Humanoid + 保留 Mesh

---

## 附：段 1 用的丧尸模型资产（非 Mixamo）

| 资产 | 位置 | 状态 |
|---|---|---|
| **ZombieMale_AAB**（工单指定，Asset Store 336744） | `Assets/ZombieMale_AAB/` | ✅ 已导入，含 **现成 URP prefab** `Prefabs/URP/ZombieMale_AAB_URP.prefab` |
| ShirtlessZombie FREE（额外，工单未提） | `Assets/NewPunch/ShirtlessZombieFree/` | 已导入，本单**不使用**（红线：不引入清单外资产） |
