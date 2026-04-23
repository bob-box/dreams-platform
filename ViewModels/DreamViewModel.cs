using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Borderland.Dreams.ViewModels;

public class DreamViewModel
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime CreatedAt { get; set; }
    public DreamStatus Status { get; set; } = DreamStatus.Draft;
    [Display(Name = "Budget")]
    public List<BudgetItemViewModel> BudgetItems { get; set; } = new List<BudgetItemViewModel>();
    public int CloudID { get; set; }
    public List<FundViewModel> Funds { get; set; } = new List<FundViewModel>();
    public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
    public List<ExpenseViewModel> Expenses { get; set; } = new List<ExpenseViewModel>();
    public List<TagViewModel> Tags { get; set; } = new List<TagViewModel>();
    public List<int> SelectedTagIds { get; set; } = [];
    public List<DreamerViewModel> Dreamers { get; set; } = new List<DreamerViewModel>();


    [Display(Name = "Requested Budget")]
    [ValidateNever]
    public IEnumerable<BudgetItemViewModel> RequestedBudgetItems => BudgetItems.Where(b => b.Type == BudgetItemType.Requesting);
    [Display(Name = "Existing Budget")]
    [ValidateNever]
    public IEnumerable<BudgetItemViewModel> ExistingBudgetItems => BudgetItems.Where(b => b.Type == BudgetItemType.Existing);

    [Display(Name = "Total Requested Budget")]
    [DataType(DataType.Currency)]
    public decimal TotalRequestedBudget => RequestedBudgetItems.Sum(b => b.MinAmount ?? 0);
    [Display(Name = "Total Existing Budget")]
    [DataType(DataType.Currency)]
    public decimal TotalExistingBudget => ExistingBudgetItems.Sum(b => b.MinAmount ?? 0);

    [Display(Name = "Total Received Funds")]
    [DataType(DataType.Currency)]
    public decimal TotalReceivedFunds => Funds.Sum(f => f.Amount);

    public bool IsOpenToFunding() => Status == DreamStatus.Active;
    public bool IsReadyForFunding() => Status == DreamStatus.ReadyForFunding;
    public bool IsEditable() => Status == DreamStatus.Draft || Status == DreamStatus.Hidden;
    public bool IsVisibleToPublic() => Status != DreamStatus.Draft && Status != DreamStatus.Hidden;

}
