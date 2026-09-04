---
name: platform
description: 游戏平台总览——拓扑、进程与目录结构、配置与运行、部署假设、非目标;开任何平台卡前先读
metadata:
  type: doc
  status: 设计中
---

# Lumio 游戏平台（总览）

`LumioPlatform` 是 Lumio 的对外入口：一个网页平台把**账号**（唯一账号权威）、**大厅**（展示并启动自研浏览器联机游戏）、**反馈**与**运营后台**放在同一个进程里。本文只回答：这是什么、长什么样、每块归哪份文档、怎么配置与运行、什么不做。取舍与弃案记架构仓 ADR-061 与本仓 [`decisions/`](../../decisions/README.md)。

## 背景 / 目标

- 引擎侧已有 Account Server（架构仓 ADR-054，`LumioServer/account-server/`）：用户名 / 口令、JSON 文件、只绑环回、WS 子协议 `lumio-account-v1`。它是切片级的，没有邮箱、头像、会话、后台。
- Owner 裁决（2026-09-04，ADR-061）：平台吸收账号服成为**唯一账号权威**，**一库两端口**（WS 契约端口不改 + HTTP 端口给网页），持久层 PostgreSQL，`AccountWorld` 保留为账号域运行态模型（将来 Steam / 移动端 / 真机客户端走同一账号端口）。
- 目标：玩家用邮箱注册、选头像、进大厅、点开游戏就在浏览器里与别人联机；运营者在后台看用户、活跃、埋点、反馈、登录记录。

## 拓扑

```text
浏览器 / 未来原生客户端（Steam / iOS / Android）
  ├── HTTPS ──> lumio-platform（Kestrel 单进程）
  │              ├── /                 SPA（大厅 / 注册登录 / 反馈 / 后台，按角色路由）
  │              ├── /api/*            JSON API（Cookie 会话；platform-port-v1）
  │              ├── /account          WS lumio-account-v1（account-port-v1，字段不改；Bot 启动器 / 集成考卷 / 工具）
  │              ├── /games/<slug>/    静态游戏页（LumioClient 既有形态）+ contract.json
  │              ├── /openapi/v1.json  API 文档（DTO 真值在 C#）
  │              └── PostgreSQL        账号 / 凭证哈希 / 登录记录 / 游戏目录 / 反馈 / 埋点 / 设置 / 审计
  └── WSS ────> Game Server（LumioServer；verify_admission 离线验票，零改动）
```

四块设计各一份文档：[账号域](account.md)、[大厅与启动](lobby-launch.md)、[反馈](feedback.md)、[后台与埋点](admin-analytics.md)。

## 进程与目录结构（骨架规格，P0-1 卡按此建）

```text
LumioPlatform/
├── global.json                       SDK 10.0.100，rollForward latestFeature
├── Directory.Build.props / .targets  与 LumioServer/account-server 同一套纪律（net10.0、C# 14、警告即错误、中央包版本、锁定还原、BannedApiAnalyzers；生产项目禁引测试包）
├── Directory.Packages.props          EF Core 10.0.x、Npgsql.EntityFrameworkCore.PostgreSQL 10.0.x、Microsoft.AspNetCore.OpenApi 10.0.x、BouncyCastle.Cryptography 2.6.1、Isopoh.Cryptography.Argon2 2.0.0、xunit.v3 3.2.2 / xunit.runner.visualstudio 3.1.5 / Microsoft.NET.Test.Sdk 18.8.1（与账号服同版本）、MailKit（P1-2 起）
├── NuGet.config / build.proj         照 account-server（traversal build：src/**、tests/**）
├── eng/
│   ├── BannedSymbols.banned-public-api.txt   照 account-server 复制
│   ├── dev-db.sh / dev-db.ps1        起本地 PostgreSQL 17（docker compose，缺 compose 时 docker run），打印可导出的连接串
│   ├── dev-run.sh / dev-run.ps1      设 PLATFORM_LISTEN_URL=http://127.0.0.1:5080 后 dotnet run
│   └── verify-contract-mirror.sh     contract/*.json 与架构仓 ORIGIN 提交字节一致
├── contract/                         ORIGIN（架构仓提交号）+ account-port-v1.json + platform-port-v1.json 镜像
├── src/Lumio.Platform/               领域与数据：EF Core `PlatformDbContext`、实体、迁移（`Data/`）、各功能的领域服务
├── src/Lumio.Platform.Account/       账号域（从 LumioServer/account-server/src/Lumio.Server.Account 搬入，见 account.md）
├── src/Lumio.Platform.App/           宿主：`Program`（子命令分发）、`PlatformHost.Build(args, options, requireDatabase)`、`PlatformOptions`（只读环境变量）、`OpenApiExport`、端点分组（Account/ Lobby/ Feedback/ Admin/ Track/）、WS 端口适配（AccountProtocolServer 搬入）、`openapi/v1.json`（入库生成物）、`wwwroot/`（不入库，由 web 构建产出）
├── tests/Lumio.Platform.Tests/       xunit.v3；宿主测试起真进程；契约用例逐条对应
├── web/                              React 19 + TS + Vite（`pnpm`）；`build` 输出到 `../src/Lumio.Platform.App/wwwroot`；`verify` = lint + typecheck + test + openapi:check
├── docker-compose.yml                postgres:17 + platform（Dockerfile 多阶段：pnpm build → dotnet publish）
└── .github/workflows/repository-policy.yml   spec-lint + README 策略 + dotnet build/test（Postgres service）+ pnpm verify
```

宿主关键形状（避免执行者重踩坑，来源见 [决策 0001](../../decisions/0001-openapi-export-command.md)）：

- `PlatformHost.Build(args, options = null, requireDatabase = true)`：`WebApplication.CreateBuilder` → `UseUrls(options.ListenUrl)` → `AddOptions<PlatformOptions>()`（`requireDatabase` 时 `.Validate(连接串非空).ValidateOnStart()`）→ `AddDbContext<PlatformDbContext>` 用 `IOptions<PlatformOptions>` 惰性取连接串 → `AddOpenApi("v1")` → 端点 → `UseDefaultFiles/UseStaticFiles` → SPA 回退（`/api`、`/openapi`、`/account`、`/games` 之外的非文件路径回 `index.html`）→ `ApplicationStarted` 打印 readiness 行。
- `Program.Main`：`openapi-export <file>` 子命令走 `OpenApiExport`；否则 `Build(args).Run()`，`OptionsValidationException` → stderr `PLATFORM_INIT_FAILED …` 退出码 1，其他异常 → `PLATFORM_FATAL …` 退出码 2，用法错误退出码 3。
- 测试起宿主：`PlatformHost.Build([], new PlatformOptions{ DatabaseConnectionString = 测试串, ListenUrl = "http://127.0.0.1:0" })` → `StartAsync` → 从 `IServerAddressesFeature` 取端口。

## 配置（全部环境变量，无配置文件、无内置默认口令）

| 变量 | 必填 | 含义 |
| --- | --- | --- |
| `PLATFORM_DB_CONNECTION_STRING` | 是 | Npgsql 连接串；缺失即启动失败（退出码 1） |
| `PLATFORM_LISTEN_URL` | 否 | 监听地址，默认 `http://127.0.0.1:0`；非环回地址须先确认访问控制 |
| `PLATFORM_PUBLIC_ORIGIN` | 生产必填 | 对外 origin（邮件链接、Cookie `Secure` 判定） |
| `PLATFORM_GAMES_ROOT` | 大厅启用后必填 | 静态游戏包根目录 |
| `PLATFORM_REGISTRATION_PROFILE` | 否 | `test` / `production`（默认 `production`），见 account.md |
| `LUMIO_ACCOUNT_ADMISSION_PRIVATE_KEY_HEX` | 是 | Ed25519 种子（32 字节 hex），与 account-server 同名 |
| `LUMIO_ACCOUNT_ADMISSION_KEY_ID` | 否 | u8 keyId，默认 1 |
| `LUMIO_ACCOUNT_BOT_TOOL_PUBLIC_KEY_HEX` | 是 | Bot 工具凭证公钥 |
| `PLATFORM_SMTP_HOST` / `_PORT` / `_USER` / `_PASSWORD` / `_FROM` | 邮件启用必填 | SMTP；未配置时注册请求返回 503 `email_unconfigured` |
| `PLATFORM_EMAIL_ALLOW_CONSOLE` | 否 | `1` 时验证码打到日志（仅开发；生产禁止） |
| `PLATFORM_BOOTSTRAP_ADMIN_EMAIL` | 否 | 启动时把该邮箱账号幂等提升为 admin 并写审计 |
| `PLATFORM_TEST_DB_CONNECTION_STRING` | 测试 | 测试库连接串；缺失即测试失败 |

## 运行与进程边界

- readiness：stdout 单行 `PLATFORM_READY ` + JSON `{"port":N,"pid":P,"listen":"http://127.0.0.1:N","database":"postgresql","accountPort":"/account","contractIds":["lumio.account-port.v1","lumio.platform-port.v1"]}`（骨架期 `contractIds` 为空数组）。
- 退出码：0 正常关闭；1 初始化失败（缺配置、迁移失败、数据库不可达）；2 运行期致命；3 参数错误。
- 关闭：SIGTERM → 停止接受连接 → 关闭 WS → flush → 0。
- 启动时自动 `Migrate`（单实例假设；多实例前改为显式迁移步骤，另立决策）。

## 部署假设（v1，待调研定案）

平台、Game Server、PostgreSQL 同一台机器、各一个容器；TLS 终结与 WSS 由反向代理或 Kestrel 直出，二选一由架构仓 `plans/2026-09-04-platform-topology-research-prompt.md` 的调研给出结论。平台设计对拓扑保持中立：launch 端口返回分配好的 `wsUrl + roomId`，分配器可换（见 lobby-launch.md）。

## 收口门槛与验证

见 [`AGENTS.md`](../../AGENTS.md)「收口门槛」。宿主级验收：`dotnet run --project src/Lumio.Platform.App` 打印 `PLATFORM_READY`；`curl /healthz` 200 且 `database: ok`；`/` 返回 SPA；`lumio-platform openapi-export` 与入库文件零 diff。

## 非目标（v1）

匹配 / 多房间分配器实现、Steam / 第三方登录、找回 / 改密、聊天 / 好友 / 排行、CDN、多实例部署、在线吊销凭证、限流（暴露公网前必补，P4-2）。

## 待解决

- 拓扑与容量（调研中）：进程 ↔ 房间 ↔ 容器映射、单机双容器成立规模、拆机信号。
- 中文用户名：当前沿用 ADR-054 ASCII grammar；放开需改凭证 `loginName` 字段类型（新 ADR）。
- `uid` 公开数字 ID 是否保留（默认保留）。
- 默认头像集数量与资产（默认 12，资产由美术给）。

## 相关

- 架构仓：ADR-054、ADR-061、`engine/wire/account-port-v1.json`、`engine/wire/platform-port-v1.json`、`knowledge/features/ds-server.md` M2
- 本仓：[`standards/repository-architecture.md`](../standards/repository-architecture.md)、[`plans/2026-09-04-platform-ms1-cards.md`](../../plans/2026-09-04-platform-ms1-cards.md)
