using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class FundingRoundMapper(IMapperFacade mapper, IAppTime appTime)
{
    public FundingRoundViewModel? ToViewModel(FundingRound model)
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
            AllocationPerUser = model.AllocationPerUser,
            TotalFundsAllocated = model.TotalFundsAllocated
        };
    }

    public FundingRound? ToModel(FundingRoundViewModel vm)
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