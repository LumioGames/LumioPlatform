# 0003 · 不用 ASP.NET Core Identity；账号域沿用 account-server 的 Argon2id 凭证库与自有账号模型

- 日期:2026-09-04
- 状态:生效

## 背景

网页端需要注册 / 登录 / Cookie 会话 / 角色。ASP.NET Core Identity 自带用户表、PBKDF2 口令哈希、角色 / Claims 表与一整套 Manager；而 `LumioServer/account-server/`（按架构仓 ADR-054）已经有 Argon2id 凭证库、`AccountWorld` 账号模型、Bot 命名空间规则与准入凭证签发，并将原码搬入本仓（ADR-061）。两套并存 = 两套口令哈希、两张用户表、两条写路径。

## 决策

- 账号模型、口令哈希（Argon2id，RFC 9106，每账号唯一盐）、凭证签发全部沿用搬入的账号域代码；持久层改为 PostgreSQL。
- 网页会话只用 ASP.NET Core **Cookie 认证中间件**（`AddAuthentication().AddCookie()`，HttpOnly、SameSite=Lax、Secure 随部署），登录成功后由账号域颁发 principal（accountId、uid、loginName、role），不引入 Identity 的 UserManager / SignInManager / EF 表。
- 角色只有 `player` / `admin` 两个值，存账号记录的 `role` 列；授权用 `RequireRole("admin")`。

## 后果

- 找回口令、改密、双因素等 Identity 自带能力需要时按需自研并各立 ADR；本切片明确不做（架构仓 ADR-054 排除项延续）。
- 账号域与 Web 会话之间只有「验证口令 → 颁发 principal」一条接口，便于将来原生客户端（Steam / 移动端）用同一账号端口。
