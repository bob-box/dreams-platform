using System.Text.Json;
using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class EmailCampaignManagementService(
    ApplicationDbContext context,
    IConfiguration configuration,
    IMapperFacade mapper,
    IAppTime appTime,
    ICloudNameResolver cloudNameResolver,
    IBackgroundJobClient backgroundJobs,
    ILogger<EmailCampaignManagementService> logger
) : IEmailCampaignManagementService
{
    public async Task<EmailSettingsViewModel> GetSettingsAsync()
    {
        var settings = await context.EmailSettings.SingleOrDefaultAsync(item => item.ID == 1)
            ?? new EmailSettings();

        return new EmailSettingsViewModel
        {
            FromAddress = settings.FromAddress,
            FromName = settings.FromName,
            IsApiKeyConfigured = !string.IsNullOrWhiteSpace(configuration["Resend:ApiKey"])
        };
    }

    public async Task SaveSettingsAsync(EmailSettingsViewModel model)
    {
        var settings = await context.EmailSettings.SingleOrDefaultAsync(item => item.ID == 1);
        if (settings == null)
        {
            settings = new EmailSettings();
            context.EmailSettings.Add(settings);
        }

        settings.FromAddress = model.FromAddress.Trim();
        settings.FromName = model.FromName?.Trim() ?? string.Empty;
        await context.SaveChangesAsync();
    }

    public async Task<List<EmailCampaignListItemViewModel>> GetCampaignsAsync()
    {
        EnsureCloudContext();
        var campaigns = await context.EmailCampaign
            .Where(campaign => campaign.CloudID == cloudNameResolver.CloudID)
            .OrderByDescending(campaign => campaign.CreatedAtUtc)
            .Take(100)
            .ToListAsync();
        return mapper.ToViewModels(campaigns);
    }

    public async Task<EmailCampaignViewModel> BuildCreateViewModelAsync()
    {
        EnsureCloudContext();
        var model = new EmailCampaignViewModel();
        await PopulateCampaignOptionsAsync(model);
        return model;
    }

    public async Task<EmailCampaignManagementResult> CreateCampaignAsync(EmailCampaignViewModel model)
    {
        EnsureCloudContext();
        var errors = await ValidateCampaignAsync(model);
        var scheduledAtUtc = ValidateScheduledAt(model.ScheduledAt, required: false, errors);
        if (errors.Count > 0)
        {
            await PopulateCampaignOptionsAsync(model);
            return ValidationFailure(model, errors);
        }

        var campaign = new EmailCampaign
        {
            CloudID = cloudNameResolver.CloudID,
            Subject = model.Subject.Trim(),
            Body = model.Body.Trim(),
            Audience = model.Audience,
            DreamStatus = model.Audience == EmailCampaignAudience.DreamStatus ? model.DreamStatus : null,
            SelectedDreamIds = SerializeSelectedDreamIds(model),
            ScheduledAtUtc = scheduledAtUtc,
            CreatedAtUtc = appTime.UtcNow,
            Status = EmailCampaignStatus.Scheduled
        };
        context.EmailCampaign.Add(campaign);
        await context.SaveChangesAsync();

        try
        {
            campaign.HangfireJobId = scheduledAtUtc.HasValue
                ? BackgroundJob.Schedule<EmailCampaignService>(
                    service => service.SendCampaignAsync(campaign.ID),
                    scheduledAtUtc.Value - appTime.UtcNow)
                : backgroundJobs.Enqueue<EmailCampaignService>(
                    service => service.SendCampaignAsync(campaign.ID));
            await context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            campaign.Status = EmailCampaignStatus.Failed;
            campaign.Error = exception.Message;
            await context.SaveChangesAsync();
            logger.LogError(exception, "Could not queue email campaign {CampaignId}.", campaign.ID);
            return new EmailCampaignManagementResult(
                EmailCampaignManagementStatus.Error,
                Message: "The email campaign could not be queued. See the server logs for details.");
        }

        return new EmailCampaignManagementResult(EmailCampaignManagementStatus.Success);
    }

    public async Task<EmailCampaignManagementResult> GetForEditAsync(int id)
    {
        EnsureCloudContext();
        var campaign = await GetCampaignAsync(id);
        if (campaign == null)
            return new EmailCampaignManagementResult(EmailCampaignManagementStatus.NotFound);
        if (!IsEditable(campaign))
            return new EmailCampaignManagementResult(
                EmailCampaignManagementStatus.Conflict,
                Message: "Only campaigns scheduled for a future time can be edited.");

        var model = new EmailCampaignViewModel
        {
            Subject = campaign.Subject,
            Body = campaign.Body,
            Audience = campaign.Audience,
            DreamStatus = campaign.DreamStatus,
            ScheduledAt = campaign.ScheduledAtUtc.HasValue
                ? appTime.ToLocal(campaign.ScheduledAtUtc.Value)
                : null,
            SelectedDreamIds = JsonSerializer.Deserialize<List<int>>(campaign.SelectedDreamIds) ?? []
        };
        await PopulateCampaignOptionsAsync(model);
        return new EmailCampaignManagementResult(EmailCampaignManagementStatus.Success, model);
    }

    public async Task<EmailCampaignManagementResult> UpdateCampaignAsync(int id, EmailCampaignViewModel model)
    {
        EnsureCloudContext();
        var campaign = await GetCampaignAsync(id);
        if (campaign == null)
            return new EmailCampaignManagementResult(EmailCampaignManagementStatus.NotFound);
        if (!IsEditable(campaign))
            return new EmailCampaignManagementResult(
                EmailCampaignManagementStatus.Conflict,
                Message: "Only campaigns scheduled for a future time can be edited.");

        var errors = await ValidateCampaignAsync(model);
        var scheduledAtUtc = ValidateScheduledAt(model.ScheduledAt, required: true, errors);
        if (errors.Count > 0)
        {
            await PopulateCampaignOptionsAsync(model);
            return ValidationFailure(model, errors);
        }

        var claimed = await context.EmailCampaign
            .Where(item =>
                item.ID == id &&
                item.CloudID == cloudNameResolver.CloudID &&
                item.Status == EmailCampaignStatus.Scheduled &&
                item.ScheduledAtUtc > appTime.UtcNow)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(item => item.Status, EmailCampaignStatus.Updating));
        if (claimed == 0)
            return new EmailCampaignManagementResult(
                EmailCampaignManagementStatus.Conflict,
                Message: "This campaign has started sending and can no longer be edited.");

        var previousJobId = campaign.HangfireJobId;
        var previousScheduledAtUtc = campaign.ScheduledAtUtc!.Value;
        string? replacementJobId = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(previousJobId))
                BackgroundJob.Delete(previousJobId);

            replacementJobId = BackgroundJob.Schedule<EmailCampaignService>(
                service => service.SendCampaignAsync(id),
                scheduledAtUtc!.Value - appTime.UtcNow);

            campaign.Subject = model.Subject.Trim();
            campaign.Body = model.Body.Trim();
            campaign.Audience = model.Audience;
            campaign.DreamStatus = model.Audience == EmailCampaignAudience.DreamStatus
                ? model.DreamStatus
                : null;
            campaign.SelectedDreamIds = SerializeSelectedDreamIds(model);
            campaign.ScheduledAtUtc = scheduledAtUtc;
            campaign.HangfireJobId = replacementJobId;
            campaign.Status = EmailCampaignStatus.Scheduled;
            campaign.Error = null;
            await context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            if (!string.IsNullOrWhiteSpace(replacementJobId))
                BackgroundJob.Delete(replacementJobId);

            campaign.Status = EmailCampaignStatus.Scheduled;
            campaign.HangfireJobId = BackgroundJob.Schedule<EmailCampaignService>(
                service => service.SendCampaignAsync(id),
                previousScheduledAtUtc - appTime.UtcNow);
            await context.SaveChangesAsync();

            logger.LogError(exception, "Could not update email campaign {CampaignId}.", id);
            return new EmailCampaignManagementResult(
                EmailCampaignManagementStatus.Error,
                Message: "The campaign could not be updated. Its previous scheduled time has been restored.");
        }

        return new EmailCampaignManagementResult(EmailCampaignManagementStatus.Success);
    }

    public async Task<EmailCampaignManagementResult> CancelCampaignAsync(int id)
    {
        EnsureCloudContext();
        var claimed = await context.EmailCampaign
            .Where(item =>
                item.ID == id &&
                item.CloudID == cloudNameResolver.CloudID &&
                item.Status == EmailCampaignStatus.Scheduled &&
                item.ScheduledAtUtc > appTime.UtcNow)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(item => item.Status, EmailCampaignStatus.Cancelled));
        if (claimed == 0)
            return new EmailCampaignManagementResult(
                EmailCampaignManagementStatus.Conflict,
                Message: "This campaign has started sending or is no longer scheduled, so it could not be cancelled.");

        var campaign = await GetCampaignAsync(id)
            ?? throw new InvalidOperationException($"Cancelled email campaign {id} could not be reloaded.");
        try
        {
            if (!string.IsNullOrWhiteSpace(campaign.HangfireJobId))
                BackgroundJob.Delete(campaign.HangfireJobId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not remove Hangfire job for cancelled email campaign {CampaignId}.", id);
        }

        return new EmailCampaignManagementResult(EmailCampaignManagementStatus.Success);
    }

    public async Task PopulateCampaignOptionsAsync(EmailCampaignViewModel model)
    {
        EnsureCloudContext();
        model.AvailableDreams = await context.Dream
            .AsNoTracking()
            .Where(dream => dream.CloudID == cloudNameResolver.CloudID)
            .OrderBy(dream => dream.Name)
            .ToListAsync();
    }

    private async Task<Dictionary<string, List<string>>> ValidateCampaignAsync(
        EmailCampaignViewModel model)
    {
        var errors = new Dictionary<string, List<string>>();
        if (!Enum.IsDefined(model.Audience))
            AddError(errors, nameof(model.Audience), "Choose a valid audience.");

        if (model.DreamStatus.HasValue && !Enum.IsDefined(model.DreamStatus.Value))
            AddError(errors, nameof(model.DreamStatus), "Choose a valid dream status.");

        if (model.Audience == EmailCampaignAudience.SelectedDreams)
        {
            if (model.SelectedDreamIds.Count == 0)
                AddError(errors, nameof(model.SelectedDreamIds), "Select at least one dream.");
            else
            {
                var matchingDreams = await context.Dream.CountAsync(dream =>
                    dream.CloudID == cloudNameResolver.CloudID &&
                    model.SelectedDreamIds.Contains(dream.ID));
                if (matchingDreams != model.SelectedDreamIds.Distinct().Count())
                    AddError(errors, nameof(model.SelectedDreamIds), "One or more selected dreams are unavailable.");
            }
        }

        if (model.Audience == EmailCampaignAudience.DreamStatus && !model.DreamStatus.HasValue)
            AddError(errors, nameof(model.DreamStatus), "Choose a dream status.");

        return errors;
    }

    private DateTime? ValidateScheduledAt(
        DateTime? scheduledAt,
        bool required,
        Dictionary<string, List<string>> errors)
    {
        if (!scheduledAt.HasValue)
        {
            if (required)
                AddError(errors, nameof(EmailCampaignViewModel.ScheduledAt), "A future send time is required.");
            return null;
        }

        try
        {
            var scheduledAtUtc = appTime.ToUtc(scheduledAt.Value);
            if (scheduledAtUtc <= appTime.UtcNow)
                AddError(errors, nameof(EmailCampaignViewModel.ScheduledAt), "Choose a future time.");
            return scheduledAtUtc;
        }
        catch (ArgumentException)
        {
            AddError(errors, nameof(EmailCampaignViewModel.ScheduledAt), "The selected local time is invalid.");
            return null;
        }
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
            errors[key] = messages = [];
        messages.Add(message);
    }

    private static EmailCampaignManagementResult ValidationFailure(
        EmailCampaignViewModel model,
        Dictionary<string, List<string>> errors) =>
        new(EmailCampaignManagementStatus.ValidationError, model, errors);

    private static string SerializeSelectedDreamIds(EmailCampaignViewModel model) =>
        JsonSerializer.Serialize(
            model.Audience == EmailCampaignAudience.SelectedDreams
                ? model.SelectedDreamIds.Distinct().ToArray()
                : Array.Empty<int>());

    private Task<EmailCampaign?> GetCampaignAsync(int id) =>
        context.EmailCampaign.SingleOrDefaultAsync(item =>
            item.ID == id &&
            item.CloudID == cloudNameResolver.CloudID);

    private bool IsEditable(EmailCampaign campaign) =>
        campaign.Status == EmailCampaignStatus.Scheduled &&
        campaign.ScheduledAtUtc > appTime.UtcNow;

    private void EnsureCloudContext()
    {
        if (cloudNameResolver.CloudID <= 0)
            throw new InvalidOperationException("Email campaigns require an active cloud.");
    }
}
