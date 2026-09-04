using System;
using System.Collections.Generic;
using System.IO;
using Lumio.Platform.Account;
using Lumio.Platform.App;
using Lumio.Platform.App.AccountPort;
using Microsoft.AspNetCore.Http;
using Xunit;

#pragma warning disable CA1707 // Contract test names intentionally mirror frozen fixture IDs.

namespace Lumio.Platform.Tests.AccountPort;

public sealed class AccountProtocolLimitTests
{
    [Fact]
    public void origin_policy_rejects_untrusted_origin()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Origin = "https://evil.example";
        var options = new AccountProtocolOptions { AllowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "https://play.example" } };

        Assert.False(AccountProtocolServer.OriginAllowed(context, options));
    }

    [Fact]
    public void origin_policy_accepts_configured_origin_case_insensitively()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Origin = "HTTPS://PLAY.EXAMPLE";
        var options = new AccountProtocolOptions { AllowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "https://play.example" } };

        Assert.True(AccountProtocolServer.OriginAllowed(context, options));
    }

    [Fact]
    public void protocol_limits_reject_invalid_frame_relationship()
    {
        var options = new AccountProtocolOptions { MaxFrameBytes = 128, MaxRequestJsonBytes = 129 };

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void protocol_limits_cover_frame_idle_concurrency_queue_and_slow_consumer()
    {
        var options = new AccountProtocolOptions
        {
            MaxFrameBytes = 1024,
            MaxRequestJsonBytes = 512,
            IdleTimeoutSeconds = 1,
            MaxConcurrentConnections = 2,
            MaxSendQueueBytes = 2048,
            SlowConsumerTimeoutSeconds = 1,
        };

        options.Validate();
        Assert.Equal(1024, options.MaxFrameBytes);
        Assert.Equal(512, options.MaxRequestJsonBytes);
        Assert.Equal(2, options.MaxConcurrentConnections);
        Assert.Equal(2048, options.MaxSendQueueBytes);
        Assert.Equal(1, options.IdleTimeoutSeconds);
        Assert.Equal(1, options.SlowConsumerTimeoutSeconds);
    }

    [Fact]
    public void login_rate_limiter_evicts_stale_keys_and_honors_memory_bound()
    {
        var limiter = new FixedWindowRateLimiter(new AccountRateLimitOptions
        {
            WindowSeconds = 60,
            MaxRequestsPerIp = 30,
            MaxRequestsPerLoginName = 30,
            MaxRequestsPerAccount = 30,
            MaxTrackedKeys = 3,
        });

        Assert.True(limiter.Allow("10.0.0.1", "alice", null, 1));
        Assert.True(limiter.TrackedKeyCount <= 3);
        Assert.True(limiter.Allow("10.0.0.2", "bob", null, 2));
        Assert.True(limiter.TrackedKeyCount <= 3);
        Assert.True(limiter.Allow("10.0.0.3", "carol", null, 62));
        Assert.True(limiter.TrackedKeyCount <= 3);
    }

    [Fact]
    public void required_key_parsing_rejects_missing_and_malformed_values()
    {
        Assert.Throws<InvalidDataException>(() => PlatformHost.ParseRequiredHexKey("TEST_KEY", null, Ed25519Keys.SeedLength, "test key"));
        Assert.Throws<InvalidDataException>(() => PlatformHost.ParseRequiredHexKey("TEST_KEY", "00", Ed25519Keys.SeedLength, "test key"));
        Assert.Throws<InvalidDataException>(() => PlatformHost.ParseRequiredHexKey("TEST_KEY", new string('z', 64), Ed25519Keys.SeedLength, "test key"));
    }

    [Fact]
    public void required_key_parsing_accepts_exactly_sized_hex()
    {
        var expected = new byte[Ed25519Keys.SeedLength];
        for (var i = 0; i < expected.Length; i++) expected[i] = (byte)i;
        var actual = PlatformHost.ParseRequiredHexKey("TEST_KEY", Convert.ToHexString(expected), expected.Length, "test key");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void account_startup_configuration_requires_both_trust_keys()
    {
        static string? Missing(string name) => name switch
        {
            "LUMIO_ACCOUNT_ADMISSION_PRIVATE_KEY_HEX" => null,
            "LUMIO_ACCOUNT_BOT_TOOL_PUBLIC_KEY_HEX" => new string('0', Ed25519Keys.PublicKeyLength * 2),
            _ => null,
        };

        Assert.Throws<InvalidDataException>(() => PlatformHost.ReadAccountConfiguration(Missing));

        static string? MissingBot(string name) => name switch
        {
            "LUMIO_ACCOUNT_ADMISSION_PRIVATE_KEY_HEX" => new string('1', Ed25519Keys.SeedLength * 2),
            "LUMIO_ACCOUNT_BOT_TOOL_PUBLIC_KEY_HEX" => null,
            _ => null,
        };

        Assert.Throws<InvalidDataException>(() => PlatformHost.ReadAccountConfiguration(MissingBot));
    }

    [Fact]
    public void account_rate_limits_bind_from_explicit_environment_names()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PLATFORM_ACCOUNT_RATE_LIMIT_WINDOW_SECONDS"] = "17",
            ["PLATFORM_ACCOUNT_RATE_LIMIT_MAX_REQUESTS_PER_IP"] = "11",
            ["PLATFORM_ACCOUNT_RATE_LIMIT_MAX_REQUESTS_PER_LOGIN_NAME"] = "13",
            ["PLATFORM_ACCOUNT_RATE_LIMIT_MAX_REQUESTS_PER_ACCOUNT"] = "19",
            ["PLATFORM_ACCOUNT_RATE_LIMIT_MAX_TRACKED_KEYS"] = "23",
        };

        var options = PlatformHost.ReadRateLimitOptions(name => values.TryGetValue(name, out var value) ? value : null);

        Assert.Equal(17, options.WindowSeconds);
        Assert.Equal(11, options.MaxRequestsPerIp);
        Assert.Equal(13, options.MaxRequestsPerLoginName);
        Assert.Equal(19, options.MaxRequestsPerAccount);
        Assert.Equal(23, options.MaxTrackedKeys);
    }
}
