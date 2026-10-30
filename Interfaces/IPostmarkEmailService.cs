namespace Borderland.Dreams.Interfaces;

public record OutboundEmail(string To, string Subject, string TextBody, string? HtmlBody = null);

public interface IPostmarkEmailService
{
    Task SendTextEmailAsync(string to, string subject, string textBody);
    Task SendBatchAsync(IReadOnlyCollection<OutboundEmail> emails, bool isBroadcast);
}
