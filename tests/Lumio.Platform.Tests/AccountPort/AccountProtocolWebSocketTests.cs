using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lumio.Platform.Account;
using Lumio.Platform.App.AccountPort;
using Lumio.Platform.Tests.Account;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Xunit;

#pragma warning disable CA1707 // Contract test names intentionally mirror frozen fixture IDs.
#pragma warning disable xUnit1051 // WebSocket cancellation is deliberately controlled by each scenario.

namespace Lumio.Platform.Tests.AccountPort;

public sealed class AccountProtocolWebSocketTests
{
    [Fact]
    public async Task origin_rejection_is_deterministic()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        await using var app = await StartAsync(runtime, new AccountProtocolOptions
        {
            AllowedOrigins = new(StringComparer.OrdinalIgnoreCase) { "https://allowed.example" },
        });
        using var client = NewClient("https://blocked.example");

        await Assert.ThrowsAsync<WebSocketException>(() => client.ConnectAsync(ToWebSocketUri(app), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task frame_limit_closes_with_invalid_request()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        await using var app = await StartAsync(runtime, new AccountProtocolOptions { MaxFrameBytes = 32, MaxRequestJsonBytes = 32 });
        using var client = await ConnectAsync(app);
        var payload = Encoding.UTF8.GetBytes("{" + new string('x', 64) + "}");

        await client.SendAsync(payload, WebSocketMessageType.Text, true, TestContext.Current.CancellationToken);
        var received = await ReceiveUntilCloseAsync(client);

        Assert.Contains("invalid_request", received, StringComparison.Ordinal);
        Assert.Equal(WebSocketState.Closed, client.State);
    }

    [Fact]
    public async Task idle_timeout_closes_idle_connection()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        await using var app = await StartAsync(runtime, new AccountProtocolOptions { IdleTimeoutSeconds = 1 });
        using var client = await ConnectAsync(app);

        var received = await ReceiveUntilCloseOutcomeAsync(client, TimeSpan.FromSeconds(5), acceptRemoteClose: true);

        Assert.Equal(string.Empty, received.Payload);
        Assert.True(
            received.RemoteClose || received.CloseStatus == WebSocketCloseStatus.PolicyViolation,
            $"expected a policy close or remote close, got status={received.CloseStatus}, state={client.State}");
        Assert.NotEqual(WebSocketState.Open, client.State);
    }

    [Fact]
    public async Task concurrent_connection_limit_rejects_excess_connection()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        await using var app = await StartAsync(runtime, new AccountProtocolOptions { MaxConcurrentConnections = 1, IdleTimeoutSeconds = 5 });
        using var first = await ConnectAsync(app);
        using var second = NewClient(null);

        await Assert.ThrowsAsync<WebSocketException>(() => second.ConnectAsync(ToWebSocketUri(app), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task send_queue_limit_closes_oversized_response()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        await using var app = await StartAsync(runtime, new AccountProtocolOptions { MaxSendQueueBytes = 1 });
        using var client = await ConnectAsync(app);
        var request = Encoding.UTF8.GetBytes("{\"messageType\":\"LoginOrRegister\",\"loginName\":\"queueuser\",\"password\":\"123456\"}");

        await client.SendAsync(request, WebSocketMessageType.Text, true, TestContext.Current.CancellationToken);
        var received = await ReceiveUntilCloseAsync(client);

        Assert.Equal(string.Empty, received);
        Assert.Equal(WebSocketState.Closed, client.State);
    }

    private static ClientWebSocket NewClient(string? origin)
    {
        var client = new ClientWebSocket();
        client.Options.AddSubProtocol(Lumio.Platform.Account.AccountPort.Subprotocol);
        if (origin is not null)
            client.Options.SetRequestHeader("Origin", origin);
        return client;
    }

    private static async Task<ClientWebSocket> ConnectAsync(WebApplication app)
    {
        var client = NewClient(null);
        await client.ConnectAsync(ToWebSocketUri(app), TestContext.Current.CancellationToken);
        return client;
    }

    private static async Task<WebApplication> StartAsync(AccountRuntime runtime, AccountProtocolOptions options)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        AccountProtocolServer.Map(app, runtime, options);
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static Uri ToWebSocketUri(WebApplication app)
        => new(app.Urls.Single().Replace("http://", "ws://", StringComparison.Ordinal).TrimEnd('/') + "/account");

    private static async Task<string> ReceiveUntilCloseAsync(ClientWebSocket client, TimeSpan? timeout = null)
        => (await ReceiveUntilCloseOutcomeAsync(client, timeout)).Payload;

    private static async Task<CloseOutcome> ReceiveUntilCloseOutcomeAsync(ClientWebSocket client, TimeSpan? timeout = null, bool acceptRemoteClose = false)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeoutSource.CancelAfter(timeout ?? TimeSpan.FromSeconds(5));
        var buffer = new byte[4096];
        var output = new StringBuilder();
        try
        {
            while (client.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                var result = await client.ReceiveAsync(buffer, timeoutSource.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    var closeStatus = client.CloseStatus;
                    if (client.State == WebSocketState.CloseReceived)
                        await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "ack", TestContext.Current.CancellationToken);
                    return new CloseOutcome(output.ToString(), closeStatus, RemoteClose: false);
                }
                output.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (result.EndOfMessage && output.Length > 0 && client.State == WebSocketState.Open)
                {
                    // Continue once to observe a policy close after the error/oversized response.
                    continue;
                }
            }
            return new CloseOutcome(output.ToString(), client.CloseStatus, RemoteClose: false);
        }
        catch (WebSocketException ex) when (acceptRemoteClose
            && (client.State is WebSocketState.Closed or WebSocketState.Aborted)
            && ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            return new CloseOutcome(output.ToString(), client.CloseStatus, RemoteClose: true);
        }
    }

    private readonly record struct CloseOutcome(string Payload, WebSocketCloseStatus? CloseStatus, bool RemoteClose);
}
