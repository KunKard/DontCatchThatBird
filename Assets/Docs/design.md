# 设计文档

本设计文档针对《Don't Catch That Bird》MVP（Unity 2022，目标平台：PC）进行详细规划。

---

## 1. 项目概述

- **项目名称**：Don't Catch That Bird（暂译《别抓那只鸟》）
- **赛事**：金海豚 GameJam（截止 2026/07/06）
- **开发引擎**：Unity 2022
- **目标平台**：Windows PC（需实体键盘）
- **开发规模**：单人开发，2 天预算完成 MVP
- **画面风格**：黑白简笔画，参考"线条小狗"风格。背景白色，线条黑色。所有角色和UI均为线条绘制。需实现**线条抖动Shader**（Shader Graph顶点噪声偏移）以体现手绘风格。

---

## 2. MVP 功能清单

- **线条抖动Shader**：用于小鸟和树枝等线条模型的动态抖动。
- **虚拟键盘界面**：显示字母键（Q~P、A~L、Z~M，共26个）、数字键（1~0，共10个）和空格键，共37个可点击目标。**纯展示，交互仅通过物理键盘(`Input.GetKeyDown`)**。
- **中心小鸟**：停留在虚拟键盘某个按键上，线稿绘制，可应用抖动Shader。
- **分数/连击显示**：左上角显示当前Score和连击Combo。
- **树枝与已抓小鸟**：屏幕下方中心显示树枝，抓到的小鸟以缩略线稿形式依次显示。第一根满10只后后方增加新树枝（前遮后）。
- **DangerKey显示**：右上角显示当前所有Danger Key文字（例如 "Danger: F, K"），按下任意一个即GameOver。
- **输入监听**：监听37个按键（26字母、10数字、空格）。使用 `Input.GetKeyDown(KeyCode.X)` 进行按键检测。
- **游戏循环**：游戏开始随机生成一只小鸟及其对应的DangerKey。小鸟每0.6~1.2秒随机跳到新键。玩家按键后如果匹配小鸟位置且不是DangerKey，则成功抓取；否则普通按键无效或Combo清零。DangerKey与小鸟绑定，鸟存活期间不变；分数达阈值后新生成的鸟带多个DangerKey。游戏时间固定60秒，或按到DangerKey，或连续Miss 5次，即结束。
- **随机事件**：无其他随机事件。重点在抓取速度和连击。
- **游戏结束**：时间到(60秒)或按下DangerKey或连续Miss 5次时Game Over，弹出结算界面显示分数、最高连击、抓取总数等，并提供重试选项。

---

## 3. 核心玩法流程

1. 游戏开始，初始化**总计时器**（60秒）、**Score=0**、**Combo=0**、**Miss=0**，生成第一个小鸟。
2. 生成一个随机**DangerKey**（来自37个键，与当前小鸟绑定，数量由当前分数决定：0-99→1个、100-199→2个、200+→3个），显示在右上角。
3. 在虚拟键盘上随机一个键（排除所有DangerKey），让**小鸟**停留其上。
4. 小鸟每隔0.6~1.2秒（可配置）随机跳到另一个键（排除DangerKey）。每次跳跃触发 Miss++。
5. 玩家按下任意支持键：
   - **如果按下任意DangerKey**：立即Game Over。
   - **如果按下小鸟当前所在键且不是DangerKey**：判定成功。
     - 计算得分：`Score += baseScore + combo × comboMultiplier`
     - Combo++，Miss=0，更新UI。
     - 将当前位置生成已抓小鸟缩略图到树枝上，立即生成新小鸟。
     - 新鸟携带对应数量的DangerKey。
   - **如果按下其他键**：Combo=0。
6. 如果 Miss ≥ 5 → Game Over。
7. 继续步骤4-6直到**总计时结束(60秒)**或**失败**。然后显示结算。

*示意图：*

- Score / Combo：左上显示。
- 小鸟：居中虚拟键盘上飞来飞去。
- 鸟移动倒计时：键盘上方显示小鸟下次跳跃剩余时间。
- DangerKey(s)：右上显示当前所有"禁止按键"（红色，多个逗号分隔）。
- Miss 计数：右上 DangerKey 下方显示 "Miss: N/5"。
- 树枝：屏幕底部中心，依序挂载已抓小鸟缩略图，满10只后方增新枝。

---

## 4. 输入规则

- 使用Unity旧版输入API：`Input.GetKeyDown(KeyCode key)`。
- 需要监听 **37 个按键**：
  - 字母键：Q、W、E、R、T、Y、U、I、O、P、A、S、D、F、G、H、J、K、L、Z、X、C、V、B、N、M (共26个)。
  - 数字键：1、2、3、4、5、6、7、8、9、0 (共10个)，对应KeyCode.Alpha1~Alpha0。
  - 空格键：KeyCode.Space。
- 其他按键（Esc、F1~F12、Shift/Ctrl/Alt等）均忽略。
- 按键事件在`Update()`中检测。
- 按下按键时判断：
  - 若按下**任意DangerKey**（右上显示的字符），立即触发Game Over。
  - 否则若按下键与当前小鸟所在键相同，则判定为**成功抓取**。
  - 其余情况：无效果，Combo清零。

---

## 5. 小鸟移动与节奏

- **出现方式**：每次新生成时随机位于虚拟键盘上一个键（不包括所有DangerKey所在键）。
- **跳跃频率**：小鸟每隔 0.6~1.2 秒（可在GameConfigSO中作为参数）随机移动到另一个键上。移动过程瞬间切换。
- **移动倒计时**：UI 显示小鸟下次跳跃的剩余时间。
- **被抓取时**：立即判定成功，该位置生成缩略图，同时生成新小鸟跳到新键上。
- **Miss 计数**：每次跳跃递增 Miss。抓取后 Miss=0。Miss ≥ 5 → GameOver。
- **树枝显示**：屏幕下方居中显示树枝。每抓住一只小鸟就在树枝上增添该小鸟的缩略线稿。每根树枝容量10只，满了之后在后方新增一根（前遮挡后）。
- **连击规则**：
  - 连续成功抓取次数增加Combo（累积递增，非每次重置）。
  - 按错键（非DangerKey）Combo清零；按DangerKey直接GameOver。
- **分数公式**：
  - `实际得分 = baseScore + combo × comboMultiplier`
  - 示例：baseScore=10, comboMultiplier=5 → 当前combo=3时一次抓取得 10+3×5=25 分。

---

## 6. UI布局

所有UI元素均基于**Canvas**（参考分辨率1920×1080，Screen Space - Overlay）。建议布局如下表：

| 元素 | 位置 | Anchor | 位置偏移 (Pos) | 尺寸 (Size) | 说明 |
| --- | --- | --- | --- | --- | --- |
| **Score** | 左上角 | (0, 1) | (20, -20) | (200×60) | "Score: 0"，字体黑色，背景透明 |
| **Combo** | 左上，Score下方 | (0, 1) | (20, -90) | (200×60) | "Combo: 0"，字体黑色 |
| **DangerKey(s)** | 右上角 | (1, 1) | (-20, -20) | (400×50) | "Danger: F, K, 3"，字体红色，多键逗号分隔 |
| **Miss 计数** | 右上，Danger下方 | (1, 1) | (-20, -90) | (160×50) | "Miss: 0/5"，≥4变红色加粗 |
| **鸟移动倒计时** | 键盘上方居中 | (0.5, 0.5) | (0, -130) | (160×40) | "Next: 0.8s"，字体黑色 |
| **虚拟键盘** | 居中下方 | (0.5, 0) | (0, 150) | (自适应) | 网格排列37键，纯展示无交互 |
| **小鸟** | 中央虚拟键盘上方 | (0.5, 0) | (0, 400) | (64×64) | 小鸟精灵，位于当前键上方，附着线条抖动Shader |
| **树枝1** | 屏幕底部 | (0.5, 0) | (0, 20) | (800×100) | 水平树枝，已抓小鸟缩略图在此排列 |
| **已抓小鸟** | 树枝上依次排列 | (0.5, 0) | 从左到右 | (48×48) | 抓取成功后新增，每根枝容量10只 |

---

## 7. 技术架构

### 场景 (Scene)
- **MainScene**：唯一场景，不存在场景切换。

### 预制件 (Prefab) — 3 个
| Prefab | 用途 |
|---|---|
| **BirdPrefab** | 小鸟对象：SpriteRenderer + Bird脚本 |
| **KeyButtonPrefab** | 单个虚拟按键：Image + Text（纯展示，无Button/Collider） |
| **CaughtBirdIconPrefab** | 树枝上已抓小鸟缩略图：Image |

### 脚本 (Script) — 4 个
| 脚本 | 职责 |
|---|---|
| **GameManager** | 游戏状态机(Playing/GameOver)、37键输入监听、分数/连击计算、Miss计数、多DangerKey管理、三种GameOver条件判定、计时器 |
| **Bird** | 挂BirdPrefab。存储CurrentKey、自动跳跃定时器(Timer 0.6~1.2s)、暴露TimeUntilJump供UI读取、跳跃时通知GameManager增加Miss |
| **KeyboardDisplay** | 生成37键QWERTY布局、HighlightKey/ClearHighlight方法、键位→世界坐标字典 |
| **UIManager** | 更新Score/Combo/DangerKeys/Miss/鸟移动倒计时文本、管理树枝上已抓小鸟列表(多根树枝层级)、GameOver面板(淡入+统计+Restart) |

### ScriptableObject — 1 个
```
GameConfigSO
├── 鸟移动间隔 (min=0.6, max=1.2)
├── 游戏总时长 (60s)
├── 基础得分 baseScore (10)
├── Combo 乘数 comboMultiplier (5)
├── 连续 Miss 上限 (5)
├── DangerKey 阈值: [{score: 0, count: 1}, {score: 100, count: 2}, {score: 200, count: 3}]
├── 每根树枝容量 (10)
├── 最大树枝数 (3)
├── 监听按键列表 (37 个 KeyCode)
└── Shader 抖动强度 (0.005)
```

### 数据流
```
Input.GetKeyDown (37键，Update 轮询)
        │
        ▼
  GameManager ◄──── GameConfigSO
  │         │
  │         ├── Match BirdKey    → Catch → Score+=, Combo++, Miss=0, Respawn
  │         ├── Match DangerKey  → GameOver
  │         └── Other            → Combo=0
  │
  │  Bird.OnJumped → Miss++
  │  Miss >= 5     → GameOver
  │  Timer >= 60s  → GameOver
  │
  └──► UIManager
        ├── Score / Combo（左上）
        ├── DangerKey(s)（右上，红色，多个逗号分隔）
        ├── 鸟移动倒计时（键盘上方）
        ├── Miss 计数 "Miss: N/5"（右上 Danger 下方）
        ├── 树枝鸟列表（多根树枝层级）
        └── GameOver 面板（淡入，含统计与 Restart）
```

### 文件夹结构
```
Assets/
├── Scripts/
│   ├── GameManager.cs
│   ├── Bird.cs
│   ├── KeyboardDisplay.cs
│   └── UIManager.cs
├── Prefabs/
│   ├── BirdPrefab.prefab
│   ├── KeyButtonPrefab.prefab
│   └── CaughtBirdIconPrefab.prefab
├── ScriptableObjects/
│   └── GameConfig.asset
├── Materials/
│   └── WobbleShader.mat
├── Sprites/
│   ├── bird.png  (简笔画小鸟)
│   ├── bird_small.png  (缩略版)
│   ├── branch.png  (树枝)
│   └── key_bg.png  (按键背景)
├── Scenes/
│   └── MainScene.unity
└── Docs/
    ├── design.md
    └── dev-plan.md
```

---

## 8. 开发原则

| 原则 | 说明 |
|---|---|
| **KISS** | 每个脚本职责单一，能不写的方法不写 |
| **YAGNI** | 不预留"以后可能需要"的接口/抽象 |
| **数据驱动** | 所有可调参数进 GameConfigSO，不硬编码 |
| **不过度设计** | 不用事件系统、不用接口抽象、不用对象池 |
| **单人节奏** | 每完成一个Task停止，等确认后再继续 |

---

## 9. 开发优先级与任务拆分

### P0（必须实现）
- 37个按键监听（Input.GetKeyDown）
- 虚拟键盘界面（纯展示，键位布局）
- 小鸟生成与随机移动（0.6~1.2s变化）
- DangerKey生成（与鸟绑定，多数随分数阈值增加）及UI显示
- 抓鸟判定逻辑（按键匹配+非DangerKey时得分；DangerKey时GameOver）
- Score和Combo基础逻辑（累积递增，按错清零）
- Miss计数与5次Miss→GameOver
- 树枝和已抓小鸟显示（多根树枝溢出）
- 60秒总计时器与GameOver面板
- 线条抖动Shader（验证风格）
- 鸟移动倒计时显示

### P1（可选加分，MVP暂时忽略）
- 音效（捕获音效、背景音乐）
- 震屏反馈
- 多种小鸟/主题切换（小猫模式切换等）
- 菜单与关卡选择
- 按键视觉反馈（虚拟键按下高亮）

---

## 10. 验收标准

完成Demo后需满足以下检查项：

- [ ] **键盘显示**：屏幕中央虚拟键盘完整可见，包含37个键（26字母、10数字、空格）。纯展示无交互。
- [ ] **小鸟交互**：小鸟（线条图）会随机出现在键上并自动移动（0.6~1.2s）。
- [ ] **按键输入**：物理键盘监听37键。按正确键可抓鸟，按错误键Combo清零，按DangerKey立即GameOver。
- [ ] **分数与连击**：`Score += baseScore + combo × comboMultiplier`，Combo累积递增，按错清零。
- [ ] **DangerKey**：与小鸟绑定，存活期间不变。分数100+出现第2个，200+出现第3个。与BirdKey永不同值。
- [ ] **Miss机制**：小鸟每次跳跃Miss++，抓取重置。Miss ≥ 5 → GameOver。
- [ ] **树枝显示**：成功抓到的小鸟缩略图依次显示在树枝上。满10只后方新增树枝（前遮后）。
- [ ] **倒计时**：显示小鸟下次跳跃剩余时间。
- [ ] **Shader抖动**：小鸟和树枝等线条有可见抖动效果，风格统一。
- [ ] **游戏循环**：游戏持续60秒或按DangerKey或Miss≥5后结束，显示原因和统计数据，可Restart。
- [ ] **三种GameOver**：DangerKey / Miss≥5 / 时间到，均有独立原因提示。
- [ ] **结束面板**：显示最终Score/最高Combo/抓取总数/结束原因/Restart按钮。
- [ ] **UI布局**：元素位置正确，数据实时更新。
- [ ] **启动正常**：无启动错误。
- [ ] **代码结构**：不超过4个核心脚本，GameConfigSO驱动所有参数。

---

## 11. 开发阶段速览

| Phase | 名称 | Task 数 | 预计时间 |
|---|---|---|---|
| 0 | 项目搭建 | 1 | 30 min |
| 1 | 核心循环 — "能抓到鸟吗？" | 4 | 3 hrs |
| 2 | 游戏流程 — "能玩完整一局吗？" | 3 | 2 hrs |
| 3 | 打磨 — "像游戏吗？" | 3 | 1.75 hrs |
| 4 | 收尾 | 1 | 1 hr |
| **合计** | | **12** | **≈ 8.25 hrs** |

详细Task拆分见 [dev-plan.md](dev-plan.md)。

---

## 12. 参考资料

- Unity官方文档：`Input.GetKeyDown`用法。
- Unity官方教程：Shader Graph顶点位移示例。
- Unity UI布局：RectTransform锚点说明。
