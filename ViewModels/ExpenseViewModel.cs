using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class ExpenseViewModel
{
    public int ID { get; set; }
    public int DreamID { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }
    public string Description { get; set; }
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime CreatedAt { get; set; }
}