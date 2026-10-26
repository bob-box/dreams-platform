using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class FundingRoundSchedulerService(
    ILogger<FundingRoundSchedulerService> logger,
    IServiceProvider serviceProvider,
    IAppTime appTime
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
        var startJobId = BackgroundJob.Schedule(
            () => ProcessFundingRoundStart(fundingRound.ID),
            fundingRound.StartDate);

        // Schedule end job at end date
        var endJobId = BackgroundJob.Schedule(
            () => ProcessFundingRoundEnd(fundingRound.ID),
            fundingRound.EndDate);

        // Update the funding round with job IDs
        fundingRound.StartJobId = startJobId;
        fundingRound.EndJobId = endJobId;

        logger.LogInformation(
            "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}, cloud ID {CloudId}): scheduled allocation job {StartJobId} for {StartDate} and end job {EndJobId} for {EndDate}",
            fundingRound.Title, fundingRound.ID, fundingRound.CloudID, startJobId, fundingRound.StartDate, endJobId, fundingRound.EndDate);
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
            fundingRound.Title,
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
        CancelFundingRoundJobs(startJobId, endJobId, fundingRoundId, null, null);
    }

    private void CancelFundingRoundJobs(
        string? startJobId,
        string? endJobId,
        int fundingRoundId,
        string? fundingRoundTitle,
        int? cloudId)
    {
        if (!string.IsNullOrEmpty(startJobId))
        {
            logger.LogInformation(
                "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}, cloud ID {CloudId}): cancelling allocation job {JobId}",
                fundingRoundTitle, fundingRoundId, cloudId, startJobId);
            BackgroundJob.Delete(startJobId);
        }
        if (!string.IsNullOrEmpty(endJobId))
        {
            logger.LogInformation(
                "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}, cloud ID {CloudId}): cancelling end job {JobId}",
                fundingRoundTitle, fundingRoundId, cloudId, endJobId);
            BackgroundJob.Delete(endJobId);
        }
    }

    [DisableConcurrentExecution(300)] // Prevent overlapping executions for the same funding round
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessFundingRoundStart(int fundingRoundId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fundingRound = await GetFundingRoundForAllocationAsync(context, fundingRoundId);

        if (fundingRound == null)
        {
            logger.LogWarning("Funding round ID {FundingRoundId} was not found; skipping allocation", fundingRoundId);
            return;
        }

        if (fundingRound.StartDate > appTime.UtcNow)
        {
            logger.LogInformation(
                "Funding round ID {FundingRoundId} now starts at {StartDate}; skipping an outdated allocation job",
                fundingRoundId,
                fundingRound.StartDate);
            return;
        }

        var cloud = fundingRound.Cloud;
        logger.LogInformation(
            "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): starting wallet allocation",
            fundingRound.Title, fundingRound.ID, cloud.Name, cloud.ID);

        if (ShouldSkipAllocation(fundingRound, cloud))
            return;

        var wallets = await GetCloudWalletsAsync(context, cloud.ID);
        var totalAllocation = CalculateTotalAllocation(wallets.Count, fundingRound.AllocationPerUser);
        if (!HasFundsForAllocation(cloud, totalAllocation))
        {
            // Warning because this is a business problem that needs admin attention
            logger.LogWarning(
                "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): insufficient funds for allocation; required {Required}, available {Available}",
                fundingRound.Title, fundingRound.ID, cloud.Name, cloud.ID, totalAllocation, cloud.TotalFundsAvailable);
            return;
        }

        var activatedDreamCount = await ActivateReadyDreamsAsync(context, cloud.ID);
        logger.LogInformation(
            "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): activated {DreamCount} ready dreams",
            fundingRound.Title, fundingRound.ID, cloud.Name, cloud.ID, activatedDreamCount);

        SetWalletBalances(wallets, fundingRound.AllocationPerUser);
        fundingRound.TotalFundsAllocated = totalAllocation;
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): allocation complete; funded {UserCount} wallets with {TotalAllocation} total",
            fundingRound.Title, fundingRound.ID, cloud.Name, cloud.ID, wallets.Count, totalAllocation);
    }

    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(300)] // Prevent overlapping executions for the same funding round
    public async Task ProcessFundingRoundEnd(int fundingRoundId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var fundingRound = await context.FundingRound.FindAsync(fundingRoundId);
        if (fundingRound == null)
        {
            logger.LogWarning("Funding round ID {FundingRoundId} was not found; skipping end processing", fundingRoundId);
            return;
        }

        if (fundingRound.EndDate > appTime.UtcNow)
        {
            logger.LogInformation(
                "Funding round ID {FundingRoundId} now ends at {EndDate}; skipping an outdated end job",
                fundingRoundId,
                fundingRound.EndDate);
            return;
        }

        var cloud = await context.Cloud.FindAsync(fundingRound.CloudID);
        if (cloud == null)
        {
            logger.LogError(
                "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}): cloud ID {CloudId} was not found during end processing",
                fundingRound.Title, fundingRound.ID, fundingRound.CloudID);
            return;
        }

        logger.LogInformation(
            "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): starting end processing",
            fundingRound.Title, fundingRound.ID, cloud.Name, cloud.ID);

        if (ShouldSkipEndProcessing(fundingRound, cloud))
            return;

        await ClearWalletBalancesAsync(context, fundingRound, cloud);
        var totalSpent = await GetTotalSpentAsync(context, fundingRound.ID);
        UpdateFundingRoundEndTotals(fundingRound, cloud, totalSpent);

        if (fundingRound.IsLastRound)
        {
            var finalizedDreamCounts = await FinalizeActiveDreamsAsync(context, cloud.ID);
            logger.LogInformation(
                "Last funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): finalized dreams — {CanceledDreamCount} canceled, {PartiallyFundedDreamCount} partially funded, {FullyFundedDreamCount} fully funded",
                fundingRound.Title,
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
            "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): end processing complete; spent {TotalSpent}, remaining funds {Remaining}",
            fundingRound.Title, fundingRound.ID, cloud.Name, cloud.ID, totalSpent, cloud.TotalFundsRemaining);
    }

    private static Task<FundingRound?> GetFundingRoundForAllocationAsync(ApplicationDbContext context, int fundingRoundId)
    {
        return context.FundingRound
            .Include(round => round.Cloud)
            .SingleOrDefaultAsync(round => round.ID == fundingRoundId);
    }

    private bool ShouldSkipAllocation(FundingRound fundingRound, Cloud cloud)
    {
        if (fundingRound.AllocationPerUser <= 0)
        {
            logger.LogWarning(
                "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): allocation per user is zero; skipping allocation",
                fundingRound.Title, fundingRound.ID, cloud.Name, cloud.ID);
            return true;
        }

        if (fundingRound.TotalFundsAllocated > 0)
        {
            logger.LogWarning(
                "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): funds are already allocated; skipping duplicate allocation",
                fundingRound.Title, fundingRound.ID, cloud.Name, cloud.ID);
            return true;
        }

        if (fundingRound.ProcessedDate != null)
        {
            logger.LogWarning(
                "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): already processed at {ProcessedDate}; skipping allocation",
                fundingRound.Title, fundingRound.ID, cloud.Name, cloud.ID, fundingRound.ProcessedDate);
            return true;
        }

        return false;
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
        return cloud.TotalFundsAvailable >= totalAllocation;
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

    private bool ShouldSkipEndProcessing(FundingRound fundingRound, Cloud cloud)
    {
        if (fundingRound.ProcessedDate == null)
            return false;

        logger.LogWarning(
            "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): already processed at {ProcessedDate}; skipping duplicate end processing",
            fundingRound.Title, fundingRound.ID, cloud.Name, cloud.ID, fundingRound.ProcessedDate);
        return true;
    }

    private async Task ClearWalletBalancesAsync(
        ApplicationDbContext context,
        FundingRound fundingRound,
        Cloud cloud)
    {
        var wallets = await GetCloudWalletsAsync(context, fundingRound.CloudID);
        logger.LogInformation(
            "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}) for cloud '{CloudName}' (ID {CloudId}): clearing {WalletCount} wallet balances",
            fundingRound.Title, fundingRound.ID, cloud.Name, cloud.ID, wallets.Count);
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
        cloud.TotalFundsRemaining = cloud.TotalFundsAvailable - cloud.TotalFundsSpent;
        fundingRound.ProcessedDate = appTime.UtcNow;
    }
}