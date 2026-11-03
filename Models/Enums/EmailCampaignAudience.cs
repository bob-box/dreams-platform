using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.Enums;

public enum EmailCampaignAudience
{
    [Display(Name = "All dreams")] 
    AllDreams,
    [Display(Name = "Specific dreams")] 
    SelectedDreams,
    [Display(Name = "Dreams by status")] 
    DreamStatus
}
