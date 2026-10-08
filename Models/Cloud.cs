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

    [Required]
    [MaxLength(128)]
    public string Slug { get; set; } = "";

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalFundsAvailable { get; set; } = 0;

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalFundsRemaining { get; set; } = 0;

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalFundsSpent { get; set; } = 0;
    
    public bool IsDeleted { get; set; } = false;
    
    [DataType(DataType.DateTime)]
    public DateTime? DreamCreatePeriodStart { get; set; }
    [DataType(DataType.DateTime)]
    public DateTime? DreamCreatePeriodEnd { get; set; }
    [DataType(DataType.DateTime)]
    public DateTime? DreamEditDeadline { get; set; }

    // Navigation properties
    public ICollection<Dream>? Dreams { get; set; }
    public ICollection<FundingRound>? FundingRounds { get; set; }
    public ICollection<Tag> Tags { get; set; } = [];
    public CloudInfo? Info { get; set; }
}
