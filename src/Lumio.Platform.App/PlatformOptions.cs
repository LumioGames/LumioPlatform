using System;

namespace Lumio.Platform.App;

public sealed class PlatformOptions
{
    public string? DatabaseConnectionString { get; set; }

    public string ListenUrl { get; set; } = "http://127.0.0.1:0";

    public static PlatformOptions FromEnvironment(Func<string, string?> getEnvironmentVariable)
    {
        return new PlatformOptions
        {
            DatabaseConnectionString = getEnvironmentVariable("PLATFORM_DB_CONNECTION_STRING"),
            ListenUrl = getEnvironmentVariable("PLATFORM_LISTEN_URL") ?? "http://127.0.0.1:0",
        };
    }
}
