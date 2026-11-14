using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class FundingRound : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }
        
    [DataType(DataType.DateTime)]
    public DateTime CreatedAtUtc { get; set; }

    public string Title { get; set; } = string.Empty;
    public int CloudID { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public bool IsLastRound { get; set; }
    public DateTime? ProcessedDateUtc { get; set; }

    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
    public decimal MaxPercentagePerDream { get; set; } = 100; // Percentage of the per-user allocation that can go to one dream

    [Range(0, double.MaxValue, ErrorMessage = "Allocation must be non-negative")]
    public decimal AllocationPerUser { get; set; } = 0; // Amount each user receives when the round is created
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalFundsAllocated { get; set; } = 0;
    public string? StartJobId { get; set; } // Hangfire job ID for allocation
    public string? EndJobId { get; set; } // Hangfire job ID for end processing

    public Cloud Cloud { get; set; } = null!;
}
