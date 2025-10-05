using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Models;

namespace Borderland.Dreams.ViewModels;

public class DreamViewModel
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DreamStatus Status { get; set; } = DreamStatus.Draft;
    [Display(Name = "Budget")]
    public List<BudgetItem> BudgetItems { get; set; } = new List<BudgetItem>();
    public int CloudID { get; set; }
    public List<Fund> Funds { get; set; } = new List<Fund>();
    public List<Comment> Comments { get; set; } = new List<Comment>();
    public List<Expense> Expenses { get; set; } = new List<Expense>();
    public List<Tag> Tags { get; set; } = new List<Tag>();
    public List<Dreamer> Dreamers { get; set; } = new List<Dreamer>();


    [Display(Name = "Requested Budget")]
    public List<BudgetItem> RequestedBudgetItems => BudgetItems.Where(b => b.Type == BudgetItemType.Requesting).ToList();
    [Display(Name = "Existing Budget")]
    public List<BudgetItem> ExistingBudgetItems => BudgetItems.Where(b => b.Type == BudgetItemType.Existing).ToList();

    [Display(Name = "Total Requested Budget")]
    [DataType(DataType.Currency)]
    public decimal TotalRequestedBudget => RequestedBudgetItems.Sum(b => b.MinAmount);
    [Display(Name = "Total Existing Budget")]
    [DataType(DataType.Currency)]
    public decimal TotalExistingBudget => ExistingBudgetItems.Sum(b => b.MinAmount);

}
