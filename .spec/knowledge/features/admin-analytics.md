---
name: admin-analytics
description: 运营后台与埋点设计——管理员角色、用户 / 登录记录 / 封禁、事件上报与看板聚合;改后台或埋点前查
metadata:
  type: doc
  status: 设计中
---

# 运营后台与埋点

后台是同一个 SPA 的 `/admin` 路由（`role = admin`），数据来自平台数据库的只读投影与少量管理写入；埋点是一张事件表 + 一个上报端点 + SQL 聚合看板，不引入分析栈。

## 背景 / 目标

Owner 要看：用户注册信息、活跃度、运营埋点、反馈、登录记录与账号状态。目标是最少实体：一个角色字段、一张事件表、一张审计表、一组 `/api/admin/*` 端点。

## 设计

### 管理员

- `accounts.role ∈ { player, admin }`；`/api/admin/*` 全部 `RequireRole("admin")`，前端按 `me.role` 守卫 `/admin`。
- 首个管理员：仅通过一次性 `lumio-platform admin bootstrap --email <address>` 命令提升为 `admin` 并写 `audit_log`（幂等；不存在时命令失败且不修改数据）。启动时不读取 `PLATFORM_BOOTSTRAP_ADMIN_EMAIL`，生产不得有 dev-only 提权后门。
- 所有管理员 Cookie 状态变更要求 CSRF token、严格 `Origin` / Fetch Metadata；管理员端点按 IP / 账号分区限流，超限返回 `429 rate_limited`。
- 管理员的写操作（封禁 / 解封 / 改角色 / 改游戏目录 / 改设置 / 处理反馈）全部写 `audit_log(id, actor_account_id, action, target, before?, after?, at)`。

### 用户与登录记录

- `GET /api/admin/users?q=&status=&page=`（按 uid / email / loginName 检索）；`GET /api/admin/users/{accountId}`（资料 + 最近登录 + 最近事件）。
- `POST /api/admin/users/{accountId}/ban` / `unban`、`PUT /api/admin/users/{accountId}/role`——经账号域写入（account.md），不直写表。
- `GET /api/admin/login-attempts?accountId=&outcome=&from=&to=`（来源 `login_attempts`）。

### 游戏目录与设置

- `GET/POST/PUT /api/admin/games`（lobby-launch.md 的 `games` 表）；`status` 切换 `draft` ↔ `published`。
- `GET/PUT /api/admin/settings`（`platform_settings` 键值，白名单键）。

### 埋点

- `events` 表：`id`、`name`（`^[a-z][a-z0-9_.]{1,63}$`）、`props`（jsonb，≤ 4 KB）、`account_id?`、`anon_id`（匿名 Cookie，首访生成）、`client_ts`、`received_at`、`page_url?`、`user_agent?`。
- `POST /api/track`：批量 ≤ 50 条 `{ name, props?, clientTs }`；失败码 `invalid_event`、`batch_too_large`。前端在路由切换、大厅曝光、点击启动、提交反馈处上报。
- `/api/track` 按 IP / 账号分区限流；限流是埋点端点的当前 DoD，不延后到 P5-2。
- 服务器侧直接落表的事件：`account.registered`、`account.login`（含 port）、`game.launched`（gameSlug、roomId）、`feedback.submitted`。
- 看板 `GET /api/admin/stats?from=&to=`：DAU / WAU（按 `account_id` 去重）、注册数、各游戏启动次数、反馈数（按状态）。全部 SQL 聚合；量大再谈物化视图（信号：单次查询 > 2s）。
- 保留策略：v1 不删；需要时另立决策。

## 待解决

- 前端图表库选型（先用最小依赖或原生 SVG；出现复杂图再定）。
- 事件量与保留（信号触发）。

## 相关

- [`platform.md`](platform.md)、[`account.md`](account.md)、[`lobby-launch.md`](lobby-launch.md)、[`feedback.md`](feedback.md)
