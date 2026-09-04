using System;

namespace Lumio.Platform.Tests;

public static class TestDatabase
{
    public static string ConnectionString()
    {
        var value = Environment.GetEnvironmentVariable("PLATFORM_TEST_DB_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("PLATFORM_TEST_DB_CONNECTION_STRING is required. Run eng/dev-db.sh and export its test connection string.");
        }

        // Host tests start the real account-enabled host without production secrets. These
        // structurally valid, test-only values keep that fixture explicit while production
        // startup still rejects missing trust material.
        Environment.SetEnvironmentVariable("LUMIO_ACCOUNT_ADMISSION_PRIVATE_KEY_HEX",
            Environment.GetEnvironmentVariable("LUMIO_ACCOUNT_ADMISSION_PRIVATE_KEY_HEX") ?? new string('0', 64));
        Environment.SetEnvironmentVariable("LUMIO_ACCOUNT_BOT_TOOL_PUBLIC_KEY_HEX",
            Environment.GetEnvironmentVariable("LUMIO_ACCOUNT_BOT_TOOL_PUBLIC_KEY_HEX") ?? new string('0', 64));

        return value;
    }
}
