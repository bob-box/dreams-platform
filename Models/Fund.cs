using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Borderland.Dreams.Models;

public class Fund
{
    public int ID { get; set; }
    public int DreamID { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; }

    public required User User { get; set; }
    public required Dream Dream { get; set; }
}
