---
name: platform-ui
description: 平台网页视觉——GameTech 令牌、.ui-* 原语与高保真屏幕原型；改大厅或后台外观时查
metadata:
  type: doc
  status: 设计中
---

# 平台网页视觉

令牌与 `.ui-*` 原语的真值在 `web/src/styles/`。令牌来自视频仓 `@lumio/video-ds` 的 **GameTech** 主题（与 `blog.lumio.games`、GitHub 徽章同一套身份色），按 16px 网页尺度重写，不是把视频 CSS 原样搬进来。

屏幕级外观与交互的真值是 [`web/docs/prototype/Lumio Prototype.dc.html`](../../../web/docs/prototype/Lumio%20Prototype.dc.html)（说明见 [`web/docs/prototype/README.md`](../../../web/docs/prototype/README.md)，决策 0005）。令牌 / 原语活样张：[`web/design-preview.html`](../../../web/design-preview.html)。实现时用 `.ui-*` 在 React 里重建，不复制原型 HTML。

## 背景 / 目标

- 视频 DS 是 4:3 / 1440×1080 的播与录：`Stage` / `Deck` / `Reveal`、228px 爆点字、`user-select: none`、没有 hover。
- 本仓是大厅 / 注册登录 / 反馈 / 运营后台。需要表单、焦点环、表格、空状态，字号要从「投影远看」收回「一臂距离」。
- 游戏内美术（动物玩偶派对三方向比稿）仍在 `LumioGame/docs/specs/art-style-pitch.md`，**不锁进平台壳**。

## 设计

### 迁什么

| 来源 | 落到本仓 | 网页上怎么用 |
| --- | --- | --- |
| GameTech 身份色 `primary / mint / ink / field` | `--ui-*`（`tokens.css`） | 全站色板 |
| 纸片卡片（不透明白底 + 实色描边 + 克制投影，不做玻璃） | `.ui-card` | 游戏卡、表单、对话框 |
| `BrandMark` 3×3 十字暗纹 | `.ui-brand` | 顶栏 Logo |
| `gt-tone` / 等距体素 / 淡网格 | `.ui-tone` `.ui-voxel` `.ui-grid-bg` | 大厅点缀；体素不是图标系统 |
| 数字渐变字（primary-d → mint） | `.ui-stat__n` | 后台看板数字 |
| Noto Sans SC + Inter | `--ui-font` / `--ui-font-num` | 中文正文、等宽数字 |

### 不迁什么

视频画布与播控：`Stage` `Deck` `Scene` `Reveal` `Sequence` `SplitText` `CountUp`。
视频版式：`SplitStage` `PhoneSlot` `BleedPair`、4:3/9:16 字阶、`--lm-fs-hero: 228px`。
视频内容件：`ChatLog` `VersusPanel` `OrgChart` `NameTag` `StickyNote` `TitleCard`。
白板主题（暖纸 `#F7F5EF` + 马克笔蓝 `#1F5FD1`）——那是短视频母题，不是平台壳。
`Idle` 常驻浮动、发光球、Silkscreen 点阵字、Chakra Petch / 得意黑爆点字。

### 网页补上的（视频里没有）

按钮（含 hover / disabled）、输入框焦点环、校验错误、状态胶囊、表格、空状态、顶栏、头像选择器、封禁确认。类名 `.ui-*`。功能页布局仍走 CSS Modules，不引入 UI 框架。

### 两档密度

- 默认（玩家面）：大厅卡片、宽松留白。
- `[data-surface="admin"]`：同一色板，圆角与标题收一档。后台看起来是同一栋楼的安静层，不是另一套产品。

### 文件

| 文件 | 职责 |
| --- | --- |
| `web/src/styles/tokens.css` | 色 / 字 / 空 / 圆角 / 动效时长 |
| `web/src/styles/base.css` | `html`/`body`、标题、焦点、减弱动效 |
| `web/src/styles/primitives.css` | 导航、卡片、按钮、表单、表格、看板 |
| `web/src/styles/deco.css` | 品牌标、体素、色调块 |
| `web/src/styles/index.css` | 以上四份的入口；SPA `main.tsx` 只 import 这一份 |
| `web/design-preview.html` | 令牌与原语活样张（组件墙，不是页面稿） |
| `web/docs/prototype/Lumio Prototype.dc.html` | 高保真可点原型（屏幕级真值；需同目录 `support.js`） |
| `web/docs/prototype/README.md` | 原型屏幕、交互、令牌与演示账号说明 |
| `web/docs/prototype/reference/` | 玩家端静态画板与早期线框，只作比对 |

P0-1 搭 `web/` 脚手架时必须保留 `web/src/styles/` 与 `web/design-preview.html`；`main.tsx` 全局引入 `src/styles/index.css`。后续 SPA 页对照原型，不对照组件墙。

### 大厅动效

大厅首页采用“场景化大厅 + 在线状态轨道”：Hero 使用分层体素块和网格背景，游戏封面使用 CSS 几何微场景，卡片入场按 80ms 错峰，在线状态使用低频方点脉冲。共享时序令牌位于 `web/src/styles/tokens.css`，关键帧位于 `web/src/styles/deco.css`；动效只改变 `transform`、`opacity` 和 `box-shadow`，并遵守 `prefers-reduced-motion`。详细屏幕规格见 [`docs/specs/2026-09-05-lobby-motion-design.md`](../../../docs/specs/2026-09-05-lobby-motion-design.md)。

## 待解决

- 12 张默认头像资产（P2-2 占位 SVG）；活样张里的 emoji 只是占位，不进生产。
- 字体是否改为自托管（现在跟视频 DS 一样走 Google Fonts `@import`）。
- 游戏封面图（`cover_url`）的画幅与安全区。

## 相关

- [决策 0004](../../decisions/0004-platform-ui-from-gametech.md)、[决策 0005](../../decisions/0005-platform-ui-hifi-prototype.md)
- [`platform.md`](platform.md)、[`lobby-launch.md`](lobby-launch.md)、[`admin-analytics.md`](admin-analytics.md)、[`feedback.md`](feedback.md)、[`account.md`](account.md)
- [`standards/code-style.md`](../standards/code-style.md)
- 开工提示词：[`plans/2026-09-04-platform-spa-from-prototype-prompt.md`](../../plans/2026-09-04-platform-spa-from-prototype-prompt.md)
