using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lumio.Platform.Account;
using Lumio.Platform.App.AccountPort;
using WireAccountPort = Lumio.Platform.Account.AccountPort;
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
            builder.Services.AddDbContextFactory<PlatformDbContext>((services, db) =>
                db.UseNpgsql(services.GetRequiredService<IOptions<PlatformOptions>>().Value.DatabaseConnectionString));
            builder.Services.AddSingleton<AccountRuntime>(_ =>
            {
                var accountConfiguration = ReadAccountConfiguration(Environment.GetEnvironmentVariable);
                return AccountRuntime.Open(new AccountServerOptions
                {
                    DbContextFactory = _.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<PlatformDbContext>>(),
                    AdmissionPrivateSeed = accountConfiguration.AdmissionPrivateSeed,
                    BotToolPublicKey = accountConfiguration.BotToolPublicKey,
                    AdmissionKeyId = ReadByte("LUMIO_ACCOUNT_ADMISSION_KEY_ID", 1),
                    RegistrationProfile = Environment.GetEnvironmentVariable("PLATFORM_REGISTRATION_PROFILE") ?? "production",
                    RateLimits = accountConfiguration.RateLimits,
                });
            });
            // Force trust-material parsing during StartAsync, before migrations complete and
            // before ApplicationStarted can emit PLATFORM_READY.
            builder.Services.AddHostedService<PlatformAccountConfigurationInitializer>();
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

        if (!string.IsNullOrWhiteSpace(selected.DatabaseConnectionString))
        {
            var protocolOptions = ReadProtocolOptions();
            AccountProtocolServer.Map(app, () => app.Services.GetRequiredService<AccountRuntime>(), protocolOptions);
        }

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
                Console.WriteLine($"PLATFORM_READY {{\"port\":{new Uri(address).Port},\"pid\":{Environment.ProcessId},\"listen\":\"{address}\",\"database\":\"postgresql\",\"accountPort\":\"/account\",\"contractIds\":[\"lumio.account-port.v1\",\"lumio.platform-port.v1\"]}}");
                Console.Out.Flush();
            });
        }
        return app;
    }

    internal static (byte[] AdmissionPrivateSeed, byte[] BotToolPublicKey, AccountRateLimitOptions RateLimits) ReadAccountConfiguration(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        var admission = ParseRequiredHexKey(
            "LUMIO_ACCOUNT_ADMISSION_PRIVATE_KEY_HEX",
            getEnvironmentVariable("LUMIO_ACCOUNT_ADMISSION_PRIVATE_KEY_HEX"),
            Ed25519Keys.SeedLength,
            "admission private key");
        var bot = ParseRequiredHexKey(
            "LUMIO_ACCOUNT_BOT_TOOL_PUBLIC_KEY_HEX",
            getEnvironmentVariable("LUMIO_ACCOUNT_BOT_TOOL_PUBLIC_KEY_HEX"),
            Ed25519Keys.PublicKeyLength,
            "bot-tool public key");
        return (admission, bot, ReadRateLimitOptions(getEnvironmentVariable));
    }

    internal static AccountRateLimitOptions ReadRateLimitOptions(Func<string, string?>? getEnvironmentVariable = null)
    {
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;
        return new AccountRateLimitOptions
        {
            WindowSeconds = ReadConfiguredPositiveInt(getEnvironmentVariable, 60,
                "PLATFORM_ACCOUNT_RATE_LIMIT_WINDOW_SECONDS", "PLATFORM_RATE_LIMIT_WINDOW_SECONDS"),
            MaxRequestsPerIp = ReadConfiguredPositiveInt(getEnvironmentVariable, 30,
                "PLATFORM_ACCOUNT_RATE_LIMIT_MAX_REQUESTS_PER_IP", "PLATFORM_RATE_LIMIT_MAX_REQUESTS_PER_IP"),
            MaxRequestsPerLoginName = ReadConfiguredPositiveInt(getEnvironmentVariable, 30,
                "PLATFORM_ACCOUNT_RATE_LIMIT_MAX_REQUESTS_PER_LOGIN_NAME", "PLATFORM_RATE_LIMIT_MAX_REQUESTS_PER_LOGIN_NAME"),
            MaxRequestsPerAccount = ReadConfiguredPositiveInt(getEnvironmentVariable, 30,
                "PLATFORM_ACCOUNT_RATE_LIMIT_MAX_REQUESTS_PER_ACCOUNT", "PLATFORM_RATE_LIMIT_MAX_REQUESTS_PER_ACCOUNT"),
            MaxTrackedKeys = ReadConfiguredPositiveInt(getEnvironmentVariable, 4096,
                "PLATFORM_ACCOUNT_RATE_LIMIT_MAX_TRACKED_KEYS", "PLATFORM_RATE_LIMIT_MAX_TRACKED_KEYS"),
        };
    }

    private static byte[] ReadRequiredHexKey(string name, int expectedLength, string description)
    {
        return ParseRequiredHexKey(name, Environment.GetEnvironmentVariable(name), expectedLength, description);
    }

    internal static byte[] ParseRequiredHexKey(string name, string? value, int expectedLength, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"{name} is required ({description}).");
        if (value.Length != expectedLength * 2)
            throw new InvalidDataException($"{name} must be exactly {expectedLength * 2} hexadecimal characters ({description}).");
        try
        {
            var key = Convert.FromHexString(value);
            if (key.Length != expectedLength)
                throw new InvalidDataException($"{name} must decode to {expectedLength} bytes ({description}).");
            return key;
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException($"{name} must contain hexadecimal characters ({description}).", ex);
        }
    }

    private static byte ReadByte(string name, byte fallback)
    {
        return byte.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
    }

    private static AccountProtocolOptions ReadProtocolOptions()
    {
        var allowed = Environment.GetEnvironmentVariable("PLATFORM_ACCOUNT_ALLOWED_ORIGINS");
        allowed ??= Environment.GetEnvironmentVariable("PLATFORM_PUBLIC_ORIGIN");
        var result = new AccountProtocolOptions
        {
            AllowedOrigins = string.IsNullOrWhiteSpace(allowed) ? new(StringComparer.OrdinalIgnoreCase) : new(allowed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), StringComparer.OrdinalIgnoreCase),
            MaxFrameBytes = ReadInt("PLATFORM_ACCOUNT_MAX_FRAME_BYTES", WireAccountPort.MaxFrameBytes),
            MaxRequestJsonBytes = ReadInt("PLATFORM_ACCOUNT_MAX_REQUEST_JSON_BYTES", WireAccountPort.MaxRequestJsonBytes),
            IdleTimeoutSeconds = ReadInt("PLATFORM_ACCOUNT_IDLE_TIMEOUT_SECONDS", 120),
            MaxConcurrentConnections = ReadInt("PLATFORM_ACCOUNT_MAX_CONCURRENT_CONNECTIONS", 100),
            MaxSendQueueBytes = ReadInt("PLATFORM_ACCOUNT_MAX_SEND_QUEUE_BYTES", 256 * 1024),
            SlowConsumerTimeoutSeconds = ReadInt("PLATFORM_ACCOUNT_SLOW_CONSUMER_TIMEOUT_SECONDS", 10),
        };
        result.Validate();
        return result;
    }

    private static int ReadInt(string name, int fallback) => int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value > 0 ? value : fallback;

    private static int ReadConfiguredPositiveInt(Func<string, string?> getEnvironmentVariable, int fallback, params string[] names)
    {
        foreach (var name in names)
        {
            var value = getEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (int.TryParse(value, out var parsed) && parsed > 0) return parsed;
            throw new InvalidDataException($"{name} must be a positive integer.");
        }

        return fallback;
    }

    private sealed class PlatformDatabaseInitializer(PlatformDbContext db) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken) => await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class PlatformAccountConfigurationInitializer(AccountRuntime runtime) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = runtime;
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
