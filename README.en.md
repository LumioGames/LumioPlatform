# LumioPlatform

> The Lumio game platform: the single account authority, the game lobby, feedback, and the operations console. One ASP.NET Core 10 process + a React 19 SPA + PostgreSQL.

[简体中文](README.md)

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

## Responsibilities

- **Single account authority**: e-mail registration, AccountId / UID / login name / default avatars, Argon2id passwords, Ed25519 admission-credential issuance, Bot-namespace enforcement; one store, two ports (the `lumio-account-v1` WebSocket contract port plus HTTP `/api/account/*`).
- **Game lobby**: the published-game catalogue, static hosting of game pages under `/games/<slug>/`, and `POST /api/games/{slug}/launch`, which issues a credential and returns the Game Server endpoint (a room-allocator interface exists from day one; v1 uses fixed endpoints).
- **Feedback**: bug / suggestion form with status workflow, one-click Feishu / QQ group links (configuration-driven).
- **Operations console and analytics**: users, login attempts, bans, game catalogue, platform settings, feedback triage; event tracking with DAU / launch dashboards.

## Explicitly not responsible for

- No ECS, DS, voxel or gameplay; the platform neither simulates games nor proxies game traffic. Credential verification (`verify_admission`) lives in `LumioServer`, game pages in `LumioClient`, the integration harness in `LumioGame`.
- No public protocol definitions: the account port, admission credential and launch port are owned by `LumioGameEngine/engine/wire/` (`lumio.account-port.v1`, `lumio.platform-port.v1`); `contract/` here is a byte-level mirror only.
- Not in v1: matchmaking / multi-room allocation, Steam or third-party login, password recovery, chat / friends / leaderboards, CDN, multi-instance deployment, online credential revocation.

## Dependencies

- Architecture repo `LumioGameEngine`: ADR-054, ADR-061, `engine/wire/account-port-v1.json`, `engine/wire/platform-port-v1.json`.
- Runtime: .NET SDK 10.0.100+, Node ≥ 22 + pnpm, PostgreSQL 17 (locally via `eng/dev-db.sh`).
- Injected by the deployment: `PLATFORM_DB_CONNECTION_STRING`, `LUMIO_ACCOUNT_ADMISSION_PRIVATE_KEY_HEX`, `LUMIO_ACCOUNT_BOT_TOOL_PUBLIC_KEY_HEX`, SMTP settings — the full table is in `.spec/knowledge/features/platform.md`.

## Closeout gate

```bash
node .spec/tools/spec-lint.mjs && node --test .spec/tools/spec-lint.test.mjs
```

Once code lands (from card P0-1 on):

```bash
dotnet build build.proj -c Release && dotnet test tests/Lumio.Platform.Tests -c Release --no-build && pnpm -C web verify
```

Tests require `PLATFORM_TEST_DB_CONNECTION_STRING` to point at a reachable PostgreSQL; when it is missing they fail instead of skipping.

## Documentation

- Hub and dispatch: [`.spec/AGENTS.md`](.spec/AGENTS.md); knowledge index: [`.spec/knowledge/README.md`](.spec/knowledge/README.md); hard rules: [`.spec/rules/system.md`](.spec/rules/system.md).
- Design: [platform overview](.spec/knowledge/features/platform.md) · [account domain](.spec/knowledge/features/account.md) · [lobby and launch](.spec/knowledge/features/lobby-launch.md) · [feedback](.spec/knowledge/features/feedback.md) · [console and analytics](.spec/knowledge/features/admin-analytics.md) · [web visual language](.spec/knowledge/features/platform-ui.md).
- Boundaries: [`repository-architecture.md`](.spec/knowledge/standards/repository-architecture.md); decisions: [`.spec/decisions/`](.spec/decisions/README.md); implementation blueprint: [`.spec/plans/2026-09-04-platform-ms1-cards.md`](.spec/plans/2026-09-04-platform-ms1-cards.md).

## Agent integration

- **LumioAgentSpec**: the spec root is `.spec/` (force-loaded by Claude Code through `@import` lines in `CLAUDE.md`; `.claude/agents`, `.claude/skills`, `.agents/skills` symlink into `.spec/`). A `.claude/settings.json` hook runs `spec-lint` before every commit.
- **Workflow (workflow.games)**: `.workflow` binds the `lumiogamesengine` project; cross-repo requirement truth lives in the Workflow requirement room, in-repo execution granularity in `.spec/tasks/`; planning and write-backs go through the Workflow Agent plugin (drafts in `.workflow-drafts/`, gitignored).
