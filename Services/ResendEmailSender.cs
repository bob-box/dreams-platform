using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Microsoft.EntityFrameworkCore;
using Resend;

namespace Borderland.Dreams.Services;

public class ResendEmailSender(
    IResend resend,
    IConfiguration configuration,
    ApplicationDbContext context
) : IResendEmailService
{
    /// <summary>
    /// Sends an HTML email through Resend.
    /// </summary>
    /// <param name="to">The recipient email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlBody">The HTML email body.</param>
    /// <param name="tag">Tag to categorize the email</param>
    public async Task SendEmailAsync(string to, string subject, string htmlBody, string? tag = null)
    {
        await SendAsync(to, subject, htmlBody, tag);
    }

    /// <summary>
    /// Sends many emails through Resend's Email Batch API.
    /// </summary>
    /// <param name="emails">The campaign messages to send.</param>
    /// <param name="tag">Tag to categorize the email</param>
    public async Task SendEmailInBatchesAsync(IReadOnlyCollection<OutboundEmail> emails, string? tag = null)
    {
        if (emails.Count == 0)
            return;

        EnsureApiKeyConfigured();
        var settings = await GetSettingsAsync();

        // Resend's Email Batch API accepts at most 100 messages per request.
        foreach (var batch in emails.Chunk(100))
        {
            var messages = batch.Select(email => CreateMessage(settings, email, tag)).ToArray();
            var result = (await resend.EmailBatchAsync(messages, EmailBatchValidationMode.Permissive)).Content;
            // Permissive mode returns per-message validation errors instead of rejecting the whole batch.
            if (result.Errors is { Count: > 0 } failures)
                throw new InvalidOperationException(
                    $"Resend rejected {failures.Count} message(s): {string.Join("; ", failures.Select(failure => $"Message {failure.Index}: {failure.Message}"))}");
        }
    }

    private async Task SendAsync(string to, string subject, string? htmlBody, string? tag)
    {
        EnsureApiKeyConfigured();
        var settings = await GetSettingsAsync();
        var email = new OutboundEmail(to, subject, htmlBody);
        _ = (await resend.EmailSendAsync(CreateMessage(settings, email, tag))).Content;
    }

    private async Task<EmailSettings> GetSettingsAsync()
    {
        var settings = await context.EmailSettings.SingleOrDefaultAsync(settings => settings.ID == 1);
        if (settings == null || string.IsNullOrWhiteSpace(settings.FromAddress))
            throw new InvalidOperationException("Configure the sender address in the admin email settings before sending email.");

        return settings;
    }

    private void EnsureApiKeyConfigured()
    {
        if (string.IsNullOrWhiteSpace(configuration["Resend:ApiKey"]))
            throw new InvalidOperationException("The Resend API key is not configured. Set Resend__ApiKey.");
    }

    private static EmailMessage CreateMessage(EmailSettings settings, OutboundEmail email, string? tag)
    {
        var message = new EmailMessage
        {
            To = email.To,
            From = string.IsNullOrWhiteSpace(settings.FromName)
                ? settings.FromAddress
                : $"{settings.FromName} <{settings.FromAddress}>",
            Subject = email.Subject,
            HtmlBody = email.HtmlBody
        };

        if (!string.IsNullOrWhiteSpace(tag))
        {
            message.Tags = [
                new EmailTag
                {
                    Name = "message_type",
                    Value = tag
                }
            ];
        }

        return message;
    }
}
