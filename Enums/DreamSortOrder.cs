using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.Enums;

public enum DreamSortOrder
{
    [Display(Name = "Name (A-Z)")]
    NameAsc,
    [Display(Name = "Name (Z-A)")]
    NameDesc,
    [Display(Name = "Most funded")]
    MostFunded,
    [Display(Name = "Least funded")]
    LeastFunded,
    [Display(Name = "Most funded (%)")]
    MostFundedPercentage,
    [Display(Name = "Least funded (%)")]
    LeastFundedPercentage,
    [Display(Name = "Highest budget")]
    MostBudget,
    [Display(Name = "Lowest budget")]
    LeastBudget
}
