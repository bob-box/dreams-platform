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
    public void ScheduleFundingRoundJobs(FundingRound fundingRound)
    {
        // Cancel existing jobs if any
        CancelFundingRoundJobs(fundingRound);

        // Schedule allocation job at start date
        var startJobId = BackgroundJob.Schedule(
            () => AllocateFundsToWallets(fundingRound.ID),
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

    public void CancelFundingRoundJobs(FundingRound fundingRound)
    {
        if (!string.IsNullOrEmpty(fundingRound.StartJobId))
        {
            logger.LogInformation(
                "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}, cloud ID {CloudId}): cancelling allocation job {JobId}",
                fundingRound.Title, fundingRound.ID, fundingRound.CloudID, fundingRound.StartJobId);
            BackgroundJob.Delete(fundingRound.StartJobId);
        }
        if (!string.IsNullOrEmpty(fundingRound.EndJobId))
        {
            logger.LogInformation(
                "Funding round '{FundingRoundTitle}' (ID {FundingRoundId}, cloud ID {CloudId}): cancelling end job {JobId}",
                fundingRound.Title, fundingRound.ID, fundingRound.CloudID, fundingRound.EndJobId);
            BackgroundJob.Delete(fundingRound.EndJobId);
        }
    }

    [DisableConcurrentExecution(300)] // Prevent overlapping executions for the same funding round
    [AutomaticRetry(Attempts = 3)]
    public async Task AllocateFundsToWallets(int fundingRoundId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fundingRound = await GetFundingRoundForAllocationAsync(context, fundingRoundId);

        if (fundingRound == null)
        {
            logger.LogWarning("Funding round ID {FundingRoundId} was not found; skipping allocation", fundingRoundId);
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

    private static Task<int> ActivateReadyDreamsAsync(ApplicationDbContext context, int cloudId)
    {
        return context.Dream
            .Where(dream => dream.CloudID == cloudId && dream.Status == DreamStatus.ReadyForFunding)
            .ExecuteUpdateAsync(setters => setters.SetProperty(dream => dream.Status, DreamStatus.Active));
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

    private void UpdateFundingRoundEndTotals(FundingRound fundingRound, Cloud cloud, decimal totalSpent)
    {
        cloud.TotalFundsSpent += totalSpent;
        cloud.TotalFundsRemaining = cloud.TotalFundsAvailable - cloud.TotalFundsSpent;
        fundingRound.ProcessedDate = appTime.UtcNow;
    }
}