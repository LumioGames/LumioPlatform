using System;
using System.Linq;
using System.Threading.Tasks;
using Lumio.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lumio.Platform.Tests.Data;

#pragma warning disable RS0030 // Fixed UTC fixtures do not read the wall clock.

public sealed class InitialSchemaTests
{
    [Fact]
    public async Task MigrationIsIdempotentAndUidStartsAtOneHundredThousand()
    {
        await using var database = await DatabaseSchema.CreateAsync();
        await using var db = database.CreateContext();

        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
        db.Accounts.AddRange(NewAccount("acct_00000000000000000000000000000001", "alice"), NewAccount("acct_00000000000000000000000000000002", "bob"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var generatedUids = await db.Accounts.OrderBy(account => account.Uid).Select(account => account.Uid).ToArrayAsync(TestContext.Current.CancellationToken);
        Assert.Equal([100000L, 100001L], generatedUids);
    }

    [Theory]
    [InlineData(UniqueAccountField.AccountId)]
    [InlineData(UniqueAccountField.Uid)]
    [InlineData(UniqueAccountField.LoginName)]
    [InlineData(UniqueAccountField.Email)]
    public async Task AccountUniqueIndexRejectsDuplicate(UniqueAccountField field)
    {
        await using var database = await DatabaseSchema.CreateAsync();
        await using (var db = database.CreateContext())
        {
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
            var existing = NewAccount("acct_00000000000000000000000000000001", "Alice");
            existing.Uid = 150000;
            existing.Email = "alice@example.com";
            db.Accounts.Add(existing);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var duplicateDb = database.CreateContext();
        var duplicate = NewAccount("acct_00000000000000000000000000000002", "alice");
        duplicate.Uid = 150001;
        duplicate.Email = "other@example.com";
        switch (field)
        {
            case UniqueAccountField.AccountId: duplicate.AccountId = "acct_00000000000000000000000000000001"; break;
            case UniqueAccountField.Uid: duplicate.Uid = 150000; break;
            case UniqueAccountField.LoginName:
                duplicateDb.Accounts.Add(duplicate);
                await duplicateDb.SaveChangesAsync(TestContext.Current.CancellationToken);
                duplicate = NewAccount("acct_00000000000000000000000000000003", "Alice");
                duplicate.Uid = 150002;
                duplicate.Email = "third@example.com";
                break;
            case UniqueAccountField.Email: duplicate.Email = "alice@example.com"; break;
            default: throw new ArgumentOutOfRangeException(nameof(field));
        }

        duplicateDb.Accounts.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateDb.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GameSlugUniqueIndexRejectsDuplicate()
    {
        await using var database = await DatabaseSchema.CreateAsync();
        await using (var db = database.CreateContext())
        {
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
            db.Games.Add(NewGame("hello"));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var duplicateDb = database.CreateContext();
        duplicateDb.Games.Add(NewGame("hello"));
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateDb.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    private static Account NewAccount(string accountId, string loginName) => new()
    {
        AccountId = accountId,
        LoginName = loginName,
        AvatarId = 1,
        Role = "player",
        Status = "active",
        CreatedAt = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc),
    };

    private static Game NewGame(string slug) => new()
    {
        Slug = slug,
        Name = "Hello",
        Summary = "Test game",
        CoverUrl = "/cover.png",
        Status = "draft",
        BundleDir = "hello",
        ServerWsUrl = "ws://127.0.0.1:1",
        Subprotocol = "lumio.mvp.v0",
        ContractId = "lumio.gameplay-envelope.v1",
        CreatedAt = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc),
    };

    public enum UniqueAccountField
    {
        AccountId,
        Uid,
        LoginName,
        Email,
    }
}

#pragma warning restore RS0030
