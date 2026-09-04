---
name: knowledge
description: 项目知识库导航——查"某事怎么做"(standards)或"某功能怎么设计的"(features)时,从这里找到对应 .md
metadata:
  type: index
---

# Knowledge(项目知识库 · 导航)

本文件是 `knowledge/` 下所有 .md 的导航 meta:一行描述 + 路径,按需下钻。

> **导航行与各文档 frontmatter `description` 同一句话口径,只写「是什么 + 何时查」。** 交付历史在 git,不进文档;长度 / status 枚举 / 登记覆盖 / 链接可达由 `node .spec/tools/spec-lint.mjs` 机械校验。

## standards/(开发规范 · 要遵守的「怎么做」)

| 文档 | 一句话 |
|------|--------|
| [`standards/workflow.md`](standards/workflow.md) | 开发工作流:分支/提交/合并·PR 与知识同步义务——动手改代码、开 PR 前查 |
| [`standards/code-style.md`](standards/code-style.md) | 代码与文档风格:C# / TypeScript 约定、命名、注释原则、生成物纪律——写代码/建文档时查 |
| [`standards/testing.md`](standards/testing.md) | 测试与验收:xunit / vitest 分层、数据库测试纪律、契约用例镜像与验证证据——实现功能/修 bug 时查 |
| [`standards/dispatch.md`](standards/dispatch.md) | 派活模板:worker 派遣与 reviewer 触发的 prompt 骨架——主 loop 扇出任务或触发审查时查 |
| [`standards/repository-architecture.md`](standards/repository-architecture.md) | 仓库边界与跨仓接缝——平台拥有什么、与 Server / Client / Game / 架构仓的契约关系;改公共语义或部署边界前查 |

## features/(功能设计与记录 · 供了解)

| 文档 | 一句话 |
|------|--------|
| [`features/_TEMPLATE.md`](features/_TEMPLATE.md) | 新功能文档模板——新增功能记录时照此建,放对 领域 / 模块 |
| [`features/platform.md`](features/platform.md) | 游戏平台总览——拓扑、进程与目录结构、配置与运行、部署假设、非目标;开任何平台卡前先读 |
| [`features/account.md`](features/account.md) | 账号域设计——AccountWorld 运行态 + PostgreSQL 持久真值、一库两端口、注册策略、会话、凭证签发;改账号 / 登录 / 凭证前查 |
| [`features/lobby-launch.md`](features/lobby-launch.md) | 大厅与启动设计——游戏目录、静态游戏页托管、launch 端口与房间分配器接口;改大厅或接入新游戏前查 |
| [`features/feedback.md`](features/feedback.md) | 反馈设计——bug / 建议表单、状态流转、社群外链设置;改反馈或群链接前查 |
| [`features/admin-analytics.md`](features/admin-analytics.md) | 运营后台与埋点设计——管理员角色、用户 / 登录记录 / 封禁、事件上报与看板聚合;改后台或埋点前查 |

## lessons(经验教训 · 复发问题暂存区)

| 文档 | 一句话 |
|------|--------|
| [`lessons.md`](lessons.md) | 经验教训:reviewer 反复退回的同类问题与 Agent 常犯坑——开工前与复盘沉淀时查 |

---

新增 / 修改 / 维护知识文档(放哪、frontmatter、同步本导航)→ 用 `spec-steward` 技能;决策记录(唯一落点)→ [`../decisions/`](../decisions/README.md)。
