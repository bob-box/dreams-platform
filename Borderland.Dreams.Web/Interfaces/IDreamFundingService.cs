using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public enum FundDreamServiceStatus
{
    Success,
    InvalidAmount,
    CantFundDream,
    WalletNotFound,
    InsufficientFunds,
    ExceedsRoundLimit,
    NoActiveFundingRound
}

public record FundDreamServiceResult(
    FundDreamServiceStatus Status,
    string? Message = null,
    FundingModalViewModel? ViewModel = null);

public interface IDreamFundingService
{
    Task<FundDreamServiceResult> GetFundingModal(int dreamId, string userId);
    Task<FundDreamServiceResult> FundDream(int dreamId, decimal amount, string userId);
}
