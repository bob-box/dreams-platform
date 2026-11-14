using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class Wallet : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAtUtc { get; set; }

    public string UserID { get; set; } = null!;
    public int CloudID { get; set; }
    public Cloud Cloud { get; set; } = null!;
    public User User { get; set; } = null!;

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Balance { get; set; } = 0;
}
