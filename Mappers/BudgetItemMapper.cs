using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class BudgetItemMapper(IMapperFacade mapper, IAppTime appTime)
{
    public  BudgetItemViewModel ToViewModel(BudgetItem model)
    {
        return new BudgetItemViewModel
        {
            ID = model.ID,
            Name = model.Name,
            Type = model.Type,
            DreamID = model.DreamID,
            CreatedAt = appTime.ToLocal(model.CreatedAt),
            MinAmount = model.MinAmount,
            MaxAmount = model.MaxAmount,
        };
    }

    public  BudgetItem ToModel(BudgetItemViewModel vm)
    {
        return new BudgetItem
        {
            ID = vm.ID,
            Name = vm.Name,
            Type = vm.Type,
            DreamID = vm.DreamID,
            MinAmount = vm.MinAmount ?? 0,
            MaxAmount = vm.MaxAmount,
        };
    }
}