using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class WalletViewModel
{
    public int ID { get; set; }
    public string UserID { get; set; }
    public int CloudID { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    public decimal Balance { get; set; } = 0;
}