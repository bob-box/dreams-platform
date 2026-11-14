using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class FundViewModel
{
    public int ID { get; set; }
    public int DreamID { get; set; }
    public string UserID { get; set; }
    public int? FundingRoundID { get; set; }
   
    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime CreatedAt { get; set; }

    public UserViewModel User { get; set; }
}