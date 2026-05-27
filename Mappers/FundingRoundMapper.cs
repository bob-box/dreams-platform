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
            StartDate = appTime.ToUtc(model.StartDate),
            EndDate = appTime.ToUtc(model.EndDate),
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
            StartDate = appTime.ToLocal(vm.StartDate),
            EndDate = appTime.ToLocal(vm.EndDate),
            MaxPercentagePerDream = vm.MaxPercentagePerDream,
            AllocationPerUser = vm.AllocationPerUser
        };
    }
}