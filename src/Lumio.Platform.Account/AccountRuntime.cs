using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Lumio.Platform.Account;

public readonly record struct LoginOrRegisterRequest(
    string? LoginName,
    string? Password,
    string? BotToolCredential = null,
    string Port = "ws",
    string? Ip = null,
    string? UserAgent = null);

public sealed class AccountRuntime : IDisposable
{
    private readonly AccountServerOptions options;
    private readonly PostgresAccountStore store;
    private readonly AccountWorld world = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> creationGates = new(StringComparer.Ordinal);
    private readonly FixedWindowRateLimiter limiter;
    private bool disposed;

    private AccountRuntime(AccountServerOptions options)
    {
        this.options = options;
        store = new PostgresAccountStore(options.DbContextFactory);
        limiter = new FixedWindowRateLimiter(options.RateLimits);
        AdmissionPublicKey = Ed25519Keys.PublicKeyFromSeed(options.AdmissionPrivateSeed);
    }

    public static AccountRuntime Open(AccountServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return new AccountRuntime(options);
    }

    public PostgresAccountStore Store => store;
    public byte[] AdmissionPublicKey { get; }
    public int EntityCount => world.Count;

    public LoginOrRegisterOutcome LoginOrRegister(string loginName, string password, string? botToolCredential)
        => LoginOrRegisterAsync(new LoginOrRegisterRequest(loginName, password, botToolCredential)).GetAwaiter().GetResult();

    public Task<LoginOrRegisterOutcome> LoginOrRegisterAsync(string loginName, string password, string? botToolCredential = null, string? ip = null, CancellationToken cancellationToken = default)
        => LoginOrRegisterAsync(new LoginOrRegisterRequest(loginName, password, botToolCredential, "ws", ip), cancellationToken);

    public async Task<LoginOrRegisterOutcome> LoginOrRegisterAsync(LoginOrRegisterRequest request, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var loginName = request.LoginName ?? string.Empty;
        if (request.LoginName is null || request.Password is null)
            return await RejectAsync(loginName, AccountErrorCode.InvalidRequest, "missing fields", request, cancellationToken).ConfigureAwait(false);
        if (!LoginNameRules.IsValid(loginName))
            return await RejectAsync(loginName, AccountErrorCode.InvalidUsername, "loginName does not match grammar", request, cancellationToken).ConfigureAwait(false);
        if (request.Password.Length < AccountPort.PasswordMinLength || request.Password.Length > AccountPort.PasswordMaxLength)
            return await RejectAsync(loginName, AccountErrorCode.InvalidPassword, "password length out of range", request, cancellationToken).ConfigureAwait(false);
        var botName = LoginNameRules.IsBotNamespace(loginName);

        // The world is an identity projection only; credentials always come from PostgreSQL.
        var record = await store.FindByLoginNameAsync(loginName, cancellationToken).ConfigureAwait(false);
        if (record is not null) Restore(record);
        if (!limiter.Allow(request.Ip, loginName, record?.AccountId, options.Clock.UnixSeconds))
            return await RejectAsync(loginName, AccountErrorCode.RateLimited, "rate limit exceeded", request, cancellationToken, record?.EntityId).ConfigureAwait(false);
        if (botName && !string.IsNullOrEmpty(request.BotToolCredential)
            && !BotToolCredential.TryVerify(request.BotToolCredential, options.BotToolPublicKey, options.Clock, out var botCode))
            return await RejectAsync(loginName, botCode, "bot-tool credential rejected", request, cancellationToken, record?.EntityId).ConfigureAwait(false);
        if (botName && string.IsNullOrEmpty(request.BotToolCredential))
            return await RejectAsync(loginName, record is null ? AccountErrorCode.BotNamespaceRegisterForbidden : AccountErrorCode.BotNamespaceLoginForbidden, "bot namespace requires a valid bot-tool credential", request, cancellationToken, record?.EntityId).ConfigureAwait(false);
        if (record is not null)
        {
            if (!string.Equals(record.Status, "active", StringComparison.Ordinal))
                return await RejectAsync(loginName, AccountErrorCode.AccountBanned, "account is banned", request, cancellationToken, record.EntityId).ConfigureAwait(false);
            if (record.PasswordHash is null || !Argon2idPasswordHasher.Verify(record.PasswordHash, request.Password))
                return await RejectAsync(loginName, AccountErrorCode.WrongPassword, "password does not match", request, cancellationToken, record.EntityId).ConfigureAwait(false);
            var active = await RefreshAndTouchActiveAsync(record, cancellationToken).ConfigureAwait(false);
            if (active is null)
                return await RejectAsync(loginName, AccountErrorCode.AccountBanned, "account is banned", request, cancellationToken, record.EntityId).ConfigureAwait(false);
            return await IssueAsync(active, false, request, cancellationToken).ConfigureAwait(false);
        }
        if (string.Equals(options.RegistrationProfile, "production", StringComparison.Ordinal) && !botName)
            return await RejectAsync(loginName, AccountErrorCode.RegistrationRequiresPlatform, "ordinary registration must use platform email registration", request, cancellationToken).ConfigureAwait(false);
        var created = await CreateOrGetAsync(loginName, request.Password, null, cancellationToken).ConfigureAwait(false);
        record = created.Record;
        if (record is null)
            return await RejectAsync(loginName, AccountErrorCode.RateLimited, "account creation could not be completed", request, cancellationToken).ConfigureAwait(false);
        Restore(record);
        if (!created.NewlyCreated)
        {
            if (!string.Equals(record.Status, "active", StringComparison.Ordinal))
                return await RejectAsync(loginName, AccountErrorCode.AccountBanned, "account is banned", request, cancellationToken, record.EntityId).ConfigureAwait(false);
            if (record.PasswordHash is null || !Argon2idPasswordHasher.Verify(record.PasswordHash, request.Password))
                return await RejectAsync(loginName, AccountErrorCode.WrongPassword, "password does not match", request, cancellationToken, record.EntityId).ConfigureAwait(false);
            var active = await RefreshAndTouchActiveAsync(record, cancellationToken).ConfigureAwait(false);
            if (active is null)
                return await RejectAsync(loginName, AccountErrorCode.AccountBanned, "account is banned", request, cancellationToken, record.EntityId).ConfigureAwait(false);
            return await IssueAsync(active, false, request, cancellationToken).ConfigureAwait(false);
        }
        options.Audit.Write("account_created", new Dictionary<string, string>(StringComparer.Ordinal) { ["accountId"] = record.AccountId, ["loginName"] = record.LoginName });
        var newlyCreatedActive = await RefreshAndTouchActiveAsync(record, cancellationToken).ConfigureAwait(false);
        if (newlyCreatedActive is null)
            return await RejectAsync(loginName, AccountErrorCode.AccountBanned, "account is banned", request, cancellationToken, record.EntityId).ConfigureAwait(false);
        return await IssueAsync(newlyCreatedActive, true, request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AccountRecord?> RegisterWithEmailAsync(string email, string loginName, string password, int avatarId = 1, CancellationToken cancellationToken = default)
    {
        if (!LoginNameRules.IsValid(loginName) || LoginNameRules.IsBotNamespace(loginName)) return null;
        if (password.Length < AccountPort.PasswordMinLength || password.Length > AccountPort.PasswordMaxLength) return null;
        var record = await store.CreateAsync(NewAccountId(), loginName, Argon2idPasswordHasher.Hash(password), email.ToLowerInvariant(), avatarId, UtcNow(), cancellationToken).ConfigureAwait(false);
        if (record is not null) Restore(record);
        return record;
    }

    public async Task<AccountRecord?> VerifyPasswordAsync(string identifier, string password, string port = "ws", string? ip = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var record = identifier.Contains('@', StringComparison.Ordinal)
            ? await FindByEmailAsync(identifier, cancellationToken).ConfigureAwait(false)
            : await store.FindByLoginNameAsync(identifier, cancellationToken).ConfigureAwait(false);
        var passwordMatches = record is not null && record.PasswordHash is not null && Argon2idPasswordHasher.Verify(record.PasswordHash, password);
        if (!passwordMatches || record is null || !string.Equals(record.Status, "active", StringComparison.Ordinal))
        {
            var failureCode = record?.Status == "banned" ? AccountErrorCode.AccountBanned : AccountErrorCode.WrongPassword;
            await store.RecordLoginAttemptAsync(identifier, port, "failure", failureCode, ip, userAgent, record?.EntityId, UtcNow(), cancellationToken).ConfigureAwait(false);
            return null;
        }

        var active = await RefreshAndTouchActiveAsync(record, cancellationToken).ConfigureAwait(false);
        if (active is null)
        {
            await store.RecordLoginAttemptAsync(identifier, port, "failure", AccountErrorCode.AccountBanned, ip, userAgent, record.EntityId, UtcNow(), cancellationToken).ConfigureAwait(false);
            return null;
        }

        await store.RecordLoginAttemptAsync(identifier, port, "success", null, ip, userAgent, active.EntityId, UtcNow(), cancellationToken).ConfigureAwait(false);
        return active;
    }

    public bool VerifyAccountAuthCredential(string credential, out AccountAuthPrincipal principal, out string errorCode)
        => AdmissionCredential.TryVerifyAccountAuth(credential, options.AdmissionKeyId, AdmissionPublicKey, options.Clock, out principal, out errorCode);

    public async Task<AccountAuthPrincipal?> VerifyAccountAuthCredentialAsync(string credential, CancellationToken cancellationToken = default)
    {
        if (!VerifyAccountAuthCredential(credential, out var principal, out _)) return null;
        var record = await store.FindByAccountIdAsync(principal.AccountId, cancellationToken).ConfigureAwait(false);
        if (record is null || record.Status != "active" || !string.Equals(record.LoginName, principal.LoginName, StringComparison.Ordinal)) return null;
        return principal;
    }

    public AdmissionVerifyOutcome VerifyAdmission(string admissionCredential, AdmissionAllocationClaims allocationContext)
        => AdmissionCredential.Verify(admissionCredential, options.AdmissionKeyId, AdmissionPublicKey, options.Clock, allocationContext);

    public void Audit(string kind, IReadOnlyDictionary<string, string> fields) => options.Audit.Write(kind, fields);

    public (string Credential, ulong ExpiresAt) IssueAdmissionCredential(AccountAuthPrincipal principal, AdmissionAllocationClaims claims)
    {
        if (!claims.IsBound) throw new ArgumentException("bound allocation claims are required", nameof(claims));
        var issuedAt = options.Clock.UnixSeconds;
        var expiresAt = issuedAt + (ulong)AccountPort.AdmissionCredentialTtlSeconds;
        var credential = AdmissionCredential.IssueBound(options.AdmissionPrivateSeed, principal.KeyId, principal.AccountId, principal.LoginName, principal.BotToolContext, issuedAt, expiresAt, claims);
        options.Audit.Write("admission_credential_issued", new Dictionary<string, string>(StringComparer.Ordinal) { ["accountId"] = principal.AccountId, ["keyId"] = principal.KeyId.ToString(System.Globalization.CultureInfo.InvariantCulture), ["expiresAt"] = expiresAt.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        return (credential, expiresAt);
    }

    public AccountIdentityComponent? FindByLoginName(string loginName) => world.TryGetByLoginName(loginName, out var identity) ? identity : null;
    public IReadOnlyList<AccountIdentityComponent> SnapshotIdentities() => world.Snapshot();

    public void Dispose()
    {
        if (disposed) return;
        CryptographicOperations.ZeroMemory(options.AdmissionPrivateSeed);
        disposed = true;
    }

    private async Task<AccountRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        // Email lookup is intentionally isolated in a fresh store query.
        await using var db = await options.DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var normalizedEmail = email.ToLowerInvariant();
        var account = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(db.Accounts.AsNoTracking(), a => a.Email == normalizedEmail, cancellationToken).ConfigureAwait(false);
        return account is null ? null : await store.FindByAccountIdAsync(account.AccountId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(AccountRecord? Record, bool NewlyCreated)> CreateOrGetAsync(string loginName, string password, string? email, CancellationToken cancellationToken)
    {
        var gate = creationGates.GetOrAdd(loginName, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await store.FindByLoginNameAsync(loginName, cancellationToken).ConfigureAwait(false);
            if (existing is not null) return (existing, false);
            var hash = Argon2idPasswordHasher.Hash(password);
            var created = await store.CreateAsync(NewAccountId(), loginName, hash, email, utcNow: UtcNow(), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (created is not null) return (created, true);
            return (await store.FindByLoginNameAsync(loginName, cancellationToken).ConfigureAwait(false), false);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<AccountRecord?> RefreshAndTouchActiveAsync(AccountRecord record, CancellationToken cancellationToken)
    {
        var current = await store.FindByAccountIdAsync(record.AccountId, cancellationToken).ConfigureAwait(false);
        if (current is null || !string.Equals(current.Status, "active", StringComparison.Ordinal)) return null;
        if (!await store.TouchLastLoginAsync(current.EntityId, UtcNow(), cancellationToken).ConfigureAwait(false)) return null;

        // Re-read after the conditional update so a concurrent durable ban is observed before issuing credentials.
        current = await store.FindByAccountIdAsync(current.AccountId, cancellationToken).ConfigureAwait(false);
        if (current is null || !string.Equals(current.Status, "active", StringComparison.Ordinal)) return null;
        Restore(current);
        return current;
    }

    private async Task<LoginOrRegisterOutcome> IssueAsync(AccountRecord record, bool newlyCreated, LoginOrRegisterRequest request, CancellationToken cancellationToken)
    {
        var issuedAt = options.Clock.UnixSeconds;
        var expiresAt = issuedAt + (ulong)AccountPort.AdmissionCredentialTtlSeconds;
        var credential = AdmissionCredential.Issue(options.AdmissionPrivateSeed, options.AdmissionKeyId, record.AccountId, record.LoginName, LoginNameRules.IsBotNamespace(record.LoginName) && !string.IsNullOrEmpty(request.BotToolCredential), issuedAt, expiresAt);
        options.Audit.Write("login_succeeded", new Dictionary<string, string>(StringComparer.Ordinal) { ["accountId"] = record.AccountId, ["accountNewlyCreated"] = newlyCreated ? "true" : "false" });
        options.Audit.Write("account_auth_credential_issued", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["accountId"] = record.AccountId,
            ["keyId"] = options.AdmissionKeyId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["expiresAt"] = expiresAt.ToString(System.Globalization.CultureInfo.InvariantCulture),
        });
        await store.RecordLoginAttemptAsync(record.LoginName, request.Port, "success", null, request.Ip, request.UserAgent, record.EntityId, UtcNow(), cancellationToken).ConfigureAwait(false);
        return LoginOrRegisterOutcome.Ok(newlyCreated, record.AccountId, record.LoginName, credential, expiresAt);
    }

    private async Task<LoginOrRegisterOutcome> RejectAsync(string loginName, string code, string detail, LoginOrRegisterRequest request, CancellationToken cancellationToken, long? entityId = null)
    {
        options.Audit.Write("login_rejected", new Dictionary<string, string>(StringComparer.Ordinal) { ["loginName"] = loginName, ["code"] = code });
        await store.RecordLoginAttemptAsync(loginName, request.Port, "failure", code, request.Ip, request.UserAgent, entityId, UtcNow(), cancellationToken).ConfigureAwait(false);
        return LoginOrRegisterOutcome.Reject(code, detail);
    }

    private void Restore(AccountRecord record)
    {
        world.Restore(new AccountIdentityComponent((ulong)record.EntityId, record.AccountId, record.Uid, record.LoginName, record.Email, record.AvatarId, record.Role, record.Status, ToUnix(record.CreatedAt), record.SecurityVersion));
    }

    private string NewAccountId()
    {
        Span<byte> raw = stackalloc byte[16];
        RandomNumberGenerator.Fill(raw);
        return "acct_" + Hex.EncodeLower(raw);
    }

#pragma warning disable RS0030
    private DateTime UtcNow() => DateTime.UnixEpoch.AddSeconds(options.Clock.UnixSeconds);
    private static ulong ToUnix(DateTime value) => (ulong)(value.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
#pragma warning restore RS0030
}

internal sealed class FixedWindowRateLimiter
{
    private readonly AccountRateLimitOptions options;
    private readonly Dictionary<string, Queue<ulong>> windows = new(StringComparer.Ordinal);
    private readonly object gate = new();
    public FixedWindowRateLimiter(AccountRateLimitOptions options) => this.options = options;
    internal int TrackedKeyCount
    {
        get { lock (gate) return windows.Count; }
    }

    public bool Allow(string? ip, string loginName, string? accountId, ulong now)
    {
        lock (gate)
        {
            CleanupStale(now);
            return AllowKey("ip:" + (ip ?? "unknown"), options.MaxRequestsPerIp, now)
                && AllowKey("name:" + loginName, options.MaxRequestsPerLoginName, now)
                && (accountId is null || AllowKey("account:" + accountId, options.MaxRequestsPerAccount, now));
        }
    }
    private bool AllowKey(string key, int max, ulong now)
    {
        if (!windows.TryGetValue(key, out var queue))
        {
            EnsureCapacity(now);
            windows[key] = queue = new Queue<ulong>();
        }
        Trim(queue, now);
        if (queue.Count >= max) return false;
        queue.Enqueue(now); return true;
    }

    private void CleanupStale(ulong now)
    {
        foreach (var pair in windows.ToArray())
        {
            Trim(pair.Value, now);
            if (pair.Value.Count == 0) windows.Remove(pair.Key);
        }
    }

    private void EnsureCapacity(ulong now)
    {
        if (windows.Count < options.MaxTrackedKeys) return;
        CleanupStale(now);
        while (windows.Count >= options.MaxTrackedKeys && windows.Count > 0)
        {
            string? oldestKey = null;
            ulong oldest = ulong.MaxValue;
            foreach (var pair in windows)
            {
                var first = pair.Value.Count == 0 ? 0 : pair.Value.Peek();
                if (oldestKey is null || first < oldest)
                {
                    oldestKey = pair.Key;
                    oldest = first;
                }
            }
            if (oldestKey is null) break;
            windows.Remove(oldestKey);
        }
    }

    private void Trim(Queue<ulong> queue, ulong now)
    {
        while (queue.Count > 0 && now >= queue.Peek() && now - queue.Peek() >= (ulong)options.WindowSeconds)
            queue.Dequeue();
    }
}
