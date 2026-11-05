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

public class EmailManagementService(
    ApplicationDbContext context,
    IConfiguration configuration,
    IMapperFacade mapper,
    IAppTime appTime,
    ICloudNameResolver cloudNameResolver,
    IBackgroundJobClient backgroundJobs,
    ILogger<EmailManagementService> logger
) : IEmailManagementService
{
    /// <summary>
    /// Gets the configured sender details and whether a Resend API key is available.
    /// </summary>
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

    /// <summary>
    /// Saves the sender details used for outbound email.
    /// </summary>
    /// <param name="model">The sender settings to save.</param>
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

    /// <summary>
    /// Gets the most recent email updates for the active cloud.
    /// </summary>
    /// <returns>Up to 100 email updates, newest first.</returns>
    public async Task<List<EmailListItemViewModel>> GetEmailsAsync()
    {
        EnsureCloudContext();
        var updates = await context.Email
            .Where(update => update.CloudID == cloudNameResolver.CloudID)
            .OrderByDescending(update => update.CreatedAtUtc)
            .Take(100)
            .ToListAsync();
        return mapper.ToViewModels(updates);
    }

    /// <summary>
    /// Creates an empty email-update form model and loads its cloud-specific options.
    /// </summary>
    /// <returns>The initialized form model.</returns>
    public async Task<EmailViewModel> BuildCreateViewModelAsync()
    {
        EnsureCloudContext();
        var model = new EmailViewModel();
        await PopulateEmailOptionsAsync(model);
        return model;
    }

    /// <summary>
    /// Validates and creates an email update, then queues it for immediate or scheduled delivery.
    /// </summary>
    /// <param name="model">The update content, audience, and optional send time.</param>
    /// <returns>The result of validation, persistence, and job scheduling.</returns>
    public async Task<EmailManagementResult> CreateEmailAsync(EmailViewModel model)
    {
        EnsureCloudContext();
        var errors = await ValidateEmailAsync(model);
        var scheduledAtUtc = ValidateScheduledAt(model.ScheduledAt, required: false, errors);
        if (errors.Count > 0)
        {
            await PopulateEmailOptionsAsync(model);
            return ValidationFailure(model, errors);
        }

        var update = new Email
        {
            CloudID = cloudNameResolver.CloudID,
            Subject = model.Subject.Trim(),
            Body = model.Body.Trim(),
            Audience = model.Audience,
            DreamStatus = model.Audience == EmailAudience.DreamStatus ? model.DreamStatus : null,
            SelectedDreamIds = SerializeSelectedDreamIds(model),
            ScheduledAtUtc = scheduledAtUtc,
            Status = EmailStatus.Scheduled
        };
        context.Email.Add(update);
        await context.SaveChangesAsync();

        try
        {
            // Persist first so the queued job can reference the generated update ID.
            update.HangfireJobId = scheduledAtUtc.HasValue
                ? BackgroundJob.Schedule<EmailSendingService>(
                    service => service.SendEmailAsync(update.ID),
                    scheduledAtUtc.Value - appTime.UtcNow)
                : backgroundJobs.Enqueue<EmailSendingService>(
                    service => service.SendEmailAsync(update.ID));
            await context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            update.Status = EmailStatus.Failed;
            update.Error = exception.Message;
            await context.SaveChangesAsync();
            logger.LogError(exception, "Could not queue email update {EmailId}.", update.ID);
            return new EmailManagementResult(
                EmailManagementStatus.Error,
                Message: "The email update could not be queued. See the server logs for details.");
        }

        return new EmailManagementResult(EmailManagementStatus.Success);
    }

    /// <summary>
    /// Loads an editable email update and maps its scheduled time to application-local time.
    /// </summary>
    /// <param name="id">The update ID.</param>
    /// <returns>The edit model, or a not-found or conflict result.</returns>
    public async Task<EmailManagementResult> GetForEditAsync(int id)
    {
        EnsureCloudContext();
        var update = await GetEmailAsync(id);
        if (update == null)
            return new EmailManagementResult(EmailManagementStatus.NotFound);
        if (!IsEditable(update))
            return new EmailManagementResult(
                EmailManagementStatus.Conflict,
                Message: "Only updates scheduled for a future time can be edited.");

        var model = new EmailViewModel
        {
            Subject = update.Subject,
            Body = update.Body,
            Audience = update.Audience,
            DreamStatus = update.DreamStatus,
            ScheduledAt = update.ScheduledAtUtc.HasValue
                ? appTime.ToLocal(update.ScheduledAtUtc.Value)
                : null,
            SelectedDreamIds = JsonSerializer.Deserialize<List<int>>(update.SelectedDreamIds) ?? []
        };
        await PopulateEmailOptionsAsync(model);
        return new EmailManagementResult(EmailManagementStatus.Success, model);
    }

    /// <summary>
    /// Updates a future-scheduled email and replaces its scheduled delivery job.
    /// </summary>
    /// <param name="id">The update ID.</param>
    /// <param name="model">The new update content, audience, and send time.</param>
    /// <returns>The result of validation and update processing.</returns>
    public async Task<EmailManagementResult> UpdateEmailAsync(int id, EmailViewModel model)
    {
        EnsureCloudContext();
        var update = await GetEmailAsync(id);
        if (update == null)
            return new EmailManagementResult(EmailManagementStatus.NotFound);
        if (!IsEditable(update))
            return new EmailManagementResult(
                EmailManagementStatus.Conflict,
                Message: "Only updates scheduled for a future time can be edited.");

        var errors = await ValidateEmailAsync(model);
        var scheduledAtUtc = ValidateScheduledAt(model.ScheduledAt, required: true, errors);
        if (errors.Count > 0)
        {
            await PopulateEmailOptionsAsync(model);
            return ValidationFailure(model, errors);
        }

        // Claim the row atomically so a concurrent send or edit cannot reuse the old scheduled job.
        var claimed = await context.Email
            .Where(item =>
                item.ID == id &&
                item.CloudID == cloudNameResolver.CloudID &&
                item.Status == EmailStatus.Scheduled &&
                item.ScheduledAtUtc > appTime.UtcNow)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(item => item.Status, EmailStatus.Updating));
        if (claimed == 0)
            return new EmailManagementResult(
                EmailManagementStatus.Conflict,
                Message: "This update has started sending and can no longer be edited.");

        var previousJobId = update.HangfireJobId;
        var previousScheduledAtUtc = update.ScheduledAtUtc!.Value;
        string? replacementJobId = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(previousJobId))
                BackgroundJob.Delete(previousJobId);

            replacementJobId = BackgroundJob.Schedule<EmailSendingService>(
                service => service.SendEmailAsync(id),
                scheduledAtUtc!.Value - appTime.UtcNow);

            update.Subject = model.Subject.Trim();
            update.Body = model.Body.Trim();
            update.Audience = model.Audience;
            update.DreamStatus = model.Audience == EmailAudience.DreamStatus
                ? model.DreamStatus
                : null;
            update.SelectedDreamIds = SerializeSelectedDreamIds(model);
            update.ScheduledAtUtc = scheduledAtUtc;
            update.HangfireJobId = replacementJobId;
            update.Status = EmailStatus.Scheduled;
            update.Error = null;
            await context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            // Restore the previous delivery time if replacing the job or saving the update fails.
            if (!string.IsNullOrWhiteSpace(replacementJobId))
                BackgroundJob.Delete(replacementJobId);

            update.Status = EmailStatus.Scheduled;
            update.HangfireJobId = BackgroundJob.Schedule<EmailSendingService>(
                service => service.SendEmailAsync(id),
                previousScheduledAtUtc - appTime.UtcNow);
            await context.SaveChangesAsync();

            logger.LogError(exception, "Could not update email update {EmailId}.", id);
            return new EmailManagementResult(
                EmailManagementStatus.Error,
                Message: "The update could not be updated. Its previous scheduled time has been restored.");
        }

        return new EmailManagementResult(EmailManagementStatus.Success);
    }

    /// <summary>
    /// Cancels an email update that has not started sending and removes its scheduled job.
    /// </summary>
    /// <param name="id">The update ID.</param>
    /// <returns>The cancellation result.</returns>
    public async Task<EmailManagementResult> CancelEmailAsync(int id)
    {
        EnsureCloudContext();
        var claimed = await context.Email
            .Where(item =>
                item.ID == id &&
                item.CloudID == cloudNameResolver.CloudID &&
                item.Status == EmailStatus.Scheduled &&
                item.ScheduledAtUtc > appTime.UtcNow)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(item => item.Status, EmailStatus.Cancelled));
        if (claimed == 0)
            return new EmailManagementResult(
                EmailManagementStatus.Conflict,
                Message: "This update has started sending or is no longer scheduled, so it could not be cancelled.");

        var update = await GetEmailAsync(id)
            ?? throw new InvalidOperationException($"Cancelled email update {id} could not be reloaded.");
        try
        {
            if (!string.IsNullOrWhiteSpace(update.HangfireJobId))
                BackgroundJob.Delete(update.HangfireJobId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not remove Hangfire job for cancelled email update {EmailId}.", id);
        }

        return new EmailManagementResult(EmailManagementStatus.Success);
    }

    /// <summary>
    /// Loads the dreams available for audience selection in the active cloud.
    /// </summary>
    /// <param name="model">The form model whose available-dream options will be populated.</param>
    public async Task PopulateEmailOptionsAsync(EmailViewModel model)
    {
        EnsureCloudContext();
        model.AvailableDreams = await context.Dream
            .AsNoTracking()
            .Where(dream => dream.CloudID == cloudNameResolver.CloudID)
            .OrderBy(dream => dream.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Validates audience-specific fields and verifies selected dreams belong to the active cloud.
    /// </summary>
    /// <param name="model">The email update form model.</param>
    /// <returns>Validation errors keyed by view-model property name.</returns>
    private async Task<Dictionary<string, List<string>>> ValidateEmailAsync(
        EmailViewModel model)
    {
        var errors = new Dictionary<string, List<string>>();
        if (!Enum.IsDefined(model.Audience))
            AddError(errors, nameof(model.Audience), "Choose a valid audience.");

        if (model.DreamStatus.HasValue && !Enum.IsDefined(model.DreamStatus.Value))
            AddError(errors, nameof(model.DreamStatus), "Choose a valid dream status.");

        if (model.Audience == EmailAudience.SelectedDreams)
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

        if (model.Audience == EmailAudience.DreamStatus && !model.DreamStatus.HasValue)
            AddError(errors, nameof(model.DreamStatus), "Choose a dream status.");

        return errors;
    }

    /// <summary>
    /// Validates a local scheduled time and converts it to UTC.
    /// </summary>
    /// <param name="scheduledAt">The application-local send time, if supplied.</param>
    /// <param name="required">Whether a send time must be provided.</param>
    /// <param name="errors">The validation errors to add to when invalid.</param>
    /// <returns>The UTC send time, or <see langword="null"/> if no time was supplied or it was invalid.</returns>
    private DateTime? ValidateScheduledAt(
        DateTime? scheduledAt,
        bool required,
        Dictionary<string, List<string>> errors)
    {
        if (!scheduledAt.HasValue)
        {
            if (required)
                AddError(errors, nameof(EmailViewModel.ScheduledAt), "A future send time is required.");
            return null;
        }

        try
        {
            // Resolve local time through IAppTime so configured timezone and invalid DST times
            // are handled consistently with the rest of the application.
            var scheduledAtUtc = appTime.ToUtc(scheduledAt.Value);
            if (scheduledAtUtc <= appTime.UtcNow)
                AddError(errors, nameof(EmailViewModel.ScheduledAt), "Choose a future time.");
            return scheduledAtUtc;
        }
        catch (ArgumentException)
        {
            AddError(errors, nameof(EmailViewModel.ScheduledAt), "The selected local time is invalid.");
            return null;
        }
    }

    /// <summary>
    /// Adds a validation message to the collection for the specified property.
    /// </summary>
    /// <param name="errors">The validation errors to update.</param>
    /// <param name="key">The view-model property name.</param>
    /// <param name="message">The validation message.</param>
    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
            errors[key] = messages = [];
        messages.Add(message);
    }

    /// <summary>
    /// Creates a validation-failure result containing the form model and its errors.
    /// </summary>
    /// <param name="model">The submitted model.</param>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A validation-error result.</returns>
    private static EmailManagementResult ValidationFailure(
        EmailViewModel model,
        Dictionary<string, List<string>> errors) =>
        new(EmailManagementStatus.ValidationError, model, errors);

    /// <summary>
    /// Serializes selected dream IDs, omitting selections when the audience does not use them.
    /// </summary>
    /// <param name="model">The email update form model.</param>
    /// <returns>A JSON array of unique selected dream IDs.</returns>
    private static string SerializeSelectedDreamIds(EmailViewModel model) =>
        JsonSerializer.Serialize(
            model.Audience == EmailAudience.SelectedDreams
                ? model.SelectedDreamIds.Distinct().ToArray()
                : Array.Empty<int>());

    /// <summary>
    /// Finds an email update belonging to the active cloud.
    /// </summary>
    /// <param name="id">The update ID.</param>
    /// <returns>The matching update, or <see langword="null"/>.</returns>
    private Task<Email?> GetEmailAsync(int id) =>
        context.Email.SingleOrDefaultAsync(item =>
            item.ID == id &&
            item.CloudID == cloudNameResolver.CloudID);

    /// <summary>
    /// Determines whether an update is scheduled for a future time and can still be edited.
    /// </summary>
    /// <param name="update">The update to check.</param>
    /// <returns><see langword="true"/> if the update can be edited.</returns>
    private bool IsEditable(Email update) =>
        update.Status == EmailStatus.Scheduled &&
        update.ScheduledAtUtc > appTime.UtcNow;

    /// <summary>
    /// Ensures the current request has an active cloud context.
    /// </summary>
    /// <exception cref="InvalidOperationException">No active cloud is selected.</exception>
    private void EnsureCloudContext()
    {
        if (cloudNameResolver.CloudID <= 0)
            throw new InvalidOperationException("Email updates require an active cloud.");
    }
}
