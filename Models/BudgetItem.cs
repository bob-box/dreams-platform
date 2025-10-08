using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Models;

public class BudgetItem : IValidatableObject
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public BudgetItemType Type { get; set; }
    public int DreamID { get; set; }

    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Amount")]
    public decimal MinAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid non-negative amount")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Max Amount", Prompt = "Max amount (if applicable)")]

    public decimal? MaxAmount { get; set; }

    [NotMapped] // TODO : Perhaps put this in a ViewModel?
    public bool IsDeleted { get; set; }

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
