---
name: 2026-09-04-platform-ms1-cards
description: LumioPlatform MS-1（骨架 / 账号权威 / 大厅 / 反馈 / 后台 / 集成退役）的任务卡集与 wave DAG;落单或派活时查
metadata:
  type: doc
  status: 设计中
---

# LumioPlatform MS-1 卡片（workflow-plan: platform-ms1）

- 真值优先级（高 → 低）：架构仓 `ADR-061` / `ADR-054`、`engine/wire/platform-port-v1.json` / `account-port-v1.json`、本仓 `knowledge/features/*.md`、本仓 `decisions/000N`、本卡正文。Workflow 上任何卡的 done、handback、closeout 报告都不是真值。
- 分工原则：卡片写清上下文、怎么决策的、具体改什么；执行者只干活，架构设计在文档。每张卡自包含。
- 卡格式照 [`tasks/README.md`](../tasks/README.md)：目标 / 涉及范围（文件集）/ 验收标准 / 依赖 / 接口（Consumes / Produces）。落 Workflow 时 `module = LumioPlatform`（跨仓卡写目标仓名），标题 `[仓名] 成果描述`。

## 落单读回（2026-09-04，Owner 授权后由架构仓主会话写入并逐对象 GET 核对；bundle `wf-20260904-platform-ms1`，蓝图 `workflow-plan: platform-ms1/r1`）

- 需求室：**RM-00012**「LumioPlatform · 游戏平台 MS-1（账号权威 / 大厅 / 反馈 / 后台）」（`01a06b90-0dcc-7b3d-b730-f891572423bc`，module `LumioPlatform`）。
- 接口冻结物：架构仓 `origin/main` `c9f017b`（PR #77：ADR-061 + `platform-port-v1.json` + `account-port-v1.json` 修订）。
- 验收项类型「需求验收」/ 初始状态「未提交」（`not_started`），`sourceKind=ai`、`sourceRef=workflow-plan: platform-ms1/r1/<临时号>`；18 条需求引用边经 `bindRequirementReference` 写入并由 `GET /requirement-graph?roomId=` 读回（`truncated=false`）。
- 未归属里程碑、未指定 owner（API 取创建人）。卡正文里前置卡以「displayKey（临时号）」标注；下游前向引用仍用临时号，对照本表。

| 临时号 | displayKey | UUID | wave | 仓 | 状态 | 验收项 |
|---|---|---|---|---|---|---|
| R0 | R-00409 | `01a06b90-1f4f-7b1a-9d04-648fd4553014` | — | LumioPlatform | backlog | 0 |
| P0-1 | R-00410 | `01a06b90-3090-7c9e-9229-af14854726c7` | 0 | LumioPlatform | backlog | 9 |
| P1-1 | R-00411 | `01a06b90-42d6-72ad-a88b-272a47d62c1f` | 1 | LumioPlatform | backlog | 5 |
| P2-1 | R-00412 | `01a06b90-556a-73b5-b134-c60f3f54f6e8` | 2 | LumioPlatform | backlog | 8 |
| P2-2 | R-00413 | `01a06b90-5d67-7b1f-8d0c-2fcc080b96fc` | 2 | LumioPlatform | backlog | 4 |
| P3-1 | R-00414 | `01a06b90-7221-73b5-92d0-346ec484b8e5` | 3 | LumioPlatform | backlog | 7 |
| P3-2 | R-00416 | `01a06b90-7362-710a-b138-d2fb16cbc7e5` | 3 | LumioPlatform | backlog | 5 |
| P3-3 | R-00415 | `01a06b90-72fb-7eda-80a7-7fe9d4fe3372` | 3 | LumioClient | backlog | 4 |
| P4-1 | R-00417 | `01a06b90-886d-7bcd-b72e-e0c82acbca72` | 4 | LumioPlatform | backlog | 5 |
| P4-2 | R-00418 | `01a06b90-89d8-7eb9-aaa3-2cf7f028fd8e` | 4 | LumioPlatform | backlog | 6 |
| P4-3 | R-00419 | `01a06b90-8bb8-78ad-8e24-807d323d577e` | 4 | LumioPlatform | backlog | 4 |
| P5-1 | R-00420 | `01a06b90-9dab-70c1-8193-15e5fbd1e59c` | 5 | LumioGame + LumioServer | backlog | 3 |
| P5-2 | R-00421 | `01a06b90-9f99-77a3-a5a9-6ab40816933c` | 5 | LumioPlatform | backlog | 5 |

派活提示词与 wave 调度按 [`cross-repo-delivery`](../skills/cross-repo-delivery/SKILL.md)：W0 只有 P0-1（R-00410）；同 wave 异仓并行、同仓串行；每卡从派活评论钉定的 `origin/main` SHA 切 worktree。

## 依赖 DAG（按 wave）

```text
W0  P0-1 骨架（LumioPlatform）
     ↓
W1  P1-1 数据模型与首个迁移（LumioPlatform）
     ↓
W2  P2-1 账号域搬入 + WS 端口 + Postgres 存储（LumioPlatform）   ∥   P2-2 SPA 骨架（LumioPlatform web/）
     ↓
W3  P3-1 HTTP 账号 / 会话 / 邮件   ∥   P3-2 大厅与启动   ∥   P3-3 游戏页接 launch（LumioClient）
     ↓
W4  P4-1 反馈   ∥   P4-2 后台（用户 / 登录记录 / 游戏目录 / 设置）   ∥   P4-3 埋点与看板
     ↓
W5  P5-1 集成考卷指向平台 + 退役 account-server（LumioGame + LumioServer）   ∥   P5-2 上线前置（限流 / 访问控制 / 镜像定稿）
```

同 wave 各卡文件集互不重叠；并行 worker 各在独立 git worktree。W0 / W1 单卡串行是因为后续全部卡消费它们的产物。

## 共同执行规范（内嵌每张卡，逐字）

```markdown
## 共同执行规范（LumioPlatform MS-1，全卡相同）
### 治理原则
- 第一性原理，如无必要勿增实体：同一职责只允许一份实现（一个账号库、一份口令哈希、一份凭证签发、一份 API 客户端类型）。
- AI Agent 友好：同一件事只在一处维护；调用点显式；生成物入库可读；每件事只有一种写法。
- 彻底清理，不留兼容：不允许兼容层、别名、第二份编解码；设计原文不合适就升级改设计原文（升级 Owner），不在代码里绕。
- 验收尺子不由实现方修改。
### 真值优先级（高→低）
1. 架构仓 `.spec/decisions/ADR-061-*.md`、`ADR-054-*.md`
2. 架构仓 `engine/wire/platform-port-v1.json`、`account-port-v1.json`（本仓 `contract/` 镜像必须与 ORIGIN 提交字节一致）
3. 本仓 `.spec/knowledge/features/platform.md` / `account.md` / `lobby-launch.md` / `feedback.md` / `admin-analytics.md` 与 `.spec/decisions/000N-*.md`
4. 本卡正文
### 硬禁令（违反任一条即退回）
- 不得跨仓冒名；不得把未观测到的值写进证据。
- 不得新建第二份账号库 / 口令哈希 / 凭证签发 / 协议真值 / DTO 类型；不得手改生成物。
- 不得在源码、测试或脚本里硬编码开发机绝对路径、口令、连接串；外部配置只经环境变量，缺失时显式失败（不 Skip、不静默降级）。
- 密钥 / 凭据 / 口令哈希不入库、不进日志、不进证据、不回显。
- 不得在 CI 必过检查失败时以 admin 合入；不得 push 受保护分支；开 PR 等主 loop 审查。
- 不得替 Owner 补产品决定：契约缺口一律升级为契约缺陷（交回物标 BLOCKED），不本地打补丁绕过。
- 生产构建不得开启任何 dev-only 开关（`PLATFORM_EMAIL_ALLOW_CONSOLE`、`PLATFORM_REGISTRATION_PROFILE=test`）。
### 工作方式
- 独立 git worktree；进场先 `git fetch` 并从派活评论钉定的 `origin/main` SHA 切分支；先读本仓 `AGENTS.md` / `.spec/` 导航到的规范与被改源文件。
- 测试先行：每条可自动化的验收先有失败测试再有生产代码；需要数据库的测试按 `decisions/0002` 纪律。
- 文件集只在本卡「涉及范围」内改动；越界即停并报告。
- 收口门槛（`AGENTS.md`）全绿后交回；证据附实际命令、退出码与关键输出。
### 交回格式（五段，缺段即退回）
一、交付物与实际变更范围；二、逐条验收证据；三、实际运行的命令与关键输出；四、偏离、风险与未完成项（没有写「无」）；五、下游集成入口与知识沉淀落点。
```

---

<!-- card:P0-1 -->
# P0-1 · [LumioPlatform] 仓库骨架：.NET 三项目 + 测试、web/ 脚手架、契约镜像、本地数据库脚本、CI

- wave: 0 · priority: P0 · risk: medium
- 前置：架构仓 ADR-061 与 `engine/wire/platform-port-v1.json` 已在 `origin/main`（本卡钉定该提交号为 `contract/ORIGIN`）。

## 涉及范围

`global.json`、`Directory.Build.props`、`Directory.Build.targets`、`Directory.Packages.props`、`NuGet.config`、`build.proj`、`eng/`（`BannedSymbols.banned-public-api.txt`、`dev-db.sh` / `dev-db.ps1`、`dev-run.sh` / `dev-run.ps1`、`verify-contract-mirror.sh`）、`contract/`（`ORIGIN`、`account-port-v1.json`、`platform-port-v1.json`）、`src/Lumio.Platform/`（`Lumio.Platform.csproj`、`Data/PlatformDbContext.cs` 空模型）、`src/Lumio.Platform.Account/`（空程序集 + README）、`src/Lumio.Platform.App/`（`Lumio.Platform.App.csproj`、`Program.cs`、`PlatformHost.cs`、`PlatformOptions.cs`、`PlatformExitCodes.cs`、`OpenApiExport.cs`、`openapi/v1.json`）、`tests/Lumio.Platform.Tests/`（csproj、`xunit.runner.json`、`TestDatabase.cs`、`PlatformHostTests.cs`）、`web/`（package.json、tsconfig*.json、vite.config.ts、vitest.config.ts、`.oxlintrc.json`、index.html、`src/main.tsx`、`src/api/client.ts`、`src/api/schema.d.ts`、`src/app/App.tsx`、健康检查面板与其测试）、`docker-compose.yml`、`Dockerfile`、`.github/workflows/repository-policy.yml`（追加 dotnet 与 pnpm job）、`.spec/AGENTS.md`「收口门槛」（去掉「代码落地后」措辞）。

## 怎么做（规格在 `features/platform.md`「进程与目录结构」与 `decisions/0001`）

1. .NET 纪律文件照 `LumioServer/account-server/` 复制并把 `AccountServerProductionProject` 改名 `PlatformProductionProject`；包版本按 platform.md 表。
2. 三项目依赖方向 `App → Account → Platform`；`App` 用 `Microsoft.NET.Sdk.Web`，`AssemblyName` = `lumio-platform`。
3. `PlatformHost.Build(args, options, requireDatabase)` / `Program.Main` / `OpenApiExport` 按 platform.md「宿主关键形状」；`/healthz` 返回 `{ status, database }`，数据库不可达 503；SPA 回退排除 `/api` `/openapi` `/account` `/games`。
4. 测试：迁移可应用（`MigrateAsync` 幂等）、`/healthz` 200 且 `database: ok`、缺连接串 `StartAsync` 抛 `OptionsValidationException`；连接串按 `decisions/0002` 只从 `PLATFORM_TEST_DB_CONNECTION_STRING` 读。
5. `web/`：create-vite react-ts 当前模板版本（React 19、Vite 8、TypeScript 6、oxlint）+ react-router 7、@tanstack/react-query 5、zustand 5、openapi-fetch、openapi-typescript、vitest + Testing Library；`build.outDir = ../src/Lumio.Platform.App/wwwroot`（不入库）；scripts：`openapi:generate`（先 `dotnet run -- openapi-export`，再 `openapi-typescript`）、`openapi:check`（生成后 `git diff --exit-code` 两个生成物）、`verify`。
6. `eng/dev-db.sh`：`docker compose up -d postgres`，不可用时 `docker run --name lumio-platform-pg -e POSTGRES_USER=lumio -e POSTGRES_PASSWORD=lumio -e POSTGRES_DB=lumio_platform -p 5432:5432 postgres:17`；再建 `lumio_platform_test`；打印两条 `export` 行。
7. `verify-contract-mirror.sh`：`git -C <架构仓> show <ORIGIN>:engine/wire/<file>` 与 `contract/<file>` `cmp` 字节一致（架构仓路径经环境变量 `LUMIO_ARCH_REPO` 或 `../LumioGameEngine` 发现，缺失 BLOCKED 报出）。
8. CI 追加 `dotnet` job（`services: postgres:17`，设 `PLATFORM_TEST_DB_CONNECTION_STRING`）与 `web` job（`pnpm install --frozen-lockfile && pnpm verify`）。

## 验收标准

- [ ] `node .spec/tools/spec-lint.mjs && node --test .spec/tools/spec-lint.test.mjs` 通过。
- [ ] `dotnet restore build.proj --locked-mode && dotnet build build.proj -c Release --no-restore` 零警告零错误（`TreatWarningsAsErrors`）。
- [ ] 设 `PLATFORM_TEST_DB_CONNECTION_STRING` 后 `dotnet test tests/Lumio.Platform.Tests -c Release --no-build` 3 条全过；不设时 3 条全部失败且错误信息含 `eng/dev-db.sh`（贴两次输出）。
- [ ] `dotnet run --project src/Lumio.Platform.App -c Release -- openapi-export src/Lumio.Platform.App/openapi/v1.json` 后 `git diff --exit-code` 为空；文档 `paths` 含 `/healthz`。
- [ ] `pnpm -C web install --frozen-lockfile && pnpm -C web verify` 通过（lint / typecheck / vitest 1 条 / openapi:check）。
- [ ] `PLATFORM_DB_CONNECTION_STRING=<dev> dotnet run --project src/Lumio.Platform.App` 打印 `PLATFORM_READY {...}`；`curl /healthz` 200；缺变量时 stderr `PLATFORM_INIT_FAILED` 且退出码 1。
- [ ] `eng/verify-contract-mirror.sh` 通过；`contract/ORIGIN` 是架构仓 `origin/main` 上的提交号。
- [ ] CI 三个 job 在 PR 上全绿（贴 run 链接）。
- [ ] 不含任何业务端点、业务表、账号代码（本卡只搭骨架）。

## 依赖

无（本仓首卡）。

## 接口

- Produces：`PlatformHost.Build(string[] args, PlatformOptions? options = null, bool requireDatabase = true) : WebApplication`；`PlatformOptions { string? DatabaseConnectionString; string ListenUrl }`（只从环境变量读）；`PlatformDbContext(DbContextOptions<PlatformDbContext>)`；`tests/Lumio.Platform.Tests/TestDatabase.ConnectionString()`；`web/src/api/client.ts` 的 `api`（openapi-fetch，`paths` 来自生成物）。

---

<!-- card:P1-1 -->
# P1-1 · [LumioPlatform] 数据模型与首个迁移：账号 / 凭证 / 邮箱验证 / 登录记录 / 游戏目录 / 反馈 / 事件 / 设置 / 审计

- wave: 1 · priority: P0 · risk: medium
- 前置：P0-1 合入。

## 涉及范围

`src/Lumio.Platform/Data/`（实体类、`PlatformDbContext` 的 `DbSet` 与 `OnModelCreating`、`Migrations/`、`PlatformDbContextFactory.cs` 设计时工厂只读环境变量）、`tests/Lumio.Platform.Tests/Data/`。

## 怎么做（表与列在 `features/account.md`、`lobby-launch.md`、`feedback.md`、`admin-analytics.md`）

- 表：`accounts`、`account_credentials`、`email_verifications`、`login_attempts`、`games`、`feedbacks`、`events`、`platform_settings`、`audit_log`。命名 snake_case，时间 `timestamptz`（UTC），`accounts.uid` 序列从 100000 起，`props` 为 `jsonb`。
- 唯一索引：`accounts.account_id`、`accounts.uid`、`accounts.login_name`（大小写敏感）、`accounts.email`（小写归一后唯一，可空）、`games.slug`、`platform_settings.key`。
- 一个迁移 `InitialPlatformSchema`；`dotnet ef migrations add` 生成，不手改。
- 实体只做数据形状，不带业务方法；账号域行为在 P2-1。

## 验收标准

- [ ] 空库 `MigrateAsync` 成功且再次执行幂等（测试）。
- [ ] 唯一约束测试：重复 `login_name` / `email` / `uid` / `slug` 各一条插入失败（`DbUpdateException`）。
- [ ] `uid` 首条为 100000 且递增。
- [ ] 迁移文件与模型同一提交；`dotnet ef migrations has-pending-model-changes` 为无。
- [ ] 收口门槛全绿。

## 依赖

P0-1。

## 接口

- Consumes：`PlatformDbContext`（P0-1）。
- Produces：实体类型 `Account { long Id; string AccountId; long Uid; string LoginName; string? Email; DateTime? EmailVerifiedAt; int AvatarId; string Role; string Status; DateTime CreatedAt; DateTime? LastLoginAt }`、`AccountCredential { long AccountId; string Argon2idHash; DateTime UpdatedAt }`、`EmailVerification`、`LoginAttempt`、`Game`、`Feedback`、`TrackedEvent`、`PlatformSetting`、`AuditLogEntry`；`DbSet<T>` 同名。

---

<!-- card:P2-1 -->
# P2-1 · [LumioPlatform] 账号域搬入：Lumio.Server.Account 原码 + AccountWorld + Postgres 存储 + WS `/account` 端口，account-port-v1 用例 19 条全过

- wave: 2 · priority: P0 · risk: high（鉴权 / 安全面，合入前单审）
- 前置：P1-1 合入；`LumioServer/account-server/` 源码钉定 `origin/main` 提交号写在派活评论。

## 涉及范围

`src/Lumio.Platform.Account/`（搬入 `Lumio.Server.Account` 全部源文件，命名空间改 `Lumio.Platform.Account`，删 `DurableAccountStore`，新增 `PostgresAccountStore` 实现同一存取接口）、`src/Lumio.Platform.App/AccountPort/`（搬入 `AccountProtocolServer`，挂 `/account` 路径；`AccountReadyLine` 并入 `PLATFORM_READY`）、`src/Lumio.Platform.App/PlatformHost.cs`（只加 `/account` 注册与 `contractIds`）、`tests/Lumio.Platform.Tests/Account/`（契约用例逐条）、`tests/Lumio.Platform.Tests/AccountPort/`。

## 怎么做（`features/account.md`「模型」「端口一」）

- `AccountWorld` / `AccountRuntime` / `CredentialStore` / `AdmissionCredential` / `BotToolCredential` / `LumioBin` / `LumioSignature` / `Argon2idPasswordHasher` / `LoginNameRules` 原样搬入，不重构；`AccountIdentityComponent` 扩展 `uid` / `email` / `avatarId` / `role` / `status`。
- 持久层：`PostgresAccountStore` 用 P1-1 的 `Account` / `AccountCredential` 表；`AccountWorld` 按 AccountId 惰性加载；并发首次注册靠唯一约束 + 事务重试收敛。
- 注册策略 `PLATFORM_REGISTRATION_PROFILE`：`test` 照 ADR-054；`production` 非 Bot 命名空间的新 loginName 在 WS 端口拒 `registration_requires_platform`（码来自 `account-port-v1.json`，ADR-061 授权扩展；WS `Error` 消息携带）。
- 登录尝试写 `login_attempts(port = ws)`；`status = banned` 拒 `account_banned`。
- 密钥仍只从 `LUMIO_ACCOUNT_*` 环境变量读。

## 验收标准

- [ ] `contract/account-port-v1.json` 的 7 条 `testCases` + 12 条 `invalidCases` 各有同名自动化测试且全过（贴测试名清单与输出）。
- [ ] `account_restart_stability`：宿主重启后同口令重登返回同一 `accountId`（Postgres 持久）。
- [ ] `concurrent_first_login_converges`：100 个并发首次登录得到同一 `accountId`，`accounts` 恰一行。
- [ ] `production` profile 下普通新 loginName 在 WS 端口被拒 `registration_requires_platform`；`test` profile 下照旧创建。
- [ ] grep 证明：`Lumio.Platform.Account` 无 JSON 文件存储、无第二份哈希 / 签名实现；日志与测试输出不含口令、哈希、凭证原文。
- [ ] `PLATFORM_READY` 行 `contractIds` 含 `lumio.account-port.v1`，`accountPort` = `/account`。
- [ ] 收口门槛全绿。

## 依赖

P1-1。

## 接口

- Consumes：P1-1 实体与 `PlatformDbContext`。
- Produces：`AccountRuntime`（`LoginOrRegisterAsync(LoginOrRegisterRequest) → LoginOrRegisterOutcome`；`RegisterWithEmailAsync(email, loginName, password) → Account`；`VerifyPasswordAsync(email | loginName, password) → Account | wrong_password`；`IssueAdmissionCredential(Account) → (credential, expiresAt)`；`SetAvatarAsync` / `BanAsync` / `UnbanAsync` / `SetRoleAsync`）；`AccountQueries`（按 accountId / uid / email / loginName 读投影）。

---

<!-- card:P2-2 -->
# P2-2 · [LumioPlatform] SPA 骨架：路由、布局、登录注册页壳、大厅 / 反馈 / 后台占位、API 客户端接线

- wave: 2 · priority: P1 · risk: low
- 前置：P0-1 合入。

## 涉及范围

`web/src/app/`（路由表、布局、角色守卫）、`web/src/features/auth/`、`web/src/features/lobby/`、`web/src/features/feedback/`、`web/src/features/admin/`（占位页）、`web/src/stores/session.ts`、`web/src/styles/`、`web/public/avatars/`（12 张默认头像占位资产，SVG）。不改 `src/`。

## 怎么做

- 路由：`/`（大厅）、`/login`、`/register`、`/feedback`、`/me`、`/admin/*`（`role === 'admin'` 守卫，否则 403 页）。
- `session` store：`me` 查询（`GET /api/account/me`，接口在 P3-1 落地前用生成类型占位不调用）与登出。
- 文案中文；CSS Modules；不引入 UI 框架。

## 验收标准

- [ ] `pnpm -C web verify` 通过；vitest 覆盖路由守卫（player 访问 `/admin` 得 403 页）。
- [ ] 不手写任何 DTO 类型；`grep -r "fetch(" web/src` 只命中 `api/client.ts`。
- [ ] 收口门槛全绿。

## 依赖

P0-1。

## 接口

- Consumes：`web/src/api/client.ts`（P0-1）。
- Produces：路由表、`useSession()`、`features/*` 目录约定。

---

<!-- card:P3-1 -->
# P3-1 · [LumioPlatform] HTTP 账号端口：邮箱验证码注册、登录、Cookie 会话、我、头像；platform-port-v1 账号用例全过

- wave: 3 · priority: P0 · risk: high（鉴权 / 安全面，合入前单审）
- 前置：P2-1、P2-2 合入。

## 涉及范围

`src/Lumio.Platform.App/Account/`（端点、DTO record、Cookie 认证配置）、`src/Lumio.Platform/Email/`（`IEmailSender`、`SmtpEmailSender`（MailKit）、`ConsoleEmailSender` 仅 `PLATFORM_EMAIL_ALLOW_CONSOLE=1`）、`src/Lumio.Platform/Account/EmailVerificationService.cs`、`Directory.Packages.props`（+MailKit）、`tests/Lumio.Platform.Tests/AccountHttp/`、`web/src/features/auth/`（真接口接线）、`web/src/features/me/`、`src/Lumio.Platform.App/openapi/v1.json` 与 `web/src/api/schema.d.ts`（重生成）。

## 怎么做（`features/account.md`「端口二」、`decisions/0003`）

- 端点表照 account.md；错误应答 `{ code, detail }`，码来自 `contract/platform-port-v1.json`。
- 验证码：6 位、10 分钟、5 次尝试、60 秒冷却、只存 SHA-256；SMTP 未配置 → 503 `email_unconfigured`。
- Cookie：`AddAuthentication().AddCookie()`，名 `lumio_platform_session`，HttpOnly、SameSite=Lax、`Secure` 随 `PLATFORM_PUBLIC_ORIGIN`；14 天滑动；每次请求核 `status`。
- 登录尝试写 `login_attempts(port = http)`；事件 `account.registered` / `account.login`。

## 验收标准

- [ ] `contract/platform-port-v1.json` 账号相关 `testCases` / `invalidCases` 各有同名测试且全过。
- [ ] 注册全链路测试：request-code → 取码（ConsoleEmailSender 测试注入）→ register 201 + Set-Cookie → me 200。
- [ ] `invalid_credentials` 对「邮箱不存在」与「口令错」返回同一应答体（防枚举，测试比对）。
- [ ] Bot 命名空间经 HTTP 注册一律 `bot_namespace_register_forbidden`。
- [ ] SMTP 未配置且未开 console 开关时 request-code 返回 503，且不落库。
- [ ] 前端：注册 / 登录 / 我的资料 / 换头像可用（vitest 组件测试 + 手动截图）。
- [ ] `openapi:check` 零 diff；收口门槛全绿。

## 依赖

P2-1、P2-2。

## 接口

- Consumes：`AccountRuntime`（P2-1）、`useSession()`（P2-2）。
- Produces：`/api/account/*`（契约）；`IEmailSender.SendAsync(to, subject, body)`；principal claims `accountId / uid / loginName / role`。

---

<!-- card:P3-2 -->
# P3-2 · [LumioPlatform] 大厅与启动：games 表 API、`/games/<slug>/` 静态托管、launch 端口签发凭证、StaticEndpointAllocator

- wave: 3 · priority: P0 · risk: medium
- 前置：P2-1、P2-2 合入。

## 涉及范围

`src/Lumio.Platform.App/Lobby/`（`GET /api/games`、`GET /api/games/{slug}`、`POST /api/games/{slug}/launch`、静态托管中间件）、`src/Lumio.Platform/Lobby/`（`IRoomAllocator`、`StaticEndpointAllocator`、`GameCatalog`）、`tests/Lumio.Platform.Tests/Lobby/`、`web/src/features/lobby/`（真接口接线）、生成物重生成。

## 怎么做（`features/lobby-launch.md`）

- 静态托管只对 `published` slug 生效，根目录 `PLATFORM_GAMES_ROOT`，缺失时启动失败（退出码 1）。
- launch：需会话；`account.status = banned` → 403；分配器返回端点；`AccountRuntime.IssueAdmissionCredential`；事件 `game.launched`。凭证不进 URL、不进日志。

## 验收标准

- [ ] `contract/platform-port-v1.json` launch 相关用例全过；应答字段与契约一致。
- [ ] 未发布游戏：列表不出现、`/games/<slug>/` 404、launch `game_not_published`。
- [ ] launch 两次得到不同 nonce 的凭证；用 `LumioServer` 的 `verify_admission` 参考实现（或本仓契约测试的解形 + 验签）验证通过。
- [ ] 大厅页展示已发布游戏并能跳转 `/games/<slug>/`。
- [ ] 收口门槛全绿。

## 依赖

P2-1、P2-2。

## 接口

- Consumes：`AccountRuntime.IssueAdmissionCredential`。
- Produces：`IRoomAllocator.AllocateAsync(Game, AccountId, ct) → RoomEndpoint(WsUrl, Subprotocol, ContractId, RoomId)`；`/api/games/*`。

---

<!-- card:P3-3 -->
# P3-3 · [LumioClient] 浏览器游戏页改为经平台 launch 端口取地址与凭证

- wave: 3 · priority: P1 · risk: medium
- 前置：架构仓 `platform-port-v1.json` 在 `origin/main`（本卡不依赖 P3-2 合入，按契约先行；联调在 P5-1）。

## 涉及范围

`LumioClient/modules/web/`（hello / chat 页面：加载后 `POST /api/games/<slug>/launch` 同源取 `wsUrl` / `subprotocol` / `admissionCredential`；`?ws=` 只在本地考卷模式保留）、其 README 与 `node --test` 用例。

## 验收标准

- [ ] 页面无 `?ws=` 时调 launch 端口，用应答建立 WebSocket；有 `?ws=` 时行为不变（考卷模式）。
- [ ] 凭证只出现在请求头 / 握手消息，不进 URL、不进 `window.__lumioResult`。
- [ ] `node --test` 覆盖两种模式；README 更新「入口」段。
- [ ] 该仓收口门槛全绿。

## 依赖

无仓内前置（契约先行）。

## 接口

- Consumes：`platform-port-v1.json` `launch` 应答形状。

---

<!-- card:P4-1 -->
# P4-1 · [LumioPlatform] 反馈：表单、我的反馈、后台处理端点、社群外链设置

- wave: 4 · priority: P1 · risk: low
- 前置：P3-1 合入。

## 涉及范围

`src/Lumio.Platform.App/Feedback/`（玩家与后台端点）、`src/Lumio.Platform/Feedback/`、`src/Lumio.Platform.App/Settings/`（`GET /api/settings/public`、`GET/PUT /api/admin/settings`）、`tests/Lumio.Platform.Tests/Feedback/`、`web/src/features/feedback/`、`web/src/features/admin/feedback/`、`web/src/features/admin/settings/`、生成物重生成。

## 验收标准（`features/feedback.md`）

- [ ] 匿名与登录提交各一条成功；超长拒 `feedback_too_long`。
- [ ] 后台按 `status` / `type` / `gameSlug` 过滤；状态变更写 `audit_log`。
- [ ] 社群外链经设置读写，大厅与反馈页按钮跳转正确。
- [ ] 事件 `feedback.submitted` 落表；收口门槛全绿。

## 依赖

P3-1。

---

<!-- card:P4-2 -->
# P4-2 · [LumioPlatform] 后台：管理员角色与 bootstrap、用户检索 / 详情 / 封禁 / 改角色、登录记录、游戏目录管理、审计

- wave: 4 · priority: P0 · risk: high（鉴权 / 访问控制，合入前单审）
- 前置：P3-1、P3-2 合入。

## 涉及范围

`src/Lumio.Platform.App/Admin/`（Users、LoginAttempts、Games；`RequireRole("admin")`）、`src/Lumio.Platform/Admin/`（bootstrap 服务、审计写入）、`tests/Lumio.Platform.Tests/Admin/`、`web/src/features/admin/users/`、`web/src/features/admin/games/`、生成物重生成。

## 验收标准（`features/admin-analytics.md`「管理员」「用户与登录记录」「游戏目录与设置」）

- [ ] player 访问任一 `/api/admin/*` 得 403；admin 得 200（测试）。
- [ ] `PLATFORM_BOOTSTRAP_ADMIN_EMAIL` 提升幂等且写审计；邮箱不存在只告警。
- [ ] 封禁后该账号两端口登录均拒 `account_banned`，已有会话下一请求 401。
- [ ] 每个管理写操作一条 `audit_log`（before / after）。
- [ ] 收口门槛全绿。

## 依赖

P3-1、P3-2。

---

<!-- card:P4-3 -->
# P4-3 · [LumioPlatform] 埋点与看板：`/api/track` 批量上报、服务器事件、`/api/admin/stats` 聚合

- wave: 4 · priority: P1 · risk: low
- 前置：P3-1、P3-2 合入。

## 涉及范围

`src/Lumio.Platform.App/Track/`、`src/Lumio.Platform.App/Admin/Stats/`、`src/Lumio.Platform/Analytics/`、`tests/Lumio.Platform.Tests/Analytics/`、`web/src/features/admin/dashboard/`、`web/src/analytics/`（前端上报封装与埋点点位）、生成物重生成。

## 验收标准（`features/admin-analytics.md`「埋点」）

- [ ] 批量 51 条拒 `batch_too_large`；非法事件名拒 `invalid_event`；`props` > 4 KB 拒。
- [ ] `stats` 对已知固定数据集返回正确 DAU / WAU / 注册数 / 各游戏启动次数 / 反馈数（测试用例带期望值）。
- [ ] 前端在大厅曝光、点击启动、提交反馈处上报（vitest 断言调用）。
- [ ] 收口门槛全绿。

## 依赖

P3-1、P3-2。

---

<!-- card:P5-1 -->
# P5-1 · [LumioGame + LumioServer] 集成考卷指向平台账号端口；退役 `LumioServer/account-server/`

- wave: 5 · priority: P0 · risk: high
- 前置：P2-1、P3-2、P3-3 合入并 push；架构仓 ADR-061 退役条件（平台过 account-port-v1 全部用例）已由 P2-1 证据满足。

## 涉及范围

`LumioGame/integration/entity-chat/`（`launcher.mjs` 起 `lumio-platform` 进程（需 `PLATFORM_DB_CONNECTION_STRING`，CI 加 Postgres service）、解析 `PLATFORM_READY`、`account-client.mjs` 连 `/account`）；`LumioServer/account-server/`（整目录删除）、`LumioServer` README / `.spec` 中账号服条目、`LumioServer/.github/workflows/repository-policy.yml` 的 account-server job。

## 验收标准

- [ ] RM-00011 集成考卷（R4-09 口径）在平台账号端口上全绿，证据日志含平台 `PLATFORM_READY` 行。
- [ ] `LumioServer` 仓 `grep -r account-server` 零命中；CI 绿。
- [ ] 两仓收口门槛全绿；证据引用已 push 的 `origin/main` 提交。

## 依赖

P2-1、P3-2、P3-3。

---

<!-- card:P5-2 -->
# P5-2 · [LumioPlatform] 上线前置：限流、非环回绑定的访问控制确认、Dockerfile / compose 定稿、密钥分发说明

- wave: 5 · priority: P0 · risk: high（安全面，合入前单审）
- 前置：P4-1 / P4-2 / P4-3 合入；架构仓拓扑调研结论已落 ADR。

## 涉及范围

`src/Lumio.Platform.App/RateLimiting/`（登录 / 注册 / 验证码 / 反馈 / track 的 ASP.NET 内置限流策略，阈值走环境变量）、`Dockerfile`、`docker-compose.yml`、`eng/`、`.spec/knowledge/features/platform.md`「部署假设」改为定案、`tests/`。

## 验收标准

- [ ] 限流反例：超阈值请求得 429 `rate_limited`，阈值改环境变量即时生效（贴两次输出）。
- [ ] `PLATFORM_LISTEN_URL` 为非环回且 `PLATFORM_PUBLIC_ORIGIN` 非 https 时启动拒绝（退出码 1），文档写明。
- [ ] `docker compose up` 起 platform + postgres + 一个 Game Server 容器，浏览器完成注册 → 大厅 → 启动 → 握手（截图 + 日志）。
- [ ] 验票公钥分发步骤写进 platform.md 并实测。
- [ ] 收口门槛全绿。

## 依赖

P4-1、P4-2、P4-3；架构仓拓扑 ADR。
