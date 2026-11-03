using System.Text;
using System.Text.Json;
using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class EmailSendingService(
    ApplicationDbContext context,
    IResendEmailService emailService,
    IAppTime appTime,
    ILogger<EmailSendingService> logger
)
{
    /// <summary>
    /// Sends the configured welcome message to the creators of a dream.
    /// </summary>
    /// <param name="dreamId">The dream whose creators should receive the message.</param>
    [AutomaticRetry(Attempts = 0)]
    public async Task SendDreamWelcomeAsync(int dreamId)
    {
        var dream = await context.Dream
            .AsNoTracking()
            .Include(dream => dream.Dreamers)
            .ThenInclude(dreamer => dreamer.User)
            .Include(dream => dream.Cloud)
            .SingleOrDefaultAsync(dream => dream.ID == dreamId);
        if (dream == null)
            throw new KeyNotFoundException($"Dream {dreamId} was not found for its welcome email.");

        if (!dream.Cloud.SendDreamWelcomeEmail)
            return;

        var addresses = dream.Dreamers
            .Select(dreamer => dreamer.User.Email)
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(address => address!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase) // distinct may not be needed?
            .ToArray();

        if (addresses.Length == 0)
            throw new InvalidOperationException($"Dream {dreamId} has no creator email addresses.");

        var subject = dream.Cloud.DreamWelcomeEmailSubject.Replace("{DreamName}", dream.Name, StringComparison.Ordinal);
        var body = dream.Cloud.DreamWelcomeEmailBody.Replace("{DreamName}", dream.Name, StringComparison.Ordinal);
        await emailService.SendEmailInBatchesAsync(
            addresses.Select(address => new OutboundEmail(address, subject, body)).ToArray(),
            "dream-welcome");
    }

    /// <summary>
    /// Sends a message to the dream creators selected by its audience settings.
    /// </summary>
    /// <param name="emailId">The email message to send.</param>
    [AutomaticRetry(Attempts = 0)]
    public async Task SendEmailAsync(int emailId)
    {
        // Claim the update atomically so concurrent Hangfire executions cannot send it twice.
        var claimed = await context.Email
            .Where(item => item.ID == emailId && item.Status == EmailStatus.Scheduled)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(item => item.Status, EmailStatus.Sending)
                .SetProperty(item => item.Error, (string?)null));
        if (claimed == 0)
            return;

        var update = await context.Email.SingleOrDefaultAsync(item => item.ID == emailId)
            ?? throw new KeyNotFoundException($"Email update {emailId} was not found.");

        try
        {
            var dreamIds = JsonSerializer.Deserialize<List<int>>(update.SelectedDreamIds)
                ?? throw new InvalidOperationException("The selected dream list for this update is invalid.");
            var dreamsQuery = context.Dream
                .AsNoTracking()
                .Include(dream => dream.Dreamers)
                .ThenInclude(dreamer => dreamer.User)
                .AsQueryable();

            // Apply the stored audience criteria within the update's cloud.
            dreamsQuery = update.Audience switch
            {
                EmailAudience.AllDreams =>
                    dreamsQuery.Where(dream => dream.CloudID == update.CloudID),
                EmailAudience.SelectedDreams =>
                    dreamsQuery.Where(dream =>
                        dream.CloudID == update.CloudID &&
                        dreamIds.Contains(dream.ID)),
                EmailAudience.DreamStatus when update.DreamStatus.HasValue =>
                    dreamsQuery.Where(dream =>
                        dream.CloudID == update.CloudID &&
                        dream.Status == update.DreamStatus.Value),
                _ => throw new InvalidOperationException("The update audience is invalid.")
            };

            var dreams = await dreamsQuery.ToListAsync();
            // Deduplicate recipients across dreams while retaining each recipient's related dream names.
            var recipientDreams = dreams
                .SelectMany(dream => dream.Dreamers
                    .Where(dreamer => !string.IsNullOrWhiteSpace(dreamer.User.Email))
                    .Select(dreamer => new { Dream = dream, Email = dreamer.User.Email!.Trim() }))
                .GroupBy(item => item.Email, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (recipientDreams.Length == 0)
                throw new InvalidOperationException("There are no email recipients in the selected audience.");

            var emails = recipientDreams.Select(group =>
            {
                var matchingDreams = group.Select(item => item.Dream).DistinctBy(dream => dream.ID).ToArray();
                var body = new StringBuilder(update.Body.TrimEnd());
                if (matchingDreams.Length > 0)
                {
                    body.AppendLine().AppendLine().AppendLine("Related dreams:");
                    foreach (var dream in matchingDreams)
                        body.Append("- ").AppendLine(dream.Name);
                }

                return new OutboundEmail(group.First().Email, update.Subject, body.ToString());
            }).ToArray();

            await emailService.SendEmailInBatchesAsync(emails, "on-demand");
            update.Status = EmailStatus.Sent;
            update.SentAtUtc = appTime.UtcNow;
            update.RecipientCount = emails.Length;
            await context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            // Persist the failure for the update history, then rethrow so Hangfire records the job failure.
            update.Status = EmailStatus.Failed;
            update.Error = exception.Message;
            await context.SaveChangesAsync();
            logger.LogError(exception, "Sending email update {EmailId} failed.", emailId);
            throw;
        }
    }
}
