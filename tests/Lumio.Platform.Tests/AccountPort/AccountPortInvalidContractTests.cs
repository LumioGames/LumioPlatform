using System;
using System.Linq;
using System.Threading.Tasks;
using Lumio.Platform.Account;
using Lumio.Platform.Tests.Account;
using Xunit;

#pragma warning disable CA1707 // Contract test names intentionally mirror frozen fixture IDs.
#pragma warning disable xUnit1051 // Runtime fixture methods carry the test cancellation where needed.

namespace Lumio.Platform.Tests.AccountPort;

public sealed class AccountPortInvalidContractTests
{
    [Fact]
    public async Task wrong_password_never_overwrites()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        var first = await runtime.LoginOrRegisterAsync("alice", "123456", ip: "10.1.0.1");
        var rejected = await runtime.LoginOrRegisterAsync("alice", "654321", ip: "10.1.0.1");
        var accepted = await runtime.LoginOrRegisterAsync("alice", "123456", ip: "10.1.0.1");

        Assert.True(first.Accepted);
        Assert.False(rejected.Accepted);
        Assert.Equal(AccountErrorCode.WrongPassword, rejected.Code);
        Assert.True(accepted.Accepted, accepted.Detail);
        Assert.Equal(first.AccountId, accepted.AccountId);
    }

    [Fact]
    public async Task ordinary_register_bot_name_rejected()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");

        var result = await runtime.LoginOrRegisterAsync("Bot77", "123456", ip: "10.1.0.2");

        Assert.False(result.Accepted);
        Assert.Equal(AccountErrorCode.BotNamespaceRegisterForbidden, result.Code);
    }

    [Fact]
    public async Task ordinary_login_existing_bot_rejected()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        var tool = BotToolCredential.Issue(fixture.BotToolKeys.Seed, "launcher", fixture.Clock.UnixSeconds, fixture.Clock.UnixSeconds + 300);
        var created = await runtime.LoginOrRegisterAsync("Bot07", "123456", tool, "10.1.0.3");
        Assert.True(created.Accepted, created.Detail);

        var result = await runtime.LoginOrRegisterAsync("Bot07", "123456", ip: "10.1.0.3");

        Assert.False(result.Accepted);
        Assert.Equal(AccountErrorCode.BotNamespaceLoginForbidden, result.Code);
    }

    [Fact]
    public async Task bot_tool_credential_malformed()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");

        var result = await runtime.LoginOrRegisterAsync("Bot09", "123456", "not-base64!", "10.1.0.4");

        Assert.False(result.Accepted);
        Assert.Equal(AccountErrorCode.BotToolCredentialMalformed, result.Code);
    }

    [Fact]
    public async Task bot_tool_credential_bad_signature()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        var other = Ed25519Keys.Generate();
        var credential = BotToolCredential.Issue(other.Seed, "launcher", fixture.Clock.UnixSeconds, fixture.Clock.UnixSeconds + 300);

        var result = await runtime.LoginOrRegisterAsync("Bot09", "123456", credential, "10.1.0.5");

        Assert.False(result.Accepted);
        Assert.Equal(AccountErrorCode.BotToolCredentialInvalid, result.Code);
    }

    [Fact]
    public async Task bot_tool_credential_expired()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");
        var credential = BotToolCredential.Issue(fixture.BotToolKeys.Seed, "launcher", fixture.Clock.UnixSeconds - 20, fixture.Clock.UnixSeconds - 1);

        var result = await runtime.LoginOrRegisterAsync("Bot09", "123456", credential, "10.1.0.6");

        Assert.False(result.Accepted);
        Assert.Equal(AccountErrorCode.BotToolCredentialExpired, result.Code);
    }

    [Fact]
    public async Task invalid_username_grammar()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test");

        var result = await runtime.LoginOrRegisterAsync("x", "123456", ip: "10.1.0.7");

        Assert.False(result.Accepted);
        Assert.Equal(AccountErrorCode.InvalidUsername, result.Code);
    }

    [Fact]
    public void admission_credential_expired()
    {
        var keys = Ed25519Keys.Generate();
        var clock = new TestAccountClock(1_700_000_100);
        var credential = AdmissionCredential.IssueBound(keys.Seed, 1, AccountId('a'), "alice", false, 1_700_000_000, 1_700_000_050, Claims());

        var rejected = Assert.IsType<AdmissionVerifyOutcome.Rejected>(AdmissionCredential.Verify(credential, 1, keys.PublicKey, clock, Claims()));

        Assert.Equal(AccountErrorCode.AdmissionCredentialExpired, rejected.Code);
    }

    [Fact]
    public void admission_credential_bad_signature()
    {
        var keys = Ed25519Keys.Generate();
        var clock = new TestAccountClock(1_700_000_000);
        var credential = AdmissionCredential.IssueBound(keys.Seed, 1, AccountId('a'), "alice", false, clock.UnixSeconds, clock.UnixSeconds + 300, Claims());
        Assert.True(Base64Url.TryDecode(credential, out var framed));
        framed[^1] ^= 1;

        var rejected = Assert.IsType<AdmissionVerifyOutcome.Rejected>(AdmissionCredential.Verify(Base64Url.Encode(framed), 1, keys.PublicKey, clock, Claims()));

        Assert.Equal(AccountErrorCode.AdmissionCredentialInvalidSignature, rejected.Code);
    }

    [Fact]
    public void ws_auth_credential_is_not_room_admittable()
    {
        var keys = Ed25519Keys.Generate();
        var clock = new TestAccountClock(1_700_000_000);
        var credential = AdmissionCredential.Issue(keys.Seed, 1, AccountId('a'), "alice", false, clock.UnixSeconds, clock.UnixSeconds + 300);

        var rejected = Assert.IsType<AdmissionVerifyOutcome.Rejected>(AdmissionCredential.Verify(credential, 1, keys.PublicKey, clock, Claims()));

        Assert.Equal(AccountErrorCode.AdmissionCredentialUnbound, rejected.Code);
    }

    [Fact]
    public void admission_binding_mismatched_audience()
        => AssertBindingMismatch(Claims(serverAudience: "other-server"), Claims());

    [Fact]
    public void admission_binding_mismatched_game()
        => AssertBindingMismatch(Claims(game: "bomber"), Claims());

    [Fact]
    public void admission_binding_mismatched_release()
        => AssertBindingMismatch(Claims(release: "hello-2"), Claims());

    [Fact]
    public void admission_binding_mismatched_contract()
        => AssertBindingMismatch(Claims(contract: "lumio.gameplay-envelope.v1"), Claims());

    [Fact]
    public void admission_binding_mismatched_room()
        => AssertBindingMismatch(Claims(room: "hello-2"), Claims());

    [Fact]
    public void admission_binding_mismatched_allocation()
        => AssertBindingMismatch(Claims(allocation: "alloc-2"), Claims());

    [Fact]
    public void admission_binding_empty_or_unbound_field()
    {
        var credentialClaims = Claims(game: Lumio.Platform.Account.AccountPort.UnboundSentinel);
        AssertBindingMismatch(credentialClaims, Claims());
    }

    [Fact]
    public void bot_admission_without_tool_context()
    {
        var keys = Ed25519Keys.Generate();
        var clock = new TestAccountClock(1_700_000_000);
        var claims = Claims();
        var credential = AdmissionCredential.IssueBound(keys.Seed, 1, AccountId('b'), "Bot07", false, clock.UnixSeconds, clock.UnixSeconds + 300, claims);

        var rejected = Assert.IsType<AdmissionVerifyOutcome.Rejected>(AdmissionCredential.Verify(credential, 1, keys.PublicKey, clock, claims));

        Assert.Equal(AccountErrorCode.BotNamespaceAdmissionForbidden, rejected.Code);
    }

    [Fact]
    public async Task production_profile_plain_register_rejected()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("production");

        var result = await runtime.LoginOrRegisterAsync("carol", "123456", ip: "10.1.0.8");

        Assert.False(result.Accepted);
        Assert.Equal(AccountErrorCode.RegistrationRequiresPlatform, result.Code);
        Assert.Equal(0, await runtime.Store.CountAsync());
        var registered = await runtime.RegisterWithEmailAsync("carol@example.com", "carol", "123456");
        Assert.NotNull(registered);
        var login = await runtime.LoginOrRegisterAsync("carol", "123456", ip: "10.1.0.8");
        Assert.True(login.Accepted, login.Detail);
        Assert.False(login.AccountNewlyCreated);
    }

    [Fact]
    public async Task ws_login_rate_limited()
    {
        await using var fixture = await AccountRuntimeFixture.CreateAsync();
        using var runtime = fixture.CreateRuntime("test", new AccountRateLimitOptions { WindowSeconds = 60, MaxRequestsPerIp = 1, MaxRequestsPerLoginName = 30, MaxRequestsPerAccount = 30 });

        var first = await runtime.LoginOrRegisterAsync("rateuser", "123456", ip: "10.1.0.9");
        var second = await runtime.LoginOrRegisterAsync("rateuser2", "123456", ip: "10.1.0.9");

        Assert.True(first.Accepted, first.Detail);
        Assert.False(second.Accepted);
        Assert.Equal(AccountErrorCode.RateLimited, second.Code);
        Assert.Equal(1, await runtime.Store.CountAsync());
    }

    [Fact]
    public void takeover_notice_missing_reason_code()
    {
        Assert.False(TakeoverNotice.TryParse("{\"reconnectEligible\":true,\"issuedAt\":1700000000}", out _, out var error));
        Assert.Equal(AccountErrorCode.TakeoverNoticeInvalid, error);
    }

    private static void AssertBindingMismatch(AdmissionAllocationClaims credentialClaims, AdmissionAllocationClaims context)
    {
        var keys = Ed25519Keys.Generate();
        var clock = new TestAccountClock(1_700_000_000);
        var credential = AdmissionCredential.IssueBound(keys.Seed, 1, AccountId('a'), "alice", false, clock.UnixSeconds, clock.UnixSeconds + 300, credentialClaims);

        var rejected = Assert.IsType<AdmissionVerifyOutcome.Rejected>(AdmissionCredential.Verify(credential, 1, keys.PublicKey, clock, context));

        Assert.Equal(AccountErrorCode.AdmissionBindingMismatch, rejected.Code);
    }

    private static AdmissionAllocationClaims Claims(
        string serverAudience = "game-server-public",
        string game = "hello",
        string release = "hello-1",
        string contract = "lumio.hello-wire.v1",
        string room = "room-1",
        string allocation = "alloc-1")
        => new(serverAudience, game, release, contract, room, allocation);

    private static string AccountId(char value) => "acct_" + new string(value, 32);
}
