using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.ViewModels;

public class BudgetItemViewModel : IValidatableObject
{
    public int ID { get; set; }
    public string Name { get; set; }
    public BudgetItemType Type { get; set; }
    public int DreamID { get; set; }

    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Display(Name = "Amount")]
    public decimal MinAmount { get; set; } = 0;

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Display(Name = "Max Amount", Prompt = "Max amount (if applicable)")]
    public decimal? MaxAmount { get; set; } = null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxAmount.HasValue && MaxAmount <= MinAmount)
        {
            yield return new ValidationResult(
                "Max amount must be greater than Min amount",
                [nameof(MaxAmount)]
            );
        }
    }
}