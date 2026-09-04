using System;

namespace Lumio.Platform.Tests.Data;

// Keeps the pre-account schema fixtures unambiguous after the account-domain namespace is introduced.
internal sealed class Account
{
    public long Id { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public long Uid { get; set; }
    public string LoginName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public long SecurityVersion { get; set; }
    public int AvatarId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public static implicit operator Lumio.Platform.Data.Account(Account value) => new()
    {
        Id = value.Id,
        AccountId = value.AccountId,
        Uid = value.Uid,
        LoginName = value.LoginName,
        Email = value.Email,
        EmailVerifiedAt = value.EmailVerifiedAt,
        SecurityVersion = value.SecurityVersion,
        AvatarId = value.AvatarId,
        Role = value.Role,
        Status = value.Status,
        CreatedAt = value.CreatedAt,
        LastLoginAt = value.LastLoginAt,
    };
}
