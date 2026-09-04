using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lumio.Platform.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lumio.Platform.App;

public static class PlatformHost
{
    public sealed record HealthResponse(string Status, string Database);

    public static WebApplication Build(string[] args, PlatformOptions? options = null, bool requireDatabase = true)
    {
        var selected = options ?? PlatformOptions.FromEnvironment(Environment.GetEnvironmentVariable);
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls(selected.ListenUrl);
        builder.Services.AddSingleton(selected);
        var optionsBuilder = builder.Services.AddOptions<PlatformOptions>().Configure(value =>
        {
            value.DatabaseConnectionString = selected.DatabaseConnectionString;
            value.ListenUrl = selected.ListenUrl;
        });
        if (requireDatabase)
        {
            optionsBuilder.Validate(value => !string.IsNullOrWhiteSpace(value.DatabaseConnectionString), "PLATFORM_DB_CONNECTION_STRING is required.").ValidateOnStart();
        }
        builder.Services.AddOpenApi("v1");
        builder.Services.Configure<JsonOptions>(json => json.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
        if (!string.IsNullOrWhiteSpace(selected.DatabaseConnectionString))
        {
            builder.Services.AddDbContext<PlatformDbContext>((services, db) =>
                db.UseNpgsql(services.GetRequiredService<IOptions<PlatformOptions>>().Value.DatabaseConnectionString));
            if (requireDatabase) builder.Services.AddHostedService<PlatformDatabaseInitializer>();
        }

        var app = builder.Build();
        app.MapOpenApi("/openapi/v1.json");
        app.MapGet("/healthz", async (IServiceProvider services, CancellationToken cancellationToken) =>
        {
            var db = services.GetService<PlatformDbContext>();
            if (db is null) return Results.Json(new HealthResponse("ok", "unconfigured"));
            var healthy = await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
            return healthy
                ? Results.Json(new HealthResponse("ok", "ok"))
                : Results.Json(new HealthResponse("degraded", "unavailable"), statusCode: StatusCodes.Status503ServiceUnavailable);
        }).WithName("Health").Produces<HealthResponse>(StatusCodes.Status200OK).Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.Use(async (context, next) =>
        {
            await next().ConfigureAwait(false);
            if (context.Response.StatusCode == StatusCodes.Status404NotFound &&
                context.Request.Method == HttpMethods.Get &&
                !Path.HasExtension(context.Request.Path) &&
                !context.Request.Path.StartsWithSegments("/api") &&
                !context.Request.Path.StartsWithSegments("/openapi") &&
                !context.Request.Path.StartsWithSegments("/account") &&
                !context.Request.Path.StartsWithSegments("/games"))
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "text/html; charset=utf-8";
                var index = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");
                if (File.Exists(index)) await context.Response.SendFileAsync(index).ConfigureAwait(false);
                else await context.Response.WriteAsync("<!doctype html><title>Lumio Platform</title>").ConfigureAwait(false);
            }
        });
        if (requireDatabase)
        {
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                var server = app.Services.GetRequiredService<IServer>();
                var address = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()?.Addresses.FirstOrDefault() ?? selected.ListenUrl;
                Console.WriteLine($"PLATFORM_READY {{\"port\":{new Uri(address).Port},\"pid\":{Environment.ProcessId},\"listen\":\"{address}\",\"database\":\"postgresql\",\"accountPort\":\"/account\",\"contractIds\":[]}}");
                Console.Out.Flush();
            });
        }
        return app;
    }

    private sealed class PlatformDatabaseInitializer(PlatformDbContext db) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken) => await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
