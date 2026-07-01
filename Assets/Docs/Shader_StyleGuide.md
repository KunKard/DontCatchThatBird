# UI Hand-Drawn Doodle Shader
> Shader Technical Requirement Document (STRD)
>
> Version: MVP v1.0
>
> Engine: Unity 2022 LTS (URP)
>
> Target Platform: Windows PC
>
> Applicable Objects:
> - Unity UI Image
> - TextMeshPro UGUI
>
> Author: Kard

---

# 1. 项目目标

本 Shader 的目标不是实现普通 UI 描边，而是复现类似 **「线条小狗」(Doodle Dog)** 的手绘风格。

参考特点：

- 黑色粗描边
- 描边不连续
- 描边具有轻微抖动
- 主体具有轻微手绘动画感
- 黑白极简
- 画面具有儿童涂鸦感

最终效果参考：

- 白色主体
- 粗黑线条
- 不规则轮廓
- 每一帧略微变化
- 像手绘动画而不是电脑渲染

重点：

**不要制作普通 Unity Outline Shader。**

目标是：

> 手绘动画（Hand-drawn Animation）

而不是：

> UI Outline。

---

# 2. 美术风格

整体参考：

- 线条小狗
- Doodle
- 手绘儿童绘本
- Marker Pen
- 蜡笔轮廓

风格关键词：

- Hand Drawn
- Rough Line
- Uneven Outline
- Doodle
- Sketch
- Ink

不允许出现：

- 科技风
- 发光
- Bloom
- 霓虹
- 漫画描边
- 矢量风

---

# 3. Shader 类型

需要开发两个 Shader：

## Image Shader

适用于：

- Unity UI Image

---

## TMP Shader

适用于：

- TextMeshPro UGUI

两者风格保持一致。

---

# 4. Image Shader 功能需求

## 4.1 保持原图

Shader 必须保持：

Sprite 原本颜色。

不能改变：

- 色相
- 饱和度
- Alpha

---

## 4.2 自动生成描边

根据 Alpha 自动生成：

黑色 Outline。

参数：

Outline Width

范围：

0~10 px

默认：

5 px

颜色：

纯黑

---

## 4.3 描边不是连续的

这是整个 Shader 最重要的部分。

描边必须：

随机断开。

类似：

████

██

██████

█

███

而不是：

████████████████

实现方式建议：

使用：

Procedural Noise

控制：

Outline Mask。

参数：

Outline Density

范围：

0~1

默认：

0.75

---

## 4.4 Outline 抖动

描边不是静止。

要求：

每一帧：

沿法线方向

轻微偏移。

振幅：

0.3~1 px

建议：

Perlin Noise

Simplex Noise

不要：

Random()

否则：

会闪烁。

参数：

Outline Jitter

默认：

0.5

Outline Speed

默认：

2

---

## 4.5 主体轻微抖动

主体也需要：

轻微变化。

不是：

整体移动。

而是：

UV

Vertex

Alpha Edge

产生：

0.2~0.5 px

左右变化。

类似：

手绘动画。

参数：

Body Jitter

Body Speed

---

## 4.6 Noise

Noise 必须：

Procedural。

禁止：

每帧生成 Noise Texture。

建议：

Hash

Value Noise

Perlin

Simplex

均可。

需要：

Noise Scale

参数。

---

# 5. TMP Shader 功能需求

TMP 不需要：

真正 Outline。

目标：

文字更像：

马克笔写出来。

实现：

Face Dilate

+

Vertex Offset

+

Noise Distortion

效果：

字体：

略微粗。

边缘：

轻微抖动。

每个字符：

保持稳定。

不要：

字符跳动。

不要：

改变排版。

不要：

修改 Character Position。

---

# 6. Material 参数

Image Shader：

| 参数 | 默认值 |
|--------|--------|
| Outline Width | 5 |
| Outline Color | Black |
| Outline Density | 0.75 |
| Outline Jitter | 0.5 |
| Outline Speed | 2 |
| Body Jitter | 0.25 |
| Body Speed | 1 |
| Noise Scale | 12 |

---

TMP：

| 参数 | 默认值 |
|--------|--------|
| Face Dilate | 0.15 |
| Noise Strength | 0.2 |
| Noise Scale | 15 |
| Noise Speed | 2 |

---

# 7. 技术要求

Unity：

2022 LTS

Pipeline：

URP

开发方式：

HLSL

不要：

Shader Graph。

原因：

方便：

代码维护

性能优化

功能扩展。

---

# 8. 性能要求

需要支持：

- 数十个 Image
- 数十个 TMP

同时使用。

要求：

Shader 无明显掉帧。

禁止：

每帧生成 Texture。

禁止：

CPU 更新 Mesh。

所有动画：

GPU 完成。

---

# 9. 可扩展性

Shader 代码需预留扩展接口。

未来计划增加：

- Hover 抖动
- 点击挤压
- Cartoon Ink
- Hover Highlight
- 呼吸动画
- Outline 波动
- Line Thickness Animation

要求：

代码模块化。

不要：

所有逻辑写在一个 Fragment 中。

---

# 10. 不需要实现

MVP 不实现：

- Glow
- Shadow
- Bloom
- Blur
- Outer Glow
- Soft Outline
- Neon
- Gradient Outline

---

# 11. 推荐开发流程

请不要一次性生成完整 Shader。

按以下阶段开发。

---

## Phase 1

普通 Outline

目标：

根据 Alpha

生成黑色描边。

完成后：

暂停。

说明：

实现原理。

等待确认。

---

## Phase 2

加入 Noise。

目标：

描边：

不连续。

完成后：

暂停。

输出：

参数说明。

---

## Phase 3

加入 Outline Jitter。

目标：

描边：

轻微摆动。

不是：

整体移动。

完成后：

暂停。

说明：

性能影响。

---

## Phase 4

加入 Body Jitter。

目标：

主体：

轻微变化。

整体：

更像手绘动画。

完成后：

暂停。

---

## Phase 5

开发 TMP Shader。

要求：

风格一致。

文字：

更粗。

边缘：

轻微抖动。

---

# 12. 验收标准

Image Shader：

- 保持 Sprite 原色
- 自动生成黑色粗描边
- 描边具有随机断续效果
- 描边轻微抖动
- 主体轻微抖动
- 参数均可实时调节

TMP Shader：

- 字体整体变粗
- 字体边缘具有轻微手绘抖动
- 不影响文字布局
- 不影响 TMP 自动换行

性能：

- 50+ UI 元件同时使用无明显掉帧
- 无 CPU Mesh 更新
- 无 Runtime Texture 生成

---

# 13. Agent 开发要求

你的身份是一名资深 Unity Graphics Programmer。

目标不是快速完成 Shader，而是实现一套具有扩展性的 UI 手绘渲染方案。

开发过程中必须遵循以下原则：

1. 不要一次性生成所有代码。
2. 每完成一个 Phase 停止并等待确认。
3. 每个 Phase 必须解释实现原理。
4. 每个 Phase 必须说明性能影响。
5. 优先保证视觉效果，再考虑优化。
6. Shader 代码保持模块化，便于未来扩展。
7. 不允许使用 Shader Graph。
8. 所有动画效果必须在 GPU 侧完成。

---

# 14. Shader 设计原则

请不要尝试完全依靠 Shader 生成「线条小狗」风格。

最终视觉应遵循：

**80% 美术资源 + 20% Shader 增强**。

也就是说：

- PNG 素材本身应包含手绘感、粗细变化和不规则轮廓；
- Shader 负责增强这种风格，包括轻微抖动、线条呼吸感和整体生命力；
- Shader 不负责凭空生成复杂手绘轮廓。

这样的实现方式更符合真实游戏项目的美术管线，也更容易获得接近参考图的最终效果。

最终目标：

实现一套能够高度还原「线条小狗」风格的 Unity UI Shader，为整个游戏提供统一的手绘视觉表现。