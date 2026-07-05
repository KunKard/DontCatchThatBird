# Post-MVP 完整游戏开发计划

> **基准**：MVP v1.0（GameJam 提交版）
> **目标**：可上架 itch.io / Steam 的完整小品游戏
> **人员**：单人
> **总工时**：~51 小时 / 8 个 Phase / 48 个 Task

---

## 总览

| Phase | 主题 | Task 数 | 预计工时 | 优先级 |
|---|---|---|---|---|
| 1 | MVP 收尾修复 | 5 | 3 h | P0 |
| 2 | 音效系统 | 6 | 4 h | P0 |
| 3 | Doodle Shader 手绘描边 | 8 | 12 h | P1 |
| 4 | 美术资产替换 | 7 | 8 h | P1 |
| 5 | 玩法深度 | 9 | 10 h | P1 |
| 6 | UI/UX 打磨 | 6 | 6 h | P2 |
| 7 | 菜单 & 流程 | 5 | 4 h | P2 |
| 8 | 多平台发布 | 4 | 4 h | P2 |
| **合计** | | **50** | **51 h** | |

### 依赖关系

```
Phase 1 (修复)
  │
Phase 2 (音效)
  │
Phase 3 (Shader)
  │
Phase 4 (美术) ← 依赖 Shader 就绪
  │
Phase 5 (玩法) ← 依赖 音效 + 美术
  │
Phase 6 (UI/UX)
  │
Phase 7 (菜单)
  │
Phase 8 (多平台) ← 可随时并行
```

---

## Phase 1 — MVP 收尾修复

> 先修 bug，再谈新功能。

### Task 1.1 — 修复 Restart 后树枝上的鸟残留

| 项 | 内容 |
|---|---|
| **预计** | 20 min |
| **内容** | BranchDisplay.OnGameStart 中确认已 destroy 所有鸟子对象；确认 `_branches` 列表重置 |
| **DoD** | 连玩 3 局 → 每局树枝清空 → 无旧鸟残留 |

### Task 1.2 — BranchDisplay 自动创建

| 项 | 内容 |
|---|---|
| **预计** | 20 min |
| **内容** | UIManager.CreateUI 中若 BranchDisplay 为 null → 自动 new GameObject + 挂 BranchDisplay + 设 mainCanvas |
| **DoD** | 新建空场景 → 创建 UIManager → CreateUI → BranchDisplay 自动出现 |

### Task 1.3 — 快速连按竞态修复

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | KeyboardDisplay.FlashColor 协程加互斥：同一键再次 Flash 前先 StopCoroutine 旧的；ClearHighlight 后恢复 normalBg |
| **DoD** | 连续快速按不同键 → 闪烁不叠加、不残留颜色 |

### Task 1.4 — Build Settings 配置

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | 确认 Player Settings：Product Name、Company、Icon（icon.png）、Default Resolution 1920×1080、Fullscreen Window；确保所有 Scene 在 Build Settings 中 |
| **DoD** | Build → exe 运行正常 → 分辨率正确 → 图标显示 |

### Task 1.5 — 完整回归测试

| 项 | 内容 |
|---|---|
| **预计** | 40 min |
| **内容** | 覆盖所有 GameOver 路径（DangerKey / Miss=5）各 5 局；测试边缘：多 DangerKey、树枝溢出、空格重开、Combo≥5 视觉 |
| **DoD** | 0 Crash，所有验收标准通过 |

---

## Phase 2 — 音效系统

> 游戏立刻从"静音"变成"有感觉"。

### Task 2.1 — 创建 AudioManager

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | 新建 `AudioManager.cs`（Singleton，挂场景 GameObject）；`Play(AudioClip)` 方法（内部 `AudioSource.PlayOneShot`）；可选 `PlayBGM(AudioClip)` 循环 |
| **DoD** | Play 模式手动调 `AudioManager.Instance.Play(testClip)` → 听到声音 |

### Task 2.2 — AudioManager 接入 GameConfigSO

| 项 | 内容 |
|---|---|
| **预计** | 20 min |
| **内容** | GameConfigSO 新增 `float masterVolume`、`float sfxVolume`、`float bgmVolume`；AudioManager 读取 |
| **DoD** | Inspector 中调 Volume 参数 → 音量实时变化 |

### Task 2.3 — 收集/制作音效资源

| 项 | 内容 |
|---|---|
| **预计** | 60 min |
| **内容** | 从 freesound.org / itch.io 获取 5 个免费音效：`catch.wav`(清脆) / `miss.wav`(低沉) / `danger.wav`(警报) / `newbest.wav`(庆祝) / `jump.wav`(轻弹)；放入 `Assets/Audio/` |
| **DoD** | `Assets/Audio/` 下有 5 个 .wav，Inspector 可预览 |

### Task 2.4 — 接入抓取/错误/Danger 音效

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | GameManager 中对应位置调用：`CatchBird` → catch / 错误键 → miss / DangerKey → danger / 破纪录 → newbest |
| **DoD** | 每种按键事件都有对应音效 |

### Task 2.5 — 接入跳跃音效

| 项 | 内容 |
|---|---|
| **预计** | 20 min |
| **内容** | Bird.OnJumped 事件中或 GM 侧回调中播放 jump 音效 |
| **DoD** | 鸟每次跳跃 → 听到轻弹音 |

### Task 2.6 — BGM 背景音乐

| 项 | 内容 |
|---|---|
| **预计** | 40 min |
| **内容** | 收集 1 首轻快循环 BGM；AudioManager.PlayBGM → 循环播放；游戏开始自动播放，GameOver 音量降低（Ducking） |
| **DoD** | 游戏开始听到 BGM → GameOver 音量自动降低 |

---

## Phase 3 — Doodle Shader 手绘描边

> 参考 `Shader_StyleGuide.md`。按 Phase 分步推进。

### Task 3.1 — 修复 DoodleImage Phase 1（基础 Outline）对全 UI 生效

| 项 | 内容 |
|---|---|
| **预计** | 60 min |
| **内容** | 给按键 Key.prefab 的 Image 换用带 6px 透明边距的 80×80 白色方块 Sprite；给 Branch.prefab 的 Image 换用带 4px 透明边距的 648×22 深色矩形 Sprite；重新引入 MaterialProvider 并正确初始化 |
| **DoD** | 鸟、按键、树枝均可见黑色描边 |

### Task 3.2 — DoodleImage Phase 2（Noise 断续描边）

| 项 | 内容 |
|---|---|
| **预计** | 60 min |
| **内容** | Shader 中添加 Procedural Hash Noise；`step(noise(uv), _OutlineDensity)` 控制描边断续；暴露 `_OutlineDensity`（0~1，默认 0.75） |
| **DoD** | 描边出现随机断裂效果 → 调 Density 0→1 从完全消失到完全连续 |

### Task 3.3 — DoodleImage Phase 3（Outline Jitter）

| 项 | 内容 |
|---|---|
| **预计** | 60 min |
| **内容** | 描边沿法线方向施加 PerlinNoise 时间偏移；暴露 `_OutlineJitter`（0~1，默认 0.5）、`_OutlineSpeed`（默认 2） |
| **DoD** | 描边轻微摆动 → 停帧观察每帧不同 → 不是整体震动 |

### Task 3.4 — DoodleImage Phase 4（Body Jitter）

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | 主体 UV 施加轻微噪声偏移（0.2~0.5px）；暴露 `_BodyJitter`、`_BodySpeed` |
| **DoD** | 主体边缘有微弱手绘感 →

### Task 3.5 — DoodleImage 参数整理

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | GameConfigSO 新增 `DoodleConfig` 子类持有所有参数；MaterialProvider 读取并应用到 Material |
| **DoD** | 所有 7 个 Shader 参数可从 GameConfigSO 调节 |

### Task 3.6 — Doodle TMP Shader Phase 5（文字手绘）

| 项 | 内容 |
|---|---|
| **预计** | 90 min |
| **内容** | 在 TMP_SDF.shader 基础上添加 Face Dilate（字体变粗）+ Noise Distortion（边缘抖动）；暴露 `_FaceDilate`、`_NoiseStrength`、`_NoiseScale`、`_NoiseSpeed`；创建 TMP Material 资产；UIManager.MakeTMP 使用此 Material 替代 TextWobble |
| **DoD** | 文字边缘可见抖动 → TextWobble 脚本可移除 → 文字布局不变 |

### Task 3.7 — 移除 TextWobble

| 项 | 内容 |
|---|---|
| **预计** | 15 min |
| **内容** | 确认 TMP Shader 效果 OK 后：UIManager.MakeTMP 移除 `go.AddComponent<TextWobble>()`；删除 TextWobble.cs |
| **DoD** | 文字仍有抖动效果 → 来源从 CPU 变为 GPU |

### Task 3.8 — 性能验证

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | 同时显示 50 键 + 10 TMP 文本 + 3 树枝 + 30 只树枝鸟 → Profiler GPU 检查 |
| **DoD** | 无明显掉帧（>60fps） |

---

## Phase 4 — 美术资产替换

> "80% 美术 + 20% Shader"

### Task 4.1 — 小鸟 Sprite 设计（3 变体）

| 项 | 内容 |
|---|---|
| **预计** | 90 min |
| **内容** | 手绘板/Procreate/Krita 绘制 3 种小鸟：普通（正面）、惊讶（被抓瞬间）、满足（树枝上）；PNG 导出 256×256，放 `Assets/Sprites/Birds/` |
| **DoD** | 3 个 Sprite 拖到 Inspector 预览正常 |

### Task 4.2 — 小鸟 Sprite 接入

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | Bird.Init 中根据场景切换 Sprite（`spriteNormal` / `spriteCaught` / `spriteBranch`）；FlyToBranch 动画末尾切换到 `spriteBranch` |
| **DoD** | 键盘上是正常鸟 → 被抓瞬间变惊讶 → 树枝上变满足 |

### Task 4.3 — 按键 Sprite 设计

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | 手绘风格圆角方块 80×80（带透明边距）；空格键长条形 400×80；替换 Key.prefab 的 Source Image |
| **DoD** | 37 键均显示手绘圆角方块 |

### Task 4.4 — 树枝 Sprite 设计

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | 不规则手绘树枝线条；3 种变体（略有不同），新树枝随机选用 |
| **DoD** | 树枝不再是矩形，有手绘质感 |

### Task 4.5 — 背景

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | 淡色纸纹背景（Canvas 底层 Image）；或纯白 + 手绘网格线 |
| **DoD** | 背景不再是一片空白 |

### Task 4.6 — 粒子效果（抓取成功）

| 项 | 内容 |
|---|---|
| **预计** | 60 min |
| **内容** | 抓取成功 → 从鸟位置爆出 5-8 颗小星星/羽毛；使用 Unity ParticleSystem 或简单 Sprite 淡出缩放 |
| **DoD** | 抓到鸟时有视觉爆裂感 |

### Task 4.7 — 粒子效果（DangerKey）

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | DangerKey 按下 → 红色墨水/碎纸飞溅粒子 |
| **DoD** | GameOver 瞬间有视觉冲击力 |

---

## Phase 5 — 玩法深度

### Task 5.1 — 分数飘出动画

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | 抓取后 "+25" 文字从鸟位置飘到左上 Score（TMP + DOTween 或协程 Lerp） |
| **DoD** | 每次抓取看到分数飞向计分板 |

### Task 5.2 — 特殊鸟：金色小鸟

| 项 | 内容 |
|---|---|
| **预计** | 60 min |
| **内容** | Bird.SpawnBird 10% 概率生成金色鸟（金色 Sprite）；抓到 Score ×3；特殊音效 |
| **DoD** | 偶尔出现金色鸟 → 抓到得分 3 倍 → 有特殊提示 |

### Task 5.3 — 特殊鸟：幽灵小鸟

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | 5% 概率生成幽灵鸟（半透明 Sprite）；抓到不触发 Miss 重置（温和型）；10s 后自动消失 |
| **DoD** | 幽灵鸟出现，抓到不会改变 Miss 计数 |

### Task 5.4 — 道具：冻结

| 项 | 内容 |
|---|---|
| **预计** | 60 min |
| **内容** | 随机键上出现雪花图标（每 30s）；按对应键激活冻结：5s 内鸟不跳跃、Miss 暂停；冻结期间计时器 UI 变化 |
| **DoD** | 激活冻结 → 鸟静止 → 5s 后恢复 |

### Task 5.5 — 道具：双倍得分

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | 随机键上出现 ×2 图标（每 45s）；激活后 10s 内所有得分 ×2；UI 显示剩余时间 |
| **DoD** | 激活后得分翻倍 → 10s 后恢复 |

### Task 5.6 — 道具：额外生命

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | 随机键上出现 +1 图标；激活后 `maxMissCount` 临时 +1；显示在 Miss 计数器旁边 |
| **DoD** | Miss 5→6，多撑一轮 |

### Task 5.7 — 关卡模式

| 项 | 内容 |
|---|---|
| **预计** | 90 min |
| **内容** | 新建 `LevelConfigSO`（ScriptableObject）：每个 Level 定义起始难度 / DangerKey 数量 / 通关需抓鸟数；LevelManager 管理关卡切换 |
| **DoD** | 3 个关卡，通关后显示"Level Complete" |

### Task 5.8 — 无尽模式排行榜

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | PlayerPrefs 存储 Top 10 分数（JSON 序列化）；排行榜 UI 面板 |
| **DoD** | 破纪录时显示排名；排行榜可查看 |

### Task 5.9 — 分数里程碑

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | Score 达到 100/200/500/1000 时弹出里程碑提示（"Amazing!" / "Legendary!"） |
| **DoD** | 各分数段有祝贺提示 |

---

## Phase 6 — UI/UX 打磨

### Task 6.1 — Score 变化动画

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | Score 变化时数字弹跳缩放；用 DOTween 或协程 `PunchScale` |
| **DoD** | Score 更新→数字弹一下 |

### Task 6.2 — Combo 断裂动画

| 项 | 内容 |
|---|---|
| **预计** | 20 min |
| **内容** | Combo 清零时文字缩小 + 红色闪烁 0.3s |
| **DoD** | 按错键 → Combo 文字抖动消失 |

### Task 6.3 — 场景过渡

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | 简单的黑色 CanvasGroup Fade In/Out（0.5s）；Restart 时用此过渡 |
| **DoD** | 游戏重启时有短暂淡入淡出 |

### Task 6.4 — 设置面板

| 项 | 内容 |
|---|---|
| **预计** | 60 min |
| **内容** | 暂停/主菜单中设置面板：音量滑块、难度选择下拉、语言切换 |
| **DoD** | 调音量 → 实时生效；切换语言 → UI 更新 |

### Task 6.5 — 本地化

| 项 | 内容 |
|---|---|
| **预计** | 90 min |
| **内容** | 创建 `LocalizationTable` ScriptableObject；中英文字符串映射；UIManager 读取本地化表替代硬编码 |
| **DoD** | 切换语言 → 所有 UI 文本（Score/Combo/Danger/GameOver）切换 |

### Task 6.6 — 色盲模式

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | DangerKey 所在键加图案标记（⚠三角形）；BirdKey 所在键加图案标记（★星形）；设置中开关 |
| **DoD** | 开启色盲模式 → 不依赖颜色也能区分 Danger/Bird |

---

## Phase 7 — 菜单 & 流程

### Task 7.1 — 主菜单场景

| 项 | 内容 |
|---|---|
| **预计** | 90 min |
| **内容** | 新建 `MainMenu` Scene；标题（手绘 TMP 字体）；按钮：开始无尽模式 / 关卡模式 / 设置 / 退出；小鸟在标题旁随机飞来飞去 |
| **DoD** | 启动游戏 → 主菜单 → 点击开始 → 进入 MainScene |

### Task 7.2 — 正式结算面板

| 项 | 内容 |
|---|---|
| **预计** | 60 min |
| **内容** | GameOver 从纯文本升级为正式面板：背景半透明遮罩、评级（S/A/B/C 根据分数）、统计数据、再来一局 / 返回主菜单按钮 |
| **DoD** | GameOver → 面板显示评级 + 数据 + 两个按钮 |

### Task 7.3 — 评级系统

| 项 | 内容 |
|---|---|
| **预计** | 20 min |
| **内容** | 根据 Score / Combo / Caught 计算评级：S(≥500) / A(≥300) / B(≥150) / C(<150) |
| **DoD** | 不同分数对应不同评级显示 |

### Task 7.4 — 本地排行榜 UI

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | 主菜单 → "排行榜" 按钮 → 显示 Top 10（日期/分数/Combo/评级）；可清空 |
| **DoD** | 排行榜正确显示历史 Top 10 |

### Task 7.5 — 教程引导

| 项 | 内容 |
|---|---|
| **预计** | 30 min |
| **内容** | 3 步引导面板（半透明遮罩 + 高亮目标）：① 这是小鸟 → ② 按下它所在的键抓住它 → ③ 不要按右上 DangerKey；首次启动触发 |
| **DoD** | 首次启动 → 教程 3 步走完 → 记住已完成 → 不再弹出 |

---

## Phase 8 — 多平台发布

### Task 8.1 — WebGL 构建

| 项 | 内容 |
|---|---|
| **预计** | 60 min |
| **内容** | Build Settings 切换到 WebGL；Input.GetKeyDown 在 WebGL 中正常工作（确认）；`Screen.fullScreen` 处理；构建 → itch.io 上传 |
| **DoD** | 浏览器中可玩 → 所有功能正常 |

### Task 8.2 — WebGL 适配

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | 响应式 Canvas 缩放；移动端触摸→虚拟键盘输入（`Input.GetMouseButtonDown` + 射线检测）；Loading 进度条 |
| **DoD** | 手机浏览器可玩 → 触摸键盘按键有效 |

### Task 8.3 — Steamworks 集成

| 项 | 内容 |
|---|---|
| **预计** | 60 min |
| **内容** | Steamworks.NET SDK；4 个成就（Combo×10 / Score 500 / 抓 100 只鸟 / 全通关）；Steam 上传配置 |
| **DoD** | Steam 客户端中可运行 → 成就可解锁 |

### Task 8.4 — Steam 商店页面准备

| 项 | 内容 |
|---|---|
| **预计** | 45 min |
| **内容** | 截图 5 张、宣传图（Capsule 460×215 / 616×353）；简短描述 |
| **DoD** | Steamworks 后台提交所有素材 |

---

## 里程碑

| 里程碑 | 完成 Phase | 预计累计工时 |
|---|---|---|
| **Alpha 1** | Phase 1（修复） | 3 h |
| **Alpha 2** | Phase 2（音效） | 7 h |
| **Beta** | Phase 3 + 4（Shader + 美术） | 27 h |
| **RC** | Phase 5（玩法） | 37 h |
| **Release** | Phase 6 + 7（UI + 菜单） | 47 h |
| **Multi-Platform** | Phase 8（多平台） | 51 h |

---

*最后更新：2026-07-01*
