using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class CloudMapper(IMapperFacade mapper, IAppTime appTime)
{
    public CloudViewModel ToViewModel(Cloud model)
    {
        return new CloudViewModel
        {
            ID = model.ID,
            Name = model.Name,
            CreatedAt = model.CreatedAt,
            TotalFundsAvailable = model.TotalFundsAvailable,
            TotalFundsSpent = model.TotalFundsSpent,
            Dreams = mapper.ToViewModels(model.Dreams),
            FundingRounds = mapper.ToViewModels(model.FundingRounds)
        };
    }

    public Cloud ToModel(CloudViewModel vm)
    {
        return new Cloud
        {
            ID = vm.ID,
            Name = vm.Name,
            TotalFundsAvailable = vm.TotalFundsAvailable,
            TotalFundsSpent = vm.TotalFundsSpent,
            Dreams = mapper.ToModels(vm.Dreams),
            FundingRounds  = mapper.ToModels(vm.FundingRounds)
        };
    }
}