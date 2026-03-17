using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class FundingRoundViewModel
{
    public int ID { get; set; }
    public int CloudID { get; set; }
    public string Title { get; set; }
    
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime StartDate { get; set; }
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime EndDate { get; set; }

    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
    public decimal MaxPercentagePerDream { get; set; } = 100;

    [Range(0, double.MaxValue, ErrorMessage = "Allocation must be non-negative")]
    public decimal AllocationPerUser { get; set; } = 0;
}