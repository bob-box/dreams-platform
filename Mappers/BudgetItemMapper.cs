using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public static class BudgetItemMapper
{
    public static BudgetItemViewModel ToViewModel(this BudgetItem model)
    {
        if (model == null) return null!;
        return new BudgetItemViewModel
        {
            ID = model.ID,
            Name = model.Name,
            Type = model.Type,
            DreamID = model.DreamID,
            CreatedDate = model.CreatedDate,
            MinAmount = model.MinAmount,
            MaxAmount = model.MaxAmount,
        };
    }

    public static BudgetItem ToModel(this BudgetItemViewModel vm)
    {
        if (vm == null) return null!;
        return new BudgetItem
        {
            ID = vm.ID,
            Name = vm.Name,
            Type = vm.Type,
            DreamID = vm.DreamID,
            CreatedDate = vm.CreatedDate,
            MinAmount = vm.MinAmount,
            MaxAmount = vm.MaxAmount,
        };
    }
}