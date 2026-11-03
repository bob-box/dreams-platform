using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Models;

namespace Borderland.Dreams.ViewModels;

public class EmailCampaignViewModel
{
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(10000)]
    [Display(Name = "Message")]
    public string Body { get; set; } = string.Empty;

    [Display(Name = "Audience")]
    public EmailCampaignAudience Audience { get; set; } = EmailCampaignAudience.AllDreams;

    [Display(Name = "Dream status")]
    public DreamStatus? DreamStatus { get; set; }

    [Display(Name = "Send at")]
    public DateTime? ScheduledAt { get; set; }

    [Display(Name = "Selected dreams")]
    public List<int> SelectedDreamIds { get; set; } = [];

    public List<Dream> AvailableDreams { get; set; } = [];
    public List<EmailCampaign> RecentCampaigns { get; set; } = [];
}
