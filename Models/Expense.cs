using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class Expense : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }
        
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }

    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }
    public int DreamID { get; set; }
}
