# 《Don't Catch That Bird》MVP 开发计划

> **赛事**：金海豚 GameJam
> **截止**：2026 年 7 月 6 日晚
> **人员**：单人开发
> **目标**：完成即可，不求完美
> **MVP 预算**：2 天（~8.25 hrs，留缓冲交稿）

---

## 一、核心规则（修订版）

| 规则 | 说明 |
|---|---|
| 小鸟位置 | 随机停留在虚拟键盘某个键上，每隔 0.6~1.2s 随机跳到新键（排除所有 DangerKeys） |
| DangerKey | 与鸟绑定，鸟存活期间不变；按到 DangerKey → **立即 GameOver** |
| 多 DangerKey | Score 0-99 → 1 个 / 100-199 → 2 个 / 200+ → 3 个 |
| 按键判定 | 按对鸟键 → 抓取成功；按错非 DangerKey → Combo 清零；按 DangerKey → GameOver |
| Combo | 连续抓取递增累积，按错键清零 |
| 连 Miss 惩罚 | 小鸟连续跳跃 5 次未抓取 → **GameOver**；抓取成功重置计数器 |
| 分数公式 | `Score += baseScore + combo × comboMultiplier` |
| 树枝溢出 | 每根树枝容量 10 只，满了后方新增树枝（前遮后） |
| 虚拟键盘 | **纯展示**，交互仅通过物理键盘（`Input.GetKeyDown`） |
| 线条抖动 | Shader Graph 顶点噪声位移，手绘风格 |
| 游戏总时长 | 60 秒（3 种 GameOver 条件任意触发即结束） |

---

## 二、技术架构

### 2.1 Scene

```
MainScene（唯一场景）
```

### 2.2 Prefab（3 个）

| Prefab | 用途 |
|---|---|
| `BirdPrefab` | 小鸟：SpriteRenderer + Bird 脚本 |
| `KeyButtonPrefab` | 单个虚拟按键：Image + Text（纯展示，无 Button/Collider） |
| `CaughtBirdIconPrefab` | 树枝上已抓小鸟缩略图：Image |

### 2.3 Script（4 个）

| 脚本 | 职责 |
|---|---|
| **GameManager** | 游戏状态机、37 键输入监听、分数/连击、Miss 计数、多 DangerKey 管理、GameOver/重启 |
| **Bird** | 挂 BirdPrefab。存储 CurrentKey、跳跃定时器、暴露 TimeUntilJump |
| **KeyboardDisplay** | 生成 37 键虚拟键盘布局、高亮/取消高亮、键位坐标映射 |
| **UIManager** | 更新 Score/Combo/DangerKeys/Miss/倒计时文本、管理树枝鸟列表、GameOver 面板 |

### 2.4 ScriptableObject（1 个）

```
GameConfigSO
├── 鸟移动间隔 (min=0.6, max=1.2)
├── 游戏总时长 (60s)
├── 基础得分 baseScore (10)
├── Combo 乘数 comboMultiplier (5)
├── 连续 Miss 上限 (5)
├── DangerKey 数量阈值: [{score: 100, count: 2}, {score: 200, count: 3}]
├── 每根树枝容量 (10)
├── 最大树枝数 (3)
├── 监听按键列表 (37 个 KeyCode)
└── Shader 抖动强度 (0.005)
```

### 2.5 数据流

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

### 2.6 文件夹结构

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
│   ├── bird.png
│   ├── bird_small.png
│   ├── branch.png
│   └── key_bg.png
├── Scenes/
│   └── MainScene.unity
└── Docs/
    ├── design.md
    └── dev-plan.md
```

---

## 三、开发阶段

### Phase 总览

| Phase | 名称 | Task 数 | 预计时间 |
|---|---|---|---|
| 0 | 项目搭建 | 1 | 30 min |
| 1 | 核心循环 — "能抓到鸟吗？" | 4 | 3 hrs |
| 2 | 游戏流程 — "能玩完整一局吗？" | 3 | 2 hrs |
| 3 | 打磨 — "像游戏吗？" | 3 | 1.75 hrs |
| 4 | 收尾 | 1 | 1 hr |
| **合计** | | **12** | **≈ 8.25 hrs** |

### 依赖关系

```
Phase 0:  0.1
            │
Phase 1:   1.1 ──┬── 1.2 ── 1.3
                 │            │
                 └── 1.4 ────┘
                              │
Phase 2:   2.1 ── 2.2 ── 2.3
                 │
Phase 3:   3.1 ── 3.2 ── 3.3（3.1/3.2/3.3 可并行）
                 │
Phase 4:   4.1
```

---

## 四、详细 Task 列表

---

### Phase 0：项目搭建

#### Task 0.1 — 创建项目与配置骨架
| 项 | 内容 |
|---|---|
| **预计时间** | 30 min |
| **内容** | ① 创建 Unity 2022 2D 项目 ② 参考分辨率 1920×1080 ③ 创建 MainScene + Canvas（Screen Space - Overlay）④ 创建所有文件夹（Scripts/Prefabs/Materials/Sprites/ScriptableObjects）⑤ 创建 GameConfigSO 填入所有参数 ⑥ 场景中创建空 GameObject "GameManager"，挂 GameManager 空壳脚本 |
| **DoD** | ✅ 项目运行无报错 ✅ Canvas 渲染正常 ✅ 文件夹结构完整 ✅ GameConfigSO 可 Inspector 编辑 |
| **Git** | `feat: 初始化项目结构与 GameConfig` |

---

### Phase 1：核心循环 — "能抓到鸟吗？"

#### Task 1.1 — 虚拟键盘显示
| 项 | 内容 |
|---|---|
| **预计时间** | 45 min |
| **内容** | ① 创建 KeyButtonPrefab（Image 白色背景+黑色边框 + TextMeshPro 黑色文字）② KeyboardDisplay 脚本：Start() 生成 3 行字母键(Q-P / A-L / Z-M) + 1 行数字键(1-0) + 底部空格键，共 37 键 ③ 布局居中偏下 ④ 实现 `HighlightKey(KeyCode)`（变黄）和 `ClearHighlight()` ⑤ 创建键位→世界坐标字典供 Bird 定位 ⑥ **纯展示，不加 Button/Collider** |
| **DoD** | ✅ 37 键 QWERTY 完整可见 ✅ 文字标注正确 ✅ 空格键拉长 ✅ 分辨率适配不重叠 |
| **Git** | `feat: 虚拟键盘 UI 布局` |

#### Task 1.2 — 小鸟生成与显示
| 项 | 内容 |
|---|---|
| **预计时间** | 30 min |
| **内容** | ① 绘制/导入简笔画小鸟 Sprite（占位：Unity 圆形+三角形拼合即可）② 创建 BirdPrefab（SpriteRenderer + Bird 脚本）③ Bird 脚本：`KeyCode CurrentKey` 属性 ④ GameManager：`SpawnBird()` — 从配置 37 键随机选一个（排除所有 DangerKeys），获取 KeyboardDisplay 坐标，实例化鸟到该键上方 ⑤ KeyboardDisplay.HighlightKey() 高亮鸟所在键 |
| **DoD** | ✅ 游戏开始，鸟出现在随机键上方 ✅ 对应键变黄色高亮 ✅ Sprite 可见 |
| **Git** | `feat: Bird Prefab 与初始生成` |

#### Task 1.3 — 小鸟自动跳跃 + 倒计时 UI
| 项 | 内容 |
|---|---|
| **预计时间** | 45 min |
| **内容** | ① Bird.Update()：累计 deltaTime，达随机间隔（config.minInterval~maxInterval）触发 Jump ② JumpTo(newKey)：newKey 排除当前键和所有 DangerKeys，通知 KeyboardDisplay 清旧高亮+设新高亮，更新 transform.position ③ 每次跳跃通知 GameManager → Miss++ ④ UIManager 显示鸟移动倒计时：`"Next Jump: X.Xs"`（由 Bird.TimeUntilJump 驱动）⑤ Miss 计数显示 `"Miss: N/5"`（右上 Danger 下方） |
| **DoD** | ✅ 鸟每 0.6~1.2s 自动跳到不同键 ✅ 旧键高亮取消，新键高亮 ✅ Miss 计数每次跳跃递增 ✅ 倒计时 UI 实时更新 ✅ 鸟不会跳到 DangerKey |
| **Git** | `feat: 小鸟自动跳跃与移动倒计时` |

#### Task 1.4 — 输入监听、抓取判定与 DangerKey 绑定
| 项 | 内容 |
|---|---|
| **预计时间** | 45 min |
| **内容** | ① GameManager.Update()：遍历 37 个 KeyCode，`Input.GetKeyDown` 检测 ② 命中键 ∈ 鸟的 DangerKeys → `GameOver()`（先 Log，Task 2.2 完善）③ 命中键 == Bird.CurrentKey → `CatchBird()`：算分（`Score += baseScore + combo × comboMultiplier`）、Combo++、Miss=0、Destroy 旧鸟、SpawnBird() ④ 命中其他键 → Combo=0 ⑤ SpawnBird()：根据当前 Score 查表决定 DangerKey 数量（1~3），随机选取且互不相同且 ≠ BirdKey，存入 Bird 实例 ⑥ UIManager 更新 DangerKeys 显示（右上，红色，多键逗号分隔如 "Danger: F, K"） |
| **DoD** | ✅ 按对鸟键 → Score/Combo 递增 ✅ 按错非 Danger 键 → Combo 清零 ✅ 按 DangerKey → GameOver ✅ 新鸟生成带对应数量 DangerKeys ✅ 多 DangerKey 正确显示 |
| **Git** | `feat: 输入监听、抓取判定与 DangerKey 绑定` |

---

### Phase 2：游戏流程 — "能玩完整一局吗？"

#### Task 2.1 — 核心 UI 面板
| 项 | 内容 |
|---|---|
| **预计时间** | 30 min |
| **内容** | ① UIManager 挂 Canvas 子节点，持有对各 Text 的 SerializeField 引用 ② Score（左上，锚点(0,1)，偏移(20,-20)）：`"Score: 0"` 黑色 ③ Combo（Score 下方，偏移(20,-90)）：`"Combo: 0"` 黑色 ④ DangerKeys（右上，锚点(1,1)，偏移(-20,-20)）：`"Danger: -"` 红色，支持多键 ⑤ Miss 计数（Danger 下方，偏移(-20,-90)）：`"Miss: 0/5"`，≥4 变红 ⑥ 鸟移动倒计时（键盘上方居中）：`"Next: 0.0s"` 黑色 ⑦ GameManager 数据变化时调用 UIManager 对应方法更新 |
| **DoD** | ✅ 5 个 UI 元素位置正确 ✅ 所有文本实时更新 ✅ 多 DangerKey 逗号分隔 ✅ Miss 到 4 变红警告 |
| **Git** | `feat: 核心 UI 面板` |

#### Task 2.2 — 三种 GameOver + 结束面板
| 项 | 内容 |
|---|---|
| **预计时间** | 45 min |
| **内容** | ① GameManager 状态机：Playing / GameOver ② **条件 A**：按下 DangerKey → GameOver（提示 "你踩到了危险键！"）③ **条件 B**：Miss 计数 ≥ 5 → GameOver（提示 "小鸟飞走了！"）④ **条件 C**：游戏总计时 ≥ 60s → GameOver（提示 "时间到！"）⑤ GameOver 面板：显示结束原因、最终 Score、最高 Combo、抓到鸟总数、Restart 按钮 ⑥ Restart：`SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)` ⑦ GameOver 时停止所有计时器、输入和 Bird 活动 |
| **DoD** | ✅ 三种条件均可触发 GameOver ✅ 面板显示完整统计 ✅ Restart 完整重置所有状态 |
| **Git** | `feat: GameOver 三条件与结束面板` |

#### Task 2.3 — 多 DangerKey 随分数升级
| 项 | 内容 |
|---|---|
| **预计时间** | 45 min |
| **内容** | ① GameConfigSO 定义 DangerKey 阈值配置：`[{score: 0, count: 1}, {score: 100, count: 2}, {score: 200, count: 3}]` ② SpawnBird() 读取当前 Score，查表决定 DangerKey 数量 ③ 生成规则：N 个 KeyCode 互不相同、均 ≠ BirdKey、不与已存在的 DangerKeys 重叠 ④ UIManager：动态显示所有 DangerKeys，数量越多字体越大/越醒目 ⑤ DangerKey 总数不能超过可用键数（37 - 1 鸟键），理论上 Score 极高时可增加到 4-5 个（由 GameConfigSO 配置） |
| **DoD** | ✅ Score 100+ 出现第 2 个 DangerKey ✅ Score 200+ 出现第 3 个 ✅ 新增 DangerKey 不重复不冲突 ✅ UI 动态适应数量 |
| **Git** | `feat: 分数阈值多 DangerKey 机制` |

---

### Phase 3：打磨 — "像游戏吗？"

#### Task 3.1 — 树枝与已抓小鸟（多树枝溢出）
| 项 | 内容 |
|---|---|
| **预计时间** | 45 min |
| **内容** | ① 绘制/导入简笔画树枝 Sprite，置于屏幕底部（键盘下方）② CaughtBirdIconPrefab（小鸟缩略图 Image，48×48）③ UIManager 维护树枝列表：第一根树枝放置 10 只小鸟（HorizontalLayoutGroup 自动排列）④ 第 11 只时实例化第二根树枝（置于第一根后方 Z 轴 -1，Y 轴微上调），鸟继续从左排列 ⑤ 同理第 3 根 ⑥ 最大树枝数由 GameConfigSO 控制 |
| **DoD** | ✅ 抓鸟后树枝出现缩略图 ✅ 第一根满→第二根生成在后方 ✅ 层级遮挡 ✅ 不出界 |
| **Git** | `feat: 树枝与已抓小鸟（多树枝溢出）` |

#### Task 3.2 — 线条抖动 Shader
| 项 | 内容 |
|---|---|
| **预计时间** | 45 min |
| **内容** | ① Shader Graph：创建 Unlit 着色器 → Position 节点 + (SimpleNoise × Time × Intensity) → Add → Vertex Position ② 暴露参数：Intensity（默认 0.005）、Speed（默认 2）③ 创建 Material 挂此 Shader，分别应用到 BirdPrefab 和 Branch 的 SpriteRenderer ④ 也可挂到 CaughtBirdIconPrefab 上 |
| **DoD** | ✅ 运行时鸟线条可见持续轻微抖动 ✅ 树枝线条可见抖动 ✅ 效果流畅不卡顿 ✅ Inspector 可调参数 |
| **Git** | `feat: 线条抖动 Shader` |

#### Task 3.3 — 视觉反馈
| 项 | 内容 |
|---|---|
| **预计时间** | 30 min |
| **内容** | ① 抓取成功：Bird transform 瞬间 Scale 弹跳（1→1.3→1，0.15s 协程）② 对应键闪绿色 0.2s ③ DangerKey 变更时红色文字闪黄色 0.2s ④ Combo ≥ 5 文字自动 Size+5/Bold ⑤ GameOver 面板 CanvasGroup alpha 0→1 Fade In（~0.5s）⑥ Miss 计数到 4 时文字变红色 Bold |
| **DoD** | ✅ 所有交互有即时视觉反馈 ✅ 无延迟无卡顿 ✅ 反馈清晰不杂乱 |
| **Git** | `feat: 交互视觉反馈与 Juicing` |

---

### Phase 4：收尾

#### Task 4.1 — 调试、Bug 修复与数值平衡
| 项 | 内容 |
|---|---|
| **预计时间** | 60 min |
| **内容** | ① 完整游玩 10+ 局，覆盖所有 GameOver 条件 ② 边缘 case 测试：多 DangerKey 与 BirdKey 永不同值、鸟跳到 DangerKey 前必须排除、树枝满后新增正常、Restart 状态完全重置、Miss 边界（4→5→GameOver）③ 数值调节：移动速度是否合适、计分手感、DangerKey 阈值是否合理、5 次 Miss 难度 ④ 确认 Shader 在所有分辨率正常 ⑤ 构建 Windows Standalone（Mono/IL2CPP），独立运行测试 |
| **DoD** | ✅ 无崩溃/卡死 ✅ 完整一局流畅可玩 ✅ 数值手感合理 ✅ Standalone 构建成功可运行 |
| **Git** | `fix: 最终调试与数值平衡` |

---

## 五、开发原则

| 原则 | 说明 |
|---|---|
| **KISS** | 每个脚本职责单一，能不写的方法不写 |
| **YAGNI** | 不预留"以后可能需要"的接口/抽象 |
| **数据驱动** | 所有可调参数进 GameConfigSO，不硬编码 |
| **不过度设计** | 不用事件系统、不用接口抽象、不用对象池 |
| **单人节奏** | 每完成一个 Task 停止，等确认后再继续 |

---

## 六、验收 Checklist

- [ ] **虚拟键盘**：37 键完整可见，QWERTY 布局，纯展示
- [ ] **小鸟交互**：随机出现，0.6~1.2s 自动跳跃，可被抓取
- [ ] **DangerKey**：与鸟绑定，多 DangerKey 随分数阈值增加，与 BirdKey 永不同值
- [ ] **按键输入**：物理键盘 37 键监听，正确键抓取、DangerKey 死亡、错误键 Combo 清零
- [ ] **分数与连击**：`Score += baseScore + combo × comboMultiplier`，实时更新
- [ ] **Miss 机制**：连续 5 次跳跃未抓取 → GameOver，抓取后重置
- [ ] **三种 GameOver**：DangerKey / Miss=5 / 60s，均有独立原因提示
- [ ] **结束面板**：显示分数/最高 Combo/抓取总数/Restart
- [ ] **树枝展示**：已抓小鸟依次排列，满 10 只后方新增树枝
- [ ] **Lines Shader**：鸟和树枝线条可见抖动
- [ ] **倒计时**：小鸟下次跳跃剩余时间可见
- [ ] **视觉反馈**：抓取弹跳、按键闪绿、Danger 闪黄、Combo≥5 变大、Miss≥4 变红
- [ ] **Restart**：重开后所有状态干净
- [ ] **构建**：Windows Standalone 正常
