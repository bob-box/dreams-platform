using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class FundMapper(MapperFacade mapper, IAppTime appTime)
{
    public FundViewModel ToViewModel(Fund model)
    {
        if (model == null) return null!;
        return new FundViewModel
        {
            ID = model.ID,
            DreamID = model.DreamID,
            UserID = model.UserID,
            FundingRoundID = model.FundingRoundID,
            Amount = model.Amount,
            CreatedAt = model.CreatedAt,
            User = mapper.ToViewModel(model.User)
        };
    }

    public Fund ToModel(FundViewModel vm)
    {
        if (vm == null) return null!;
        return new Fund
        {
            ID = vm.ID,
            DreamID = vm.DreamID,
            UserID = vm.UserID,
            FundingRoundID = vm.FundingRoundID,
            Amount = vm.Amount,
        };
    }
}