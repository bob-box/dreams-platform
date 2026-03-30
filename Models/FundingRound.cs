using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Borderland.Dreams.Models;

public class FundingRound
{
    public int ID { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CloudID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ProcessedDate { get; set; }

    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
    public decimal MaxPercentagePerDream { get; set; } = 100; // Percentage of the per-user allocation that can go to one dream

    [Range(0, double.MaxValue, ErrorMessage = "Allocation must be non-negative")]
    public decimal AllocationPerUser { get; set; } = 0; // Amount each user receives when the round is created
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalFundsAllocated { get; set; } = 0;
    public string? StartJobId { get; set; } // Hangfire job ID for allocation
    public string? EndJobId { get; set; } // Hangfire job ID for end processing

    public Cloud Cloud { get; set; }

    // These 3 methods below exist both on the model and the viewmodel.
    public bool IsActiveOrPassed() => IsActive() || HasPassed();
    public bool IsActive() => StartDate <= DateTime.UtcNow && EndDate >= DateTime.UtcNow;
    public bool HasPassed() => EndDate < DateTime.UtcNow;
}
