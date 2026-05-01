using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class Cloud : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Name is a unique index, see ApplicationDbContext.OnModelCreating()
    /// </summary>
    [Required]
    public required string Name { get; set; }
    

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
    public ICollection<Dream>? Dreams { get; set; } = [];
    public ICollection<FundingRound>? FundingRounds { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public bool IsDeleted { get; set; } = false;
}
