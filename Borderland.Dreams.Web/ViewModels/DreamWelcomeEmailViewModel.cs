using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class DreamWelcomeEmailViewModel
{
    [Display(Name = "Send dream welcome email")]
    public bool Enabled { get; set; }

    [StringLength(200)]
    [Display(Name = "Welcome email subject")]
    public string? Subject { get; set; }

    [StringLength(10000)]
    [Display(Name = "Welcome email message")]
    public string? Body { get; set; }
}
