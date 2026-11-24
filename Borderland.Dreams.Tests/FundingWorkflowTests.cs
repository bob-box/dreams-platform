using System.Data.Common;
using System.Security.Claims;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.Scheduling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Borderland.Dreams.Tests;

public class FundingWorkflowTests
{
    [Theory]
    [InlineData("main", false, true, true)]
    [InlineData("co", false, true, false)]
    [InlineData("outsider", false, false, false)]
    [InlineData("outsider", true, true, true)]
    public async Task DraftPermissionsRespectDreamerRoleAndCloud(
        string userId, bool admin, bool canRead, bool canDelete)
    {
        await using var database = new FundingTestDatabase();
        await Seed(database, firstDreamStatus: DreamStatus.Draft);
        await using var scope = database.CreateScope();
        await using var otherCloud = database.CreateScope(2);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        if (admin)
            claims.Add(new Claim(ClaimTypes.Role, Constants.AdminRole));

        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        try
        {
            var dreams = scope.ServiceProvider.GetRequiredService<IDreamService>();
            Assert.Equal(canRead ? DreamServiceStatus.Success : DreamServiceStatus.NotAllowed,
                (await dreams.GetFull(1)).Status);
            Assert.Equal(canDelete ? DreamServiceStatus.Success : DreamServiceStatus.NotAllowed,
                (await dreams.GetForDelete(1)).Status);
            Assert.Equal(DreamServiceStatus.NotFound,
                (await otherCloud.ServiceProvider.GetRequiredService<IDreamService>().GetForDelete(1)).Status);
        }
        finally
        {
            accessor.HttpContext = null;
        }
    }

    [Fact]
    public async Task FundingRejectsWrongCloudAndIneligibleMembersWithoutDebiting()
    {
        await using var database = new FundingTestDatabase();
        await Seed(database);
        await StartRound(database);
        await using var scope = database.CreateScope();
        var funding = scope.ServiceProvider.GetRequiredService<IDreamFundingService>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var otherDream = await context.Dream.SingleAsync(dream => dream.ID == 3);
        otherDream.Status = DreamStatus.Active;
        await context.SaveChangesAsync();

        Assert.Equal(FundDreamServiceStatus.CantFundDream, (await funding.FundDream(3, 10m, "main")).Status);
        Assert.Equal(FundDreamServiceStatus.CantFundDream, (await funding.FundDream(1, 10m, "outsider")).Status);
        database.Membership.Check = () => Task.FromResult(false);
        Assert.Equal(FundDreamServiceStatus.CantFundDream, (await funding.FundDream(1, 10m, "main")).Status);

        Assert.Empty(await context.Fund.ToListAsync());
        Assert.Equal(100m, await Balance(context, 1));
        Assert.Equal(17m, await Balance(context, 2));
    }

    [Fact]
    public async Task RoundJobsCanRetryAfterFailureAndAfterSuccess()
    {
        var failure = new FailNextCommit();
        await using var database = new FundingTestDatabase(failure);
        await Seed(database);
        await using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var scheduler = scope.ServiceProvider.GetRequiredService<FundingRoundSchedulerService>();
        var funding = scope.ServiceProvider.GetRequiredService<IDreamFundingService>();
        var rounds = scope.ServiceProvider.GetRequiredService<IFundingRoundService>();

        database.Clock.UtcNow = database.Clock.UtcNow.AddSeconds(-1);
        Assert.Null(await rounds.GetActiveRound(1));
        database.Clock.UtcNow = database.Clock.UtcNow.AddSeconds(1);

        failure.Armed = true;
        await Assert.ThrowsAsync<IOException>(() => scheduler.ProcessFundingRoundStart(1));
        Assert.Equal(0m, await Balance(context, 1));
        Assert.Equal(0m, (await context.FundingRound.AsNoTracking().SingleAsync()).TotalFundsAllocated);
        Assert.Equal(DreamStatus.ReadyForFunding, await Status(context, 1));

        await scheduler.ProcessFundingRoundStart(1);
        Assert.Equal(100m, await Balance(context, 1));
        Assert.Equal(100m, (await context.FundingRound.AsNoTracking().SingleAsync()).TotalFundsAllocated);
        Assert.Equal(DreamStatus.Active, await Status(context, 1));
        Assert.Equal(DreamStatus.ReadyForFunding, await Status(context, 3));
        Assert.Equal(FundDreamServiceStatus.Success, (await funding.FundDream(1, 20m, "main")).Status);
        Assert.Equal(FundDreamServiceStatus.Success, (await funding.FundDream(1, 15m, "main")).Status);
        await scheduler.ProcessFundingRoundStart(1);
        Assert.Equal(65m, await Balance(context, 1));
        Assert.Equal(35m, (await context.Fund.AsNoTracking().SingleAsync()).Amount);

        database.Clock.UtcNow = database.Clock.UtcNow.AddHours(1);
        Assert.NotNull(await rounds.GetActiveRound(1));
        Assert.Null(await rounds.GetActiveRound(2));

        failure.Armed = true;
        await Assert.ThrowsAsync<IOException>(() => scheduler.ProcessFundingRoundEnd(1));
        Assert.Equal(65m, await Balance(context, 1));
        Assert.Null((await context.FundingRound.AsNoTracking().SingleAsync()).ProcessedDateUtc);

        await scheduler.ProcessFundingRoundEnd(1);
        Assert.Null(await rounds.GetActiveRound(1));
        var processedAt = database.Clock.UtcNow;
        database.Clock.UtcNow = processedAt.AddSeconds(1);
        await scheduler.ProcessFundingRoundEnd(1);
        await scheduler.ProcessFundingRoundStart(1);

        Assert.Equal(0m, await Balance(context, 1));
        Assert.Equal(17m, await Balance(context, 2));
        Assert.Equal(35m, await context.Cloud.Where(cloud => cloud.ID == 1).Select(cloud => cloud.TotalFundsSpent).SingleAsync());
        Assert.Equal(processedAt, (await context.FundingRound.AsNoTracking().SingleAsync()).ProcessedDateUtc);
        Assert.Equal(DreamStatus.FullyFunded, await Status(context, 1));
        Assert.Equal(DreamStatus.Canceled, await Status(context, 2));
        Assert.Equal(DreamStatus.ReadyForFunding, await Status(context, 3));
        Assert.Equal(35m, (await context.Fund.AsNoTracking().SingleAsync()).Amount);
    }

    [Theory]
    [InlineData(2, 60, 60, 1, 40)]
    [InlineData(2, 30, 40, 2, 30)]
    [InlineData(1, 30, 40, 2, 30)]
    public async Task ConcurrentContributionsUseTheCurrentWalletBalance(
        int secondDreamId, int firstAmount, int secondAmount, int successes, int remaining)
    {
        await using var database = new FundingTestDatabase();
        await Seed(database);
        await StartRound(database);

        // Both requests load the original wallet before either can enter the write transaction.
        var bothCheckingMembership = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        database.Membership.Check = async () =>
        {
            if (Interlocked.Increment(ref arrivals) == 2)
                bothCheckingMembership.SetResult();
            await bothCheckingMembership.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return true;
        };

        async Task<FundDreamServiceStatus> Contribute(int dreamId, int amount)
        {
            await using var scope = database.CreateScope();
            var result = await scope.ServiceProvider.GetRequiredService<IDreamFundingService>()
                .FundDream(dreamId, amount, "main");
            return result.Status;
        }

        var results = await Task.WhenAll(Contribute(1, firstAmount), Contribute(secondDreamId, secondAmount));
        Assert.Equal(successes, results.Count(status => status == FundDreamServiceStatus.Success));
        Assert.Equal(2 - successes, results.Count(status => status == FundDreamServiceStatus.InsufficientFunds));

        await using var inspection = database.CreateScope();
        var context = inspection.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal((decimal)remaining, await Balance(context, 1));
        var contributions = await context.Fund.ToListAsync();
        Assert.Equal(secondDreamId == 1 ? 1 : successes, contributions.Count);
        Assert.Equal(100m - remaining, contributions.Sum(fund => fund.Amount));
        Assert.Equal(17m, await Balance(context, 2));
    }

    private static async Task StartRound(FundingTestDatabase database)
    {
        await using var scope = database.CreateScope();
        await scope.ServiceProvider.GetRequiredService<FundingRoundSchedulerService>().ProcessFundingRoundStart(1);
    }

    private static Task<decimal> Balance(ApplicationDbContext context, int cloudId) =>
        context.Wallet.Where(wallet => wallet.CloudID == cloudId).Select(wallet => wallet.Balance).SingleAsync();

    private static Task<DreamStatus> Status(ApplicationDbContext context, int dreamId) =>
        context.Dream.Where(dream => dream.ID == dreamId).Select(dream => dream.Status).SingleAsync();

    private static async Task Seed(
        FundingTestDatabase database, DreamStatus firstDreamStatus = DreamStatus.ReadyForFunding)
    {
        await using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        context.Cloud.AddRange(
            new Cloud { ID = 1, Name = "First", Slug = "first", TotalFundsAvailable = 500m },
            new Cloud { ID = 2, Name = "Second", Slug = "second" });
        context.Users.AddRange(
            new User { Id = "main", UserName = "main", Email = "main@example.invalid" },
            new User { Id = "co", UserName = "co" },
            new User { Id = "outsider", UserName = "outsider" });
        context.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "main", ClaimType = Constants.JOINED_CLOUD_CLAIM, ClaimValue = "1"
        });
        context.Wallet.AddRange(
            new Wallet { UserID = "main", CloudID = 1 },
            new Wallet { UserID = "main", CloudID = 2, Balance = 17m });
        context.Dream.AddRange(
            new Dream { ID = 1, Name = "First dream", CloudID = 1, Status = firstDreamStatus },
            new Dream { ID = 2, Name = "Second dream", CloudID = 1, Status = DreamStatus.ReadyForFunding },
            new Dream { ID = 3, Name = "Other cloud dream", CloudID = 2, Status = DreamStatus.ReadyForFunding });
        context.Dreamer.AddRange(
            new Dreamer { DreamID = 1, UserID = "main", IsMainDreamer = true },
            new Dreamer { DreamID = 1, UserID = "co" });
        context.FundingRound.Add(new FundingRound
        {
            ID = 1, CloudID = 1, AllocationPerUser = 100m, IsLastRound = true,
            StartDateUtc = database.Clock.UtcNow, EndDateUtc = database.Clock.UtcNow.AddHours(1)
        });
        await context.SaveChangesAsync();
    }

    private sealed class FailNextCommit : DbTransactionInterceptor
    {
        public bool Armed { get; set; }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction, TransactionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            if (Armed)
            {
                Armed = false;
                throw new IOException("Simulated failure before committing the funding job.");
            }

            return new ValueTask<InterceptionResult>(result);
        }
    }
}
