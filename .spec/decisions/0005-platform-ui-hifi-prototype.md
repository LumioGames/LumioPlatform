# 0005 · 玩家端与运营后台的屏幕级视觉以 web/docs/prototype 高保真原型为准

- 日期:2026-09-04
- 状态:生效

## 背景

[决策 0004](0004-platform-ui-from-gametech.md) 已锁定令牌与 `.ui-*` 原语。设计师另交付了一份覆盖大厅 / 注册登录 / 我的 / 反馈 / Roadmap / 过场 / 403 / 后台的高保真可点原型。需要明确：页面布局、间距用法、文案、动效以哪份为准；与既有产品文档或 MS-1 卡片冲突时谁赢。

## 决策

- 屏幕级外观与交互（布局、字阶用法、间距、文案、toast / 弹层、空态、过场）以 [`web/docs/prototype/Lumio Prototype.dc.html`](../../web/docs/prototype/Lumio%20Prototype.dc.html) 为准；说明见同目录 [`README.md`](../../web/docs/prototype/README.md)。
- 色板、纸片卡片、品牌标与 `.ui-*` 类仍以 `web/src/styles/` 与 0004 为准。实现时用这些类在 React SPA 里重建，**不**复制原型 HTML、`support.js` 或行内样式。
- [`web/design-preview.html`](../../web/design-preview.html) 继续作为令牌 / 原语活样张，不是页面稿。`reference/` 下线框与静态画板只作决策记录与逐屏比对。
- 产品范围、API、数据模型仍以 `knowledge/features/{platform,account,lobby-launch,feedback,admin-analytics}.md`、MS-1 卡片与 `contract/` 为准。原型多出来的后端能力不得默默加接口或改契约。

## 后果

- P2-2 及后续 SPA 卡的外观验收对照原型，而不是对照 `design-preview.html` 的组件墙。
- `/games/<slug>/` 仍是 LumioClient 静态游戏页；平台 SPA 只做 launch 过场后跳进该路径，不在 SPA 里重做游戏画布。
- 原型演示账号、本地 blob 截图、`模拟朋友加入` 不是生产行为。
