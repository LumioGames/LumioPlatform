# 0002 · 需要数据库的测试缺连接串即失败，不跳过；连接串只来自环境变量

- 日期:2026-09-04
- 状态:生效

## 背景

平台持久真值是 PostgreSQL（架构仓 ADR-061，Owner 裁决「从第一天用 Postgres」）。宿主启动、迁移、账号端口、后台查询的测试都需要真实数据库。常见做法是「没有数据库就 Skip」，但被跳过的测试在本地与 CI 上都显示绿色，等于假守护（架构仓 `lessons.md`「有一份看起来在守护的东西必须证明它会响」）。

## 决策

- 测试从 `PLATFORM_TEST_DB_CONNECTION_STRING` 读取连接串；缺失时抛出带修复指引的异常（提示运行 `eng/dev-db.sh`），测试**失败**而非跳过。
- 服务进程只从 `PLATFORM_DB_CONNECTION_STRING` 读取连接串；不读配置文件、不内置默认值、不在源码或测试里硬编码开发机路径与口令。
- 本地开发数据库由 `eng/dev-db.sh` 以 Docker 起 `postgres:17`（compose 不可用时退化为 `docker run`），脚本输出可直接导出的连接串；CI 用 GitHub Actions service container。
- 测试数据库与开发数据库分离（`lumio_platform_test` / `lumio_platform`）；测试允许对测试库做迁移与清表，不得指向开发库。

## 后果

- `dotnet test` 在没有数据库的机器上必红，这是有意的：绿灯只在真的验证过时出现。
- 本机 Docker 不可用时（2026-09-04 Owner 开发机 colima 因 lima 跑在 Rosetta 下无法启动）需先修 Docker 或另备 PostgreSQL；不为此放宽判据。
