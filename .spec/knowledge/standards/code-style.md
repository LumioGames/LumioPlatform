---
name: code-style
description: 代码与文档风格——C# / TypeScript 约定、命名、注释原则、生成物纪律;写代码/建文档时查
metadata:
  type: doc
  status: 设计中
---

# 代码与文档风格

> 能交给工具（formatter / linter / analyzer）强制的，优先交给工具；本文只写工具管不了、需要人 / Agent 判断的部分。

## 语言与文件命名（通用）

- **规范主体使用中文**；例外是根 `CLAUDE.md`、既有英文 Skill。单份文档内保持语言一致。
- 公共协议名、字段、状态值、错误码、环境变量名保持精确英文拼写；Markdown 与 JSON/YAML 保持 LF（根 `.gitattributes`）。
- 文档与目录命名 kebab-case；C# 项目名 `Lumio.Platform[.Xxx]`；TS 文件 kebab-case 或组件 PascalCase.tsx。

## 注释原则（通用）

- 注释只写**代码表达不了的约束**（为什么这样做、边界条件、外部依赖的坑）。
- 不写「改动说明」式注释——那是给评审人的话，进交回物或提交信息。
- 注释密度、命名、习语向周边既有代码看齐。

## 生成物纪律（通用）

- 生成物不得手改：`src/Lumio.Platform.App/openapi/v1.json`（`lumio-platform openapi-export`）、`web/src/api/schema.d.ts`（`pnpm openapi:generate`）、EF Core 迁移（`dotnet ef migrations add`）只能经生成命令更新，并与生成源一起提交（红线见 [`rules/system.md`](../../rules/system.md)；理由见 [决策 0001](../../decisions/0001-openapi-export-command.md)）。

## C#（后端）

- 与 `LumioServer/account-server` 同一套纪律：`net10.0`、`LangVersion 14.0`、`Nullable` 启用、`ImplicitUsings` 关闭、`TreatWarningsAsErrors`、`AnalysisLevel latest-recommended`、`BannedApiAnalyzers`（`eng/BannedSymbols.banned-public-api.txt`）、中央包版本（`Directory.Packages.props`）、`packages.lock.json` 锁定还原（`--locked-mode`）。
- 三个生产项目 + 一个测试项目，依赖方向固定：`Lumio.Platform.App → Lumio.Platform.Account → Lumio.Platform`（数据与领域）。生产项目不得引用测试包（`Directory.Build.targets` 机器强制）。
- HTTP 面用 Minimal API；请求 / 应答 DTO 是 `sealed record`，放在对应功能目录（`Account/`、`Lobby/`、`Feedback/`、`Admin/`）；一个端点一个文件组。错误应答统一 `{ "code": "<errorCode>", "detail": "<string>" }`，码来自契约。
- 账号域搬入代码保持原命名与结构（`AccountRuntime` / `AccountWorld` / `CredentialStore` / `AdmissionCredential` / `LumioBin` / `LumioSignature`…），只改命名空间与持久层；不重构。
- 日志用 `LoggerMessage` 源生成（CA1848 是错误级）；进程边界行（`PLATFORM_READY` / `PLATFORM_INIT_FAILED` / `PLATFORM_FATAL`）直接写 stdout / stderr。
- 时间一律 UTC；数据库时间列 `timestamp with time zone`。

## TypeScript（web/）

- React 19 + TypeScript strict（`verbatimModuleSyntax`、`erasableSyntaxOnly`）、Vite、React Router 7、TanStack Query 5、Zustand、CSS Modules；lint 用 oxlint（create-vite 当前默认），测试用 vitest + Testing Library。
- API 调用只经 `src/api/client.ts`（`openapi-fetch` + 生成的 `paths` 类型）；**不得手写 DTO 类型**，不得直接 `fetch('/api/...')`。
- 目录：`src/app/`（路由与布局）、`src/features/<name>/`（页面与组件）、`src/api/`（客户端与生成类型）、`src/stores/`（Zustand）。后台页面在 `src/features/admin/`，按 `role === 'admin'` 路由守卫。
- 文案中文；不引入 UI 框架大包（先用 CSS Modules），需要时另立决策。

## 数据库（PostgreSQL）

- 表名 / 列名 `snake_case`，主键 `id`（`bigint` 自增或 `uuid`）；账号业务键 `account_id`（`acct_` + 32 hex）与 `uid`（`bigint`，从 100000 起）另设唯一索引。
- 迁移只增不改历史；每张卡最多一个迁移；迁移文件与模型改动同一提交。
- 不在数据库里放口令明文、凭证原文、私钥；哈希列与身份列分表（`account_credentials` / `accounts`）。
