using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class ExpenseMapper(MapperFacade mapper, IAppTime appTime)
{
    public ExpenseViewModel ToViewModel(Expense model)
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

    public Expense ToModel(ExpenseViewModel vm)
    {
        if (vm == null) return null!;
        return new Expense
        {
            ID = vm.ID,
            DreamID = vm.DreamID,
            Amount = vm.Amount,
            Description = vm.Description,
        };
    }
}