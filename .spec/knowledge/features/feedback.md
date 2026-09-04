---
name: feedback
description: 反馈设计——bug / 建议表单、状态流转、社群外链设置;改反馈或群链接前查
metadata:
  type: doc
  status: 设计中
---

# 反馈

玩家在平台里提 bug 或建议，运营者在后台处理；平台同时提供飞书群 / QQ 群一键跳转。

## 背景 / 目标

反馈是运营闭环的入口，要够轻：一个表单、一张表、一个后台列表；社群链接是配置，不写死在前端。

## 设计

- `feedbacks` 表：`id`、`type`（`bug` / `suggestion`）、`title`（≤ 80）、`body`（≤ 4000）、`game_slug?`、`page_url?`、`contact?`（≤ 120，可为空）、`account_id?`（登录用户自动带上）、`status`（`new` / `triaged` / `closed`）、`admin_note?`、`created_at` / `updated_at`。
- `POST /api/feedback`（可匿名；登录则带 `accountId`）→ 201 `{ id }`；失败码 `invalid_request`、`feedback_too_long`。暴露公网前加限流（P4-2）。
- `GET /api/feedback/mine`（会话）→ 本人反馈列表与状态。
- 社群外链存 `platform_settings`（键值表：`community.feishu_url`、`community.qq_url`、`community.qq_group_number`），`GET /api/settings/public` 返回给前端；后台可改（admin-analytics.md）。大厅与反馈页显示「加入飞书群 / 加入 QQ 群」按钮。
- 后台：`GET /api/admin/feedback?status=&type=&gameSlug=`、`PUT /api/admin/feedback/{id}`（`status`、`adminNote`）；状态变更写 `audit_log`。
- 事件：`feedback.submitted`（admin-analytics.md）。

## 待解决

- 附件（截图）：v1 不做；需要时另立决策（对象存储，未配置即 503，不本地落盘——GameFlow 教训）。

## 相关

- [`platform.md`](platform.md)、[`admin-analytics.md`](admin-analytics.md)
