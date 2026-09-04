using System;
using System.Text.Json;

namespace Lumio.Platform.Account;

/// <summary>Client-facing notice emitted when a newer connection supersedes an older one.</summary>
public readonly record struct TakeoverNotice(
    string ReasonCode,
    bool ReconnectEligible,
    ulong IssuedAt,
    string? Detail = null)
{
    public const string ConnectionSuperseded = "connection_superseded";

    public bool IsValid => string.Equals(ReasonCode, ConnectionSuperseded, StringComparison.Ordinal);

    public static bool TryParse(string? json, out TakeoverNotice notice, out string errorCode)
    {
        notice = default;
        errorCode = AccountErrorCode.TakeoverNoticeInvalid;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("reasonCode", out var reason)
                || reason.ValueKind != JsonValueKind.String
                || !string.Equals(reason.GetString(), ConnectionSuperseded, StringComparison.Ordinal)
                || !root.TryGetProperty("reconnectEligible", out var reconnect)
                || (reconnect.ValueKind != JsonValueKind.True && reconnect.ValueKind != JsonValueKind.False)
                || !root.TryGetProperty("issuedAt", out var issuedAt)
                || issuedAt.ValueKind != JsonValueKind.Number
                || !issuedAt.TryGetUInt64(out var issued))
                return false;

            string? detail = null;
            if (root.TryGetProperty("detail", out var detailElement))
            {
                if (detailElement.ValueKind != JsonValueKind.String)
                    return false;
                detail = detailElement.GetString();
            }

            notice = new TakeoverNotice(reason.GetString()!, reconnect.GetBoolean(), issued, detail);
            errorCode = string.Empty;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
