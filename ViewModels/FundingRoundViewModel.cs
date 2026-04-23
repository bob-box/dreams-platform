using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.ViewModels;

public class FundingRoundViewModel
{
    public int ID { get; set; }
    public int CloudID { get; set; }
    public string Title { get; set; }

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [DataType(DataType.DateTime)]
    public DateTime StartDate { get; set; }

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [DataType(DataType.DateTime)]
    public DateTime EndDate { get; set; }

    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
    public decimal MaxPercentagePerDream { get; set; } = 100;

    [Range(0, double.MaxValue, ErrorMessage = "Allocation must be non-negative")]
    public decimal AllocationPerUser { get; set; } = 0;
    [DataType(DataType.Currency)]
    public decimal TotalFundsAllocated { get; set; }

    public int UsersInCloudCount { get; set; }

    public bool IsActiveOrPassed(IAppTime appTime) => IsActive(appTime) || HasPassed(appTime);
    public bool IsActive(IAppTime appTime) => StartDate <= appTime.Now && EndDate >= appTime.Now;
    public bool HasPassed(IAppTime appTime) => EndDate < appTime.Now;

    public decimal MaxPerDream => AllocationPerUser * (MaxPercentagePerDream / 100m);
}