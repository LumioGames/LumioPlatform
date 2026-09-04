> 设计参考，非生产代码：本目录是 2026-09-04 玩家端 + 运营后台高保真可点原型，落仓供对照实现。令牌真值仍是 `web/src/styles/`（[决策 0004](../../../.spec/decisions/0004-platform-ui-from-gametech.md)）；屏幕级外观与交互以本目录 `Lumio Prototype.dc.html` 为准（[决策 0005](../../../.spec/decisions/0005-platform-ui-hifi-prototype.md)）。索引见 [`.spec/knowledge/features/platform-ui.md`](../../../.spec/knowledge/features/platform-ui.md)。
>
> 打开主原型：用浏览器直接打开 `Lumio Prototype.dc.html`（需同目录 `support.js`）。`reference/` 下两份画板的脚本路径已改为 `../support.js`。

# Handoff: Lumio 游戏平台（玩家端 + 运营后台）

## Overview
Lumio 是开源体素游戏平台的官网 + 应用：唯一账号权威、游戏大厅、房间人数与邀请、反馈通道、Roadmap，以及管理员后台（看板 / 用户 / 游戏目录 / 反馈队列 / 平台设置）。本包内的原型覆盖全部界面与主要交互流程。

## About the Design Files
本包内的 HTML 文件是**设计参考**（用 HTML 写的可交互原型），用于表达外观与行为，**不是可直接上线的生产代码**。任务是在目标代码库的既有环境里**重建这些设计**（本项目为 ASP.NET Core 10 + React 19 SPA，见 `.spec/knowledge/features/platform-ui.md` 与 `web/src/styles/*`），沿用其既有组件与约定。样式令牌已与 `web/src/styles/tokens.css` / `primitives.css` / `deco.css` 对齐，实现时应直接使用那些 CSS 类，而不是复制原型里的行内样式。

## Fidelity
**High-fidelity。** 颜色、字阶、间距、圆角、阴影、交互与文案均为最终稿。`reference/Lumio Wireframes.dc.html` 是早期低保真结构探索（8 组屏幕 × 3 方案），仅作决策记录；`reference/Lumio Hi-fi Player v2.dc.html` 是静态版画板（每屏一帧、含标注），便于逐屏比对。主交付物是 `Lumio Prototype.dc.html`（可点击全流程）。

## Screens / Views

所有页面共用：顶部 `.ui-nav`（sticky，高 56px，背景 `rgba(249,251,255,.93)` + `blur(8px)`，下边框 `1px solid #e7ecf6`），主内容宽度 `min(100% - 64px, 1120px)` 居中，后台为 `1344px`。页面进入动画 `lm-in`（8px 上移 + 淡入，320ms ease）。

### 1. TopNav
- **用途**：全站导航与身份状态。
- **布局**：左侧品牌锁定（32×32 圆角 9px 渐变方块 `linear-gradient(150deg,#7c8cff,#9aa6ff)`，内含 3×3 像素点阵，白色、偶数点 `opacity .35`；右侧两行文字 `Lumio` 800/15px Inter + `GAMES` 700/8px letter-spacing .22em `#9aa6be`）。右侧导航项 `gap:6px`。
- **导航项**：`大厅` `反馈` `Roadmap`（按钮，选中态背景 `#eaf0ff`、文字 `#6171f0`、圆角 10px、padding 6px 12px；未选中 `#6b7894` 无底色）；`开源引擎 ↗`（外链）；`开发者交流群`（按钮，打开弹窗）。
- **身份区**：
  - 游客：`登录`（白底 `#fff` + `1px solid #e7ecf6`，min-height 36px，圆角 12px）+ `注册`（渐变主按钮）。
  - 已登录：头像胶囊（28px 圆头像 `#eaf0ff` + 用户名）→ `/me`；`退出`（`#9aa6be` 文本按钮）。
  - 管理员额外显示 `后台` 胶囊（`#eaf0ff` / `1px solid #c9d3f5` / `#6171f0`）→ `/admin`；player 访问后台走 403 页。

### 2. 大厅 `/`
- **Hero**：背景 `linear-gradient(150deg,#eef1ff,#e6f7f2)` + 32px 网格线 `rgba(124,140,255,.1)`，`mask-image: linear-gradient(160deg,#000 8%,transparent 62%)`；右侧 4 个漂浮方块（54/36/30/22px，色 `#7c8cff` `#5de2c6` `#ffb86b` `#ff7ea6`，`lm-float` 5.4–7.5s 循环，rotate(45deg) ±9px）。
  内容：96×96 圆角 26px 大 logo（同品牌渐变 + 3×3 点阵，阴影 `0 26px 52px -18px rgba(124,140,255,.8)`），主标题「开源体素游戏平台」900/44px `#243056`，副行 `LUMIO GAMES` 700/14px Inter letter-spacing .18em `#9aa6be`。
  三个入口（min-height 48px，圆角 14px）：`▶ 开始游戏`（渐变主按钮）、`{ } 开源引擎 ↗`（白底）、`◷ Roadmap`（`#f3f6ff` / `1px solid #c9d3f5` / `#6171f0`）。**Hero 不放统计数字与说明段落。**
- **游戏网格**：`repeat(auto-fill,minmax(20rem,1fr))`，gap 24px。卡片：`#fff` + `1px solid #e7ecf6` + 圆角 16px + 阴影 `0 1px 2px rgba(30,42,58,.04),0 14px 40px -16px rgba(53,68,120,.2)`（首发卡用 pop 阴影 `0 22px 56px -18px rgba(53,68,120,.42)` 并 `translateY(-2px)`）。
  封面 `aspect-ratio:16/10`，按游戏调色：紫 `#e3e7ff→#c9d0ff`、绿 `#e2f7f1→#c6efe4`、粉 `#ffeef3→#ffd9e4`；内含 28px 白色网格线、左上分类胶囊（`对战`/`策略`/`撤离`，白底 12px 700）、漂浮方块、右下在线胶囊（白底 94% + 绿点 `#1f8a5b` + 「N 人在玩」）。
  正文区：标题 700/18px `#243056` + 状态胶囊（已发布 `#e3f7ed`/`#1f8a5b`；即将推出 白底 + `1px solid #e7ecf6`/`#9aa6be`）；元信息胶囊（🗓 更新日、⏱ 单局时长，12px `#f7faff` 底）；房间条（`#f7faff` + `1px solid #e7ecf6` + 圆角 12px，左侧 26px 头像堆叠 -8px 重叠、超 4 人显示 `+N` 紫底，右侧「房间 ID · 5/8」）；底部 `开始游戏`（flex:1 渐变）+ `分享`（ghost）。未发布卡整卡 `opacity .78`，按钮为不可点的「敬请期待」。

### 3. 注册 `/register`
- **步骤指示**：`1 邮箱 —— 2 资料`，当前步 22px 圆点渐变白字，已完成步 `#e3f7ed`/`#1f8a5b`，未到 `#eaf0ff`/`#9aa6be`。
- **Step 1**：卡片宽 520px（内容 416px），标题「注册」900/28px；邮箱输入（min-height 40px，圆角 8px，错误态边框 `#d9342a` + 13px `#d9342a` 提示）；主按钮「发送验证码」；底部「已有账号？登录」。
- **Step 2**：顶部一行 = 返回按钮（32×32 圆角 10px 白底 `←`）+ 「完善资料」900/22px + 右侧只读邮箱（12px 等宽 `#6b7894`）。字段顺序固定为 **用户名 → 密码 → 头像 → 验证码**，每项独占一行。
  验证码为 6 个独立输入框（48×56px，圆角 12px，700/22px Inter，居中；已填时边框 `#7c8cff`），支持自动跳位、Backspace 回退、整串粘贴；标签行右侧为「发送验证码 / 重新发送（Ns）」文本按钮。
  头像：12 个 48px 圆形选项，背景 `#eaf0ff`，选中 `2px solid #7c8cff` + `0 0 0 3px rgba(124,140,255,.28)`。原型用 emoji 占位，实现时替换为同一套图形资产。
- **校验**：邮箱格式；已注册（`alice@example.com` 演示）；验证码错误；密码 < 8 位；用户名需 `^[A-Za-z][A-Za-z0-9_-]{2,31}$`；`Bot\d` 前缀保留；用户名占用。成功 → `/` + toast「注册成功，欢迎来到 Lumio」。

### 4. 登录 `/login`
- 卡片 416px；`?next=` 时标题「登录后继续」、按钮「登录并进入」，登录成功直接进入 launch 流程。
- 错误：`邮箱或密码不对`（两种错误同文案，输入框边框转红）；`该账号已被停用`（`#fce7ec` 底、`#c2415b` 文字的提示块）。

### 5. 我的资料 `/me`
- 两栏 `1fr 1.1fr`，gap 24px。左卡：72px 头像（`2px solid #7c8cff`）+ 用户名 800/24px Inter + `UID` 行（等宽 13px + `复制` ghost 小按钮，复制后按钮文案变「已复制」+ toast）；下方 12 头像网格（点选即保存 + toast「头像已更新」）；再下方「我玩过的」信息条。
- 右卡：用户名 / 邮箱 / 加入时间三个只读字段（底色 `#f7faff`，用户名行右侧标注「不可修改」）+ `退出登录`。v1 无改密改邮箱。

### 6. 反馈 `/feedback`
- 两栏 `minmax(0,1fr) 300px`，gap 28px，`align-items:start`；页标题「反馈」900/28px。
- 左侧表单卡（padding 24px，gap 18px）：
  - 类型胶囊 `Bug` / `建议`（min-height 38px，选中 `#eaf0ff` + `1px solid #c9d3f5` + `#6171f0`，前置 8px 方点 `#6171f0` / `#5de2c6`）。
  - 标题、详情：标签行右端为计数（`0/80`、`0/4000`，12px 等宽 `#9aa6be`）；输入 min-height 44px / textarea min-height 150px，圆角 10px。
  - **截图区**：虚线框（`1px dashed #c9d3f5`，底 `#f7faff`，min-height 96px），拖拽悬停时边框 `#7c8cff`、底 `#f3f6ff`。支持三种录入：拖入、点击 `＋`（96×72 白底卡，内含隐藏 file input，`accept="image/*" multiple`）、在详情或截图区 `⌘/Ctrl+V` 粘贴。缩略图 96×72 圆角 10px，右上角 20px 删除按钮（`rgba(30,42,58,.72)` 圆底白叉）；上限 4 张；添加后 toast「已添加截图」。
  - 相关游戏（select）+ 联系方式（input）两列。
  - 底部分隔线上：`匿名提交`（18px 方形勾选，选中填充 `#6171f0`）+ `提交`（min-height 44px 渐变）。
- 右栏：`开发者交流群` 卡片按钮（40px 渐变方块「群」+ 标题 + `›`，点击开弹窗）；`我的反馈` 卡片（条目 = 标题 + 状态胶囊；游客/无数据显示 `—`）。
- 提交校验：标题非空、详情 ≥ 5 字；成功后清空表单与截图并 toast「收到了，谢谢」，新条目以「新提交」进入列表。

### 7. Roadmap `/roadmap`
- 宽 880px；标题 `Roadmap` 900/28px。每行一张卡（`#fff` + `1px solid #e7ecf6` + 圆角 12px，padding 16px 20px，gap 16px）：季度标签（700/12px Inter letter-spacing .08em `#9aa6be`，宽 64px）+ 10px 状态方点（已完成 `#5de2c6` / 进行中 `#7c8cff` / 计划中 `#c9d3f5`）+ 标题 700/15px + 右侧状态胶囊（已完成 `#e3f7ed`/`#1f8a5b`；进行中 `#eaf0ff`/`#6171f0`；计划中 `#eef1f6`/`#6b7894`）。
- 数据：2026 Q3 账号体系与大厅上线（已完成）、体素炸弹人首发（已完成）；2026 Q4 房间与观战（进行中）、起床战争俯视改编（进行中）；2027 Q1 逃离鸭科夫（计划中）、引擎 SDK 开放（计划中）。

### 8. 开发者交流群弹窗
- 遮罩 `rgba(36,48,86,.34)`；卡片 480px，padding 28px，gap 20px。标题「开发者交流群」+ `✕`。
- 上半：两张二维码并排（`1fr 1fr`，`aspect-ratio:1`，圆角 12px + `1px solid #e7ecf6`），**无文字标签**。
- 下半：两个链接按钮 `飞书群 ↗`（渐变主按钮）、`QQ 群 ↗`（ghost），min-height 44px。

### 9. 进入游戏过场
- 居中：64px 体素立方（三面 `#bcc5ff` / `#8b99ff` / `#5a66c8`，`rotateX(-24deg) rotateZ(-45deg)`，`lm-spin` 3.2s 线性旋转）；标题「正在进入 体素炸弹人…」700/20px；进度条 240×4px（`#e7ecf6` 轨道 + `linear-gradient(135deg,#6171f0,#5de2c6)`，宽度 12% → 62% → 100%，`transition:width .4s ease`）；阶段文案「领取入场券… / 连接游戏服务器…」；`取消` 按钮回大厅。
- 时序：0ms → 12%，700ms → 62%，1600ms → 进入游戏并房间人数 +1。
- 平台层不显示 wsUrl / token。

### 10. 游戏内 `/games/:slug/`
- 顶部操作条：`← 离开房间`；房间胶囊（白底圆角 999px：头像堆叠 + 「房间 vb-1 · 5/8 人在玩」+ `进行中` 绿胶囊）；右侧 `邀请朋友` 渐变按钮。
- 画布：`aspect-ratio:16/9`，底 `#1e2a3a`，44px 网格 `rgba(124,140,255,.14)`，3 个漂浮方块；左上计分胶囊列表（白底 92%，20px 头像 + 名称 + 分数）。
- 邀请面板（右下，300px 卡片）：房间人数行、80px 二维码、只读邀请链接 `…/voxel-bomber/?room=vb-1`、`复制邀请链接`（复制后文案变「已复制」+ toast）、`模拟朋友加入`（演示用，人数 +1，满员提示「房间满了」）。

### 11. 失败 / 403
- launch 失败：48px `#fce7ec` 圆角块 + ⚠ + 「现在进不去，稍后再试」+ `再试一次` / `回到大厅`。
- 403：`403` 800/48px 渐变文字（`linear-gradient(135deg,#6171f0,#5de2c6)` + background-clip:text）+「这里只有管理员能进」+ `回到大厅`。

### 12. 后台 `/admin`（仅 admin）
- 布局：`200px 1fr`，gap 24px。左侧导航卡（圆角 12px，条目 padding 9px 12px、圆角 10px；选中为渐变底白字，未选中白底 `#6b7894`），顶部 `ADMIN` 700/11px letter-spacing .12em。
- **看板**：4 格统计（`grid-template-columns:repeat(4,1fr)`，1px 分隔线拼接，数字 800/32px 渐变文字）——当前在玩 / 开着的房间 / 新注册（7 天）/ 待处理反馈；下方两卡：`房间实时人数`（每行 slug + 8px 进度条 + `5/8`）、`分享统计`（分享次数、带来的访问、带来的注册、转化率）。
- **用户**：表格（min-width 760px，外层 `overflow-x:auto`）UID / 用户名 / 邮箱 / 角色 / 状态 / 加入 / 操作。角色与状态胶囊需 `white-space:nowrap`。操作：`停用`（弹确认框）/`恢复`、`升为 admin`/`降为 player`，均带 toast。
- **游戏目录**：slug / 名称 / 房间容量（可编辑，写回大厅房间容量）/ 排序 / 状态 / `发布`↔`下架`。
- **反馈队列**：类型 / 标题 / 提交人 / 游戏 / 时间 / 状态（select 三档，改动即 toast）。
- **平台设置**：飞书群链接、QQ 群链接、QQ 群号 + `保存`（toast「设置已保存」）。
- 表格样式：`th` 500/12px Inter `#9aa6be` letter-spacing .04em，`td` 13px，行下边框 `1px solid #e7ecf6`，行高约 40px。

## Interactions & Behavior
- **路由/屏幕**：lobby / register(step1,2) / login / me / feedback / roadmap / launching / game / launchfail / 403 / admin(dash,users,games,fb,set)。
- **鉴权门**：游客点「开始游戏」→ `/login?next=game`，登录成功直接续跑 launch。管理员登录后落地后台。
- **弹层**：分享弹窗、开发者交流群弹窗、危险操作确认框（`取消` + 红色 `#d9342a` 确认）。遮罩 `rgba(36,48,86,.34)`，卡片 `lm-in` 240ms。
- **Toast**：底部居中，白底 + `1px solid #e7ecf6` + pop 阴影 + 左侧 8px 绿方点，2200ms 自动消失；用于复制、保存、加入房间、提交成功等所有确认反馈。
- **复制**：按钮文案切换为「已复制」并 toast，实现用 `navigator.clipboard.writeText`。
- **动画**：`lm-in`（进入）、`lm-float`（装饰方块，5.4–7.5s）、`lm-spin`（过场立方 3.2s）。`transition` 统一 120–180ms `cubic-bezier(.32,.72,0,1)`。
- **焦点态**：输入 `border-color:#7c8cff` + `box-shadow:0 0 0 3px rgba(124,140,255,.22)`。
- **响应式**：卡片网格 auto-fill 自适应；后台表格横向滚动；主内容 `min(100% - 64px, N)`。

## State Management
- 身份：`role: guest|player|admin`、`name`、`email`、`uid`、`avatar`、`plays`、`next`（登录后回跳目标）。
- 注册：`regStep`、`regEmail`、`regCode`、`regPwd`、`regName`、`cooldown`（重发倒计时 47s）、错误字段。
- 登录：`loginEmail`、`loginPwd`、`loginErr`。
- 反馈：`fbKind`、`fbTitle`、`fbBody`、`fbGame`、`fbContact`、`fbAnon`、`shots[]`（截图 blob URL）、`dragOver`、`feedback[]`。
- 房间：`roomId`、`roomFill`、`roomCap`、`onlineTotal`、`roomCount`、`vbOnline`；launch 进度 `launchPct`、`launchStage`。
- 后台：`users[]`、`games[]`、`feedback[]`、`settings{feishu,qq,qqNo}`、`adminTab`、`confirm`。
- UI：`shareOpen`、`groupOpen`、`inviteOpen`、`toast`、各 `copied` 标记。
- **数据获取（实现时）**：大厅需要游戏目录 + 各游戏在线/房间数（轮询或 SSE，原型注释按 5s 刷新设计）；launch 走后端签发入场券再连游戏服务；后台各页对应管理 API；反馈截图需上传接口（原型仅本地预览）。

## Design Tokens
与 `web/src/styles/tokens.css` 一致：

- **色**：primary `#7c8cff`、primary-d `#6171f0`、mint `#5de2c6`、amber `#ffb86b`、rose `#ff7ea6`、ink `#1e2a3a`、title `#243056`、muted `#6b7894`、faint `#9aa6be`、line `#e7ecf6`、field `#f7faff`、bg-2 `#eaf0ff`、up `#1f8a5b`/`#e3f7ed`、down `#c2415b`/`#fce7ec`、danger `#d9342a`、ghost 底 `#f3f6ff` + 边 `#c9d3f5`。
- **渐变**：主按钮 `linear-gradient(135deg,#7c8cff,#6171f0)`；品牌块 `linear-gradient(150deg,#7c8cff,#9aa6ff)`；数字/强调文字 `linear-gradient(135deg,#6171f0,#5de2c6)`。
- **字**：`"Noto Sans SC", Inter, system-ui`；数字/拉丁用 Inter。字阶 hero 40–44 / display 32 / h1 28 / h2 20 / h3 18 / body 16 / cap 13 / micro 12；字重 400/500/700/800/900；行高 1.08 / 1.28 / 1.5。
- **间距**：4 / 8 / 12 / 16 / 24 / 32 / 48 / 64。
- **圆角**：卡片 16、后台卡 12、按钮 12–14、输入 8–10、胶囊 999、品牌块 9（大 logo 26）。
- **阴影**：card `0 1px 2px rgba(30,42,58,.04), 0 14px 40px -16px rgba(53,68,120,.2)`；pop `0 22px 56px -18px rgba(53,68,120,.42)`；主按钮 glow `0 18px 38px -14px rgba(97,113,240,.8)` + `inset 0 1px 0 rgba(255,255,255,.35)`。
- **控件尺寸**：导航按钮 36、常规按钮 40、强调按钮 44–48、输入 40–44、头像 28/48/72/96。

## Assets
- 原型未使用外部图片。需要真实资产替换的位置：
  1. **游戏封面**（16:10）——现为渐变 + 网格 + 漂浮方块占位。
  2. **12 个头像**——现为 emoji 占位，需一套统一风格图形（建议 128/256px 圆形裁切）。
  3. **二维码**——分享弹窗、交流群弹窗、邀请面板现为棋盘格占位，实现时由后端或前端生成。
  4. **品牌标记**——3×3 像素点阵 + 渐变圆角方块，纯 CSS，可直接复用或替换为 SVG。
- 字体自 Google Fonts 载入（Inter 500–800、Noto Sans SC 400–900）。

## Files
- `Lumio Prototype.dc.html` — 主交付：可点击全流程原型（含后台）。直接用浏览器打开，需同目录 `support.js`。
- `support.js` — 原型运行时（仅原型需要，不要移植）。
- `reference/Lumio Hi-fi Player v2.dc.html` — 玩家端静态画板，每屏一帧 + 路由/角色/主操作标注，便于逐屏比对。
- `reference/Lumio Wireframes.dc.html` — 早期低保真结构探索（8 组 × 3 方案），仅作决策记录。

### 原型演示账号
- `admin@lumio.games` + 任意 8 位密码 → 管理员（右上出现「后台」）。
- `bob@example.com` → 已停用错误态。
- 其他合法邮箱 → 普通玩家。
- 注册验证码演示值：`482193`；`alice@example.com` 会触发「已注册」。
