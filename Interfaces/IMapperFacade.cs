using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public interface IMapperFacade
{
    #region ToViewModel
    DreamViewModel? ToViewModel(Dream model);
    CloudViewModel? ToViewModel(Cloud model);
    TagViewModel? ToViewModel(Tag model);
    UserViewModel? ToViewModel(User model);
    WalletViewModel? ToViewModel(Wallet model);
    FundingRoundViewModel? ToViewModel(FundingRound model);
    BudgetItemViewModel? ToViewModel(BudgetItem model);
    CommentViewModel? ToViewModel(Comment model);
    FundViewModel? ToViewModel(Fund model);
    ExpenseViewModel? ToViewModel(Expense model);
    DreamerViewModel? ToViewModel(Dreamer model);
    #endregion

    #region ToViewModels
    List<DreamViewModel> ToViewModels(ICollection<Dream> models);
    List<CloudViewModel> ToViewModels(ICollection<Cloud> models);
    List<TagViewModel> ToViewModels(ICollection<Tag> models);
    List<UserViewModel> ToViewModels(ICollection<User> models);
    List<WalletViewModel> ToViewModels(ICollection<Wallet> models);
    List<FundingRoundViewModel> ToViewModels(ICollection<FundingRound> models);
    List<BudgetItemViewModel> ToViewModels(ICollection<BudgetItem> models);
    List<CommentViewModel> ToViewModels(ICollection<Comment> models);
    List<FundViewModel> ToViewModels(ICollection<Fund> models);
    List<ExpenseViewModel> ToViewModels(ICollection<Expense> models);
    List<DreamerViewModel> ToViewModels(ICollection<Dreamer> models);
    #endregion

    #region ToModel
    Dream? ToModel(DreamViewModel vm);
    Cloud? ToModel(CloudViewModel vm);
    Tag? ToModel(TagViewModel vm);
    Wallet? ToModel(WalletViewModel vm);
    FundingRound? ToModel(FundingRoundViewModel vm);
    BudgetItem? ToModel(BudgetItemViewModel vm);
    Comment? ToModel(CommentViewModel vm);
    Fund? ToModel(FundViewModel vm);
    Expense? ToModel(ExpenseViewModel vm);
    Dreamer? ToModel(DreamerViewModel vm);
    #endregion

    #region ToModels
    List<Dream> ToModels(ICollection<DreamViewModel> vms);
    List<Cloud> ToModels(ICollection<CloudViewModel> vms);
    List<Tag> ToModels(ICollection<TagViewModel> vms);
    List<FundingRound> ToModels(ICollection<FundingRoundViewModel> vms);
    List<BudgetItem> ToModels(ICollection<BudgetItemViewModel> vms);
    List<Comment> ToModels(ICollection<CommentViewModel> vms);
    List<Fund> ToModels(ICollection<FundViewModel> vms);
    List<Expense> ToModels(ICollection<ExpenseViewModel> vms);
    List<Dreamer> ToModels(ICollection<DreamerViewModel> vms);
    #endregion
}