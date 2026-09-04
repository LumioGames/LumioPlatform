namespace Lumio.Platform.Account;

public sealed class AccountIdentityComponent
{
    public AccountIdentityComponent(
        ulong entityId,
        string accountId,
        long uid,
        string loginName,
        string? email,
        int avatarId,
        string role,
        string status,
        ulong createdAtUnixSeconds,
        long securityVersion = 1)
    {
        EntityId = entityId;
        AccountId = accountId;
        Uid = uid;
        LoginName = loginName;
        Email = email;
        AvatarId = avatarId;
        Role = role;
        Status = status;
        SecurityVersion = securityVersion;
        CreatedAtUnixSeconds = createdAtUnixSeconds;
    }

    public AccountIdentityComponent(ulong entityId, string accountId, string loginName, ulong createdAtUnixSeconds)
        : this(entityId, accountId, 0, loginName, null, 1, "player", "active", createdAtUnixSeconds)
    {
    }

    public ulong EntityId { get; }

    public string AccountId { get; }

    public long Uid { get; }

    public string LoginName { get; }

    public string? Email { get; }

    public int AvatarId { get; }

    public string Role { get; }

    public string Status { get; }

    public long SecurityVersion { get; }

    public ulong CreatedAtUnixSeconds { get; }
}

