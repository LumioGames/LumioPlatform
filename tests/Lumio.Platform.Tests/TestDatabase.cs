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

        return value;
    }
}
