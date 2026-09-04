---
name: repository-architecture
description: 仓库边界与跨仓接缝——平台拥有什么、与 Server / Client / Game / 架构仓的契约关系;改公共语义或部署边界前查
metadata:
  type: doc
  status: 设计中
---

# LumioPlatform 仓库边界与契约纪律

> 本文是 LumioPlatform 对公共架构的只读规则镜像。项目治理与验证入口见 [`AGENTS.md`](../../AGENTS.md)、[`knowledge/README.md`](../README.md) 和 [`rules/system.md`](../../rules/system.md)；平台设计正文见 [`features/platform.md`](../features/platform.md)。

## 1. 唯一事实源

- 公共架构与决策的唯一来源是架构仓 `LumioGameEngine`（`.spec/knowledge/features/architecture.md`、`.spec/decisions/`）。与本仓直接相关的公共决策：ADR-054（账号端口与准入凭证）、ADR-061（LumioPlatform 仓加入、账号权威归属、一库两端口、launch 端口）。
- 公共契约真值是架构仓 `engine/wire/*.json`：`account-port-v1.json`（`lumio.account-port.v1`）与 `platform-port-v1.json`（`lumio.platform-port.v1`）。本仓 `contract/` 只放字节级镜像 + `ORIGIN` 钉定提交，`eng/verify-contract-mirror.sh` 校验一致；实现、README 或生成物不得反向定义公共语义。
- 公共语义要变：先在架构仓走 ADR → `engine/wire` 契约 → `node eng/verify-wire.mjs` → 合入 `origin/main`，再更新本仓镜像与实现。发现契约缺口 → 停下，交回物标 BLOCKED，不本地绕过。

## 2. 八仓边界与本仓所有权

| 仓库 | 拥有 | LumioPlatform 不得接管 |
| --- | --- | --- |
| `LumioGameEngine` | SDK 组装、ABI / wire 契约、架构说明与公共决策 | — |
| `LumioNativeCore` / `LumioVoxelEngine` / `LumioGameRuntime` | Native 内核、体素、ECS / Tick / Replication / GAS | 任何引擎内部语义 |
| `LumioServer` | Game Server 宿主、准入五步、`verify_admission` 离线验票、Room 与连接生命周期 | 准入验票、房间模拟、连接管理 |
| `LumioClient` | 浏览器游戏页（静态 ES module）、C# 客户端、Bot | 游戏页本身的协议逻辑 |
| `LumioGame` | 玩法内容、集成考卷启动器（`integration/`） | 集成验收尺子 |
| `LumioConfig` | 配表源与导表 | 配表 |
| **`LumioPlatform`（本仓）** | **唯一账号权威**（注册 / 登录 / 口令哈希 / 准入凭证签发 / Bot 命名空间设防）、游戏目录与大厅、launch 端口与房间分配器接口、静态游戏页托管、反馈、运营后台、埋点、平台数据库 | 见上列 |

本仓是 SDK **consumer / Host**：不引用 Native 库，不实现 ECS、DS、Voxel 或玩法；只持有 Ed25519 私钥签发凭证，验票公钥经部署面分发给 Game Server。

## 3. 跨仓接缝（谁消费谁）

| 接缝 | 方向 | 契约 |
| --- | --- | --- |
| 账号登录 / 准入凭证签发（Bot 启动器、集成考卷、工具） | 客户端 → 平台 WS `/account`（子协议 `lumio-account-v1`） | `account-port-v1.json`，字段一字不改 |
| 网页注册 / 登录 / 会话 / 启动游戏 | 浏览器 → 平台 HTTP `/api/*` | `platform-port-v1.json` |
| 游戏页取服务器地址与凭证 | `LumioClient/modules/web` 页面 → `POST /api/games/{slug}/launch` | `platform-port-v1.json` `launch` |
| 凭证验票 | Game Server 进程内 `verify_admission`（公钥 keyId 对应） | `account-port-v1.json`，Game Server 零改动 |
| 集成考卷 | `LumioGame/integration/*/launcher.mjs` 起平台进程（需 PostgreSQL），读 `PLATFORM_READY` 行 | `platform.md` §运行 |

## 4. 变更与审查红线

- 口令、哈希、凭证原文、私钥不入库、不进日志、不进证据、不回显。
- 同一职责只允许一份实现：一个账号库、一份口令哈希实现、一份凭证签发、一份 API 客户端（TS 类型只从生成物来）。
- 端口默认只绑环回并动态分配；暴露非环回地址前必须确认访问控制与限流（P5-2 卡）。
- 生成物（`openapi/v1.json`、`web/src/api/schema.d.ts`、EF Core 迁移）只能经生成命令更新并与源一起提交。
- 代码、文档和配置完成后至少运行收口门槛（[`AGENTS.md`](../../AGENTS.md)），交付证据记录实际命令、退出码与关键输出；没有新鲜证据不得声称完成。
