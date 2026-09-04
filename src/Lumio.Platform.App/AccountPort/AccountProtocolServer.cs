using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lumio.Platform.Account;
using WireAccountPort = Lumio.Platform.Account.AccountPort;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Lumio.Platform.App.AccountPort;

public static class AccountProtocolServer
{
    public static void Map(WebApplication app, AccountRuntime runtime, AccountProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        Map(app, () => runtime, options);
    }

    public static void Map(WebApplication app, Func<AccountRuntime> runtimeFactory, AccountProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(runtimeFactory);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        var connections = new SemaphoreSlim(options.MaxConcurrentConnections, options.MaxConcurrentConnections);
        app.UseWebSockets();
        app.Map("/account", branch => branch.Run(context => HandleRequestAsync(context, runtimeFactory(), options, connections)));
    }

    private static async Task HandleRequestAsync(HttpContext context, AccountRuntime runtime, AccountProtocolOptions options, SemaphoreSlim connections)
    {
        if (!context.WebSockets.IsWebSocketRequest || !context.WebSockets.WebSocketRequestedProtocols.Contains(WireAccountPort.Subprotocol, StringComparer.Ordinal))
        {
            runtime.Audit("account_connection_rejected", Fields(context, ("reason", "invalid_handshake")));
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        if (!OriginAllowed(context, options))
        {
            runtime.Audit("account_connection_rejected", Fields(context, ("reason", "origin_denied")));
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        if (!await connections.WaitAsync(0, context.RequestAborted).ConfigureAwait(false))
        {
            runtime.Audit("account_limit_exceeded", Fields(context, ("limit", "connections"), ("outcome", "rejected")));
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }
        try
        {
            using var socket = await context.WebSockets.AcceptWebSocketAsync(WireAccountPort.Subprotocol).ConfigureAwait(false);
            runtime.Audit("account_connection_accepted", Fields(context, ("subprotocol", WireAccountPort.Subprotocol)));
            await RunConnectionAsync(context, socket, runtime, options).ConfigureAwait(false);
        }
        catch (WebSocketException)
        {
            runtime.Audit("account_protocol_error", Fields(context, ("phase", "connection")));
        }
        finally
        {
            runtime.Audit("account_connection_closed", Fields(context, ("outcome", "closed")));
            connections.Release();
        }
    }

    private static async Task RunConnectionAsync(HttpContext context, WebSocket socket, AccountRuntime runtime, AccountProtocolOptions options)
    {
        var cancellation = context.RequestAborted;
        while (socket.State == WebSocketState.Open && !cancellation.IsCancellationRequested)
        {
            byte[]? payload;
            WebSocketMessageType type;
            try
            {
                (payload, type) = await ReceiveMessageAsync(socket, options.MaxFrameBytes, options.IdleTimeoutSeconds, cancellation).ConfigureAwait(false);
            }
            catch (InvalidDataException ex)
            {
                runtime.Audit("account_protocol_error", Fields(context, ("code", AccountErrorCode.InvalidRequest), ("phase", "receive")));
                await WriteErrorAndCloseAsync(socket, AccountErrorCode.InvalidRequest, ex.Message, options, cancellation).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
            {
                runtime.Audit("account_limit_exceeded", Fields(context, ("limit", "idle_timeout"), ("outcome", "closed")));
                await CloseQuietlyAsync(socket, WebSocketCloseStatus.PolicyViolation, "idle_timeout", cancellation).ConfigureAwait(false);
                return;
            }
            catch (WebSocketException)
            {
                runtime.Audit("account_protocol_error", Fields(context, ("phase", "receive")));
                await CloseQuietlyAsync(socket, WebSocketCloseStatus.InternalServerError, "connection_error", cancellation).ConfigureAwait(false);
                return;
            }
            if (payload is null || type == WebSocketMessageType.Close)
            {
                if (socket.State == WebSocketState.CloseReceived)
                    await CloseQuietlyAsync(socket, socket.CloseStatus ?? WebSocketCloseStatus.NormalClosure, socket.CloseStatusDescription ?? "closed", cancellation).ConfigureAwait(false);
                return;
            }
            if (type != WebSocketMessageType.Text)
            {
                runtime.Audit("account_protocol_error", Fields(context, ("code", AccountErrorCode.InvalidRequest), ("phase", "message_type")));
                await WriteErrorAndCloseAsync(socket, AccountErrorCode.InvalidRequest, "expected a text frame", options, cancellation).ConfigureAwait(false);
                return;
            }
            if (payload.Length > options.MaxRequestJsonBytes)
            {
                runtime.Audit("account_limit_exceeded", Fields(context, ("limit", "request_json"), ("outcome", "closed")));
                await WriteErrorAndCloseAsync(socket, AccountErrorCode.InvalidRequest, "request JSON exceeds maxRequestJsonBytes", options, cancellation).ConfigureAwait(false);
                return;
            }
            if (!TryReadLogin(payload, out var loginName, out var password, out var botToolCredential))
            {
                runtime.Audit("account_protocol_error", Fields(context, ("code", AccountErrorCode.InvalidRequest), ("phase", "json")));
                await WriteErrorAndCloseAsync(socket, AccountErrorCode.InvalidRequest, "malformed LoginOrRegister", options, cancellation).ConfigureAwait(false);
                return;
            }
            var request = new LoginOrRegisterRequest(loginName, password, botToolCredential, "ws", context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString());
            var outcome = await runtime.LoginOrRegisterAsync(request, cancellation).ConfigureAwait(false);
            if (outcome.Accepted)
            {
                try { await WriteAckAsync(socket, outcome, options, cancellation).ConfigureAwait(false); }
                catch (InvalidDataException) { runtime.Audit("account_limit_exceeded", Fields(context, ("limit", "send_queue"), ("outcome", "closed"))); await CloseQuietlyAsync(socket, WebSocketCloseStatus.PolicyViolation, "send_queue_limit", cancellation).ConfigureAwait(false); return; }
                catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { runtime.Audit("account_limit_exceeded", Fields(context, ("limit", "slow_consumer"), ("outcome", "closed"))); await CloseQuietlyAsync(socket, WebSocketCloseStatus.PolicyViolation, "slow_consumer", cancellation).ConfigureAwait(false); return; }
                catch (WebSocketException) { runtime.Audit("account_protocol_error", Fields(context, ("phase", "send"))); await CloseQuietlyAsync(socket, WebSocketCloseStatus.InternalServerError, "connection_error", cancellation).ConfigureAwait(false); return; }
            }
            else
            {
                var code = outcome.Code ?? AccountErrorCode.InvalidRequest;
                if (code == AccountErrorCode.RateLimited)
                    runtime.Audit("account_limit_exceeded", Fields(context, ("limit", "login"), ("outcome", "rejected")));
                try { await WriteErrorAsync(socket, code, outcome.Detail ?? string.Empty, options, cancellation).ConfigureAwait(false); }
                catch (InvalidDataException) { runtime.Audit("account_limit_exceeded", Fields(context, ("limit", "send_queue"), ("outcome", "closed"))); await CloseQuietlyAsync(socket, WebSocketCloseStatus.PolicyViolation, "send_queue_limit", cancellation).ConfigureAwait(false); return; }
                catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { runtime.Audit("account_limit_exceeded", Fields(context, ("limit", "slow_consumer"), ("outcome", "closed"))); await CloseQuietlyAsync(socket, WebSocketCloseStatus.PolicyViolation, "slow_consumer", cancellation).ConfigureAwait(false); return; }
                catch (WebSocketException) { runtime.Audit("account_protocol_error", Fields(context, ("phase", "send"))); await CloseQuietlyAsync(socket, WebSocketCloseStatus.InternalServerError, "connection_error", cancellation).ConfigureAwait(false); return; }
                if (outcome.Code == AccountErrorCode.InvalidRequest) return;
            }
        }
    }

    internal static bool OriginAllowed(HttpContext context, AccountProtocolOptions options)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin)) return options.AllowedOrigins.Count == 0;
        return options.AllowedOrigins.Contains(origin);
    }

    private static bool TryReadLogin(byte[] json, out string loginName, out string password, out string? botToolCredential)
    {
        loginName = string.Empty; password = string.Empty; botToolCredential = null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("messageType", out var messageType) || messageType.ValueKind != JsonValueKind.String || messageType.GetString() != WireAccountPort.LoginOrRegisterMessageType
                || !root.TryGetProperty("loginName", out var login) || login.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("password", out var pass) || pass.ValueKind != JsonValueKind.String) return false;
            loginName = login.GetString() ?? string.Empty; password = pass.GetString() ?? string.Empty;
            if (root.TryGetProperty("botToolCredential", out var bot) && bot.ValueKind != JsonValueKind.Null)
            {
                if (bot.ValueKind != JsonValueKind.String) return false;
                botToolCredential = bot.GetString();
            }
            return true;
        }
        catch (JsonException) { return false; }
    }

    private static async Task<(byte[]? Payload, WebSocketMessageType Type)> ReceiveMessageAsync(WebSocket socket, int maxFrameBytes, int idleTimeoutSeconds, CancellationToken cancellationToken)
    {
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        idle.CancelAfter(TimeSpan.FromSeconds(idleTimeoutSeconds));
        var writer = new ArrayBufferWriter<byte>(1024);
        var buffer = new byte[Math.Min(4096, maxFrameBytes)];
        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, idle.Token).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close) return (null, WebSocketMessageType.Close);
            if (writer.WrittenCount + result.Count > maxFrameBytes) throw new InvalidDataException("frame exceeds limits");
            writer.Write(buffer.AsSpan(0, result.Count));
            if (result.EndOfMessage) return (writer.WrittenSpan.ToArray(), result.MessageType);
        }
    }

    private static async Task WriteAckAsync(WebSocket socket, LoginOrRegisterOutcome outcome, AccountProtocolOptions options, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("messageType", WireAccountPort.LoginOrRegisterAckMessageType); writer.WriteBoolean("accepted", true);
            writer.WriteBoolean("accountNewlyCreated", outcome.AccountNewlyCreated); writer.WriteString("accountId", outcome.AccountId); writer.WriteString("loginName", outcome.LoginName);
            writer.WriteString("accountAuthCredential", outcome.AccountAuthCredential); writer.WriteNumber("accountAuthExpiresAt", outcome.AccountAuthExpiresAt); writer.WriteEndObject();
        }
        await SendAsync(socket, stream.ToArray(), options, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteErrorAsync(WebSocket socket, string code, string detail, AccountProtocolOptions options, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        { writer.WriteStartObject(); writer.WriteString("messageType", WireAccountPort.ErrorMessageType); writer.WriteString("code", code); writer.WriteString("detail", detail); writer.WriteEndObject(); }
        await SendAsync(socket, stream.ToArray(), options, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteErrorAndCloseAsync(WebSocket socket, string code, string detail, AccountProtocolOptions options, CancellationToken cancellationToken)
    {
        try { await WriteErrorAsync(socket, code, detail, options, cancellationToken).ConfigureAwait(false); }
        catch (WebSocketException) { }
        catch (InvalidDataException) { }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
        await CloseQuietlyAsync(socket, WebSocketCloseStatus.PolicyViolation, code, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SendAsync(WebSocket socket, byte[] payload, AccountProtocolOptions options, CancellationToken cancellationToken)
    {
        if (payload.Length > options.MaxSendQueueBytes) throw new InvalidDataException("send queue limit exceeded");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.SlowConsumerTimeoutSeconds));
        await socket.SendAsync(payload, WebSocketMessageType.Text, true, timeout.Token).ConfigureAwait(false);
    }

    private static async Task CloseQuietlyAsync(WebSocket socket, WebSocketCloseStatus status, string description, CancellationToken cancellationToken)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try { await socket.CloseAsync(status, description, cancellationToken).ConfigureAwait(false); } catch (WebSocketException) { }
            catch (OperationCanceledException) { }
        }
    }

    private static Dictionary<string, string> Fields(HttpContext context, params (string Key, string Value)[] fields)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["connectionId"] = context.TraceIdentifier,
        };
        foreach (var (key, value) in fields) result[key] = value;
        return result;
    }
}
