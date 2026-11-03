namespace Borderland.Dreams.Interfaces;

public record OutboundEmail(string To, string Subject, string? HtmlBody = null);

public interface IResendEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, string? tag = null);
    Task SendEmailInBatchesAsync(IReadOnlyCollection<OutboundEmail> emails, string? tag = null);
}
