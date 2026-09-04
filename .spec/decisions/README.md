# Decisions(决策记录 · ADR)

用 ADR(Architecture Decision Record)记录决策:为什么这样调度、为什么定这种结构、为什么划这条边界。**本目录是全仓决策记录的唯一落点**——功能内决策与框架级决策都记这里,feature 文档只描述设计现状,不留决策记录。

> 跨仓公共语义的决策只在架构仓 `LumioGameEngine/.spec/decisions/`（本仓相关：ADR-054 账号端口、ADR-061 平台仓与账号权威归属）维护；本目录仅记录平台内部实现决策，从 `0001` 开始编号。

## 怎么写一条 ADR

- 一个决策 = 一个文件 `NNNN-<slug>.md`,编号从 `0001` 递增;写完在下方索引加一行。
- **一旦记录不改写**:被推翻就新增一条,把旧的状态标成「被 NNNN 取代」,历史留痕。
- 无 frontmatter。格式照抄:

      # NNNN · <一句话决策>

      - 日期:YYYY-MM-DD
      - 状态:生效 | 被 NNNN 取代

      ## 背景
      面对什么问题。

      ## 决策
      定了什么。

      ## 后果
      接受了什么代价。

## 索引

| 编号 | 决策 | 状态 |
|------|------|------|
| [0001](0001-openapi-export-command.md) | OpenAPI 文档经 `lumio-platform openapi-export` 命令导出入库，不用构建时生成 | 生效 |
| [0002](0002-database-tests-fail-loudly.md) | 需要数据库的测试缺连接串即失败，不跳过；连接串只来自环境变量 | 生效 |
| [0003](0003-no-aspnet-identity.md) | 不用 ASP.NET Core Identity；账号域沿用 account-server 的 Argon2id 凭证库与自有账号模型 | 生效 |
