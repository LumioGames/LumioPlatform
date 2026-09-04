using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Lumio.Platform.Account;
using WireAccountPort = Lumio.Platform.Account.AccountPort;

namespace Lumio.Platform.App.AccountPort;

public readonly record struct AccountReadyLine(int Port, int Pid, string AccountPortPath, IReadOnlyList<string> ContractIds)
{
    public const string Prefix = "PLATFORM_READY ";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public override string ToString() => Prefix + JsonSerializer.Serialize(new Payload(Port, Pid, AccountPortPath, ContractIds), JsonOptions);

    public static bool TryParse(string? line, out AccountReadyLine ready)
    {
        ready = default;
        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(line.AsSpan(Prefix.Length), JsonOptions);
            if (payload is null || payload.Port <= 0 || payload.Pid <= 0 || payload.AccountPort != "/account" || payload.ContractIds is null || !payload.ContractIds.Contains(WireAccountPort.ContractId, StringComparer.Ordinal)) return false;
            ready = new AccountReadyLine(payload.Port, payload.Pid, payload.AccountPort, payload.ContractIds);
            return true;
        }
        catch (JsonException) { return false; }
    }

    private sealed record Payload(int Port, int Pid, string AccountPort, IReadOnlyList<string> ContractIds);
}
