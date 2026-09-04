---
name: 2026-09-04-platform-spa-from-prototype-prompt
description: 按已入库高保真原型重建平台 SPA 的主 loop 开工提示词;派实现 Agent 时整段粘贴
metadata:
  type: doc
  status: 设计中
---

# 平台 SPA 对照原型重建 · 开工提示词（2026-09-04）

> 把下面「提示词正文」整段交给负责实现的**主 loop Agent**（本仓 `LumioPlatform`）。导入会话只落了原型与决策，**没有写实现、没有拆卡**。收件 Agent 从读规范 + `writing-plans` 开始，不要直接铺页面。

---

## 提示词正文

你是 `LumioPlatform` 的主 loop。目标：按已入库的高保真原型，在既有 ASP.NET Core 10 + React 19 SPA 里**重建**玩家端与运营后台界面，并接到 MS-1 已规划的 API 上。

本提示词是 Owner 授权。视觉按原型；后端按既有 feature 文档 / 契约 / MS-1 卡。不要重开配色讨论，不要把原型 HTML 搬进生产。

### 0. 角色与真值

真值优先级（冲突时序号小的赢）：

1. 本仓 `.spec/AGENTS.md` 与 `.spec/rules/system.md`。
2. 架构仓 ADR-061 / ADR-054，以及本仓 `contract/` 镜像（不得手改协议）。
3. 本仓决策 [0004](../decisions/0004-platform-ui-from-gametech.md)、[0005](../decisions/0005-platform-ui-hifi-prototype.md)、[0001](../decisions/0001-openapi-export-command.md)–[0003](../decisions/0003-no-aspnet-identity.md)。
4. 功能文档：[`platform.md`](../knowledge/features/platform.md)、[`account.md`](../knowledge/features/account.md)、[`lobby-launch.md`](../knowledge/features/lobby-launch.md)、[`feedback.md`](../knowledge/features/feedback.md)、[`admin-analytics.md`](../knowledge/features/admin-analytics.md)、[`platform-ui.md`](../knowledge/features/platform-ui.md)。
5. MS-1 卡片 [`2026-09-04-platform-ms1-cards.md`](2026-09-04-platform-ms1-cards.md)（P2-2 是 SPA 骨架卡；P3/P4 才接线真实 API）。
6. 原型 [`web/docs/prototype/README.md`](../../web/docs/prototype/README.md) 与 `Lumio Prototype.dc.html`：只作屏幕级外观与交互参考。

### 1. 先读（只读一次，之后用路径引用）

1. `.spec/AGENTS.md`、`.spec/knowledge/README.md`，再读 `standards/{workflow,code-style,testing,dispatch}.md`。
2. 上列 feature 文档与决策 0004 / 0005。
3. MS-1 卡片全文，至少吃透 P2-2、P3-1、P3-2、P4-1、P4-2、P4-3。
4. 用浏览器打开 `web/docs/prototype/Lumio Prototype.dc.html`（需同目录 `support.js`），把所有屏幕点一遍；需要逐屏标注时打开 `web/docs/prototype/reference/Lumio Hi-fi Player v2.dc.html`。线框 `Lumio Wireframes.dc.html` 只是决策记录。
5. 代码：`web/src/app/App.tsx`、`web/src/styles/*`、`web/src/api/client.ts`、`web/design-preview.html`、`web/package.json`。
6. 技能：`before-you-code`、`writing-plans`、`subagent-driven-development`、`test-driven-development`、`verification-before-completion`。大任务走 TDD；收口前过 `verification-before-completion`。

读完先做 Pre-Flight：对照下面「范围」列出文件集，确认能拆成互不重叠的卡。有缺口一次性打包问 Owner，不要逐条打断。

### 2. 当前仓库事实

- `web/src/app/App.tsx` 仍是健康检查页，没有路由。
- 令牌与 `.ui-*` 原语已在 `web/src/styles/`；`web/design-preview.html` 是组件墙，不是页面稿。
- 原型已落在 `web/docs/prototype/`，不要再从 `~/Downloads/design_handoff_lumio_platform` 拷。
- 后端按 MS-1 推进；P2-2 允许页面壳 + mock / 生成类型占位，不调用尚未落地的端点。
- 技术栈锁定：React 19、TS strict、Vite、React Router 7、TanStack Query 5、Zustand、CSS Modules、oxlint、vitest。不引入 UI 框架。API 只经 `web/src/api/client.ts`，不得手写 DTO、不得直接 `fetch('/api/...')`。

### 3. 本轮做什么 / 不做什么

**做：**

- 按原型重建 SPA 壳与页面：顶栏、大厅、注册（两步）、登录、我的资料、反馈、Roadmap（静态）、launch 过场、launch 失败、403、后台（看板 / 用户 / 游戏目录 / 反馈队列 / 设置）。
- 路由与守卫按 P2-2：`/` `/login` `/register` `/feedback` `/me` `/admin/*`；另加 `/roadmap`。player 访问 `/admin` 必须 403 页。
- 视觉用 `web/src/styles/` 的 `.ui-*` 与 CSS Modules 重建，对照原型的布局 / 文案 / 间距 / 动效。
- P2-2 阶段用占位数据让页面可点；真实接线留给 P3-1 / P3-2 / P4-*，但组件 props 与文案现在就要按生产形状来，避免返工。
- 12 张默认头像：P2-2 要求 `web/public/avatars/` 占位 SVG，禁止把原型 emoji 带进生产。

**不做：**

- 不复制 `Lumio Prototype.dc.html` / `support.js` / 行内样式进 `web/src/`。
- 不在 SPA 里实现 `/games/:slug/` 游戏画布、计分板、`模拟朋友加入`。Launch 成功后跳到平台托管的静态游戏页 `/games/<slug>/`（P3-2 / P3-3）。过场可以在 SPA 里做，过场结束用 `window.location` 或 router 进游戏页。
- 不实现反馈截图上传（`feedback.md`：v1 不做）。反馈页不要做会误导用户的截图投放区。
- 不新增房间实时人数 / 分享转化 / 在线人数 API；大厅卡片可做分享（复制当前 origin + 游戏路径），人数与房间条在没有 API 前用空态或省略，不要假数据冒充直播。
- 不给 `games` 表加「房间容量」列、不改 `contract/`、不手改 OpenAPI 生成物。
- 不把原型演示账号（`admin@lumio.games` / `482193`）写进生产或测试口令。
- 不改架构仓，不写 Workflow 流转（只读 MS-1 卡号）。
- 不顺手重构后端，不升级依赖，不引入组件库。

### 4. Owner 已拍板的范围裁剪（不要再问）

| 原型有、文档没有或冲突 | 本轮 |
| --- | --- |
| `/roadmap` 静态页 + 顶栏入口 | 做。内容按原型文案写死，不建 CMS。 |
| 顶栏「开源引擎 ↗」 | 做。href 用 `https://github.com/LumioGames`，除非代码里已有更准的对外文档 URL。 |
| 开发者交流群弹窗（双二维码 + 飞书/QQ 链） | 做 UI。链接来自 `GET /api/settings/public`（P4-1 前可空态）。QR 用链接前端生成，不存图。 |
| 反馈截图 | 不做。 |
| 匿名提交勾选 | 做。未登录本来就可匿名；勾选 = 已登录也不带账号身份（若 API 尚无此字段，P2-2 只做 UI，接线时再对 `feedback.md`，对不上就停并上报）。 |
| 大厅房间条 / 「N 人在玩」 | 无 API 则不做假直播。卡片结构按原型，人数位空态。 |
| 分享 | 做客户端复制链接 + toast。 |
| 游戏内邀请 / 画布 | 不做（属 LumioClient）。 |
| 后台「房间实时人数」「分享统计」 | 看板布局按原型；这两块无 API 则空态或隐藏，不要编数字。绑定现有 `GET /api/admin/stats` 的字段（DAU/WAU/注册/启动/反馈）。 |
| 后台编辑房间容量 | 不做。游戏目录只做文档里的 slug/名称/排序/发布。 |
| 「我玩过的」 | 无 API 则空态一行，不编历史。 |
| 注册字段顺序 用户名→密码→头像→验证码 | 做。API 仍是 `request-code` + 一次 `register`。 |

其它缺口：停，打包问 Owner。不要本地发明第二个真值。

### 5. 工作方式

1. **先计划，后代码。** 用 `writing-plans` 把实现计划写到 `.spec/plans/YYYY-MM-DD-platform-spa-from-prototype.md`。wave 0 = 契约卡（路由表、session store、layout/nav、toast/dialog 原语的签名）。wave 1+ = 页面卡，文件集互不重叠。每卡 `## 接口` 写 Consumes / Produces。本仓计划目录是 `.spec/plans/`，不是技能默认的 `docs/plans/`。
2. 创造性视觉已经定了，**跳过 brainstorming**；只在裁剪表之外的产品缺口才升级 Owner。
3. 用 `subagent-driven-development` 执行计划。契约卡过 lint + 类型检查后再扇出。宿主若不能派 worktree worker，按该技能 Inline Fallback 串行，仍要 TDD。
4. 测试：P2-2 至少覆盖路由守卫（player → `/admin` = 403）与关键表单/导航的组件测试。`pnpm -C web verify` 必须绿。改了 `.spec/` 则 `node .spec/tools/spec-lint.mjs`。
5. 文案中文，与原型逐字对齐（错误码对用户的句子用原型：「邮箱或密码不对」「该账号已被停用」「收到了，谢谢」等）。
6. 不夹带。一次提交一类事。提交前等 Owner 说「提交」再 `git commit`；未要求则不要提交、不要 push、不要开 PR。
7. 高风险（鉴权 / 安全 / `rules/`）至少快审。纯页面壳可走 closeout-gate；拿不准就快审。

### 6. 硬约束（每张 worker 简报都带上）

- 只用 `web/src/styles/` 令牌；功能文件不写新的色板 hex（装饰性渐变若 primitives 没有，先加 token 再引用，并更新 `platform-ui.md`）。
- 生成物不得手改。
- 私钥、口令、验证码不进日志、不进测试明文仓库约定之外的位置。
- 生产不得开 `PLATFORM_EMAIL_ALLOW_CONSOLE` / `PLATFORM_REGISTRATION_PROFILE=test`。
- 平台层不展示 `wsUrl` / token。
- 收口门槛见 `.spec/AGENTS.md`。声称「完成」前必须跑命令并贴输出。

### 7. 交回物

按 `.spec/AGENTS.md` 交回物格式：改动清单、验证证据（命令 + 关键输出）、known gaps、知识沉淀落点。计划阶段以任务卡集合 + 待澄清项代替测试证据。

把「对照原型仍缺的屏幕 / 交互」写进 known gaps，不要假装像素级已完。
