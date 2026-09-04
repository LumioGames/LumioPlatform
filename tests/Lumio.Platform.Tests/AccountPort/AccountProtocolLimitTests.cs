using System;
using System.Collections.Generic;
using Lumio.Platform.Account;
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
}
