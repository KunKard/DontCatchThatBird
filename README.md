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
- **双鸟模式**：1000 分后 10% 概率出现两只鸟，0.2 秒内同时抓到 → 得分 ×3
- **金鸟**：Combo 50/100/150... 时出现，抓到 → 得分 ×2

### 道具

| 图标 | 道具 | 效果 |
|---|---|---|
| ❄ 雪花 | 冻结 | 鸟静止 5 秒，全屏淡蓝遮罩 |
| 💗 心形 | 无敌 | 4 秒内按错不扣、DangerKey 不死，全屏淡粉遮罩 |

道具每抓 8~12 只鸟出现一次，分数越高停留越短（2.5s → 1s）。

### 计分

```text
每次抓取 = 10 + Combo × 5
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
| 按鸟所在的键 | 抓取 + 得分 + 粒子效果 |
| 按 DangerKey | Game Over + 全屏红闪 |
| 按其他键 | Combo 清零 + Miss +1 |
| 按道具键 | 拾取道具 |
| Tab | 打开/关闭帮助面板 |
| 空格（Ready） | 开始游戏 |
| 空格（GameOver） | 重新开始 |
| 按住 Alt | 显示鼠标光标 |

---

## 技术

| 类别 | 详情 |
|---|---|
| 引擎 | Unity 2022 LTS |
| 平台 | Windows PC |
| 脚本 | 13 个 C# 文件 |
| 代码行数 | ~2,500 行 |
| 配置 | ScriptableObject 数据驱动（`GameConfigSO`） |
| UI | TextMeshPro + STHUPO 中文字体 |
| 音效 | AudioManager + 5 SFX + 随机 BGM + 占位 beep |
| 架构 | GameManager (Singleton) + 组件解耦 |

### 项目结构

```
Assets/Scripts/
├── GameManager.cs          # 主控制器（状态机/输入/计分/道具）
├── GameConfigSO.cs         # 数据配置
├── Bird.cs                 # 小鸟实体（跳跃/DangerKey/飞行）
├── KeyboardDisplay.cs      # 虚拟键盘生成与高亮
├── UIManager.cs            # UI 文本 + 帮助面板
├── BranchDisplay.cs        # 树枝管理（多根/随机落点）
├── AudioManager.cs         # 音效管理 + BGM
├── ParticleManager.cs      # 粒子效果 + 分数飘出
├── BackgroundGenerator.cs  # 程序化网格背景
├── TextWobble.cs           # 文字手绘抖动
├── KeyCodeUtility.cs       # KeyCode 工具
├── KeyboardGridHelper.cs   # 物理键盘布局（邻近选键）
└── MaterialProvider.cs     # Doodle Shader 材质
```

完整架构文档见 [Architecture.md](Assets/Docs/Architecture.md)。

---

## 版本历史

| 版本 | 日期 | 备注 |
|---|---|---|
| v0.1 | 2026-07-01 | MVP 核心玩法 |
| v0.2 | 2026-07-01 | Doodle Shader + 音效 + Ready 界面 |
| v0.3 | 2026-07-02 | 重构 + 中文 UI + QWERTY DangerKey + 道具系统 |
| **v1.0** | **2026-07-02** | **正式版** — 双鸟 + 金鸟 + QA 修复 + 帮助面板 |

---

## 文档

| 文档 | 说明 |
|---|---|
| [game-rules.md](Assets/Docs/game-rules.md) | 完整游戏规则 |
| [project-status.md](Assets/Docs/project-status.md) | 项目功能清单 |
| [design.md](Assets/Docs/design.md) | 游戏设计文档 |
| [dev-plan.md](Assets/Docs/dev-plan.md) | 开发计划 |
| [post-mvp-plan.md](Assets/Docs/post-mvp-plan.md) | 后续开发计划 |
| [Architecture.md](Assets/Docs/Architecture.md) | 代码架构 |
| [TODO.md](Assets/Docs/TODO.md) | 后续功能清单 |

---

## License

MIT
