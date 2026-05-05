using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class CloudViewModel
{
    public int ID { get; set; }
    public required string Name { get; set; }
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    public decimal TotalFundsAvailable { get; set; }
    [DataType(DataType.Currency)]
    public decimal TotalFundsRemaining { get; set; } = 0;
    [DataType(DataType.Currency)]
    public decimal TotalFundsSpent { get; set; }
    public List<DreamViewModel>? Dreams { get; set; } = [];
    public List<FundingRoundViewModel>? FundingRounds { get; set; } = [];
}