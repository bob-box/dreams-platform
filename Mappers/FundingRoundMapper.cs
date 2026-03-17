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
            CloudID = model.CloudID,
            Title = model.Title,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            MaxPercentagePerDream = model.MaxPercentagePerDream,
            AllocationPerUser = model.AllocationPerUser
        };
    }

    public static FundingRound ToModel(this FundingRoundViewModel vm)
    {
        if (vm == null) return null!;
        return new FundingRound
        {
            ID = vm.ID,
            CloudID = vm.CloudID,
            Title = vm.Title,
            StartDate = vm.StartDate,
            EndDate = vm.EndDate,
            MaxPercentagePerDream = vm.MaxPercentagePerDream,
            AllocationPerUser = vm.AllocationPerUser
        };
    }
}