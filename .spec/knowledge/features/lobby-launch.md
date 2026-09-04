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

`POST /api/games/{slug}/launch` 接受浏览器 Cookie 或 `Authorization: Bearer <accountAuthCredential>`（二选一；同时出现拒 `invalid_request`）。请求 body 必须为空，调用方唯一提供的路由选择是 path slug；任何 audience / game / release / contract / room / allocation / endpoint hint 都拒绝。成功 → 200：

```json
{ "wsUrl": "wss://…", "subprotocol": "lumio.mvp.v0", "contractId": "lumio.gameplay-envelope.v1",
  "serverAudience": "game-fleet-a", "gameId": "bomber", "gameReleaseId": "bomber-0.1.0",
  "roomId": "…", "allocationId": "alloc_…",
  "admissionCredential": "base64url…", "admissionExpiresAt": 1757000000,
  "accountId": "acct_…", "loginName": "alice" }
```

失败码还包括 `rate_limited`、`untrusted_ws_endpoint`、`admission_binding_mismatch`。浏览器同源 `fetch` 使用 Cookie；Bot、工具与 RM-00011 用 WS 签发的 unbound `accountAuthCredential` Bearer 交换 Room-bound `admissionCredential`。Platform 验签、校验 expiry/unbound sentinel、账号状态与 botToolContext 后才分配；原 credential 不能直接进 Room。每次 launch 新签发 Room credential（新 nonce / expiry）并记 `game.launched` 事件。

签名载荷绑定 `serverAudience`、`gameId`、`gameReleaseId`、`contractId`、`roomId` 与 `allocationId`，Platform 返回前先与 allocator context 逐项比对；Game Server 再以自身 registry 的 trusted context 逐项校验。凭证不进 URL 或日志。allowlist 以 `allocationId` 精确绑定 scheme / host / port / path 与六元上下文，禁止 userinfo、query、fragment、redirect 和 wildcard host；生产仅返回 allowlisted `wss://`，`ws://` 只允许显式 test profile + loopback record。

### 房间分配器接口

```csharp
interface IRoomAllocator { Task<RoomEndpoint> AllocateAsync(Game game, AccountId account, CancellationToken ct); }
record RoomEndpoint(string WsUrl, string Subprotocol, string ContractId, string ServerAudience,
                    string GameId, string GameReleaseId, string RoomId,
                    string AllocationId);
```

- v1 实现 `StaticEndpointAllocator`：从受信游戏目录返回 `games.server_ws_url`、固定 `serverAudience` / release / contract，并以 `roomId = slug` 生成 `allocationId`（一进程一房间）。客户端不能覆盖这些值。
- 多房间时换实现（房间登记表 + 心跳，或外部 fleet 服务），由架构仓拓扑调研定案后另立 ADR；`launch` 应答形状不变。

### Bundle 与发布不变性

游戏包必须是同源、第一方、不可变发布物，目录含 `index.html`、`contract.json` 与 release/hash 元数据；发布记录绑定唯一 `gameReleaseId` 和内容摘要。平台不接受客户端提供的 `wsUrl`、版本、contract 或 audience，也不把准入凭证放进 URL、日志或浏览器持久存储。

### 游戏页接入（LumioClient 侧，P2-2 卡）

页面加载 → `fetch('./contract.json')` 照旧 → `POST /api/games/<slug>/launch`（同源，带 Cookie）→ 用应答的 `wsUrl` / `subprotocol` 与 `admissionCredential` 走五步准入 → 之后照旧。`?ws=` 查询参数只在集成考卷的本地模式保留（考卷仍可直连），产品路径不再从 URL 取地址。

Bot / 工具 / RM-00011：WS `/account` 登录 → 取得 unbound `accountAuthCredential` → 仅以 Bearer + path slug 调 Launch（无 Cookie、无 body）→ 取得 Room-bound `admissionCredential` → Game Server 用 server-owned allocation context 验票。任何步骤都不接受客户端 allocation claims。

### 大厅页（SPA）

`GET /api/games` 列已发布游戏（`slug`、`name`、`summary`、`coverUrl`），点卡片进 `/games/<slug>/`；未登录点启动先跳登录再回跳。

## 待解决

- 多房间分配器与房间登记（调研后 ADR）。
- 游戏包发布流程是否需要后台上传（当前不需要）。

## 相关

- 架构仓：ADR-058 §11、`ds-server.md` M1 / M9、`engine/wire/platform-port-v1.json`
- 本仓：[`platform.md`](platform.md)、[`account.md`](account.md)、[`admin-analytics.md`](admin-analytics.md)
