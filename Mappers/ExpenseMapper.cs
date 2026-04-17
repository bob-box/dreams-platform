using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public static class ExpenseMapper
{
    public static ExpenseViewModel ToViewModel(this Expense model)
    {
        if (model == null) return null!;
        return new ExpenseViewModel
        {
            ID = model.ID,
            DreamID = model.DreamID,
            Amount = model.Amount,
            Description = model.Description,
            CreatedAt = model.CreatedAt
        };
    }

    public static Expense ToModel(this ExpenseViewModel vm)
    {
        if (vm == null) return null!;
        return new Expense
        {
            ID = vm.ID,
            DreamID = vm.DreamID,
            Amount = vm.Amount,
            Description = vm.Description,
            CreatedAt = vm.CreatedAt
        };
    }
}