using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class BudgetItem : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }

    public string Name { get; set; } = string.Empty;
    public BudgetItemType Type { get; set; }
    public int DreamID { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal MinAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal? MaxAmount { get; set; }
}
