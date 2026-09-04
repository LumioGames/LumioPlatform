using System;

namespace Lumio.Platform.Data;

#pragma warning disable RS0030 // UTC persistence fields use DateTime; wall-clock access remains outside data shapes.

public sealed class Account
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
}

public sealed class AccountCredential
{
    public long AccountId { get; set; }
    public string Argon2idHash { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public sealed class EmailVerification
{
    public string ChallengeId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CodeHmac { get; set; } = string.Empty;
    public int PepperVersion { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class LoginAttempt
{
    public long Id { get; set; }
    public long? AccountId { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime At { get; set; }
}

public sealed class Game
{
    public long Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BundleDir { get; set; } = string.Empty;
    public string ServerWsUrl { get; set; } = string.Empty;
    public string Subprotocol { get; set; } = string.Empty;
    public string ContractId { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class Feedback
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? GameSlug { get; set; }
    public string? PageUrl { get; set; }
    public string? Contact { get; set; }
    public long? AccountId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class TrackedEvent
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Props { get; set; }
    public long? AccountId { get; set; }
    public string AnonId { get; set; } = string.Empty;
    public DateTime ClientTs { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string? PageUrl { get; set; }
    public string? UserAgent { get; set; }
}

public sealed class PlatformSetting
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public sealed class AuditLogEntry
{
    public long Id { get; set; }
    public long ActorAccountId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? Before { get; set; }
    public string? After { get; set; }
    public DateTime At { get; set; }
}

#pragma warning restore RS0030
