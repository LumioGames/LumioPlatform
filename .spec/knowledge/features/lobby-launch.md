---
name: lobby-launch
description: 大厅与启动设计——游戏目录、静态游戏页托管、launch 端口与房间分配器接口;改大厅或接入新游戏前查
metadata:
  type: doc
  status: 设计中
---

# 大厅与启动

大厅展示已发布的自研游戏；玩家点开一款游戏，平台签发准入凭证并给出该局的 Game Server 地址，浏览器里的游戏页据此建立 WSS 连接并走 DS 五步准入。平台**不**模拟游戏、不代理游戏流量。

## 背景 / 目标

- 今天的浏览器游戏页是无构建的静态 ES module（`LumioClient/modules/web/`），从 URL `?ws=` 取服务器地址、旁边读 `contract.json`；集成考卷靠脚本把这些摆好。平台要把「摆好」变成产品化的目录 + 托管 + 启动。
- 一进程一 GameWorld（ADR-058 §11），多房间 = 多进程 + 路由。v1 每款游戏一个固定端点；分配器接口从第一天就在，实现可换，API 不改。

## 设计

### 游戏目录

`games` 表：`slug`（URL 安全、唯一）、`name`、`summary`、`cover_url`、`status`（`draft` / `published`）、`bundle_dir`（相对 `PLATFORM_GAMES_ROOT` 的目录名）、`server_ws_url`（v1 固定端点，`ws://` 或 `wss://`）、`subprotocol`、`contract_id`、`sort_order`、`created_at` / `updated_at`。后台 CRUD（admin-analytics.md）。

发布一款游戏 = 运维把静态包目录（含 `index.html` 与 `contract.json`）放到 `PLATFORM_GAMES_ROOT/<bundle_dir>/` + 后台把记录置为 `published`。平台不做上传与构建。

### 静态托管

- `/games/<slug>/` → `PLATFORM_GAMES_ROOT/<bundle_dir>/` 静态文件；只对 `published` 的 slug 生效，其余 404。
- 游戏页按既有形态保持零构建、零框架；它只多做一件事：向平台要地址与凭证（见下）。

### 启动端口（`platform-port-v1.json` `launch`）

`POST /api/games/{slug}/launch`（需会话）→ 200：

```json
{ "wsUrl": "wss://…", "subprotocol": "lumio.mvp.v0", "contractId": "lumio.gameplay-envelope.v1",
  "roomId": "…", "admissionCredential": "base64url…", "admissionExpiresAt": 1757000000,
  "accountId": "acct_…", "loginName": "alice" }
```

失败码：`unauthorized`、`account_banned`、`game_not_found`、`game_not_published`、`no_room_available`。凭证**不进 URL**；游戏页同源 `fetch`，用 Cookie 会话拿到后立即握手。每次 launch 新签发凭证（新 nonce / expiry）并记 `game.launched` 事件。

### 房间分配器接口

```csharp
interface IRoomAllocator { Task<RoomEndpoint> AllocateAsync(Game game, AccountId account, CancellationToken ct); }
record RoomEndpoint(string WsUrl, string Subprotocol, string ContractId, string RoomId);
```

- v1 实现 `StaticEndpointAllocator`：返回 `games.server_ws_url` 与 `roomId = slug`（一进程一房间）。
- 多房间时换实现（房间登记表 + 心跳，或外部 fleet 服务），由架构仓拓扑调研定案后另立 ADR；`launch` 应答形状不变。

### 游戏页接入（LumioClient 侧，P2-2 卡）

页面加载 → `fetch('./contract.json')` 照旧 → `POST /api/games/<slug>/launch`（同源，带 Cookie）→ 用应答的 `wsUrl` / `subprotocol` 与 `admissionCredential` 走五步准入 → 之后照旧。`?ws=` 查询参数只在集成考卷的本地模式保留（考卷仍可直连），产品路径不再从 URL 取地址。

### 大厅页（SPA）

`GET /api/games` 列已发布游戏（`slug`、`name`、`summary`、`coverUrl`），点卡片进 `/games/<slug>/`；未登录点启动先跳登录再回跳。

## 待解决

- 多房间分配器与房间登记（调研后 ADR）。
- 游戏包发布流程是否需要后台上传（当前不需要）。

## 相关

- 架构仓：ADR-058 §11、`ds-server.md` M1 / M9、`engine/wire/platform-port-v1.json`
- 本仓：[`platform.md`](platform.md)、[`account.md`](account.md)、[`admin-analytics.md`](admin-analytics.md)
