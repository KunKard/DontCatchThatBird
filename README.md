# Don't Catch That Bird — 别抓那只鸟

**金海豚 GameJam 2026 · 单人作品 · 开发周期 2 天**

一只简笔画小鸟在虚拟键盘上疯狂跳跃，你的任务是按下它所在的键抓住它，同时绝对避开危险键。按错一个键，游戏结束。

---

## 玩法

- 虚拟键盘 37 个键（26 字母 + 10 数字 + 空格），纯视觉展示
- 小鸟随机停在某个键上，每隔 0.6~2.5 秒自动跳到新键
- **按下鸟所在的键** → 抓取成功，得分，鸟飞到树枝上
- **按下 DangerKey**（右上红色文字提示）→ 全屏红闪，Game Over
- **按错其他键** → Combo 清零，Miss +1
- Miss 达到 5 次 → Game Over

### 计分

```text
每次抓取 = 10 + 当前 Combo × 5
```

| Combo | 视觉反馈 |
|---|---|
| 0~4 | 黑色 |
| 5~9 | 蓝色 + `!` |
| 10~14 | 橙色 + `!!` |
| 15+ | 红色 + `!!!` |

### 难度曲线

| 分数区间 | 跳跃间隔 | DangerKey 数量 |
|---|---|---|
| 0~49 | 2.0~2.5s | 1 个 |
| 50~99 | 1.5~2.0s | 1 个 |
| 100~199 | 1.0~1.5s | **2 个** |
| 200+ | 0.6~1.0s | **3 个** |

### 道具系统

每抓取 8~12 只小鸟出现一次道具（道具持续期间的抓取不计入计数），分数越高道具停留越短。

| 道具 | 效果 |
|---|---|
| ❄ 冻结（雪花） | 鸟静止 5 秒 |
| 💗 无敌（心形） | 4 秒内不扣 Combo / Miss，按到 DangerKey 也不会死 |

### 金鸟

Combo 达到 30 的倍数时，下一只鸟变为金色，抓到得分 ×2，爆出华丽金色粒子。

---

## 操作

| 按键 | 功能 |
|---|---|
| 按鸟所在键 | 抓取 |
| 按 DangerKey | Game Over |
| 按其他键 | Combo 清零 + Miss |
| 空格（Ready） | 开始游戏 |
| 空格（GameOver） | 重新开始 |
| Tab | 帮助面板 |
| Esc | 退出游戏 |
| Alt | 显示鼠标 |

---

## 技术栈

| 类别 | 说明 |
|---|---|
| 引擎 | Unity 2022 LTS |
| 脚本 | 13 个 C# 文件，~2,500 行 |
| 架构 | GameManager (Singleton) + 组件解耦 |
| 配置 | ScriptableObject 数据驱动 |
| UI | TextMeshPro + STHUPO 繁圆字体 |
| 音效 | 6 种 SFX + 随机 BGM + 程序生成占位 |
| 渲染 | 黑白手绘简笔画 + Perlin Noise 抖动 |

### 项目结构

```
Assets/Scripts/
├── GameManager.cs          # 主控制器（状态机/输入/计分/道具）
├── GameConfigSO.cs         # 数据配置（所有参数可调）
├── Bird.cs                 # 小鸟实体
├── KeyboardDisplay.cs      # 虚拟键盘生成与高亮
├── UIManager.cs            # UI 文本 + 帮助面板
├── BranchDisplay.cs        # 树枝管理
├── AudioManager.cs         # 音效管理
├── ParticleManager.cs      # 粒子效果
├── BackgroundGenerator.cs  # 程序化网格背景
├── TextWobble.cs           # 文字手绘抖动
├── KeyCodeUtility.cs       # KeyCode 工具
├── KeyboardGridHelper.cs   # 物理键盘布局
└── MaterialProvider.cs     # Doodle Shader 材质
```

---

## 版本历史

| 版本 | 日期 | 备注 |
|---|---|---|
| v0.1 | 2026-07-01 | MVP 核心玩法 |
| v0.2 | 2026-07-01 | Shader + 音效 + Ready 界面 |
| v0.3 | 2026-07-02 | 重构 + 中文 UI + 道具 + 金鸟 |
| v1.0 | 2026-07-02 | 正式版 |

---

## 文档

| 文档 | 说明 |
|---|---|
| [game-rules.md](Assets/Docs/game-rules.md) | 完整游戏规则 |
| [design.md](Assets/Docs/design.md) | 游戏设计文档 |
| [dev-plan.md](Assets/Docs/dev-plan.md) | 开发计划 |
| [Architecture.md](Assets/Docs/Architecture.md) | 代码架构 |
| [project-status.md](Assets/Docs/project-status.md) | 功能清单 |

---

## License

MIT
