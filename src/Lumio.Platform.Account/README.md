# LumioPlatform account boundary

This project owns the single account authority for LumioPlatform. `AccountRuntime` projects durable account identity and Argon2id credentials from PostgreSQL through `PostgresAccountStore`; the WebSocket transport is hosted by `Lumio.Platform.App` at `/account` using the frozen `lumio.account-port.v1` contract.
