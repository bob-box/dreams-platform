using System.Security.Claims;
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

public enum DreamDeleteStatus
{
    Success,
    NotFound
}

public record DreamDeleteResult(DreamDeleteStatus Status, Dream? Dream = null);

public interface IDreamService
{
    IQueryable<Dream> GetDreamsByCloudIdBaseQuery();
    Task<DreamFilterViewModel> BuildDreamFilterViewModel(
        string? searchText,
        DreamStatus? statusFilter,
        int? tagFilter,
        DreamSortOrder sortOrder,
        string? currentUserId,
        bool isAdmin);

    Task<int> Create(DreamViewModel viewModel);
    Task Update(Dream dreamToUpdate, DreamViewModel viewModel);
    Task<DreamDeleteResult> GetForDelete(int id);
    Task<DreamDeleteResult> Delete(int id);
    Task<FundDreamServiceResult> GetFundingModal(int dreamId, string userId);
    Task<FundDreamServiceResult> FundDream(int dreamId, decimal amount, string userId);
    bool DreamExists(int iD, int cloudID);
    Task<bool> CheckIfUserCanEditDreamers(IEnumerable<IDreamersForAuthorization> dreamers, ClaimsPrincipal user);
}