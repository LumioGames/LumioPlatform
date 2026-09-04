# LumioPlatform

> Lumio 游戏平台：唯一账号权威、游戏大厅、反馈与运营后台。ASP.NET Core 10 单进程 + React 19 SPA + PostgreSQL。

[English](README.en.md)

<!-- lumio-community:start -->
<div align="center">
<table>
<tr>
<td align="center" width="50%" valign="top">
<a href="https://qm.qq.com/q/PGkXh4tCyQ"><img src="https://raw.githubusercontent.com/LumioGames/.github/main/profile/assets/qr-qq.svg" width="170" alt="QQ 交流群 972220164"></a><br>
<a href="https://qm.qq.com/q/PGkXh4tCyQ"><img src="https://img.shields.io/badge/QQ%20%E4%BA%A4%E6%B5%81%E7%BE%A4-972220164-6171F0?style=for-the-badge&logo=tencentqq&logoColor=white" alt="QQ 交流群 972220164"></a><br>
<sub>什么都能聊</sub>
</td>
<td align="center" width="50%" valign="top">
<a href="https://applink.feishu.cn/client/chat/chatter/add_by_link?link_token=fffn1ae7-fd83-4315-96ac-6fa3aba3968e"><img src="https://raw.githubusercontent.com/LumioGames/.github/main/profile/assets/qr-engine.svg" width="170" alt="LumioEngine 开发者社区"></a><br>
<a href="https://applink.feishu.cn/client/chat/chatter/add_by_link?link_token=fffn1ae7-fd83-4315-96ac-6fa3aba3968e"><img src="https://img.shields.io/badge/%E9%A3%9E%E4%B9%A6%E7%BE%A4-LumioEngine%20%E5%BC%80%E5%8F%91%E8%80%85%E7%A4%BE%E5%8C%BA-5DE2C6?style=for-the-badge&logoColor=1E2A3A" alt="LumioEngine 开发者社区"></a><br>
<sub>飞书话题群 · Rust / C# 引擎层</sub>
</td>
</tr>
</table>
<sub>先进群再看代码。其它群和整体介绍见 <a href="https://github.com/LumioGames">LumioGames 主页</a>。</sub>
</div>
<!-- lumio-community:end -->

## 职责

- **唯一账号权威**：邮箱注册、AccountId / UID / 用户名 / 默认头像、Argon2id 口令、Ed25519 准入凭证签发、Bot 命名空间设防；一库两端口（WS `lumio-account-v1` 契约端口 + HTTP `/api/account/*`）。
- **游戏大厅**：已发布游戏目录、`/games/<slug>/` 静态游戏页托管、`POST /api/games/{slug}/launch` 签发凭证并给出 Game Server 地址（房间分配器接口从第一天存在，v1 固定端点）。
- **反馈**：bug / 建议表单与状态流转，飞书群 / QQ 群一键跳转（配置驱动）。
- **运营后台与埋点**：用户、登录记录、封禁、游戏目录、平台设置、反馈处理；事件上报与 DAU / 启动次数看板。

## 明确不负责什么

- 不实现 ECS、DS、Voxel 或玩法；不模拟游戏、不代理游戏流量；Game Server 验票（`verify_admission`）在 `LumioServer`，游戏页本身在 `LumioClient`，集成考卷在 `LumioGame`。
- 不定义公共协议：账号端口、准入凭证、launch 端口的唯一真值在架构仓 `LumioGameEngine/engine/wire/`（`lumio.account-port.v1`、`lumio.platform-port.v1`），本仓 `contract/` 只放字节级镜像。
- v1 不做：匹配 / 多房间分配、Steam / 第三方登录、找回 / 改密、聊天 / 好友 / 排行、CDN、多实例部署、在线吊销凭证。

## 依赖

- 架构仓 `LumioGameEngine`：ADR-054、ADR-061、`engine/wire/account-port-v1.json`、`engine/wire/platform-port-v1.json`。
- 运行：.NET SDK 10.0.100+、Node ≥ 22 + pnpm、PostgreSQL 17（本地经 `eng/dev-db.sh` 起 Docker 容器）。
- 部署面注入：`PLATFORM_DB_CONNECTION_STRING`、`LUMIO_ACCOUNT_ADMISSION_PRIVATE_KEY_HEX`、`LUMIO_ACCOUNT_BOT_TOOL_PUBLIC_KEY_HEX`、SMTP 等，全表见 `.spec/knowledge/features/platform.md`。

## 收口门槛

```bash
node .spec/tools/spec-lint.mjs && node --test .spec/tools/spec-lint.test.mjs
```

代码骨架收口时执行：

```bash
dotnet restore build.proj --locked-mode && dotnet build build.proj -c Release --no-restore && dotnet test tests/Lumio.Platform.Tests -c Release --no-build && pnpm -C web verify
```

测试需要 `PLATFORM_TEST_DB_CONNECTION_STRING` 指向可用 PostgreSQL；缺失即失败，不跳过。

## 文档入口

- 中心文档与调度：[`.spec/AGENTS.md`](.spec/AGENTS.md)；知识导航：[`.spec/knowledge/README.md`](.spec/knowledge/README.md)；硬红线：[`.spec/rules/system.md`](.spec/rules/system.md)。
- 设计：[平台总览](.spec/knowledge/features/platform.md) · [账号域](.spec/knowledge/features/account.md) · [大厅与启动](.spec/knowledge/features/lobby-launch.md) · [反馈](.spec/knowledge/features/feedback.md) · [后台与埋点](.spec/knowledge/features/admin-analytics.md) · [网页视觉](.spec/knowledge/features/platform-ui.md)。
- 边界：[`repository-architecture.md`](.spec/knowledge/standards/repository-architecture.md)；决策：[`.spec/decisions/`](.spec/decisions/README.md)；实施蓝图：[`.spec/plans/2026-09-04-platform-ms1-cards.md`](.spec/plans/2026-09-04-platform-ms1-cards.md)。

## Agent 接入

- **LumioAgentSpec**：规范根 `.spec/`（Claude Code 经 `CLAUDE.md` 的 `@import` 强制载入；`.claude/agents`、`.claude/skills`、`.agents/skills` 软链进 `.spec/`）。提交前 `spec-lint` 由 `.claude/settings.json` 钩子兜底。
- **Workflow（workflow.games）**：`.workflow` 绑定项目 `lumiogamesengine`；跨仓需求真值在 Workflow 需求室，仓内执行粒度在 `.spec/tasks/`；规划与写回经 Workflow Agent 插件（草稿落 `.workflow-drafts/`，已 gitignore）。
