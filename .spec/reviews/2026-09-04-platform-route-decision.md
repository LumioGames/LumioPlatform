# LumioPlatform MS-1 路线裁决记录

- 日期：2026-09-04
- 状态：生效
- 范围：RM-00012 平台需求室与 MS-1 卡片；不改 Workflow 线上状态

## 输入证据

- 技术架构评审：`/Users/cui/Downloads/LumioPlatform_Technical_Architecture_Review_2026-09-04.md`，结论为 Conditional Go / 当前生产 No-Go，要求 Gate 0-5、受众绑定准入、真实 Runtime 网络与恢复证据、容量研究和安全前置。
- 当前卡片计划：[`../plans/2026-09-04-platform-ms1-cards.md`](../plans/2026-09-04-platform-ms1-cards.md)。
- 账号权威裁决：架构仓 `.spec/decisions/ADR-061-lumioplatform-repository-and-account-authority.md`，明确 Platform 是唯一账号权威、PostgreSQL 真值并保留 `AccountWorld`。
- 契约基线：架构仓 `origin/main` `933f755e4074fb4db26bd3c2da100f36aae88660` 已通过 PR #79 合入 audience-bound / account-auth exchange 扩展（source commit `a8cb9d3c8821d3f5ef51577a34cd586afb9908a8`）；历史 `c9f017b` 仅对应 PR #77，不含该扩展。R0 仍须把 `933f755e4074fb4db26bd3c2da100f36aae88660` 写入 `contract/ORIGIN`、更新镜像并启用漂移 CI 才算通过。

## 当前排期事实

截至裁决时，Workflow `RM-00012` 有 13 张卡（R-00409 至 R-00421），仅 R-00410 为 `in_progress`，其余为 `backlog`；需求室为 `in_progress`，65 条验收项尚未通过，没有 WorkItem，也没有 target date。`MS-00001`（112 个 requirement）虽有 2026-10-31 target，但 RM-00012 尚未挂入；`releaseWindows`、`ownerLoads` 与 `projectedEndOn` 为空。因此现有 W0-W5 只是逻辑依赖图，不是可执行日历排期。

## 决定

采用混合路线：

1. 保留模块化单体、PostgreSQL、Platform / Server / Client 边界、Server 权威和 `AccountWorld`（可从数据库重建，数据库是唯一持久真值）。删除 `AccountWorld` 属于与 ADR-061 冲突的建议，除非 Owner 另立 ADR。
2. 在 R0 / R-00409 建立 Gate 0，冻结契约治理、镜像 SHA、生成物漂移检查和 Owner 裁决，再进入 P0-1。
3. 把安全要求放入拥有行为的卡：R-00412（WS Origin、大小、并发、空闲、慢消费者）、R-00414（CSRF、分区限流、HMAC OTP 原子消费、Data Protection keys、session epoch）、R-00416（Audience-bound Launch、WSS、不可变 first-party bundle）、R-00418（访问控制、一次性 CLI bootstrap、session epoch 与审计）。限流不再作为 R-00421 的晚期新增功能。
4. 两类凭证都使用 300 秒有界 Bearer 策略：WS `accountAuthCredential` 为 unbound、不可入 Room，只能作为 Bearer 向 Launch 换票；Room `admissionCredential` 绑定 audience/game/release/contract/room/allocation。WSS/TLS 与审计降低暴露风险；v1 不引入在线 nonce 消费表，也不宣称离线可强制全局单活跃会话。
5. R-00420 只依赖账号、Launch、Client 接入和契约证据，可与反馈 / analytics 并行；拓扑与容量研究并行开展，但必须门控 R-00421。R-00421 负责备份恢复、`kill -9`、Drain、Rollback、Key Rotation、Soak 与容量最终门。
6. 不在本次变更中创建 target date、release window、owner load 或 WorkItem；依赖和 priority 是当前唯一排期真值。

## 结果

平台继续保持 Conditional Go / No-Go，直到 Gate 0-5 的证据完整。该记录与相关 living feature 文档、MS-1 卡片计划一起作为后续实现与审查的决策依据。
