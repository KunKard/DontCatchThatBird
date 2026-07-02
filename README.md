# Don't Catch That Bird

> **金海豚 GameJam 2026 · 单人作品**
>
> 一只小鸟在键盘上跳来跳去——抓住它！但千万避开 DangerKey。

---

## 玩法

- 一只简笔画小鸟随机停在虚拟键盘的某个按键上
- 每隔 1~2 秒自动跳到新键
- **按下鸟所在的键** → 抓住！得分，鸟飞到树枝上
- **按下 DangerKey**（右上红色提示）→ 立即 Game Over，全屏红色闪烁
- **按错键** → Combo 清零 + Miss 计数 +1
- **小鸟连续跳跃 5 次没抓到** → Game Over
- **小鸟跳到 DangerKey 上** → 不计 Miss，但按该键立即 Game Over ⚠️
- 分数越高，鸟跳得越快；DangerKey 数量随分数增加
- DangerKey 更有可能出现在小鸟物理键盘位置的四周

### 计分

```text
Score += 10 + Combo × 5
```

| Combo | 颜色 | 特效 |
|---|---|---|
| 0~4 | 黑色 | — |
| 5~9 | 🔵 蓝 #007AFF | `!` 字号 34 |
| 10~14 | 🟠 橙 #FF8000 | `!!` 字号 36 |
| 15+ | 🔴 红 #FF3F00 | `!!!` 字号 38 |

---

## 操作

| 操作 | 效果 |
|---|---|
| 按鸟所在的键 | 抓取成功，彩色粒子爆出 |
| 按 DangerKey | Game Over + 全屏红闪 |
| 按其他键 | Combo 清零 + Miss +1 |
| 空格键（Game Over 后） | 重新开始 |
| 按住 Alt | 显示鼠标光标 |

---

## 技术

| 类别 | 详情 |
|---|---|
| 引擎 | Unity 2022 LTS |
| 平台 | Windows PC |
| 脚本 | 12 个 C# 文件 |
| 配置 | ScriptableObject 数据驱动 (`GameConfigSO`) |
| UI | TextMeshPro + STHUPO 中文字体 |
| 音效 | AudioManager + 6 种 SFX + 随机 BGM |
| 架构 | GameManager (Singleton) + Bird (回调注入) + 模块化组件 |

### 项目结构

```text
Assets/Scripts/
├── GameManager.cs          # 游戏主控制器（状态机/输入/计分）
├── GameConfigSO.cs         # 数据配置
├── Bird.cs                 # 小鸟实体（跳跃/DangerKey判定/飞行）
├── KeyboardDisplay.cs      # 虚拟键盘生成与高亮
├── UIManager.cs            # UI 文本与 GameOver
├── BranchDisplay.cs        # 树枝管理（多根/随机落点）
├── AudioManager.cs         # 音效管理 + BGM
├── ParticleManager.cs      # 粒子效果 + 全屏闪烁
├── MaterialProvider.cs     # Doodle Shader 材质
├── BackgroundGenerator.cs  # 程序化网格背景
├── TextWobble.cs           # 文字手绘抖动
├── KeyCodeUtility.cs       # KeyCode 工具
└── KeyboardGridHelper.cs   # 物理键盘布局（DangerKey 邻近选键）
```

完整架构文档见 [Architecture.md](Assets/Docs/Architecture.md)。

---

## 开发原则

- **KISS** — 每个脚本职责单一
- **YAGNI** — 不预留"以后可能需要"的接口
- **数据驱动** — 所有参数进 `GameConfigSO`
- **80% 美术 + 20% Shader** — 手绘风格由素材主导

---

## 构建

1. 用 Unity 2022 LTS 打开项目
2. `File → Build Settings → Build`
3. 选择输出目录，点击 Build
4. 运行生成的 `.exe`

---

## 版本

| 版本 | 日期 | 备注 |
|---|---|---|
| v0.1 | 2026-07-01 | MVP 核心玩法 |
| v0.2 | 2026-07-01 | Doodle Shader + 音效 + 背景 + Ready 界面 |
| v0.3 | 2026-07-02 | 重构完成 + 中文 UI + QWERTY DangerKey + 粒子特效 |

---

## 文档

| 文档 | 说明 |
|---|---|
| [design.md](Assets/Docs/design.md) | 游戏设计文档 |
| [dev-plan.md](Assets/Docs/dev-plan.md) | 开发计划与 Task 拆分 |
| [post-mvp-plan.md](Assets/Docs/post-mvp-plan.md) | 后续完整开发计划 |
| [Architecture.md](Assets/Docs/Architecture.md) | 代码架构文档 |
| [TODO.md](Assets/Docs/TODO.md) | 后续功能清单 |

---

## License

MIT
