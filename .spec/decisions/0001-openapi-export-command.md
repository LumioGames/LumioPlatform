# 0001 · OpenAPI 文档经 `lumio-platform openapi-export` 命令导出入库，不用构建时生成

- 日期:2026-09-04
- 状态:生效

## 背景

API 的 DTO 真值在 C#（Minimal API + `Microsoft.AspNetCore.OpenApi`），web/ 用 `openapi-typescript` 从 OpenAPI 文档生成 TS 类型，同一件事只在一处维护。首选方案本是 `Microsoft.Extensions.ApiDescription.Server` 在 `dotnet build` 时生成文档，实测（2026-09-04，.NET SDK 10.0.400，包 10.0.11）两个问题：① 该工具运行入口程序集的 `Main` 并让宿主真正 `Run`，任何启动前置校验（缺数据库连接串即拒绝启动）都会让生成失败或产出被它当作错误的 stderr 行；② Minimal API 的端点数据源要到中间件管线构建（宿主 Start）后才对 ApiExplorer 可见，未 Start 时导出的文档 `paths` 为空。两者都要求为工具链放松生产启动纪律，是为生成物迁就宿主。

## 决策

- 入口程序集提供子命令 `lumio-platform openapi-export <file>`：以 `requireDatabase=false` 构建宿主，在 `http://127.0.0.1:0` 临时端口 Start，GET `/openapi/v1.json` 写入 `src/Lumio.Platform.App/openapi/v1.json`（末尾单个换行），Stop 后退出码 0。该模式不注册数据库启动校验，不触碰数据库。
- `web/package.json`：`openapi:generate` = 先跑上述命令再 `openapi-typescript` 生成 `src/api/schema.d.ts`；`openapi:check` = 生成后 `git diff --exit-code` 两个文件零差异；`pnpm verify` 含 `openapi:check`。
- `openapi/v1.json` 与 `schema.d.ts` 都入库（生成物可读、可 diff、可审），手改即 `openapi:check` 失败。
- 服务模式（无子命令）保持 `ValidateOnStart`：缺 `PLATFORM_DB_CONNECTION_STRING` 在 Kestrel 监听前拒绝启动，退出码 1。

## 后果

- 多一个子命令与一次进程启动（毫秒级），换来生产启动纪律与生成物纪律互不迁就。
- 不引入 `Microsoft.Extensions.ApiDescription.Server`；将来若官方工具改为「只 Build 不 Run 且端点可见」，可另立 ADR 取代本条。
