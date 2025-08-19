using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Borderland.Dreams.Models;

public class BudgetItem
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal MinAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal MaxAmount { get; set; }
    
}
