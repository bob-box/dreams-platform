using System.Text.Json;
using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class FundingRoundOutboxDispatcher(
    ApplicationDbContext context,
    FundingRoundSchedulerService schedulerService,
    IAppTime appTime,
    ILogger<FundingRoundOutboxDispatcher> logger)
{
    public const string SynchronizeEvent = "Synchronize";
    public const string CancelEvent = "Cancel";

    /// <summary>
    /// Adds an outbox message to synchronize the scheduled jobs with the persisted funding round.
    /// </summary>
    /// <param name="context">The database context that will persist the funding round and message together.</param>
    /// <param name="fundingRoundId">The ID of the funding round to synchronize.</param>
    /// <param name="createdAt">The UTC time when the message is enqueued.</param>
    public static void EnqueueSynchronize(ApplicationDbContext context, int fundingRoundId, DateTime createdAt)
    {
        context.FundingRoundOutbox.Add(new FundingRoundOutboxMessage
        {
            FundingRoundID = fundingRoundId,
            EventType = SynchronizeEvent,
            EnqueuedAt = createdAt
        });
    }

    /// <summary>
    /// Adds an outbox message to cancel a deleted funding round's scheduled jobs.
    /// </summary>
    /// <param name="context">The database context that will persist the deletion and message together.</param>
    /// <param name="fundingRound">The funding round whose existing job IDs must be canceled.</param>
    /// <param name="createdAt">The UTC time when the message is enqueued.</param>
    public static void EnqueueCancel(
        ApplicationDbContext context,
        FundingRound fundingRound,
        DateTime createdAt)
    {
        context.FundingRoundOutbox.Add(new FundingRoundOutboxMessage
        {
            FundingRoundID = fundingRound.ID,
            EventType = CancelEvent,
            Payload = JsonSerializer.Serialize(new CancelJobsPayload(
                fundingRound.StartJobId,
                fundingRound.EndJobId)),
            EnqueuedAt = createdAt
        });
    }

    /// <summary>
    /// Dispatches pending funding-round outbox messages to Hangfire and marks them processed.
    /// </summary>
    [AutomaticRetry(Attempts = 5)]
    [DisableConcurrentExecution(300)]
    public async Task DispatchPendingAsync()
    {
        var messages = await context.FundingRoundOutbox
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.ID)
            .Take(100)
            .ToListAsync();

        foreach (var messageGroup in messages.GroupBy(message => message.FundingRoundID))
        {
            var round = await context.FundingRound
                .SingleOrDefaultAsync(fundingRound => fundingRound.ID == messageGroup.Key);

            if (round != null)
            {
                schedulerService.ScheduleFundingRoundJobs(round);
            }
            else
            {
                CancelDeletedRoundJobs(messageGroup);
            }

            foreach (var message in messageGroup)
                message.ProcessedAt = appTime.UtcNow;

            await context.SaveChangesAsync();
            logger.LogInformation(
                "Dispatched {MessageCount} funding-round outbox messages for funding round {FundingRoundId}",
                messageGroup.Count(),
                messageGroup.Key);
        }
    }

    /// <summary>
    /// Cancels the stored Hangfire jobs for a deleted funding round.
    /// </summary>
    /// <param name="messages">Pending outbox messages associated with the deleted round.</param>
    private void CancelDeletedRoundJobs(IEnumerable<FundingRoundOutboxMessage> messages)
    {
        foreach (var message in messages.Where(message => message.EventType == CancelEvent))
        {
            var payload = JsonSerializer.Deserialize<CancelJobsPayload>(message.Payload)
                ?? throw new InvalidOperationException(
                    $"Funding-round outbox message {message.ID} has an invalid cancellation payload.");

            schedulerService.CancelFundingRoundJobs(payload.StartJobId, payload.EndJobId, message.FundingRoundID);
        }
    }

    private sealed record CancelJobsPayload(string? StartJobId, string? EndJobId);
}
