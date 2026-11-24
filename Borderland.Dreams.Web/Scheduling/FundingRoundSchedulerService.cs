using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Scheduling;

public class FundingRoundSchedulerService(
    ILogger<FundingRoundSchedulerService> logger,
    IServiceProvider serviceProvider,
    IAppTime appTime,
    IBackgroundJobClient backgroundJobs
    )
{
    /// <summary>
    /// Replaces the funding round's existing Hangfire jobs with jobs scheduled for its current dates.
    /// </summary>
    /// <param name="fundingRound">The round whose start and end jobs should be scheduled.</param>
    public void ScheduleFundingRoundJobs(FundingRound fundingRound)
    {
        // Cancel existing jobs if any
        CancelFundingRoundJobs(fundingRound);

        // Schedule allocation job at start date
        var startJobId = backgroundJobs.Schedule(
            () => ProcessFundingRoundStart(fundingRound.ID),
            fundingRound.StartDateUtc);

        // Schedule end job at end date
        var endJobId = backgroundJobs.Schedule(
            () => ProcessFundingRoundEnd(fundingRound.ID),
            fundingRound.EndDateUtc);

        // Update the funding round with job IDs
        fundingRound.StartJobId = startJobId;
        fundingRound.EndJobId = endJobId;

        logger.LogInformation(
            "Funding round (ID {FundingRoundId}: cloud ID {CloudId}) scheduled allocation job {StartJobId} for {StartDate} and end job {EndJobId} for {EndDate}",
            fundingRound.ID, fundingRound.CloudID, startJobId, fundingRound.StartDateUtc, endJobId, fundingRound.EndDateUtc);
    }

    /// <summary>
    /// Cancels the scheduled start and end jobs recorded on a funding round.
    /// </summary>
    /// <param name="fundingRound">The round containing the Hangfire job IDs.</param>
    public void CancelFundingRoundJobs(FundingRound fundingRound)
    {
        CancelFundingRoundJobs(
            fundingRound.StartJobId,
            fundingRound.EndJobId,
            fundingRound.ID,
            fundingRound.CloudID);
    }

    /// <summary>
    /// Cancels scheduled jobs by their IDs when the funding-round record has already been deleted.
    /// </summary>
    /// <param name="startJobId">The scheduled allocation job ID, if any.</param>
    /// <param name="endJobId">The scheduled end-processing job ID, if any.</param>
    /// <param name="fundingRoundId">The ID used to identify the round in logs.</param>
    public void CancelFundingRoundJobs(string? startJobId, string? endJobId, int fundingRoundId)
    {
        CancelFundingRoundJobs(startJobId, endJobId, fundingRoundId, null);
    }

    private void CancelFundingRoundJobs(
        string? startJobId,
        string? endJobId,
        int fundingRoundId,
        int? cloudId)
    {
        if (!string.IsNullOrEmpty(startJobId))
        {
            logger.LogInformation(
                "Funding round (ID {FundingRoundId}: Cloud ID {CloudId}) cancelling allocation job {JobId}",
                fundingRoundId, cloudId, startJobId);
            backgroundJobs.Delete(startJobId);
        }
        if (!string.IsNullOrEmpty(endJobId))
        {
            logger.LogInformation(
                "Funding round (ID {FundingRoundId}: Cloud ID {CloudId}) cancelling end job {JobId}",
                fundingRoundId, cloudId, endJobId);
            backgroundJobs.Delete(endJobId);
        }
    }

    [DisableConcurrentExecution(300)] // Prevent overlapping executions for the same funding round
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessFundingRoundStart(int fundingRoundId)
    {
        logger.LogInformation("Funding round (ID {FundingRoundId}): Started processing start sequence", fundingRoundId);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var fundingRound = await GetFundingRoundForAllocationAsync(context, fundingRoundId);

        if (fundingRound == null)
        {
            logger.LogWarning("Funding round (ID {FundingRoundId}): Not found. Stopping..", fundingRoundId);
            return;
        }

        var utcNow = appTime.UtcNow;
        if (fundingRound.StartedAtUtc != null || fundingRound.ProcessedDateUtc != null ||
            utcNow < fundingRound.StartDateUtc || utcNow >= fundingRound.EndDateUtc)
            return;

        var cloud = fundingRound.Cloud;
        var precedingRounds = await context.FundingRound
            .Where(round => round.CloudID == cloud.ID && round.EndDateUtc < fundingRound.StartDateUtc &&
                            round.ProcessedDateUtc == null)
            .OrderBy(round => round.EndDateUtc)
            .ToListAsync();

        // Keep settlement and allocation under the same SQLite writer lock.
        var walletOwnerId = await GetWalletOwnerId(context, cloud.ID);
        foreach (var precedingRound in precedingRounds)
            await SettleRound(context, precedingRound, walletOwnerId, utcNow);

        var wallets = await GetCloudWalletsAsync(context, cloud.ID);
        var totalAllocation = wallets.Count * fundingRound.AllocationPerUser;
        if (fundingRound.AllocationPerUser > 0 && !HasFundsForAllocation(cloud, totalAllocation))
        {
            logger.LogWarning(
                "Funding round {FundingRoundId} in Cloud {CloudId} needs {Required} to allocate but only {Remaining} remains",
                fundingRound.ID, cloud.ID, totalAllocation, cloud.TotalFundsAvailable - cloud.TotalFundsSpent);
        }
        else
        {
            SetWalletBalances(wallets, fundingRound.AllocationPerUser);
            fundingRound.TotalFundsAllocated = totalAllocation;
            fundingRound.StartedAtUtc = utcNow;
            await ActivateReadyDreamsAsync(context, cloud.ID);
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        if (fundingRound.StartedAtUtc != null)
            logger.LogInformation("Funding round {FundingRoundId} started with {TotalAllocation} allocated",
                fundingRound.ID, fundingRound.TotalFundsAllocated);
    }

    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(300)]
    public async Task ProcessFundingRoundEnd(int fundingRoundId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var fundingRound = await GetFundingRoundForAllocationAsync(context, fundingRoundId);
        var utcNow = appTime.UtcNow;
        if (fundingRound == null || fundingRound.ProcessedDateUtc != null || utcNow < fundingRound.EndDateUtc)
            return;

        var walletOwnerId = await GetWalletOwnerId(context, fundingRound.CloudID);
        await SettleRound(context, fundingRound, walletOwnerId, utcNow);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        logger.LogInformation("Funding round {FundingRoundId} closed", fundingRound.ID);
    }

    private static Task<int?> GetWalletOwnerId(ApplicationDbContext context, int cloudId) =>
        FundingRoundService.WalletOwningRoundQuery(context, cloudId)
            .Select(round => (int?)round.ID)
            .FirstOrDefaultAsync();

    private static async Task SettleRound(
        ApplicationDbContext context, FundingRound round, int? walletOwnerId, DateTime utcNow)
    {
        if (round.ID == walletOwnerId)
            SetWalletBalances(await GetCloudWalletsAsync(context, round.CloudID), 0);

        round.Cloud.TotalFundsSpent += await GetTotalSpentAsync(context, round.ID);
        round.ProcessedDateUtc = utcNow;

        if (round.IsLastRound)
            await FinalizeActiveDreamsAsync(context, round.CloudID);
    }

    private static Task<FundingRound?> GetFundingRoundForAllocationAsync(ApplicationDbContext context, int fundingRoundId)
    {
        return context.FundingRound
            .Include(round => round.Cloud)
            .SingleOrDefaultAsync(round => round.ID == fundingRoundId);
    }

    private static Task<List<Wallet>> GetCloudWalletsAsync(ApplicationDbContext context, int cloudId)
    {
        return context.Wallet
            .Where(wallet => wallet.CloudID == cloudId)
            .ToListAsync();
    }

    private static bool HasFundsForAllocation(Cloud cloud, decimal totalAllocation)
    {
        return cloud.TotalFundsAvailable - cloud.TotalFundsSpent >= totalAllocation;
    }

    private static async Task ActivateReadyDreamsAsync(ApplicationDbContext context, int cloudId)
    {
        var dreams = await context.Dream
            .Where(dream => dream.CloudID == cloudId && dream.Status == DreamStatus.ReadyForFunding)
            .ToListAsync();

        foreach (var dream in dreams)
            dream.Status = DreamStatus.Active;
    }

    private static void SetWalletBalances(IEnumerable<Wallet> wallets, decimal allocationPerUser)
    {
        foreach (var wallet in wallets)
            wallet.Balance = allocationPerUser;
    }

    private static Task<decimal> GetTotalSpentAsync(ApplicationDbContext context, int fundingRoundId)
    {
        return context.Fund
            .Where(fund => fund.FundingRoundID == fundingRoundId)
            .SumAsync(fund => fund.Amount);
    }

    /// <summary>
    /// Finalizes active dreams after the last round based on their total funding and minimum requested budget.
    /// </summary>
    private static async Task FinalizeActiveDreamsAsync(
        ApplicationDbContext context,
        int cloudId)
    {
        var activeDreams = await context.Dream
            .Include(dream => dream.BudgetItems)
            .Include(dream => dream.Funds)
            .Where(dream => dream.CloudID == cloudId && dream.Status == DreamStatus.Active)
            .ToListAsync();

        foreach (var dream in activeDreams)
        {
            var totalFunded = dream.Funds.Sum(fund => fund.Amount);
            var minimumGoal = dream.BudgetItems
                .Where(item => item.Type == BudgetItemType.Requesting)
                .Sum(item => item.MinAmount);

            if (totalFunded <= 0)
                dream.Status = DreamStatus.Canceled;
            else if (totalFunded >= minimumGoal)
                dream.Status = DreamStatus.FullyFunded;
            else
                dream.Status = DreamStatus.PartiallyFunded;
        }
    }
}
