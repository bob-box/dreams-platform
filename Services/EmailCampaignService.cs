using System.Text;
using System.Text.Json;
using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class EmailCampaignService(
    ApplicationDbContext context,
    IPostmarkEmailService emailService,
    IAppTime appTime,
    ILogger<EmailCampaignService> logger
)
{
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
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (addresses.Length == 0)
            throw new InvalidOperationException($"Dream {dreamId} has no creator email addresses.");

        var subject = dream.Cloud.DreamWelcomeEmailSubject.Replace("{DreamName}", dream.Name, StringComparison.Ordinal);
        var body = dream.Cloud.DreamWelcomeEmailBody.Replace("{DreamName}", dream.Name, StringComparison.Ordinal);
        await emailService.SendBatchAsync(
            addresses.Select(address => new OutboundEmail(address, subject, body)).ToArray(),
            isBroadcast: false);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task SendCampaignAsync(int campaignId)
    {
        var claimed = await context.EmailCampaign
            .Where(item => item.ID == campaignId && item.Status == EmailCampaignStatus.Scheduled)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(item => item.Status, EmailCampaignStatus.Sending)
                .SetProperty(item => item.Error, (string?)null));
        if (claimed == 0)
            return;

        var campaign = await context.EmailCampaign.SingleOrDefaultAsync(item => item.ID == campaignId)
            ?? throw new KeyNotFoundException($"Email campaign {campaignId} was not found.");

        try
        {
            var dreamIds = JsonSerializer.Deserialize<List<int>>(campaign.SelectedDreamIds)
                ?? throw new InvalidOperationException("The selected dream list for this campaign is invalid.");
            var dreamsQuery = context.Dream
                .AsNoTracking()
                .Include(dream => dream.Dreamers)
                .ThenInclude(dreamer => dreamer.User)
                .AsQueryable();

            dreamsQuery = campaign.Audience switch
            {
                EmailCampaignAudience.AllDreams =>
                    dreamsQuery.Where(dream => dream.CloudID == campaign.CloudID),
                EmailCampaignAudience.SelectedDreams =>
                    dreamsQuery.Where(dream =>
                        dream.CloudID == campaign.CloudID &&
                        dreamIds.Contains(dream.ID)),
                EmailCampaignAudience.DreamStatus when campaign.DreamStatus.HasValue =>
                    dreamsQuery.Where(dream =>
                        dream.CloudID == campaign.CloudID &&
                        dream.Status == campaign.DreamStatus.Value),
                _ => throw new InvalidOperationException("The campaign audience is invalid.")
            };

            var dreams = await dreamsQuery.ToListAsync();
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
                var body = new StringBuilder(campaign.Body.TrimEnd());
                if (matchingDreams.Length > 0)
                {
                    body.AppendLine().AppendLine().AppendLine("Related dreams:");
                    foreach (var dream in matchingDreams)
                        body.Append("- ").AppendLine(dream.Name);
                }

                return new OutboundEmail(group.First().Email, campaign.Subject, body.ToString());
            }).ToArray();

            await emailService.SendBatchAsync(emails, isBroadcast: true);
            campaign.Status = EmailCampaignStatus.Sent;
            campaign.SentAtUtc = appTime.UtcNow;
            campaign.RecipientCount = emails.Length;
            await context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            campaign.Status = EmailCampaignStatus.Failed;
            campaign.Error = exception.Message;
            await context.SaveChangesAsync();
            logger.LogError(exception, "Sending email campaign {CampaignId} failed.", campaignId);
            throw;
        }
    }
}
