using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public enum FundingRoundServiceStatus
{
    Success,
    NotFound,
    Error
}

public record FundingRoundServiceResult(
    FundingRoundServiceStatus Status,
    FundingRoundViewModel? ViewModel = null,
    string? Message = null,
    Dictionary<string, List<string>>? Errors = null);
    
public interface IFundingRoundService
{
    Task<List<FundingRoundViewModel>> GetAll();
    Task<FundingRoundViewModel> BuildCreateViewModel();
    Task<FundingRoundServiceResult> Create(FundingRoundViewModel viewModel);
    Task<FundingRoundServiceResult> GetForEdit(int id);
    Task<FundingRoundServiceResult> Update(FundingRoundViewModel viewModel);
    Task<FundingRoundServiceResult> GetForDelete(int id);
    Task<FundingRoundServiceResult> Delete(int id);
}