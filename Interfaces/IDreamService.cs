using Borderland.Dreams.Enums;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public enum FundDreamServiceStatus
{
    Success,
    InvalidAmount,
    DreamNotFound,
    UserNotFound,
    WalletNotFound,
    InsufficientFunds,
    ExceedsRoundLimit,
    NoActiveFundingRound
}

public record FundDreamServiceResult(
    FundDreamServiceStatus Status,
    string? Message = null,
    FundingModalViewModel? ViewModel = null);

public interface IDreamService
{
    IQueryable<Dream> GetDreamsByCloudIdBaseQuery();
    Task<DreamFilterViewModel> BuildDreamFilterViewModel(
        string? searchText,
        DreamStatus? statusFilter,
        int? tagFilter,
        DreamSortOrder sortOrder,
        string? currentUserId,
        bool isAdmin,
        bool isAuthenticated);

    Task<int> Create(DreamViewModel viewModel);
    Task Update(Dream dreamToUpdate, DreamViewModel viewModel);
    Task<FundDreamServiceResult> GetFundingModal(int dreamId, string userId);
    Task<FundDreamServiceResult> FundDream(int dreamId, decimal amount, string userId);
}