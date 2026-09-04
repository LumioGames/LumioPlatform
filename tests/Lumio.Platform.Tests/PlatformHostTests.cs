using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using System.Threading.Tasks;
using Lumio.Platform.App;
using Lumio.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumio.Platform.Tests;

public sealed class PlatformHostTests
{
    [Fact]
    public async Task EmptyDatabaseMigratesAndHealthReportsOk()
    {
        await using var app = PlatformHost.Build([], new PlatformOptions { DatabaseConnectionString = TestDatabase.ConnectionString(), ListenUrl = "http://127.0.0.1:0" });
        await app.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
            Assert.True(await db.Database.CanConnectAsync(TestContext.Current.CancellationToken));
        }
        finally { await app.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task StartAsyncValidatesRequiredDatabaseOption()
    {
        _ = TestDatabase.ConnectionString();
        await using var app = PlatformHost.Build([], new PlatformOptions { ListenUrl = "http://127.0.0.1:0" });
        await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HealthEndpointIsAvailableWhenDatabaseIsConfigured()
    {
        await using var app = PlatformHost.Build([], new PlatformOptions { DatabaseConnectionString = TestDatabase.ConnectionString(), ListenUrl = "http://127.0.0.1:0" });
        await app.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var address = app.Urls.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            var response = await client.GetAsync("/healthz", TestContext.Current.CancellationToken);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);
            Assert.NotNull(body);
            Assert.Equal("ok", body.Status);
            Assert.Equal("ok", body.Database);
        }
        finally { await app.StopAsync(TestContext.Current.CancellationToken); }
    }

    private sealed record HealthResponse(string Status, string Database);
}
