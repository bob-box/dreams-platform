using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public static class FundMapper
{
    public static FundViewModel ToViewModel(this Fund model)
    {
        if (model == null) return null!;
        return new FundViewModel
        {
            ID = model.ID,
            DreamID = model.DreamID,
            UserID = model.UserID,
            FundingRoundID = model.FundingRoundID,
            Amount = model.Amount,
            CreatedDate = model.CreatedDate,
            User = model.User?.ToViewModel()
        };
    }

    public static Fund ToModel(this FundViewModel vm)
    {
        if (vm == null) return null!;
        return new Fund
        {
            ID = vm.ID,
            DreamID = vm.DreamID,
            UserID = vm.UserID,
            FundingRoundID = vm.FundingRoundID,
            Amount = vm.Amount,
            CreatedDate = vm.CreatedDate
        };
    }
}