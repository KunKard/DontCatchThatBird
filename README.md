# Don't Catch That Bird

> **金海豚 GameJam 2026 · 单人作品**
>
> 一只小鸟在键盘上跳来跳去——抓住它！但千万避开 DangerKey。

---

## 玩法

- 一只简笔画小鸟随机停在虚拟键盘的某个按键上
- 每隔 1~2 秒自动跳到新键
- **按下鸟所在的键** → 抓住！得分，鸟飞到树枝上
- **按下 DangerKey**（右上红色提示）→ 立刻 Game Over
- **按错键** → Combo 清零
- **鸟连续跳跃 5 次没抓到** → 小鸟飞走了，Game Over
- 分数越高，鸟跳得越快；抓到一定分数后 DangerKey 数量增加

### 计分

```text
Score += 10 + Combo × 5
```

连续快速抓取，Combo 越滚越高。

---

## 操作

| 操作 | 效果 |
|---|---|
| 按鸟所在的键 | 抓取成功，得分 |
| 按 DangerKey | Game Over |
| 按其他键 | Combo 清零 |
| 空格键（Game Over 后） | 重新开始 |

仅支持**物理键盘**，虚拟键盘为纯视觉展示。

---

## 技术

| 类别 | 详情 |
|---|---|
| 引擎 | Unity 2022 LTS |
| 平台 | Windows PC |
| 渲染管线 | Built-in / URP 兼容 |
| 脚本 | 8 个 C# 文件（经过 9 轮重构） |
| 配置 | ScriptableObject 数据驱动 (`GameConfigSO`) |
| UI | TextMeshPro |
| 架构 | GameManager (Singleton) + Bird (回调注入) + KeyboardDisplay + BranchDisplay + UIManager |

### 项目结构

```text
Assets/Scripts/
├── GameManager.cs          # 游戏主控制器
├── GameConfigSO.cs         # 数据配置
├── Bird.cs                 # 小鸟实体
├── KeyboardDisplay.cs      # 虚拟键盘生成与高亮
├── UIManager.cs            # UI 文本与 GameOver
├── BranchDisplay.cs        # 树枝管理与鸟排列
├── TextWobble.cs           # 文字手绘抖动
└── KeyCodeUtility.cs       # 工具类
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

## 文档

| 文档 | 说明 |
|---|---|
| [design.md](Assets/Docs/design.md) | 游戏设计文档 |
| [dev-plan.md](Assets/Docs/dev-plan.md) | 开发计划与 Task 拆分 |
| [Architecture.md](Assets/Docs/Architecture.md) | 代码架构文档 |
| [TODO.md](Assets/Docs/TODO.md) | 后续功能清单 |
| [Shader_StyleGuide.md](Assets/Docs/Shader_StyleGuide.md) | Doodle Shader 需求规格 |

---

## License

MIT
