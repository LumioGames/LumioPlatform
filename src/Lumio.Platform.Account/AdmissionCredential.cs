using System;
using System.Security.Cryptography;

namespace Lumio.Platform.Account;

public readonly record struct AdmissionAllocationClaims(
    string ServerAudience,
    string GameId,
    string GameReleaseId,
    string ContractId,
    string RoomId,
    string AllocationId)
{
    public bool IsBound => !string.IsNullOrEmpty(ServerAudience) && !string.IsNullOrEmpty(GameId)
        && !string.IsNullOrEmpty(GameReleaseId) && !string.IsNullOrEmpty(ContractId)
        && !string.IsNullOrEmpty(RoomId) && !string.IsNullOrEmpty(AllocationId)
        && ServerAudience != AccountPort.UnboundSentinel && GameId != AccountPort.UnboundSentinel
        && GameReleaseId != AccountPort.UnboundSentinel && ContractId != AccountPort.UnboundSentinel
        && RoomId != AccountPort.UnboundSentinel && AllocationId != AccountPort.UnboundSentinel;
}

public readonly record struct AdmissionCredentialPayload(
    byte KeyId,
    string AccountId,
    string LoginName,
    bool BotToolContext,
    ulong IssuedAt,
    ulong ExpiresAt,
    byte[] Nonce,
    string ServerAudience,
    string GameId,
    string GameReleaseId,
    string ContractId,
    string RoomId,
    string AllocationId)
{
    public AdmissionCredentialPayload(byte keyId, string accountId, string loginName, bool botToolContext, ulong issuedAt, ulong expiresAt, byte[] nonce)
        : this(keyId, accountId, loginName, botToolContext, issuedAt, expiresAt, nonce,
            AccountPort.UnboundSentinel, AccountPort.UnboundSentinel, AccountPort.UnboundSentinel,
            AccountPort.UnboundSentinel, AccountPort.UnboundSentinel, AccountPort.UnboundSentinel)
    {
    }

    public bool IsUnbound => ServerAudience == AccountPort.UnboundSentinel && GameId == AccountPort.UnboundSentinel
        && GameReleaseId == AccountPort.UnboundSentinel && ContractId == AccountPort.UnboundSentinel
        && RoomId == AccountPort.UnboundSentinel && AllocationId == AccountPort.UnboundSentinel;

    public AdmissionAllocationClaims AllocationClaims => new(ServerAudience, GameId, GameReleaseId, ContractId, RoomId, AllocationId);
}

public sealed record AccountAuthPrincipal(string AccountId, string LoginName, bool BotToolContext, byte KeyId, ulong ExpiresAt);

public abstract class AdmissionVerifyOutcome
{
    private AdmissionVerifyOutcome() { }

    public sealed class Accepted(AdmissionCredentialPayload payload) : AdmissionVerifyOutcome
    {
        public AdmissionCredentialPayload Payload { get; } = payload;
    }

    public sealed class Rejected(string code) : AdmissionVerifyOutcome
    {
        public string Code { get; } = code;
    }
}

public static class AdmissionCredential
{
    public static string Issue(ReadOnlySpan<byte> privateSeed, byte keyId, string accountId, string loginName, bool botToolContext, ulong issuedAt, ulong expiresAt)
        => IssueFromPayload(privateSeed, new AdmissionCredentialPayload(keyId, accountId, loginName, botToolContext, issuedAt, expiresAt, NewNonce()));

    public static string IssueBound(ReadOnlySpan<byte> privateSeed, byte keyId, string accountId, string loginName, bool botToolContext, ulong issuedAt, ulong expiresAt, AdmissionAllocationClaims claims)
        => IssueFromPayload(privateSeed, new AdmissionCredentialPayload(keyId, accountId, loginName, botToolContext, issuedAt, expiresAt, NewNonce(), claims.ServerAudience, claims.GameId, claims.GameReleaseId, claims.ContractId, claims.RoomId, claims.AllocationId));

    public static AdmissionVerifyOutcome Verify(string wire, byte expectedKeyId, ReadOnlySpan<byte> publicKey, IAccountClock clock)
        => Verify(wire, expectedKeyId, publicKey, clock, null);

    /// <summary>
    /// Verifies a room-bound credential and, when supplied, compares every
    /// allocation field with the trusted server-owned allocation context.
    /// </summary>
    public static AdmissionVerifyOutcome Verify(
        string wire,
        byte expectedKeyId,
        ReadOnlySpan<byte> publicKey,
        IAccountClock clock,
        AdmissionAllocationClaims? allocationContext)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (!TrySplit(wire, out var payloadBytes, out var signature) || !TryDecodePayload(payloadBytes, out var payload))
            return new AdmissionVerifyOutcome.Rejected(AccountErrorCode.AdmissionCredentialMalformed);
        if (payload.KeyId != expectedKeyId || !LumioSignature.Verify(publicKey, AccountPort.AdmissionTrustDomain, AccountPort.AdmissionPayloadType, payloadBytes, signature))
            return new AdmissionVerifyOutcome.Rejected(AccountErrorCode.AdmissionCredentialInvalidSignature);
        if (clock.UnixSeconds > payload.ExpiresAt)
            return new AdmissionVerifyOutcome.Rejected(AccountErrorCode.AdmissionCredentialExpired);
        if (payload.IsUnbound)
            return new AdmissionVerifyOutcome.Rejected(AccountErrorCode.AdmissionCredentialUnbound);
        if (!payload.AllocationClaims.IsBound)
            return new AdmissionVerifyOutcome.Rejected(AccountErrorCode.AdmissionBindingMismatch);
        if (allocationContext is { } trusted
            && (!trusted.IsBound || !payload.AllocationClaims.Equals(trusted)))
            return new AdmissionVerifyOutcome.Rejected(AccountErrorCode.AdmissionBindingMismatch);
        if (LoginNameRules.IsBotNamespace(payload.LoginName) && !payload.BotToolContext)
            return new AdmissionVerifyOutcome.Rejected(AccountErrorCode.BotNamespaceAdmissionForbidden);
        return new AdmissionVerifyOutcome.Accepted(payload);
    }

    public static bool TryVerifyAccountAuth(string wire, byte expectedKeyId, ReadOnlySpan<byte> publicKey, IAccountClock clock, out AccountAuthPrincipal principal, out string errorCode)
    {
        principal = null!;
        errorCode = AccountErrorCode.AdmissionCredentialMalformed;
        if (!TrySplit(wire, out var payloadBytes, out var signature) || !TryDecodePayload(payloadBytes, out var payload)) return false;
        if (payload.KeyId != expectedKeyId || !LumioSignature.Verify(publicKey, AccountPort.AdmissionTrustDomain, AccountPort.AdmissionPayloadType, payloadBytes, signature))
        {
            errorCode = AccountErrorCode.AdmissionCredentialInvalidSignature;
            return false;
        }
        if (clock.UnixSeconds > payload.ExpiresAt) { errorCode = AccountErrorCode.AdmissionCredentialExpired; return false; }
        if (!payload.IsUnbound) { errorCode = AccountErrorCode.AdmissionBindingMismatch; return false; }
        principal = new AccountAuthPrincipal(payload.AccountId, payload.LoginName, payload.BotToolContext, payload.KeyId, payload.ExpiresAt);
        errorCode = string.Empty;
        return true;
    }

    internal static byte[] EncodePayload(AdmissionCredentialPayload payload)
    {
        var writer = new LumioBinWriter();
        writer.WriteU16(AccountPort.AdmissionPayloadVersion);
        writer.WriteU8(payload.KeyId);
        writer.WriteAscii(payload.AccountId);
        writer.WriteAscii(payload.LoginName);
        writer.WriteU8(payload.BotToolContext ? (byte)1 : (byte)0);
        writer.WriteU64(payload.IssuedAt);
        writer.WriteU64(payload.ExpiresAt);
        writer.WriteFixedBytes(payload.Nonce);
        writer.WriteAscii(payload.ServerAudience);
        writer.WriteAscii(payload.GameId);
        writer.WriteAscii(payload.GameReleaseId);
        writer.WriteAscii(payload.ContractId);
        writer.WriteAscii(payload.RoomId);
        writer.WriteAscii(payload.AllocationId);
        return writer.ToArray();
    }

    internal static bool TryDecodePayload(byte[] bytes, out AdmissionCredentialPayload payload)
    {
        payload = default;
        var reader = new LumioBinReader(bytes);
        if (!reader.TryReadU16(out var version) || version != AccountPort.AdmissionPayloadVersion
            || !reader.TryReadU8(out var keyId) || !reader.TryReadAscii(out var accountId)
            || !reader.TryReadAscii(out var loginName) || !reader.TryReadU8(out var bot) || (bot != 0 && bot != 1)
            || !reader.TryReadU64(out var issuedAt) || !reader.TryReadU64(out var expiresAt)
            || !reader.TryReadFixedBytes(16, out var nonce) || !reader.TryReadAscii(out var audience)
            || !reader.TryReadAscii(out var gameId) || !reader.TryReadAscii(out var release)
            || !reader.TryReadAscii(out var contract) || !reader.TryReadAscii(out var room)
            || !reader.TryReadAscii(out var allocation) || reader.Remaining != 0)
            return false;
        if (!IsAccountId(accountId) || !LoginNameRules.IsValid(loginName))
            return false;
        payload = new AdmissionCredentialPayload(keyId, accountId, loginName, bot == 1, issuedAt, expiresAt, nonce, audience, gameId, release, contract, room, allocation);
        return true;
    }

    internal static string IssueFromPayload(ReadOnlySpan<byte> privateSeed, AdmissionCredentialPayload payload)
    {
        var payloadBytes = EncodePayload(payload);
        var signature = LumioSignature.Sign(privateSeed, AccountPort.AdmissionTrustDomain, AccountPort.AdmissionPayloadType, payloadBytes);
        var framed = new byte[payloadBytes.Length + Ed25519Keys.SignatureLength];
        payloadBytes.CopyTo(framed, 0);
        signature.CopyTo(framed.AsSpan(payloadBytes.Length));
        return Base64Url.Encode(framed);
    }

    private static byte[] NewNonce()
    {
        var nonce = new byte[16];
        RandomNumberGenerator.Fill(nonce);
        return nonce;
    }

    private static bool TrySplit(string wire, out byte[] payload, out byte[] signature)
    {
        payload = Array.Empty<byte>(); signature = Array.Empty<byte>();
        if (string.IsNullOrEmpty(wire) || wire.Length > AccountPort.MaxFrameBytes * 2 || !Base64Url.TryDecode(wire, out var framed) || framed.Length <= Ed25519Keys.SignatureLength) return false;
        payload = framed[..^Ed25519Keys.SignatureLength]; signature = framed[^Ed25519Keys.SignatureLength..]; return true;
    }

    private static bool IsAccountId(string value)
    {
        if (value.Length != 37 || !value.StartsWith("acct_", StringComparison.Ordinal))
            return false;
        for (var i = 5; i < value.Length; i++)
        {
            var c = value[i];
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                return false;
        }
        return true;
    }
}
