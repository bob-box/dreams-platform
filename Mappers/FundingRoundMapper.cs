using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class FundingRoundMapper(IMapperFacade mapper, IAppTime appTime)
{
    public FundingRoundViewModel ToViewModel(FundingRound model)
    {
        return new FundingRoundViewModel
        {
            ID = model.ID,
            CloudID = model.CloudID,
            Title = model.Title,
            StartDate = appTime.ToLocal(model.StartDate),
            EndDate = appTime.ToLocal(model.EndDate),
            IsLastRound = model.IsLastRound,
            MaxPercentagePerDream = model.MaxPercentagePerDream,
            AllocationPerUser = model.AllocationPerUser,
            TotalFundsAllocated = model.TotalFundsAllocated
        };
    }

    public FundingRound ToModel(FundingRoundViewModel vm)
    {
        return new FundingRound
        {
            ID = vm.ID,
            CloudID = vm.CloudID,
            Title = vm.Title,
            StartDate = appTime.ToUtc(vm.StartDate),
            EndDate = appTime.ToUtc(vm.EndDate),
            IsLastRound = vm.IsLastRound,
            MaxPercentagePerDream = vm.MaxPercentagePerDream,
            AllocationPerUser = vm.AllocationPerUser
        };
    }
}