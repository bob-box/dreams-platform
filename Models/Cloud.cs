using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Borderland.Dreams.Models;

public class Cloud
{
    public int ID { get; set; }

    /// <summary>
    /// Name is a unique index, see ApplicationDbContext.OnModelCreating()
    /// </summary>
    [Required]
    public required string Name { get; set; }

    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalFundsAvailable { get; set; } = 0;

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalFundsRemaining { get; set; } = 0;

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalFundsSpent { get; set; } = 0;

    // Navigation properties
    public List<Dream>? Dreams { get; set; } = [];
    public List<FundingRound>? FundingRounds { get; set; } = [];
}
