using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Borderland.Dreams.Tests;

public class SqliteMigrationTests
{
    // Schema at main commit 3a52b3e, the parent of the comment-deletion merge.
    private const string PreviousSchema = "20261121213506_AddCommentEditedUtc";

    [Fact]
    public async Task EmptyDatabaseMigratesToCurrentSchema()
    {
        await using var database = new FundingTestDatabase();
        await using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        await AssertAllMigrationsApplied(context);
        Assert.Empty(await context.Dream.ToListAsync());
        Assert.Empty(await context.Wallet.ToListAsync());
        Assert.Empty(await context.Fund.ToListAsync());
    }

    [Fact]
    public async Task PreviousSchemaUpgradePreservesDreamsWalletsAndContributions()
    {
        await using var database = new FundingTestDatabase();
        await using (var scope = database.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync(PreviousSchema);
            Assert.Equal(PreviousSchema, (await context.Database.GetAppliedMigrationsAsync()).Last());

            // Synthetic data inserted against the historical schema, independently of the current EF model.
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO Cloud (ID, Name, Slug, TotalFundsAvailable, TotalFundsSpent, IsDeleted,
                    CreatedAtUtc, SendDreamWelcomeEmail)
                VALUES (10, 'Fixture cloud', 'fixture-cloud', '100.0', '0.0', 0, '2026-11-21', 0);

                INSERT INTO AspNetUsers (Id, UserName, Name, EmailConfirmed, TwoFactorEnabled,
                    LockoutEnabled, AccessFailedCount)
                VALUES ('fixture-user', 'fixture-user', 'Fixture user', 0, 0, 0, 0);

                INSERT INTO Dream (ID, Name, Description, Status, CloudID, IsDeleted, CreatedAtUtc)
                VALUES (20, 'Fixture dream', 'A synthetic dream', 1, 10, 0, '2026-11-21');

                INSERT INTO FundingRound (ID, CloudID, StartDateUtc, EndDateUtc, MaxPercentagePerDream,
                    AllocationPerUser, TotalFundsAllocated, IsLastRound, CreatedAtUtc)
                VALUES (30, 10, '2026-11-21', '2026-11-23', '100.0', '100.0', '100.0', 0, '2026-11-21');

                INSERT INTO Wallet (ID, UserID, CloudID, Balance, CreatedAtUtc)
                VALUES (40, 'fixture-user', 10, '74.75', '2026-11-21');

                INSERT INTO Fund (ID, DreamID, UserID, FundingRoundID, Amount, CreatedAtUtc)
                VALUES (50, 20, 'fixture-user', 30, '25.25', '2026-11-22');
                """);
        }

        await using var upgraded = database.CreateScope();
        var current = upgraded.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await current.Database.MigrateAsync();
        await AssertAllMigrationsApplied(current);

        var dream = await current.Dream.SingleAsync();
        Assert.Equal((20, 10, "Fixture dream", "A synthetic dream", DreamStatus.Draft),
            (dream.ID, dream.CloudID, dream.Name, dream.Description, dream.Status));
        var wallet = await current.Wallet.SingleAsync();
        Assert.Equal((40, 10, "fixture-user", 74.75m), (wallet.ID, wallet.CloudID, wallet.UserID, wallet.Balance));
        var contribution = await current.Fund.SingleAsync();
        Assert.Equal((50, 20, "fixture-user", (int?)30, 25.25m),
            (contribution.ID, contribution.DreamID, contribution.UserID, contribution.FundingRoundID, contribution.Amount));
        Assert.Equal(10, (await current.FundingRound.SingleAsync()).CloudID);
    }

    private static async Task AssertAllMigrationsApplied(ApplicationDbContext context)
    {
        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }
}
