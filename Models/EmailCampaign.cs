using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Models;

public class EmailCampaign
{
    public int ID { get; set; }
    public int CloudID { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public EmailCampaignAudience Audience { get; set; }
    public DreamStatus? DreamStatus { get; set; }
    public string SelectedDreamIds { get; set; } = "[]";
    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public int? RecipientCount { get; set; }
    public EmailCampaignStatus Status { get; set; }
    public string? HangfireJobId { get; set; }
    public string? Error { get; set; }
}
