using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Models;

public class DreamBudget
{
    public int ID { get; set; }
    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; }
    public BudgetType BudgetType { get; set; }
    public List<BudgetItem> Items { get; set; } = new List<BudgetItem>();
}
