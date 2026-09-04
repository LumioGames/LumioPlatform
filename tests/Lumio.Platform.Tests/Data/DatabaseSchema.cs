using System;
using System.Threading.Tasks;
using Lumio.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Lumio.Platform.Tests.Data;

internal sealed class DatabaseSchema : IAsyncDisposable
{
    private readonly string _adminConnectionString;

    private DatabaseSchema(string adminConnectionString, string connectionString, string schemaName)
    {
        _adminConnectionString = adminConnectionString;
        ConnectionString = connectionString;
        SchemaName = schemaName;
    }

    public string ConnectionString { get; }

    public string SchemaName { get; }

    public static async Task<DatabaseSchema> CreateAsync()
    {
        var adminConnectionString = TestDatabase.ConnectionString();
        var schemaName = $"platform_test_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand($"CREATE SCHEMA \"{schemaName}\"", connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString) { SearchPath = schemaName };
        return new DatabaseSchema(adminConnectionString, builder.ConnectionString, schemaName);
    }

    public PlatformDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(ConnectionString).Options;
        return new PlatformDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{SchemaName}\" CASCADE", connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
