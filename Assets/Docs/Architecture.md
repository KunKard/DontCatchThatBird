# Architecture.md — 项目架构文档

> 自动生成于 2026-07-01
>
> 项目：Don't Catch That Bird
>
> 脚本总数：9

---

## 脚本清单

```
Assets/Scripts/
├── GameManager.cs          # 游戏主控制器（Singleton）
├── GameConfigSO.cs         # ScriptableObject 配置数据
├── Bird.cs                 # 小鸟实体
├── KeyboardDisplay.cs      # 虚拟键盘 UI 生成与管理
├── UIManager.cs            # UI 文本与 GameOver 展示
├── BranchDisplay.cs        # 树枝多根管理与鸟排列
├── MaterialProvider.cs     # Doodle Shader 材质（Singleton）
├── TextWobble.cs           # TMP 文字 Perlin 抖动
└── KeyCodeUtility.cs       # KeyCode 转字符串工具
```

---

## ===== 核心层 =====

---

# GameManager.cs

**路径**：`Assets/Scripts/GameManager.cs`

**职责**：
- 游戏状态机（Playing / GameOver）
- 37 键物理键盘输入监听（`Input.GetKeyDown`）
- 按键判定分发：DangerKey → GameOver / BirdKey → 抓取 / 其他 → Combo 清零 + Miss++
- 分数计算（`Score += baseScore + combo × comboMultiplier`）
- Combo 累积与清零
- Miss 计数（跳跃 + 按错键）
- DangerKey 管理（随机选取、数量按分数阈值递增、与 BirdKey 互斥）
- 鸟的生成（注入回调：PickNextKey / OnJumped / JumpInterval）
- GameOver 条件判定（DangerKey / Miss≥5）
- BestScore 持久化（PlayerPrefs）
- 空格键重新开始

**依赖**：
```
GameConfigSO        (数据配置)
KeyboardDisplay     (按键视觉反馈)
UIManager           (UI 更新 / GameOver / 树枝定位)
Bird                (当前活跃鸟)
MaterialProvider    (Doodle 材质初始化)
Canvas              (鸟的父节点)
PlayerPrefs         (BestScore 持久化)
```

**被哪些脚本调用**：
```
Bird                (通过注入的 PickNextKey Func / OnJumped Action)
BranchDisplay       (读取 config.branchCapacity / config.maxBranchCount)
UIManager           (读取 GameOverReason / state / currentBird)
KeyboardDisplay     (不再直接调用 — Refactor 7 解耦)
```

**公开接口**：
| 成员 | 说明 |
|---|---|
| `Instance` | Singleton 实例 |
| `state` | GameState 枚举（Playing / GameOver） |
| `score` / `combo` / `missCount` / `totalCaught` | 运行时状态 |
| `currentBird` | 当前活跃鸟 |
| `PickRandomKeyExcluding(KeyCode[])` → KeyCode | 随机选键（排除指定列表） |
| `GetBirdInterval(int score)` → (float, float) | 难度曲线查表 |
| `SpawnBird()` | 生成新鸟 |
| `OnBirdJumped()` | 鸟跳跃回调（Miss++） |
| `Restart()` | 重新开始 |
| `ReasonToText(GameOverReason)` → string | 结束原因→英文文本 |
| `GetBirdInterval(int score)` → (float, float) | 难度曲线查表 |

**维护说明**：
- 不要在这里处理 UI 布局（→ UIManager）
- 不要在这里处理树枝（→ BranchDisplay）
- 不要在这里处理材质（→ MaterialProvider）
- 不要在这里处理小鸟动画（→ Bird）
- Input 监听已在此处，未来扩展组合键需重构 `OnKeyPressed`

**未来扩展**：
- 双鸟并发：`currentBird` → `List<Bird>`
- 道具系统：在 `CatchBird` 前后挂载道具 Hook
- 事件总线：`OnKeyPressed` 改为分发事件而非直接调用

---

# GameConfigSO.cs

**路径**：`Assets/Scripts/GameConfigSO.cs`

**职责**：
- 所有可调参数的 ScriptableObject 数据容器
- 数据驱动，不硬编码

**依赖**：
```
无（纯数据类）
```

**被哪些脚本调用**：
```
GameManager         (读取所有配置)
Bird                (读取 difficultyLevels)
BranchDisplay       (读取 branchCapacity / maxBranchCount)
MaterialProvider    (读取 outlineWidth)
```

**公开接口**：
| 字段 | 说明 |
|---|---|
| `difficultyLevels[]` | 难度曲线（scoreThreshold / minInterval / maxInterval） |
| `baseScore` (10) | 基础得分 |
| `comboMultiplier` (5) | Combo 乘数 |
| `maxMissCount` (5) | 最大 Miss 数 |
| `dangerKeyThresholds[]` | DangerKey 数量阈值 |
| `branchCapacity` (10) | 每根树枝鸟容量 |
| `maxBranchCount` (3) | 最大树枝数 |
| `validKeys[]` | 37 个监听 KeyCode |
| `outlineWidth` (5) | Shader 描边宽度 |

**维护说明**：
- 纯数据，不含行为逻辑
- 所有魔法数字移入此处
- 新增可调参数一律加到此 SO
- CreateAssetMenu 路径：`Don't Catch That Bird/GameConfig`

**未来扩展**：
- 不同难度预设（Easy / Normal / Hard 多份 SO 资产）
- 关卡特定配置

---

# MaterialProvider.cs

**路径**：`Assets/Scripts/MaterialProvider.cs`

**职责**：
- Doodle Shader 材质的创建与缓存
- Singleton，全局访问

**依赖**：
```
GameConfigSO        (读取 outlineWidth)
Shader.Find("UI/DoodleImage")
```

**被哪些脚本调用**：
```
GameManager         (Start 中调 Init)
KeyboardDisplay     (ApplyWobbleToAllKeys)
BranchDisplay       (SpawnBranchInternal / ApplyDoodle)
Bird                (不直接调用 — 由 GameManager 在 SpawnBird 中设置)
```

**公开接口**：
| 成员 | 说明 |
|---|---|
| `Instance` | Singleton |
| `Init(GameConfigSO)` | 创建材质并设置参数 |
| `GetWobbleMaterial()` → Material | 获取 Doodle 材质 |

**维护说明**：
- 材质创建逻辑集中一处，方便后续切换到 Phase 2-5 Doodle Shader 变体
- 如果 Shader 迁移到 URP 原生 HLSL，仅需改这一个文件的 Shader.Find 字符串

**未来扩展**：
- 支持多种 Doodle 风格材质（不同参数预设）
- 运行时动态切换材质

---

## ===== 实体层 =====

---

# Bird.cs

**路径**：`Assets/Scripts/Bird.cs`

**职责**：
- 小鸟键位管理（CurrentKey）
- 自动跳跃（JumpInterval 驱动定时器）
- DangerKey 判定（IsDangerKey）
- 飞向树枝动画（FlyRoutine 协程）
- 圆形占位 Sprite 生成（CreateCircleSprite 静态方法）

**依赖**：
```
GameConfigSO        (读取 difficultyLevels — 已解耦，由 GM 注入 JumpInterval)
PickNextKey (Func)  (外部注入 — GameManager)
OnJumped (event)    (外部注入 — GameManager)
JumpInterval (属性) (外部注入 — GameManager)
```

**被哪些脚本调用**：
```
GameManager         (SpawnBird / OnKeyPressed / CatchBird / TriggerGameOver)
```

**公开接口**：
| 成员 | 说明 |
|---|---|
| `CurrentKey` | 小鸟当前所在的 KeyCode |
| `TimeUntilJump` | 距下次跳跃剩余秒数（UI 读取） |
| `PickNextKey` | Func 委托：排除列表→随机键（GM 注入） |
| `OnJumped` | event：跳跃后触发（GM 注入 ClearHighlight + Move + OnBirdJumped） |
| `JumpInterval` | (min, max) 属性（GM 注入） |
| `Init(...)` | 初始化（键位 / DangerKeys / Config / Sprite / Color / isActive） |
| `SetActive(bool)` | 启用/暂停（GameOver 时停鸟） |
| `IsDangerKey(KeyCode)` → bool | 判定键是否为 DangerKey |
| `FlyToBranch(Transform, float)` | 飞向树枝并停留 |
| `CreateCircleSprite(int)` → Sprite | 静态工具：生成圆形占位图 |

**维护说明**：
- 不再直接依赖 `GameManager.Instance`（Refactor 6 解耦）
- 难度曲线通过 `JumpInterval` 属性由 GM 控制，Bird 不关心分数
- 所有跳跃时的 UI 操作（高亮切换）由 `OnJumped` 事件在 GM 侧执行
- CreateCircleSprite 应迁至独立 SpriteFactory（P2 重构）

**未来扩展**：
- 双鸟并发：Bird 已是独立实体，天然支持多实例
- Boss 鸟：新增 `OnBossJumped` 事件，GM 侧特殊处理
- 不同鸟类型：派生 Bird 或注入不同 Init 参数

---

## ===== UI 层 =====

---

# KeyboardDisplay.cs

**路径**：`Assets/Scripts/KeyboardDisplay.cs`

**职责**：
- 虚拟键盘 QWERTY 布局生成（Editor `Setup Keys` / Runtime `RebuildMap`）
- 按键高亮管理（HighlightKey / ClearHighlight）
- 按键闪烁反馈（FlashCorrectKey / FlashWrongKey / FlashDangerKey）
- Doodle 材质应用到所有键

**依赖**：
```
TextMeshProUGUI    (按键标签)
Image / Outline    (按键视觉)
MaterialProvider   (Doodle 材质)
```

**被哪些脚本调用**：
```
GameManager         (HighlightKey / ClearHighlight / FlashCorrect / FlashWrong / FlashDanger / GetKeyPosition)
```

**公开接口**：
| 成员 | 说明 |
|---|---|
| `SetupKeys()` | Editor ContextMenu：生成 37 键 |
| `GetKeyPosition(KeyCode)` → Vector3 | 键的世界坐标（鸟定位用） |
| `HighlightKey(KeyCode)` | 黄底高亮 |
| `ClearHighlight()` | 清除高亮 |
| `FlashCorrectKey(KeyCode)` | 浅绿色闪烁 0.25s |
| `FlashWrongKey(KeyCode)` | 浅红色闪烁 0.25s |
| `FlashDangerKey(KeyCode)` | 深红色闪烁 0.25s |

**维护说明**：
- Editor `Setup Keys` 生成键后，场景中即为静态 GameObject，Runtime 通过 `RebuildMap` 重建 `_keyMap`
- `[ExecuteInEditMode]` 仅用于编辑器，Runtime 行为通过 `Start / Awake` 管理
- Outline 组件与 Doodle Shader 描边功能重叠，Shader 稳定后可移除 Outline

**未来扩展**：
- 按键视觉反馈动画（呼吸/脉冲）
- 键盘布局主题切换（QWERTY / AZERTY）
- 空格键特殊视觉处理

---

# UIManager.cs

**路径**：`Assets/Scripts/UIManager.cs`

**职责**：
- UI 文本元素管理（Score / Combo / Danger / Miss / 倒计时 / BestScore）
- GameOver 简易文本展示（复用现有文本行）
- `[ContextMenu]` 一键生成全部 TMP 文本 + 自动添加 TextWobble
- 树枝操作委托给 BranchDisplay

**依赖**：
```
TextMeshProUGUI    (所有 UI 文本)
TextWobble         (自动添加到每个 TMP)
BranchDisplay      (树枝管理委托)
KeyCodeUtility     (KeyCode → 字符串)
```

**被哪些脚本调用**：
```
GameManager         (UpdateScore / UpdateCombo / UpdateMiss / UpdateDangerKeys /
                      OnGameStart / ShowGameOverText / UpdateBestScore /
                      GetBranchContainer / GetNextBranchX)
```

**公开接口**：
| 成员 | 说明 |
|---|---|
| `CreateUI()` | Editor ContextMenu：生成所有 TMP + 触发 BranchDisplay |
| `OnGameStart()` | 重置所有文本 + 委托 BranchDisplay.OnGameStart |
| `UpdateScore/Combo/Miss/DangerKeys/BestScore(int/KeyCode[])` | 实时更新 |
| `ShowGameOverText(reason, score, best, combo, caught)` | GameOver 文本展示 |
| `GetBranchContainer()` → Transform | 委托 BranchDisplay |
| `GetNextBranchX()` → float | 委托 BranchDisplay |
| `ResetComboColor()` | 还原 Combo 文字颜色 |
| `branchDisplay` | BranchDisplay 引用 |

**维护说明**：
- 不处理树枝逻辑（→ BranchDisplay）
- 不处理 GameOver 面板（旧版已删除）
- TMP 文本自动加 TextWobble，后续可替换为 GPU 抖动（Phase 5）
- `OnGameStart` 调用 `branchDisplay.OnGameStart`，时序耦合需保持

**未来扩展**：
- 本地化（TMP 文本→多语言表）
- 动画过渡（Score 飞入/Combo 放大）
- 自定义字体替换

---

# BranchDisplay.cs

**路径**：`Assets/Scripts/BranchDisplay.cs`

**职责**：
- 树枝 GameObject 生命周期（生成 / 复用 / 溢出）
- 已抓小鸟在树枝上的排列（从左到右）
- 多树枝溢出自动创建新枝（前遮后）
- Doodle 材质应用
- `[ContextMenu]` 一键创建树枝

**依赖**：
```
GameManager.Instance.config  (读取 branchCapacity / maxBranchCount)
MaterialProvider             (Doodle 材质)
Canvas                       (父节点)
```

**被哪些脚本调用**：
```
UIManager           (委托调用 GetBranchContainer / GetNextBranchX / OnGameStart)
GameManager         (间接通过 UIManager)
```

**公开接口**：
| 成员 | 说明 |
|---|---|
| `CreateEditorBranch()` | Editor ContextMenu：一键生成树枝 |
| `OnGameStart()` | 新局开始：清空鸟 + 仅保留第一根树枝 |
| `GetBranchContainer()` → Transform | 当前树枝的鸟容器 |
| `GetNextBranchX()` → float | 下一只鸟的本地 X 偏移（自动处理满枝溢出） |
| `branchPrefab` | 树枝 Prefab（可拖入美术资源） |
| `mainCanvas` | Canvas 引用 |

**维护说明**：
- `ScanForExistingBranches` 在 Start 时复用编辑器创建的树枝（非序列化 `_branches` 列表的补偿方案）
- `_branches` 是 `List<BranchInfo>` 不序列化，需在 Start 重建
- 树枝创建路径：Prefab 或占位矩形（通过 `branchPrefab` == null 判断）
- 未来可移除 `branchPrefab == null` 的占位分支（美术就位后）

**未来扩展**：
- 树枝样式主题（不同美术资源的 Prefab）
- 鸟在树枝上的动画（蹦跳/扇翅膀）
- 树枝交互（点击树枝上的鸟查看信息）

---

# TextWobble.cs

**路径**：`Assets/Scripts/TextWobble.cs`

**职责**：
- 对 RectTransform 施加 PerlinNoise 驱动的持续抖动
- 模拟手绘抖动效果（CPU 方案，替代 GPU Shader）

**依赖**：
```
RectTransform   (anchoredPosition)
PerlinNoise     (数学噪声)
```

**被哪些脚本调用**：
```
UIManager.MakeTMP    (自动添加到每个 TMP 文本)
```

**公开接口**：
| 成员 | 说明 |
|---|---|
| `intensity` (2f) | 抖动幅度（像素） |
| `speed` (8f) | 抖动速度 |

**维护说明**：
- 根据 `Shader_StyleGuide.md` Phase 5 计划，后续应由 TMP Doodle Shader 在 GPU 侧替代
- 当前是纯 CPU 方案，每个文本一个 Update 调用，50+ 文本会有性能考虑
- 可用 `enabled = false` 临时关闭

**未来扩展**：
- 被 Phase 5 TMP Doodle Shader 替换后移除此脚本
- 或不替换，作为轻量级替代方案保留

---

# KeyCodeUtility.cs

**路径**：`Assets/Scripts/KeyCodeUtility.cs`

**职责**：
- KeyCode 枚举 → 显示字符串转换（消除重复代码）

**依赖**：
```
无
```

**被哪些脚本调用**：
```
KeyboardDisplay     (KeyToString)
UIManager           (KeyDisplay)
```

**公开接口**：
| 成员 | 说明 |
|---|---|
| `ToDisplayString(KeyCode)` → string | Alpha0→"0", Space→"SPACE", Q→"Q" |

**维护说明**：
- 纯静态工具，无状态
- 需要新增特殊键显示时只改此文件

**未来扩展**：
- 本地化键名映射
- 更多特殊键处理（Keypad / Function 键）

---

## 依赖关系图

```
GameConfigSO (数据) ─────────────────────────────┐
                                                  │
MaterialProvider (Singleton)                       │
  └─ Shader.Find("UI/DoodleImage")                │
                                                  │
GameManager (Singleton)                            │
  ├── GameConfigSO                                 │
  ├── MaterialProvider                             │
  ├── KeyboardDisplay     ← MaterialProvider       │
  ├── UIManager           ← BranchDisplay          │
  ├── Bird                ← (回调注入)              │
  └── Canvas                                       │
                                                  │
UIManager                                          │
  ├── TextWobble         (自动添加)                │
  ├── KeyCodeUtility                               │
  └── BranchDisplay                               │
       ├── GameConfigSO (via GM.Instance.config)   │
       └── MaterialProvider                        │
                                                  │
KeyboardDisplay                                    │
  ├── MaterialProvider                             │
  └── KeyCodeUtility                               │
                                                  │
Bird                                               │
  ├── (PickNextKey Func)      ← GM 注入           │
  ├── (OnJumped event)        ← GM 注入           │
  └── (JumpInterval 属性)     ← GM 注入           │
```

---

## 设计模式总结

| 模式 | 应用位置 | 备注 |
|---|---|---|
| **Singleton** | GameManager, MaterialProvider | 全局访问点。MaterialProvider 是 Refactor 7 提取的 |
| **ScriptableObject** | GameConfigSO | 数据驱动配置 |
| **回调注入** | Bird.PickNextKey / Bird.OnJumped / Bird.JumpInterval | Refactor 6 解耦，替代直接访问 Singleton |
| **委托** | UIManager → BranchDisplay | Refactor 9，职责分离 |
| **静态工具类** | KeyCodeUtility | 消除重复 |
| **MonoBehaviour 组件** | TextWobble | 可插拔的手绘抖动效果 |

---

## 重构历史

| 日期 | 重构 | 说明 |
|---|---|---|
| 2026-07-01 | Refactor 1 | 删除死代码（GameOver 面板、TimeUp） |
| 2026-07-01 | Refactor 2 | 提取 KeyCodeUtility |
| 2026-07-01 | Refactor 3 | 提取 ReasonToText 静态方法 |
| 2026-07-01 | Refactor 4 | 删除鸟 Prefab 双路径 |
| 2026-07-01 | Refactor 5 | 随机选键算法改为池化采样 |
| 2026-07-01 | Refactor 6 | Bird 与 GameManager.Instance 解耦（回调注入） |
| 2026-07-01 | Refactor 7 | 提取 MaterialProvider |
| 2026-07-01 | Refactor 8 | 树枝编辑器残留修复（扫描复用代替暴力删除） |
| 2026-07-01 | Refactor 9 | UIManager 拆出 BranchDisplay |
