using System;
using Lumio.Platform.Account;
using Xunit;

namespace Lumio.Platform.Tests.AccountPort;

public sealed class CredentialContractTests
{
    [Fact]
    public void AccountAuthCredentialIsUnboundAndNotRoomAdmissible()
    {
        var keys = Ed25519Keys.Generate();
        var clock = new TestClock(1_700_000_000);
        var credential = AdmissionCredential.Issue(keys.Seed, 1, "acct_" + new string('a', 32), "alice", false, clock.UnixSeconds, clock.UnixSeconds + 300);

        Assert.True(AdmissionCredential.TryVerifyAccountAuth(credential, 1, keys.PublicKey, clock, out var principal, out var error), error);
        Assert.Equal("alice", principal.LoginName);
        var rejected = Assert.IsType<AdmissionVerifyOutcome.Rejected>(AdmissionCredential.Verify(credential, 1, keys.PublicKey, clock));
        Assert.Equal(AccountErrorCode.AdmissionCredentialUnbound, rejected.Code);
    }

    [Fact]
    public void BoundCredentialRequiresExactSixFieldAllocationContext()
    {
        var keys = Ed25519Keys.Generate();
        var clock = new TestClock(1_700_000_000);
        var claims = new AdmissionAllocationClaims("game-server", "hello", "hello-1", "lumio.hello-wire.v1", "room-1", "alloc-1");
        var credential = AdmissionCredential.IssueBound(keys.Seed, 2, "acct_" + new string('b', 32), "alice", false, clock.UnixSeconds, clock.UnixSeconds + 300, claims);

        var rejected = Assert.IsType<AdmissionVerifyOutcome.Rejected>(AdmissionCredential.Verify(credential, 2, keys.PublicKey, clock));
        Assert.Equal(AccountErrorCode.AdmissionBindingMismatch, rejected.Code);

        var accepted = Assert.IsType<AdmissionVerifyOutcome.Accepted>(AdmissionCredential.Verify(credential, 2, keys.PublicKey, clock, claims));
        Assert.Equal(claims, accepted.Payload.AllocationClaims);
        Assert.False(accepted.Payload.IsUnbound);
    }

    private sealed class TestClock(ulong unixSeconds) : IAccountClock
    {
        public ulong UnixSeconds { get; } = unixSeconds;
    }
}
