using Borderland.Dreams.Extensions;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public static class CloudMapper
{
    public static CloudViewModel ToViewModel(this Cloud model)
    {
        if (model == null) return null!;
        return new CloudViewModel
        {
            ID = model.ID,
            Name = model.Name,
            CreatedAt = model.CreatedAt,
            TotalFundsAvailable = model.TotalFundsAvailable,
            TotalFundsSpent = model.TotalFundsSpent,
            Dreams = model.Dreams.MapList(x => x.ToViewModel()),
            FundingRounds = model.FundingRounds.MapList(x => x.ToViewModel())
        };
    }

    public static Cloud ToModel(this CloudViewModel vm)
    {
        if (vm == null) return null!;
        return new Cloud
        {
            ID = vm.ID,
            Name = vm.Name,
            CreatedAt = vm.CreatedAt,
            TotalFundsAvailable = vm.TotalFundsAvailable,
            TotalFundsSpent = vm.TotalFundsSpent,
            Dreams = vm.Dreams.MapList(x => x.ToModel()),
            FundingRounds  = vm.FundingRounds.MapList(x => x.ToModel())
        };
    }
}