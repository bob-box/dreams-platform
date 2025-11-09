using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public static class FundingRoundMapper
{
    public static FundingRoundViewModel ToViewModel(this FundingRound model)
    {
        if (model == null) return null!;
        return new FundingRoundViewModel
        {
            ID = model.ID,
            Title = model.Title,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            AmountToSpendPerUser = model.AmountToSpendPerUser,
            MaxAmountPerDreamPerUser = model.MaxAmountPerDreamPerUser
        };
    }

    public static FundingRound ToModel(this FundingRoundViewModel vm)
    {
        if (vm == null) return null!;
        return new FundingRound
        {
            ID = vm.ID,
            Title = vm.Title,
            StartDate = vm.StartDate,
            EndDate = vm.EndDate,
            AmountToSpendPerUser = vm.AmountToSpendPerUser,
            MaxAmountPerDreamPerUser = vm.MaxAmountPerDreamPerUser
        };
    }
}