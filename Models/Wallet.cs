using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Borderland.Dreams.Models;

public class Wallet
{
    public Wallet() { }
    public Wallet(int cloudID)
    {
        CloudID = cloudID;
    }
    public int ID { get; set; }
    public string UserID { get; set; } = null!;
    public int CloudID { get; set; }
    public User User { get; set; } = null!;

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Balance { get; set; } = 0;
}
