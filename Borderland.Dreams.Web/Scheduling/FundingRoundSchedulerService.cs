using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
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
        var fundingRound = await GetFundingRoundForAllocationAsync(context, fundingRoundId);

        if (fundingRound == null)
        {
            logger.LogWarning("Funding round (ID {FundingRoundId}): Not found. Stopping..", fundingRoundId);
            return;
        }

        // if (fundingRound.StartDate > appTime.UtcNow)
        // {
        //     logger.LogInformation(
        //         "Funding round ID {FundingRoundId} now starts at {StartDate}; skipping an outdated allocation job",
        //         fundingRoundId,
        //         fundingRound.StartDate);
        //     return;
        // }

        var cloud = fundingRound.Cloud;
        if (ShouldRunAllocation(fundingRound, cloud))
        {
            var wallets = await GetCloudWalletsAsync(context, cloud.ID);
            var totalAllocation = CalculateTotalAllocation(wallets.Count, fundingRound.AllocationPerUser);
            if (!HasFundsForAllocation(cloud, totalAllocation))
            {
                // Warning because this is a business problem that needs admin attention
                logger.LogWarning(
                    "Funding round (ID {FundingRoundId}): Cloud '{CloudName}' (ID {CloudId}) has insufficient funds for allocation; required {Required}, available {Available}",
                    fundingRound.ID, cloud.Name, cloud.ID, totalAllocation, cloud.TotalFundsAvailable);
                return;
            }

            SetWalletBalances(wallets, fundingRound.AllocationPerUser);
            fundingRound.TotalFundsAllocated = totalAllocation;
            await context.SaveChangesAsync();

            logger.LogInformation(
                "Funding round (ID {FundingRoundId}): Cloud '{CloudName}' (ID {CloudId}) allocation complete; funded {UserCount} wallets with {TotalAllocation} total",
                fundingRound.ID, cloud.Name, cloud.ID, wallets.Count, totalAllocation);
        }

        var activatedDreamCount = await ActivateReadyDreamsAsync(context, cloud.ID);
        await context.SaveChangesAsync();
        logger.LogInformation(
            "Funding round (ID {FundingRoundId}): Cloud '{CloudName}' (ID {CloudId}) activated {DreamCount} ready dreams",
            fundingRound.ID, cloud.Name, cloud.ID, activatedDreamCount);

        logger.LogInformation("Funding round (ID {FundingRoundId}): Finished processing start sequence", fundingRoundId);
    }

    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(300)] // Prevent overlapping executions for the same funding round
    public async Task ProcessFundingRoundEnd(int fundingRoundId)
    {
        logger.LogInformation("Funding round (ID {FundingRoundId}): Started processing end sequence", fundingRoundId);
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var fundingRound = await GetFundingRoundForAllocationAsync(context, fundingRoundId);
        if (fundingRound == null)
        {
            logger.LogWarning("Funding round (ID {FundingRoundId}): Not found. Stopping..", fundingRoundId);
            return;
        }

        // if (fundingRound.EndDate > appTime.UtcNow)
        // {
        //     logger.LogInformation(
        //         "Funding round ID {FundingRoundId} now ends at {EndDate}; skipping an outdated end job",
        //         fundingRoundId,
        //         fundingRound.EndDate);
        //     return;
        // }

        var cloud = fundingRound.Cloud;
        if (ShouldRunEndProcessing(fundingRound, cloud))
        {
            await ClearWalletBalancesAsync(context, fundingRound, cloud);
            var totalSpent = await GetTotalSpentAsync(context, fundingRound.ID);
            UpdateFundingRoundEndTotals(fundingRound, cloud, totalSpent);

            if (fundingRound.IsLastRound)
            {
                var finalizedDreamCounts = await FinalizeActiveDreamsAsync(context, cloud.ID);
                logger.LogInformation(
                    "Funding round (ID {FundingRoundId}): This was the last funding round for Cloud '{CloudName}' (ID {CloudId}) finalized dreams — {CanceledDreamCount} canceled, {PartiallyFundedDreamCount} partially funded, {FullyFundedDreamCount} fully funded",
                    fundingRound.ID,
                    cloud.Name,
                    cloud.ID,
                    finalizedDreamCounts.Canceled,
                    finalizedDreamCounts.PartiallyFunded,
                    finalizedDreamCounts.FullyFunded);
            }

            await context.SaveChangesAsync();
            // Summary log at the end is useful for financial auditing
            logger.LogInformation(
                "Funding round (ID {FundingRoundId}): Cloud '{CloudName}' (ID {CloudId}) end processing complete; spent {TotalSpent}",
                fundingRound.ID, cloud.Name, cloud.ID, totalSpent);
        }

        logger.LogInformation("Funding round (ID {FundingRoundId}): Finished processing end sequence", fundingRoundId);
    }

    private bool ShouldRunAllocation(FundingRound fundingRound, Cloud cloud)
    {
        if (fundingRound.AllocationPerUser <= 0)
        {
            logger.LogWarning(
                "Funding round (ID {FundingRoundId}) allocation per user is zero; skipping allocation",
                fundingRound.ID);
            return false;
        }

        if (fundingRound.TotalFundsAllocated > 0)
        {
            logger.LogWarning(
                "Funding round (ID {FundingRoundId}): funds are already allocated; skipping duplicate allocation",
                fundingRound.ID);
            return false;
        }

        if (fundingRound.ProcessedDateUtc != null)
        {
            logger.LogWarning(
                "Funding round (ID {FundingRoundId}): already processed at {ProcessedDate}; skipping allocation",
                fundingRound.ID, fundingRound.ProcessedDateUtc);
            return false;
        }

        return true;
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

    private static decimal CalculateTotalAllocation(int walletCount, decimal allocationPerUser)
    {
        return walletCount * allocationPerUser;
    }

    private static bool HasFundsForAllocation(Cloud cloud, decimal totalAllocation)
    {
        return cloud.TotalFundsAvailable - cloud.TotalFundsSpent >= totalAllocation;
    }

    private static async Task<int> ActivateReadyDreamsAsync(ApplicationDbContext context, int cloudId)
    {
        var dreams = await context.Dream
            .Where(dream => dream.CloudID == cloudId && dream.Status == DreamStatus.ReadyForFunding)
            .ToListAsync();

        foreach (var dream in dreams)
            dream.Status = DreamStatus.Active;

        return dreams.Count;
    }

    private static void SetWalletBalances(IEnumerable<Wallet> wallets, decimal allocationPerUser)
    {
        foreach (var wallet in wallets)
            wallet.Balance = allocationPerUser;
    }

    private bool ShouldRunEndProcessing(FundingRound fundingRound, Cloud cloud)
    {
        if (fundingRound.ProcessedDateUtc == null)
            return true;

        logger.LogWarning(
            "Funding round (ID {FundingRoundId}): Cloud '{CloudName}' (ID {CloudId}) already processed at {ProcessedDate}; skipping duplicate end processing",
            fundingRound.ID, cloud.Name, cloud.ID, fundingRound.ProcessedDateUtc);
        return false;
    }

    private async Task ClearWalletBalancesAsync(ApplicationDbContext context, FundingRound fundingRound, Cloud cloud)
    {
        var wallets = await GetCloudWalletsAsync(context, fundingRound.CloudID);
        logger.LogInformation(
            "Funding round (ID {FundingRoundId}): Cloud '{CloudName}' (ID {CloudId}) clearing {WalletCount} wallet balances",
            fundingRound.ID, cloud.Name, cloud.ID, wallets.Count);
        SetWalletBalances(wallets, 0);
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
    private static async Task<(int Canceled, int PartiallyFunded, int FullyFunded)> FinalizeActiveDreamsAsync(
        ApplicationDbContext context,
        int cloudId)
    {
        var activeDreams = await context.Dream
            .Include(dream => dream.BudgetItems)
            .Include(dream => dream.Funds)
            .Where(dream => dream.CloudID == cloudId && dream.Status == DreamStatus.Active)
            .ToListAsync();

        var canceledCount = 0;
        var partiallyFundedCount = 0;
        var fullyFundedCount = 0;

        foreach (var dream in activeDreams)
        {
            var totalFunded = dream.Funds.Sum(fund => fund.Amount);
            var minimumGoal = dream.BudgetItems
                .Where(item => item.Type == BudgetItemType.Requesting)
                .Sum(item => item.MinAmount);

            if (totalFunded <= 0)
            {
                dream.Status = DreamStatus.Canceled;
                canceledCount++;
            }
            else if (totalFunded >= minimumGoal)
            {
                dream.Status = DreamStatus.FullyFunded;
                fullyFundedCount++;
            }
            else
            {
                dream.Status = DreamStatus.PartiallyFunded;
                partiallyFundedCount++;
            }
        }

        return (canceledCount, partiallyFundedCount, fullyFundedCount);
    }

    private void UpdateFundingRoundEndTotals(FundingRound fundingRound, Cloud cloud, decimal totalSpent)
    {
        cloud.TotalFundsSpent += totalSpent;
        fundingRound.ProcessedDateUtc = appTime.UtcNow;
    }
}