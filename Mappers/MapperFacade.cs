using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

// TODO : Create an interface for this, register the indivitual mappers for DI as singletons, and inject the mappers instead of creating them. 
// This will allow for easier unit testing and better separation of concerns.
public class MapperFacade : IMapperFacade
{
    private readonly DreamMapper _dreamMapper;
    private readonly CloudMapper _cloudMapper;
    private readonly TagMapper _tagMapper;
    private readonly UserMapper _userMapper;
    private readonly WalletMapper _walletMapper;
    private readonly FundingRoundMapper _fundingRoundMapper;
    private readonly BudgetItemMapper _budgetItemMapper;
    private readonly CommentMapper _commentMapper;
    private readonly FundMapper _fundMapper;
    private readonly ExpenseMapper _expenseMapper;
    private readonly DreamerMapper _dreamerMapper;
    private readonly AuditLogMapper _auditLogMapper;

    public MapperFacade(IAppTime appTime)
    {
        _dreamMapper = new DreamMapper(this, appTime);
        _cloudMapper = new CloudMapper(this, appTime);
        _tagMapper = new TagMapper(this, appTime);
        _userMapper = new UserMapper(this, appTime);
        _walletMapper = new WalletMapper(this, appTime);
        _fundingRoundMapper = new FundingRoundMapper(this, appTime);
        _budgetItemMapper = new BudgetItemMapper(this, appTime);
        _commentMapper = new CommentMapper(this, appTime);
        _fundMapper = new FundMapper(this, appTime);
        _expenseMapper = new ExpenseMapper(this, appTime);
        _dreamerMapper = new DreamerMapper(this, appTime);
        _auditLogMapper = new AuditLogMapper(this, appTime);
    }

    #region ToViewModel
    public DreamViewModel ToViewModel(Dream model) => _dreamMapper.ToViewModel(model);
    public CloudViewModel ToViewModel(Cloud model) => _cloudMapper.ToViewModel(model);
    public TagViewModel ToViewModel(Tag model) => _tagMapper.ToViewModel(model);
    public UserViewModel ToViewModel(User model) => _userMapper.ToViewModel(model);
    public WalletViewModel ToViewModel(Wallet model) => _walletMapper.ToViewModel(model);
    public FundingRoundViewModel ToViewModel(FundingRound model) => _fundingRoundMapper.ToViewModel(model);
    public BudgetItemViewModel ToViewModel(BudgetItem model) => _budgetItemMapper.ToViewModel(model);
    public CommentViewModel ToViewModel(Comment model) => _commentMapper.ToViewModel(model);
    public FundViewModel ToViewModel(Fund model) => _fundMapper.ToViewModel(model);
    public ExpenseViewModel ToViewModel(Expense model) => _expenseMapper.ToViewModel(model);
    public DreamerViewModel ToViewModel(Dreamer model) => _dreamerMapper.ToViewModel(model);
    public AuditLogItemViewModel ToViewModel(AuditLogEntry model, string cloudName) =>
        _auditLogMapper.ToViewModel(model, cloudName);
    #endregion ToViewModel

    #region ToViewModels
    public List<DreamViewModel> ToViewModels(ICollection<Dream> models) => models?.Select(ToViewModel).ToList() ?? [];
    public List<FundingRoundViewModel> ToViewModels(ICollection<FundingRound> models) => models?.Select(ToViewModel).ToList() ?? [];
    public List<BudgetItemViewModel> ToViewModels(ICollection<BudgetItem> models) => models?.Select(ToViewModel).ToList() ?? [];
    public List<CommentViewModel> ToViewModels(ICollection<Comment> models) => models?.Select(ToViewModel).ToList() ?? [];
    public List<FundViewModel> ToViewModels(ICollection<Fund> models) => models?.Select(ToViewModel).ToList() ?? [];
    public List<ExpenseViewModel> ToViewModels(ICollection<Expense> models) => models?.Select(ToViewModel).ToList() ?? [];
    public List<DreamerViewModel> ToViewModels(ICollection<Dreamer> models) => models?.Select(ToViewModel).ToList() ?? [];
    public List<TagViewModel> ToViewModels(ICollection<Tag> models) => models?.Select(ToViewModel).ToList() ?? [];
    public List<CloudViewModel> ToViewModels(ICollection<Cloud> models) => models?.Select(ToViewModel).ToList() ?? [];
    public List<UserViewModel> ToViewModels(ICollection<User> models) => models?.Select(ToViewModel).ToList() ?? [];
    public List<WalletViewModel> ToViewModels(ICollection<Wallet> models) => models?.Select(ToViewModel).ToList() ?? [];
    #endregion ToViewModels

    #region ToModel
    public Dream ToModel(DreamViewModel vm) => _dreamMapper.ToModel(vm);
    public Cloud ToModel(CloudViewModel vm) => _cloudMapper.ToModel(vm);
    public Tag ToModel(TagViewModel vm) => _tagMapper.ToModel(vm);
    // public User ToModel(UserViewModel vm) => _userMapper.ToModel(vm);
    public Wallet ToModel(WalletViewModel vm) => _walletMapper.ToModel(vm);
    public FundingRound ToModel(FundingRoundViewModel vm) => _fundingRoundMapper.ToModel(vm);
    public BudgetItem ToModel(BudgetItemViewModel vm) => _budgetItemMapper.ToModel(vm);
    public Comment ToModel(CommentViewModel vm) => _commentMapper.ToModel(vm);
    public Fund ToModel(FundViewModel vm) => _fundMapper.ToModel(vm);
    public Expense ToModel(ExpenseViewModel vm) => _expenseMapper.ToModel(vm);
    public Dreamer ToModel(DreamerViewModel vm) => _dreamerMapper.ToModel(vm);
    #endregion ToModel

    #region ToModels
    public List<Dream> ToModels(ICollection<DreamViewModel> vms) => vms.Select(ToModel).ToList();
    public List<FundingRound> ToModels(ICollection<FundingRoundViewModel> vms) => vms.Select(ToModel).ToList();
    public List<BudgetItem> ToModels(ICollection<BudgetItemViewModel> vms) => vms.Select(ToModel).ToList();
    public List<Comment> ToModels(ICollection<CommentViewModel> vms) => vms.Select(ToModel).ToList();
    public List<Fund> ToModels(ICollection<FundViewModel> vms) => vms.Select(ToModel).ToList();
    public List<Expense> ToModels(ICollection<ExpenseViewModel> vms) => vms.Select(ToModel).ToList();
    public List<Dreamer> ToModels(ICollection<DreamerViewModel> vms) => vms.Select(ToModel).ToList();
    public List<Tag> ToModels(ICollection<TagViewModel> vms) => vms.Select(ToModel).ToList();
    public List<Cloud> ToModels(ICollection<CloudViewModel> vms) => vms.Select(ToModel).ToList();
    #endregion ToModels
}