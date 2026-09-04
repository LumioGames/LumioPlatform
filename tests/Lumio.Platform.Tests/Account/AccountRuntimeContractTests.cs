using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lumio.Platform.Account;
using Lumio.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

#pragma warning disable CA1707 // Contract test names intentionally mirror frozen fixture IDs.
#pragma warning disable xUnit1051 // Runtime fixture methods carry the test cancellation where needed.

namespace Lumio.Platform.Tests.Account;

public sealed class AccountRuntimeContractTests
{
    [Fact]
    public async Task register_new_account()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");

        var result = await runtime.LoginOrRegisterAsync("alice", "123456", ip: "10.0.0.1");

        Assert.True(result.Accepted);
        Assert.True(result.AccountNewlyCreated);
        Assert.Matches(Lumio.Platform.Account.AccountPort.AccountIdPattern, result.AccountId!);
        Assert.True(runtime.VerifyAccountAuthCredential(result.AccountAuthCredential!, out _, out var error), error);
        Assert.Equal((ulong)Lumio.Platform.Account.AccountPort.AdmissionCredentialTtlSeconds, result.AccountAuthExpiresAt - fixture.Clock.UnixSeconds);
    }

    [Fact]
    public async Task idempotent_relogin_same_account()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");

        var first = await runtime.LoginOrRegisterAsync("alice", "123456", ip: "10.0.0.1");
        var second = await runtime.LoginOrRegisterAsync("alice", "123456", ip: "10.0.0.1");

        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.False(second.AccountNewlyCreated);
        Assert.Equal(first.AccountId, second.AccountId);
        Assert.NotEqual(first.AccountAuthCredential, second.AccountAuthCredential);
    }

    [Fact]
    public async Task concurrent_first_login_converges()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");

        var results = await Task.WhenAll(
            runtime.LoginOrRegisterAsync("bob", "123456", ip: "10.0.0.2"),
            runtime.LoginOrRegisterAsync("bob", "123456", ip: "10.0.0.3"));

        Assert.All(results, value => Assert.True(value.Accepted, value.Detail));
        Assert.Equal(results[0].AccountId, results[1].AccountId);
        Assert.Equal(1, await runtime.Store.CountAsync());
        Assert.Equal(1, runtime.EntityCount);
    }

    [Fact]
    public async Task bot_login_with_tool_credential()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("production");
        var toolKeys = fixture.BotToolKeys;
        var toolCredential = BotToolCredential.Issue(toolKeys.Seed, "launcher", fixture.Clock.UnixSeconds, fixture.Clock.UnixSeconds + 300);

        var result = await runtime.LoginOrRegisterAsync("Bot07", "123456", toolCredential, "10.0.0.4");

        Assert.True(result.Accepted, result.Detail);
        Assert.True(result.AccountNewlyCreated);
        Assert.True(runtime.VerifyAccountAuthCredential(result.AccountAuthCredential!, out var principal, out var error), error);
        Assert.True(principal.BotToolContext);
        Assert.IsType<AdmissionVerifyOutcome.Rejected>(runtime.VerifyAdmission(result.AccountAuthCredential!));
    }

    [Fact]
    public void verify_admission_accept()
    {
        var keys = Ed25519Keys.Generate();
        var clock = new TestAccountClock(1_700_000_000);
        var claims = Claims("hello", "hello-1", "room-1", "alloc-1");
        var credential = AdmissionCredential.IssueBound(keys.Seed, 1, AccountId('a'), "alice", false, clock.UnixSeconds, clock.UnixSeconds + 300, claims);

        var accepted = Assert.IsType<AdmissionVerifyOutcome.Accepted>(AdmissionCredential.Verify(credential, 1, keys.PublicKey, clock, claims));

        Assert.Equal(claims, accepted.Payload.AllocationClaims);
        Assert.Equal("alice", accepted.Payload.LoginName);
    }

    [Fact]
    public async Task verify_admission_bound_credential()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        var login = await runtime.LoginOrRegisterAsync("alice", "123456", ip: "10.0.0.6");
        Assert.True(login.Accepted, login.Detail);
        Assert.True(runtime.VerifyAccountAuthCredential(login.AccountAuthCredential!, out var principal, out var authError), authError);
        var claims = Claims("hello", "hello-1", "room-1", "alloc-1");

        var bound = runtime.IssueAdmissionCredential(principal, claims);
        var accepted = Assert.IsType<AdmissionVerifyOutcome.Accepted>(runtime.VerifyAdmission(bound.Credential, claims));

        Assert.Equal(claims, accepted.Payload.AllocationClaims);
        Assert.Equal(login.AccountId, accepted.Payload.AccountId);
    }

    [Fact]
    public async Task account_restart_stability()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        var toolCredential = BotToolCredential.Issue(fixture.BotToolKeys.Seed, "launcher", fixture.Clock.UnixSeconds, fixture.Clock.UnixSeconds + 300);
        string accountId;
        using (var firstRuntime = fixture.CreateRuntime("production"))
        {
            var first = await firstRuntime.LoginOrRegisterAsync("Bot01", "123456", toolCredential, "10.0.0.7");
            Assert.True(first.Accepted, first.Detail);
            accountId = first.AccountId!;
        }

        using var restarted = fixture.CreateRuntime("production");
        var second = await restarted.LoginOrRegisterAsync("Bot01", "123456", toolCredential, "10.0.0.8");

        Assert.True(second.Accepted, second.Detail);
        Assert.False(second.AccountNewlyCreated);
        Assert.Equal(accountId, second.AccountId);
    }

    [Fact]
    public async Task banned_status_rejects_login()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        var created = await runtime.LoginOrRegisterAsync("banned", "123456", ip: "10.0.0.10");
        Assert.True(created.Accepted, created.Detail);
        await using (var db = fixture.CreateContext())
        {
            await db.Accounts.Where(value => value.LoginName == "banned")
                .ExecuteUpdateAsync(setters => setters.SetProperty(value => value.Status, "banned"));
        }

        var result = await runtime.LoginOrRegisterAsync("banned", "123456", ip: "10.0.0.10");

        Assert.False(result.Accepted);
        Assert.Equal(AccountErrorCode.AccountBanned, result.Code);
    }

    [Fact]
    public async Task verify_password_rechecks_durable_ban_before_success()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        var created = await runtime.LoginOrRegisterAsync("bannedverify", "123456", ip: "10.0.0.12");
        Assert.True(created.Accepted, created.Detail);
        await using (var db = fixture.CreateContext())
        {
            await db.Accounts.Where(value => value.LoginName == "bannedverify")
                .ExecuteUpdateAsync(setters => setters.SetProperty(value => value.Status, "banned"));
        }

        var verified = await runtime.VerifyPasswordAsync("bannedverify", "123456", ip: "10.0.0.12");

        Assert.Null(verified);
    }

    [Fact]
    public async Task bot_credential_attempt_is_limited_before_verification()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test", new AccountRateLimitOptions
        {
            MaxRequestsPerIp = 1,
            MaxRequestsPerLoginName = 30,
            MaxRequestsPerAccount = 30,
        });
        var invalid = await runtime.LoginOrRegisterAsync("Bot77", "123456", "not-base64!", "10.0.0.13");
        var validCredential = BotToolCredential.Issue(fixture.BotToolKeys.Seed, "launcher", fixture.Clock.UnixSeconds, fixture.Clock.UnixSeconds + 300);
        var limited = await runtime.LoginOrRegisterAsync("Bot77", "123456", validCredential, "10.0.0.13");

        Assert.Equal(AccountErrorCode.BotToolCredentialMalformed, invalid.Code);
        Assert.Equal(AccountErrorCode.RateLimited, limited.Code);
    }

    [Fact]
    public async Task login_attempts_are_recorded()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        var created = await runtime.LoginOrRegisterAsync("audituser", "123456", ip: "10.0.0.11");
        Assert.True(created.Accepted, created.Detail);
        var rejected = await runtime.LoginOrRegisterAsync("audituser", "654321", ip: "10.0.0.11");
        Assert.Equal(AccountErrorCode.WrongPassword, rejected.Code);

        await using var db = fixture.CreateContext();
        var attempts = await db.LoginAttempts.Where(value => value.Identifier == "audituser").OrderBy(value => value.Id).ToListAsync();

        Assert.Equal(2, attempts.Count);
        Assert.Equal(["success", "failure"], attempts.Select(value => value.Outcome));
        Assert.Equal(AccountErrorCode.WrongPassword, attempts[1].ErrorCode);
        Assert.DoesNotContain(attempts, value => value.Identifier.Contains("123456", StringComparison.Ordinal));
    }

    [Fact]
    public void takeover_notice_well_formed()
    {
        var json = "{\"reasonCode\":\"connection_superseded\",\"reconnectEligible\":true,\"issuedAt\":1700000000}";

        Assert.True(TakeoverNotice.TryParse(json, out var notice, out var error), error);
        Assert.True(notice.IsValid);
        Assert.True(notice.ReconnectEligible);
        Assert.Equal((ulong)1_700_000_000, notice.IssuedAt);
    }

    private static AdmissionAllocationClaims Claims(string game, string release, string room, string allocation)
        => new("game-server-public", game, release, "lumio.hello-wire.v1", room, allocation);

    private static string AccountId(char value) => "acct_" + new string(value, 32);

}

internal sealed class AccountRuntimeFixture : IAsyncDisposable
{
    private readonly string adminConnectionString;
    private readonly string schemaName;
    private readonly string connectionString;

    private AccountRuntimeFixture(string adminConnectionString, string schemaName, string connectionString)
    {
        this.adminConnectionString = adminConnectionString;
        this.schemaName = schemaName;
        this.connectionString = connectionString;
    }

    public TestAccountClock Clock { get; } = new(1_700_000_000);

    public (byte[] Seed, byte[] PublicKey) AdmissionKeys { get; } = Ed25519Keys.Generate();

    public (byte[] Seed, byte[] PublicKey) BotToolKeys { get; } = Ed25519Keys.Generate();

    public static async Task<AccountRuntimeFixture> CreateAsync()
    {
        var admin = TestDatabase.ConnectionString();
        var schema = $"account_test_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(admin);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using (var command = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", connection))
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        var builder = new NpgsqlConnectionStringBuilder(admin) { SearchPath = schema };
        var fixture = new AccountRuntimeFixture(admin, schema, builder.ConnectionString);
        await using var db = fixture.CreateContext();
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
        return fixture;
    }

    public AccountRuntime CreateRuntime(string registrationProfile, AccountRateLimitOptions? rateLimits = null)
        => AccountRuntime.Open(new AccountServerOptions
        {
            DbContextFactory = new Factory(connectionString),
            AdmissionPrivateSeed = (byte[])AdmissionKeys.Seed.Clone(),
            BotToolPublicKey = (byte[])BotToolKeys.PublicKey.Clone(),
            AdmissionKeyId = 1,
            Clock = Clock,
            RegistrationProfile = registrationProfile,
            RateLimits = rateLimits ?? new AccountRateLimitOptions(),
        });

    public PlatformDbContext CreateContext()
        => new(new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connectionString).Options);

    public async ValueTask DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE", connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private sealed class Factory(string connectionString) : IDbContextFactory<PlatformDbContext>
    {
        public PlatformDbContext CreateDbContext()
            => new(new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connectionString).Options);

        public Task<PlatformDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}

internal sealed class TestAccountClock(ulong unixSeconds) : IAccountClock
{
    public ulong UnixSeconds { get; set; } = unixSeconds;
}
