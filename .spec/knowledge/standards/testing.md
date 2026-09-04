---
name: testing
description: 测试与验收——xunit / vitest 分层、数据库测试纪律、契约用例镜像与验证证据;实现功能/修 bug 时查
metadata:
  type: doc
  status: 设计中
---

# 测试与验收

> 方法层面的 TDD 节奏在 `skills/test-driven-development`；本文只定本仓的分层政策、数据库纪律、契约镜像与验收证据口径。

## 分层

| 层 | 工具 | 覆盖 | 需要什么 |
| --- | --- | --- | --- |
| 后端单元 | xunit.v3（`tests/Lumio.Platform.Tests`） | 账号域纯逻辑（grammar、哈希、凭证编解码、Bot 命名空间）、分配器、埋点校验 | 无外部依赖 |
| 后端宿主 | xunit.v3，同一测试项目 | 在 `http://127.0.0.1:0` 起真宿主：HTTP 端点、WS `/account` 端口、迁移、后台查询 | PostgreSQL（见下） |
| 前端单元 | vitest + Testing Library（`web/`） | 组件与 store；API 用 MSW 或 openapi-fetch 的 `fetch` 注入 mock | 无 |
| 端到端 | Playwright（后置，由集成卡决定） | 注册 → 登录 → 大厅 → 启动 → 真游戏服握手 | 平台 + Postgres + Game Server |

## 数据库纪律（[决策 0002](../../decisions/0002-database-tests-fail-loudly.md)）

- 需要数据库的测试读 `PLATFORM_TEST_DB_CONNECTION_STRING`；缺失即**失败**并给出修复指引，不 Skip。
- 每个测试类用独立 schema 或事务回滚保证隔离；不得依赖执行顺序；不得指向开发库。
- 迁移测试：对空库 `MigrateAsync` 成功且再次执行幂等。

## 契约用例镜像

- `contract/account-port-v1.json` 当前冻结的全部 `testCases` / `invalidCases` **逐条**有自动化测试或在验收报告里逐条对应证据（契约 `fixturesNote` 原文要求）；不得硬编码会随契约演进失效的用例数量。测试名与用例 `name` 一致，便于对账。
- `contract/platform-port-v1.json` 同口径。
- 契约镜像与架构仓 `origin/main` 字节一致由 `eng/verify-contract-mirror.sh` 保证；漂移 = 收口失败。

## 假守护与反例

- 每条新增判据（校验、限流、鉴权、生成物零 diff）同一提交内必须有按该判据构造的失败用例；「build 通过」不构成守护生效的证据。
- 变异型探针先 `git diff` 确认变异落地，再看测试转红且红在被测断言上（架构仓 `lessons.md` 同名条目）。

## 收口门槛与证据

- 命令见 [`AGENTS.md`](../../AGENTS.md)「收口门槛」；交付证据必须包含实际命令、退出码与关键输出（测试条数、失败条数、生成物 diff 为空）。
- 计数类陈述写清口径（用例条数 / 断言条数 / 日志行数），改前改后 like-for-like 各贴一次。
- 宿主人格：本机 x64-on-Rosetta 或原生 arm64 要在证据里标注。
