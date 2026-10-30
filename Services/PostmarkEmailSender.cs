using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class PostmarkEmailSender(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ApplicationDbContext context,
    ILogger<PostmarkEmailSender> logger
) : IEmailSender, IPostmarkEmailService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    public Task SendEmailAsync(string email, string subject, string htmlMessage) =>
        SendEmailAsync(email, subject, null, htmlMessage);

    public async Task SendTextEmailAsync(string to, string subject, string textBody)
    {
        var htmlBody = $"<p>{HtmlEncoder.Default.Encode(textBody).Replace(Environment.NewLine, "<br />")}</p>";
        await SendEmailAsync(to, subject, textBody, htmlBody);
    }

    public async Task SendBatchAsync(IReadOnlyCollection<OutboundEmail> emails, bool isBroadcast)
    {
        if (emails.Count == 0)
            return;

        var settings = await GetSettingsAsync();
        var stream = isBroadcast ? settings.BroadcastMessageStream : settings.TransactionalMessageStream;
        using var httpClient = CreateClient();

        foreach (var batch in emails.Chunk(500))
        {
            var messages = batch.Select(email => CreateMessage(settings, stream, email)).ToArray();
            using var request = new HttpRequestMessage(HttpMethod.Post, "email/batch")
            {
                Content = JsonContent.Create(messages, options: JsonOptions)
            };
            request.Headers.Add("X-Postmark-Server-Token", GetServerToken());

            using var response = await httpClient.SendAsync(request);
            await EnsurePostmarkSuccessAsync(response);

            var results = await response.Content.ReadFromJsonAsync<List<PostmarkResponse>>(JsonOptions)
                ?? throw new InvalidOperationException("Postmark returned an empty batch response.");
            if (results.Count != batch.Length)
                throw new InvalidOperationException("Postmark returned an unexpected number of batch results.");

            var failures = results.Where(result => result.ErrorCode != 0).ToArray();
            if (failures.Length > 0)
                throw new InvalidOperationException(
                    $"Postmark rejected {failures.Length} message(s): {string.Join("; ", failures.Select(result => result.Message))}");
        }
    }

    private async Task SendEmailAsync(string to, string subject, string? textBody, string? htmlBody)
    {
        var settings = await GetSettingsAsync();
        using var httpClient = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "email")
        {
            Content = JsonContent.Create(
                CreateMessage(settings, settings.TransactionalMessageStream, new OutboundEmail(to, subject, textBody ?? string.Empty, htmlBody)),
                options: JsonOptions)
        };
        request.Headers.Add("X-Postmark-Server-Token", GetServerToken());

        using var response = await httpClient.SendAsync(request);
        await EnsurePostmarkSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<PostmarkResponse>(JsonOptions)
            ?? throw new InvalidOperationException("Postmark returned an empty response.");
        if (result.ErrorCode != 0)
            throw new InvalidOperationException($"Postmark rejected the message: {result.Message}");
    }

    private async Task<EmailSettings> GetSettingsAsync()
    {
        var settings = await context.EmailSettings.SingleOrDefaultAsync(settings => settings.ID == 1);
        if (settings == null || string.IsNullOrWhiteSpace(settings.FromAddress))
            throw new InvalidOperationException("Configure the sender address in the admin email settings before sending email.");

        return settings;
    }

    private string GetServerToken() =>
        configuration["Postmark:ServerToken"]
        ?? throw new InvalidOperationException("The Postmark server token is not configured. Set Postmark__ServerToken.");

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient(nameof(PostmarkEmailSender));
        client.BaseAddress = new Uri("https://api.postmarkapp.com/");
        return client;
    }

    private static PostmarkMessage CreateMessage(EmailSettings settings, string messageStream, OutboundEmail email) =>
        new()
        {
            From = string.IsNullOrWhiteSpace(settings.FromName)
                ? settings.FromAddress
                : $"{settings.FromName} <{settings.FromAddress}>",
            To = email.To,
            Subject = email.Subject,
            TextBody = email.TextBody,
            HtmlBody = email.HtmlBody,
            MessageStream = messageStream
        };

    private async Task EnsurePostmarkSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var details = await response.Content.ReadAsStringAsync();
        logger.LogError("Postmark request failed with status {StatusCode}: {Details}", response.StatusCode, details);
        throw new HttpRequestException(
            $"Postmark request failed with status {(int)response.StatusCode}: {details}",
            null,
            response.StatusCode);
    }

    private sealed class PostmarkMessage
    {
        public string From { get; init; } = string.Empty;
        public string To { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string TextBody { get; init; } = string.Empty;
        public string? HtmlBody { get; init; }
        public string MessageStream { get; init; } = string.Empty;
    }

    private sealed class PostmarkResponse
    {
        public int ErrorCode { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
