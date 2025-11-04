using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Models;

namespace Borderland.Dreams.ViewModels;

public class DreamViewModel: IDreamWithDreamersForAuthorization
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DreamStatus Status { get; set; } = DreamStatus.Draft;
    [Display(Name = "Budget")]
    public List<BudgetItemViewModel> BudgetItems { get; set; } = new List<BudgetItemViewModel>();
    public int CloudID { get; set; }
    public List<FundViewModel> Funds { get; set; } = new List<FundViewModel>();
    public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
    public List<ExpenseViewModel> Expenses { get; set; } = new List<ExpenseViewModel>();
    public List<TagViewModel> Tags { get; set; } = new List<TagViewModel>();
    public List<DreamerViewModel> Dreamers { get; set; } = new List<DreamerViewModel>();


    [Display(Name = "Requested Budget")]
    public List<BudgetItemViewModel> RequestedBudgetItems => BudgetItems.Where(b => b.Type == BudgetItemType.Requesting).ToList();
    [Display(Name = "Existing Budget")]
    public List<BudgetItemViewModel> ExistingBudgetItems => BudgetItems.Where(b => b.Type == BudgetItemType.Existing).ToList();

    [Display(Name = "Total Requested Budget")]
    [DataType(DataType.Currency)]
    public decimal TotalRequestedBudget => RequestedBudgetItems.Sum(b => b.MinAmount);
    [Display(Name = "Total Existing Budget")]
    [DataType(DataType.Currency)]
    public decimal TotalExistingBudget => ExistingBudgetItems.Sum(b => b.MinAmount);

}
