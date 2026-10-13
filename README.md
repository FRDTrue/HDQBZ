# 海盗抢宝藏联机版（HDQBZ）

本项目基于开源复刻项目 Mutiny X 改造而来：Mutiny X 使用 Unity 复刻 Nitrome Flash 游戏《Mutiny》，玩家在二维海盗战场上轮流选择角色、调整站位并使用武器，目标是让对方队伍全员出局。原版以 550 × 400 的 Flash 舞台、25 FPS 运行，单人选关覆盖第 1–15 关，双人选关覆盖第 16–33 关。

在保留 Mutiny X 全部单机玩法复刻的基础上，本项目的改动重点是：**局域网联机对战**（Mirror 实现，一台设备控制一支队伍）、**中文/英文双语界面**、**手柄与移动端触控输入**、**联机大厅规则开关**（快速战斗、先手规则、允许作弊）以及一套可从编辑器菜单反复执行的**验证体系**。

## 玩法与复刻内容

- **关卡与世界**：解析原版关卡 XML，构建 33 个编号关卡的地形、碰撞、角色、物件和水域；战场中会出现补充武器的空投宝箱。
- **回合与战斗**：实现选人、抛掷移动、武器操作、行动结算、伤害、击退、落水和胜负判定。模拟使用与原版时序对应的固定 1/50 秒步长（50 Hz）。
- **武器**：实现樱桃炸弹、巨石、炸药、八片币、朗姆酒瓶、香蕉、降落伞炸弹、木箱、火药桶、海鸥、地雷、大炮（含炮弹）、船锚、巫毒娃娃、海啸共 15 种武器，以及空投宝箱与拾取。
- **对手 AI**：为角色移动和武器生成候选（原版同款的 50 次抛掷采样），预测落点并比较收益，再通过正式行动入口执行；大炮、船锚、火药桶等复杂武器有对应的专用决策路径。
- **界面与反馈**：重建主菜单、单人和双人选关、战斗 HUD、结果弹窗、角色动画、镜头、音效和音乐。双人模式保留双方胜局计数；单人进度（PlayerPrefs）与双人对战分开。
- **本地化（本项目新增）**：基于 Unity Localization 的中文（简体）/英文双语言表，文案源为 `Assets/Mutiny/Localization/Mutiny.tsv`，运行时经 `MutinyLocalization` 查询。

## 联机对战（本项目新增）

- **传输与同步**：使用 Mirror（TelepathyTransport，TCP 7777）做命令级 lockstep 同步；本地操作以命令形式提交，对端按同一顺序重放，保证两端一致。回合所有权按队伍门控（`IsLocalTeam`）：房主执 1 队、客户端执 2 队，一台机器只控制一支队伍，对手回合的菜单与输入被屏蔽。
- **局域网房间**：`MutinyLanDiscovery` 基于 UDP 广播（端口 47777）实现建房、房间列表与一键加入；大厅实时显示「玩家已加入」状态。
- **大厅规则开关（房主专属）**：
  - *快速战斗*：空投宝箱下落提速且镜头不再跟随，缩短单局节奏；
  - *先手规则*：随机 / 1P 先手 / 2P 先手三态循环，随对局种子同步且不扰动随机序列；
  - *允许作弊*：默认关闭，开启后联机对局内才显示 GM 面板。
- **断线与重开**：对端断线或主动退出会完整清理会话（停服、释放端口），可直接再次建房/加入；跨队命令在收包处校验丢弃，防止越权操作。

## 输入

- 桌面端使用鼠标选择角色、拖动调整方向与力度，松开提交。
- 支持**手柄**完整操作（十字键/摇杆导航、肩键蓄力、双键组合，含大炮装填等特殊流程）。
- Android 触摸适配：拖拽瞄准、捏合双指缩放镜头（0.6×–2.0×）。真机表现仍需进一步验证。

## 使用 Unity 运行

1. 安装 **Unity 2022.3.62f3**（见 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)），用 Unity Hub 打开包含 `Assets/`、`Packages/` 和 `ProjectSettings/` 的本目录。
2. 等待 Unity 导入资源、解析包并完成脚本编译。工程使用 URP 2D；依赖由 [Packages/manifest.json](Packages/manifest.json) 管理（Unity Localization 等），Mirror 以源码形式位于 `Assets/Mirror/`。
3. 打开 [Assets/Scenes/Main.unity](Assets/Scenes/Main.unity)，点击编辑器的 **Play**。从标题画面进入单人、本地双人或「局域网对战」：主机点「创建主机」，另一台设备在同网段刷新房间列表加入。

游戏内的 **GM** 按钮提供语言切换、解锁全关卡、解锁武器、AI 调试、爆炸镜头等按钮式调试命令（联机对局需房主开启「允许作弊」）。

## 验证

工程自带大量可重复执行的校验入口，集中在 `Assets/Mutiny/Scripts/Verification/`，通过编辑器菜单 `Mutiny/` 下的各项命令运行，例如：

- `Mutiny/Online/Validate Online Basics`、`Validate Match Determinism`：联机消息往返、命令队列归属、确定性回归；
- `Mutiny/Parity/`：关卡生命周期、表现与原版对齐、移动端手势等 Play Mode 校验；
- 控制器、回合操作 UI、本地化、全关卡回归等独立套件。

注意：部分断言硬编码英文文案，需把语言切到 en 后运行；个别预存在失败项与本次改动无关（详见仓库内的排查记录）。

## 目录

| 路径 | 内容 |
| --- | --- |
| `Assets/Mutiny/Scripts/Level/` | XML 解析、关卡构建与加载 |
| `Assets/Mutiny/Scripts/Simulation/` | 回合、角色、物理、武器与 AI |
| `Assets/Mutiny/Scripts/Presentation/` | 前端、输入、HUD、镜头、动画与音频 |
| `Assets/Mutiny/Scripts/Network/` | 联机管理器、LAN 房间发现、网络消息（本项目新增） |
| `Assets/Mutiny/Scripts/Persistence/` | 单人进度保存 |
| `Assets/Mutiny/Scripts/Verification/` | 校验入口与编辑器验证菜单 |
| `Assets/Mutiny/Localization/` | 中英双语言表与文案源 tsv |
| `Assets/Mutiny/Resources/` | 运行时关卡和视听资源 |
| `Assets/Mirror/` | Mirror 网络库（联机依赖） |
| `Assets/Scenes/Main.unity` | 入口场景 |

## 与上游的关系

本项目沿用 Mutiny X 对原版 SWF、ActionScript 与关卡 XML 的逆向成果与资源映射，并在此基础上独立开发联机、本地化与输入扩展。原游戏及其原始美术、音频版权归 Nitrome 等相应权利人所有；Mutiny X 及本项目均用于研究与复刻实践，未取得原版内容的商业发布授权。
