using System;
using System.Collections.Generic;
using Lumio.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace Lumio.Platform.Account;

public sealed class AccountServerOptions
{
    public required IDbContextFactory<PlatformDbContext> DbContextFactory { get; init; }
    public required byte[] AdmissionPrivateSeed { get; init; }
    public required byte[] BotToolPublicKey { get; init; }
    public byte AdmissionKeyId { get; init; }
    public IAccountClock Clock { get; init; } = new SystemAccountClock();
    public IAccountAuditSink Audit { get; init; } = NullAccountAuditSink.Instance;
    public string RegistrationProfile { get; init; } = "production";
    public AccountRateLimitOptions RateLimits { get; init; } = new();

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(DbContextFactory);
        ArgumentNullException.ThrowIfNull(AdmissionPrivateSeed);
        ArgumentNullException.ThrowIfNull(BotToolPublicKey);
        ArgumentNullException.ThrowIfNull(Clock);
        ArgumentNullException.ThrowIfNull(Audit);
        if (AdmissionPrivateSeed.Length != Ed25519Keys.SeedLength)
            throw new ArgumentException("admission private seed must be 32 bytes.", nameof(AdmissionPrivateSeed));
        if (BotToolPublicKey.Length != Ed25519Keys.PublicKeyLength)
            throw new ArgumentException("bot-tool public key must be 32 bytes.", nameof(BotToolPublicKey));
        if (!string.Equals(RegistrationProfile, "test", StringComparison.Ordinal)
            && !string.Equals(RegistrationProfile, "production", StringComparison.Ordinal))
            throw new ArgumentException("registration profile must be test or production.", nameof(RegistrationProfile));
        RateLimits.Validate();
    }
}

public sealed class AccountRateLimitOptions
{
    public int WindowSeconds { get; init; } = 60;
    public int MaxRequestsPerIp { get; init; } = 30;
    public int MaxRequestsPerLoginName { get; init; } = 30;
    public int MaxRequestsPerAccount { get; init; } = 30;
    public int MaxTrackedKeys { get; init; } = 4096;

    public void Validate()
    {
        if (WindowSeconds <= 0 || MaxRequestsPerIp <= 0 || MaxRequestsPerLoginName <= 0 || MaxRequestsPerAccount <= 0 || MaxTrackedKeys <= 0)
            throw new ArgumentException("account rate limits must be positive.");
    }
}

public sealed class AccountProtocolOptions
{
    public HashSet<string> AllowedOrigins { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public int MaxFrameBytes { get; init; } = AccountPort.MaxFrameBytes;
    public int MaxRequestJsonBytes { get; init; } = AccountPort.MaxRequestJsonBytes;
    public int IdleTimeoutSeconds { get; init; } = 120;
    public int MaxConcurrentConnections { get; init; } = 100;
    public int MaxSendQueueBytes { get; init; } = 256 * 1024;
    public int SlowConsumerTimeoutSeconds { get; init; } = 10;

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(AllowedOrigins);
        if (MaxFrameBytes <= 0 || MaxRequestJsonBytes <= 0 || MaxRequestJsonBytes > MaxFrameBytes
            || IdleTimeoutSeconds <= 0 || MaxConcurrentConnections <= 0 || MaxSendQueueBytes <= 0
            || SlowConsumerTimeoutSeconds <= 0)
            throw new ArgumentException("invalid account WebSocket limits.");
    }
}
