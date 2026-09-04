---
name: account
description: 账号域设计——AccountWorld 运行态 + PostgreSQL 持久真值、一库两端口、注册策略、会话、凭证签发;改账号 / 登录 / 凭证前查
metadata:
  type: doc
  status: 设计中
---

# 账号域

平台是 Lumio **唯一账号权威**。账号域 = `Lumio.Platform.Account` 程序集（从 `LumioServer/account-server/src/Lumio.Server.Account` 搬入，命名与结构不变）+ PostgreSQL 持久层 + 两个端口。任何客户端（浏览器、Bot 启动器、将来的 Steam / 移动端）都只经这两个端口拿身份与准入凭证；Game Server 只验票不认人。

## 背景 / 目标

- 字段真值：架构仓 `engine/wire/account-port-v1.json`（WS 端口，一字不改）与 `engine/wire/platform-port-v1.json`（HTTP 端口）；裁决：ADR-054、ADR-061。
- 要解决的新需求：邮箱注册、公开 ID、用户名、头像、网页会话、后台可管；同时不破坏 RM-00011 考卷与 Bot 启动器已消费的 WS 协议。

## 设计

### 模型

**运行态：`AccountWorld`**（低频 ECS World，ADR-054 §2 原语义）。`AccountEntity` 按稳定 `AccountId` 在登录时加载或创建，登出只结束会话；账号身份数据是 `AccountIdentityComponent`（扩展字段见下表）；凭证材料只在 `CredentialStore`（静态哈希），绝不进组件、不回响应、不进日志。

**持久真值：PostgreSQL**。`DurableAccountStore`（JSON 文件）由 EF Core 存储实现取代；`AccountWorld` 是活跃账号的运行态，数据库是全量持久真值。账号的一切写入只经 `AccountRuntime` 一条路径（含后台的封禁 / 改角色）；后台读走数据库只读投影。

| 字段 | 存储 | 规则 |
| --- | --- | --- |
| `accountId` | `accounts.account_id` 唯一 | `^acct_[0-9a-f]{32}$`，密码学随机、永不复用；进准入凭证 |
| `uid` | `accounts.uid` 唯一 | 公开数字 ID，从 100000 起自增；UI / 反馈 / 后台检索用 |
| `loginName` | `accounts.login_name` 唯一、大小写敏感 | = 用户名；ADR-054 grammar `^[A-Za-z][A-Za-z0-9_-]{2,31}$`；Bot 命名空间 `^Bot[0-9]+$` 规则不变；进准入凭证 |
| `email` | `accounts.email` 唯一 | 小写归一；HTTP 登录标识；Bot 账号与 `test` profile 创建的账号可为空 |
| `emailVerifiedAt` | `accounts.email_verified_at` | 注册验证码通过时写入 |
| `avatarId` | `accounts.avatar_id` | 系统默认头像集编号（1..N，N 由 `web/public/avatars/` 资产数决定，默认 12）；不支持上传 |
| `role` | `accounts.role` | `player` / `admin` |
| `status` | `accounts.status` | `active` / `banned`（封禁后两端口登录均拒 `account_banned`，已发凭证到期自然失效） |
| `createdAt` / `lastLoginAt` | `accounts.*` | UTC |
| 口令哈希 | `account_credentials(account_id, argon2id_hash, updated_at)` | Argon2id（RFC 9106），每账号唯一盐；与身份分表 |

其余表：`email_verifications(email, code_hash, expires_at, attempts, created_at)`、`login_attempts(id, account_id?, identifier, port ws|http, outcome, error_code?, ip, user_agent, at)`。

### 端口一：WS `lumio-account-v1`（路径 `/account`）

- 消息形状、失败码、limits、准入凭证格式、Bot 工具凭证、顶号通知：全部照 `account-port-v1.json`，本仓不另写。`AccountProtocolServer` 搬入 `Lumio.Platform.App`，挂在 Kestrel 的 `/account` 路径（同进程同端口）。
- **注册策略 profile**（`PLATFORM_REGISTRATION_PROFILE`）：
  - `test`：`login_or_register` 照 ADR-054 对任何合法 loginName 登录即注册（集成考卷、开发）；口令测试档案 `123456` 按契约 `passwordProfile.testProfile`。
  - `production`（默认）：WS 端口只允许 **Bot 命名空间 + 有效工具凭证**注册；普通 loginName 不存在时拒绝 `registration_requires_platform`（HTTP 契约新增码，WS 侧以 `Error` 消息携带）；已存在的人类账号仍可用 loginName + 口令登录并取凭证。生产不得开 `test`。
- 就绪行与退出码并入平台进程边界（platform.md）；契约里 `process.accountServer` 字段随 ADR-061 更新（`storePath` → `database`）。

### 端口二：HTTP `/api/account/*`（`platform-port-v1.json`）

| 方法 路径 | 鉴权 | 请求 | 成功 | 失败码 |
| --- | --- | --- | --- | --- |
| `POST /api/account/register/request-code` | 无 | `{ email }` | 204 | `invalid_email`、`email_taken`、`email_unconfigured`(503)、`code_resend_cooldown` |
| `POST /api/account/register` | 无 | `{ email, code, loginName, password }` | 201 `Profile` + Set-Cookie | `invalid_email`、`email_taken`、`verification_code_invalid`、`verification_code_expired`、`invalid_username`、`username_taken`、`bot_namespace_register_forbidden`、`invalid_password` |
| `POST /api/account/login` | 无 | `{ email, password }` | 200 `Profile` + Set-Cookie | `invalid_credentials`（不区分邮箱不存在与口令错）、`account_banned` |
| `POST /api/account/logout` | 会话 | — | 204 | `unauthorized` |
| `GET /api/account/me` | 会话 | — | 200 `Profile` | `unauthorized` |
| `PUT /api/account/me/avatar` | 会话 | `{ avatarId }` | 200 `Profile` | `invalid_avatar` |
| `GET /api/account/avatars` | 无 | — | 200 `[{ id, url }]` | — |

`Profile = { accountId, uid, loginName, email, avatarId, role, createdAt }`。错误应答统一 `{ code, detail }`，HTTP 状态：400 校验类、401 `unauthorized`、403 `forbidden` / `account_banned`、409 `*_taken`、503 `email_unconfigured`。

- 邮箱验证码：6 位数字，10 分钟有效，最多 5 次尝试，重发冷却 60 秒；库中只存 SHA-256；邮件经 SMTP（MailKit）。SMTP 未配置 → 503，不静默；`PLATFORM_EMAIL_ALLOW_CONSOLE=1` 仅开发。
- 会话：ASP.NET Cookie 认证（[决策 0003](../../decisions/0003-no-aspnet-identity.md)）；Cookie `lumio_platform_session`，HttpOnly、SameSite=Lax、`Secure` 当 `PLATFORM_PUBLIC_ORIGIN` 为 https；14 天滑动；principal 载 `accountId / uid / loginName / role`。封禁生效于下一次请求（每次请求核 `status`）。
- 登录记录：两端口所有登录尝试（成功 / 失败 / 码）写 `login_attempts`。

### 准入凭证签发（不变）

格式、签名（LumioBinV1 + LumioSignatureV1 Ed25519）、`keyId`、TTL 300s 全部照 `account-port-v1.json`；签发点两个：WS `LoginOrRegisterAck`（原样）与 HTTP `POST /api/games/{slug}/launch`（见 lobby-launch.md）。私钥只经环境变量注入。

### 失败语义

- `wrong_password` / `invalid_credentials` 零覆写；并发首次注册必收敛为一个 AccountId（数据库唯一约束 + 事务重试）。
- Bot 命名空间四触点（register / claim / login / admission）设防只在账号域一处，HTTP 注册对 Bot 命名空间一律 `bot_namespace_register_forbidden`。
- 口令、哈希、凭证原文、私钥不进响应、审计、日志、组件。

## 待解决

- 中文用户名（改凭证字段类型，需新 ADR）。
- 找回 / 改密、限流（P4-2）、外部身份提供方（Steam / Apple）——各另立 ADR。

## 相关

- 架构仓：ADR-054、ADR-061、`engine/wire/account-port-v1.json`、`engine/wire/platform-port-v1.json`
- 本仓：[`platform.md`](platform.md)、[`lobby-launch.md`](lobby-launch.md)、[决策 0003](../../decisions/0003-no-aspnet-identity.md)
