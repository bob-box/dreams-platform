using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Extensions;
using Borderland.Dreams.Interfaces;

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

    [DataType(DataType.DateTime)]
    [DisplayName("Dreaming Period Start")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? DreamCreatePeriodStart { get; set; }
    
    [DataType(DataType.DateTime)]
    [DisplayName("Dreaming Period End")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? DreamCreatePeriodEnd { get; set; }
    
    [DataType(DataType.DateTime)]
    [DisplayName("Dream Edit Deadline")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? DreamEditDeadline { get; set; }

    public List<DreamViewModel>? Dreams { get; set; } = [];
    public List<FundingRoundViewModel>? FundingRounds { get; set; } = [];

    public bool IsDreamingPeriodActive(IAppTime appTime)
    {
        return appTime.UtcNow.IsBetween(DreamCreatePeriodStart, DreamCreatePeriodEnd);
    }
}