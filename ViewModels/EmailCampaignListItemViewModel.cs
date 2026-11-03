using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.ViewModels;

public class EmailCampaignListItemViewModel
{
    public int ID { get; set; }
    public string Subject { get; set; } = string.Empty;
    public EmailCampaignAudience Audience { get; set; }
    public DreamStatus? DreamStatus { get; set; }
    public EmailCampaignStatus Status { get; set; }

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
    public DateTime? ScheduledAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
    public DateTime? SentAt { get; set; }

    public int? RecipientCount { get; set; }
    public string? Error { get; set; }
}
