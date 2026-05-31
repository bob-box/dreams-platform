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
            "Scheduler: Added job for FundingRound {ID}. StartJob: {StartJobId} at {StartDate}, EndJob: {EndJobId} at {EndDate}",
            fundingRound.ID, startJobId, fundingRound.StartDate, endJobId, fundingRound.EndDate);
    }

    public void CancelFundingRoundJobs(FundingRound fundingRound)
    {
        if (!string.IsNullOrEmpty(fundingRound.StartJobId))
        {
            logger.LogInformation("Scheduler: FundingRound {ID} - Cancelling existing start job {JobId}",
                fundingRound.ID, fundingRound.StartJobId);
            BackgroundJob.Delete(fundingRound.StartJobId);
        }
        if (!string.IsNullOrEmpty(fundingRound.EndJobId))
        {
            logger.LogInformation("Scheduler: FundingRound {ID} - Cancelling existing end job {JobId}",
                fundingRound.ID, fundingRound.EndJobId);
            BackgroundJob.Delete(fundingRound.EndJobId);
        }
    }

    [DisableConcurrentExecution(300)] // Prevent overlapping executions for the same funding round
    [AutomaticRetry(Attempts = 3)]
    public async Task AllocateFundsToWallets(int fundingRoundId)
    {
        logger.LogInformation("Scheduler: FundingRound {ID} - Starting wallet allocation", fundingRoundId);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var fundingRound = await context.FundingRound
            .Include(fr => fr.Cloud)
            .SingleOrDefaultAsync(fr => fr.ID == fundingRoundId);

        if (fundingRound == null)
        {
            logger.LogWarning("Scheduler: FundingRound {ID} -  not found, skipping allocation", fundingRoundId);
            return;
        }
        if (fundingRound.AllocationPerUser <= 0)
        {
            logger.LogWarning("Scheduler: FundingRound {ID} -  has no allocation per user, skipping", fundingRoundId);
            return;
        }
        if (fundingRound.TotalFundsAllocated > 0)
        {
            logger.LogWarning("Scheduler: FundingRound {ID} - already allocated, skipping (idempotency check)", fundingRoundId);
            return;
        }
        if (fundingRound.ProcessedDate != null)
        {
            logger.LogWarning("Scheduler: FundingRound {ID} -  already processed, skipping", fundingRoundId);
            return;
        }

        var cloud = fundingRound.Cloud;
        var usersInCloud = await context.Wallet
            .Where(w => w.CloudID == cloud.ID)
            .ToListAsync();

        decimal totalAllocation = usersInCloud.Count * fundingRound.AllocationPerUser;
        if (cloud.TotalFundsAvailable < totalAllocation)
        {
            // Warning because this is a business problem that needs admin attention
            logger.LogWarning(
                "Scheduler: FundingRound {ID} - insufficient funds. Required: {Required}, Available: {Available}",
                fundingRoundId, totalAllocation, fundingRound.Cloud.TotalFundsAvailable);
            return;
        }

        var affectedRows = await context.Dream
            .Where(d => d.CloudID == cloud.ID && d.Status == DreamStatus.ReadyForFunding)
            .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.Status, DreamStatus.Active));
        logger.LogInformation("Scheduler: FundingRound {ID} - Number of dreams updated from ReadyForFunding to Active: {affectedRows}.",
        fundingRoundId, affectedRows);

        foreach (var wallet in usersInCloud)
            wallet.Balance = fundingRound.AllocationPerUser;

        fundingRound.TotalFundsAllocated = totalAllocation;
        await context.SaveChangesAsync();

        logger.LogInformation(
        "Scheduler: FundingRound {ID} - allocation complete. {UserCount} wallets given funds, total allocated: {TotalAllocation}",
        fundingRoundId, usersInCloud.Count, totalAllocation);
    }

    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(300)] // Prevent overlapping executions for the same funding round
    public async Task ProcessFundingRoundEnd(int fundingRoundId)
    {
        logger.LogInformation("Scheduler: FundingRound {ID} - Starting end processing", fundingRoundId);
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var fundingRound = await context.FundingRound.FindAsync(fundingRoundId);
        if (fundingRound == null)
        {
            logger.LogWarning("Scheduler: FundingRound {ID} - not found, skipping end processing", fundingRoundId);
            return;
        }
        if (fundingRound.ProcessedDate != null)
        {
            logger.LogWarning(
                "Scheduler: FundingRound {ID} - already processed on {ProcessedDate}, skipping (idempotency check)",
                fundingRoundId, fundingRound.ProcessedDate);
            return;
        }
        var cloud = await context.Cloud.FindAsync(fundingRound.CloudID);
        if (cloud == null)
        {
            logger.LogError(
                "Scheduler: FundingRound {ID} - Cloud {CloudID} not found during end processing",
                fundingRoundId, fundingRound.CloudID);
            return;
        }

        var wallets = await context.Wallet
            .Where(w => w.CloudID == fundingRound.CloudID)
            .ToListAsync();

        // Remove remaining funds
        logger.LogInformation(
            "Scheduler: FundingRound {ID} - zeroing balances for {WalletCount} wallets",
            fundingRoundId, wallets.Count);
        foreach (var wallet in wallets)
            wallet.Balance = 0;

        var totalSpent = await context.Fund
            .Where(f => f.FundingRoundID == fundingRound.ID)
            .SumAsync(f => f.Amount);

        cloud.TotalFundsSpent += totalSpent;
        cloud.TotalFundsRemaining = cloud.TotalFundsAvailable - cloud.TotalFundsSpent;
        fundingRound.ProcessedDate = appTime.UtcNow;

        await context.SaveChangesAsync();
        // Summary log at the end is useful for financial auditing
        logger.LogInformation(
            "Scheduler: FundingRound {ID} - end processing complete. TotalSpent: {TotalSpent}, CloudID: {CloudID}, remaining funds: {Remaining}",
            fundingRoundId, totalSpent, cloud.ID, cloud.TotalFundsRemaining);
    }
}