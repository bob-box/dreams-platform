using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class FundingRoundViewModel
{
    public int ID { get; set; }
    public int CloudID { get; set; }
    public string Title { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    public decimal AmountToSpendPerUser { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    public decimal MaxAmountPerDreamPerUser { get; set; }

}