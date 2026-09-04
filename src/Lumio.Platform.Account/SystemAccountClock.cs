using System;

namespace Lumio.Platform.Account;

internal sealed class SystemAccountClock : IAccountClock
{
#pragma warning disable RS0030
    public ulong UnixSeconds => (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
#pragma warning restore RS0030
}

