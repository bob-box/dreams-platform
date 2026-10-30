using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class EmailSettingsViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Verified sender email")]
    public string FromAddress { get; set; } = string.Empty;

    [Display(Name = "Sender name")]
    public string FromName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Transactional message stream")]
    public string TransactionalMessageStream { get; set; } = "outbound";

    [Required]
    [Display(Name = "Broadcast message stream")]
    public string BroadcastMessageStream { get; set; } = "broadcast";

    public bool IsServerTokenConfigured { get; set; }
}
