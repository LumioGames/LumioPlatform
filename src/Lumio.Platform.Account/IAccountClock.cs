namespace Lumio.Platform.Account;

public interface IAccountClock
{
    ulong UnixSeconds { get; }
}

