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
    [Display(Name="Dreaming Period Start", Description = "The period during which new dreams can be created. After this period ends, no new dreams can be created, but existing dreams can still be edited until the edit deadline. If not set, dream creation is open indefinitely.")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? DreamCreatePeriodStart { get; set; }
    
    [DataType(DataType.DateTime)]
    [Display(Name="Dream Period End")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? DreamCreatePeriodEnd { get; set; }
    
    [DataType(DataType.DateTime)]
    [Display(Name="Dream Edit Deadline")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? DreamEditDeadline { get; set; }

    public List<DreamViewModel>? Dreams { get; set; } = [];
    public List<FundingRoundViewModel>? FundingRounds { get; set; } = [];

    public bool IsDreamingPeriodActive(IAppTime appTime)
    {
        return appTime.Now.IsBetween(DreamCreatePeriodStart, DreamCreatePeriodEnd);
    }

    /// <summary>
    /// Returns true if the current time is before the DreamEditDeadline, or if no deadline is set.
    /// </summary>
    public bool IsBeforeEditDeadline(IAppTime appTime)
    {
        return DreamEditDeadline == null || appTime.Now <= DreamEditDeadline.Value;
    }
}