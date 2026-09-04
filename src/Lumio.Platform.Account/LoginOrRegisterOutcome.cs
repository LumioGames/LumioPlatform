namespace Lumio.Platform.Account;

public readonly record struct LoginOrRegisterOutcome(
    bool Accepted,
    bool AccountNewlyCreated,
    string? AccountId,
    string? LoginName,
    string? AccountAuthCredential,
    ulong AccountAuthExpiresAt,
    string? Code,
    string? Detail)
{
    public static LoginOrRegisterOutcome Ok(
        bool accountNewlyCreated,
        string accountId,
        string loginName,
        string accountAuthCredential,
        ulong accountAuthExpiresAt)
    {
        return new LoginOrRegisterOutcome(
            true,
            accountNewlyCreated,
            accountId,
            loginName,
            accountAuthCredential,
            accountAuthExpiresAt,
            null,
            null);
    }

    public static LoginOrRegisterOutcome Reject(string code, string detail)
    {
        return new LoginOrRegisterOutcome(false, false, null, null, null, 0, code, detail);
    }
}

