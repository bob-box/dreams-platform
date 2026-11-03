using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Models;

namespace Borderland.Dreams.ViewModels;

public class EmailViewModel
{
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(10000)]
    [Display(Name = "Message")]
    public string Body { get; set; } = string.Empty;

    [Display(Name = "Audience")]
    public EmailAudience Audience { get; set; } = EmailAudience.AllDreams;

    [Display(Name = "Dream status")]
    public DreamStatus? DreamStatus { get; set; }

    [Display(Name = "Send at")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
    public DateTime? ScheduledAt { get; set; }

    [Display(Name = "Selected dreams")]
    public List<int> SelectedDreamIds { get; set; } = [];

    public List<Dream> AvailableDreams { get; set; } = [];
    public List<Email> RecentUpdates { get; set; } = [];
}
