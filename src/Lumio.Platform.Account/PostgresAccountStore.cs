using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lumio.Platform.Data;
using Microsoft.EntityFrameworkCore;
using DataAccount = Lumio.Platform.Data.Account;

#pragma warning disable RS0030
namespace Lumio.Platform.Account;

public sealed record AccountRecord(
    long EntityId,
    string AccountId,
    long Uid,
    string LoginName,
    string? Email,
    DateTime? EmailVerifiedAt,
    long SecurityVersion,
    int AvatarId,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    string? PasswordHash);

public sealed class PostgresAccountStore
{
    private readonly IDbContextFactory<PlatformDbContext> contextFactory;

    public PostgresAccountStore(IDbContextFactory<PlatformDbContext> contextFactory)
    {
        this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<AccountRecord?> FindByLoginNameAsync(string loginName, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.LoginName == loginName, cancellationToken).ConfigureAwait(false);
        return account is null ? null : await WithCredentialAsync(db, account, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AccountRecord?> FindByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.AccountId == accountId, cancellationToken).ConfigureAwait(false);
        return account is null ? null : await WithCredentialAsync(db, account, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AccountRecord?> CreateAsync(
        string accountId,
        string loginName,
        string passwordHash,
        string? email = null,
        int avatarId = 1,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = utcNow ?? DateTime.UnixEpoch;
        var account = new DataAccount
        {
            AccountId = accountId,
            LoginName = loginName,
            Email = email,
            AvatarId = avatarId,
            Role = "player",
            Status = "active",
            SecurityVersion = 1,
            CreatedAt = now,
        };
        db.Accounts.Add(account);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            db.AccountCredentials.Add(new AccountCredential
            {
                AccountId = account.Id,
                Argon2idHash = passwordHash,
                UpdatedAt = now,
            });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return ToRecord(account, passwordHash);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }
    }

    public async Task<bool> TouchLastLoginAsync(long entityId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var changed = await db.Accounts.Where(a => a.Id == entityId && a.Status == "active")
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.LastLoginAt, utcNow), cancellationToken).ConfigureAwait(false);
        return changed == 1;
    }

    public async Task RecordLoginAttemptAsync(string identifier, string port, string outcome, string? errorCode, string? ip, string? userAgent, long? accountId = null, DateTime? utcNow = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        db.LoginAttempts.Add(new LoginAttempt
        {
            Identifier = identifier,
            Port = port,
            Outcome = outcome,
            ErrorCode = errorCode,
            Ip = ip,
            UserAgent = userAgent,
            AccountId = accountId,
            At = utcNow ?? DateTime.UnixEpoch,
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Accounts.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<AccountRecord> WithCredentialAsync(PlatformDbContext db, DataAccount account, CancellationToken cancellationToken)
    {
        var hash = await db.AccountCredentials.AsNoTracking().Where(c => c.AccountId == account.Id).Select(c => c.Argon2idHash)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return ToRecord(account, hash);
    }

    private static AccountRecord ToRecord(DataAccount account, string? hash) => new(
        account.Id, account.AccountId, account.Uid, account.LoginName, account.Email, account.EmailVerifiedAt,
        account.SecurityVersion, account.AvatarId, account.Role, account.Status, account.CreatedAt, account.LastLoginAt, hash);
}
#pragma warning restore RS0030
