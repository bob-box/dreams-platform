using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class ExpenseMapper(IAppTime appTime)
{
    public ExpenseViewModel ToViewModel(Expense model)
    {
        return new ExpenseViewModel
        {
            ID = model.ID,
            DreamID = model.DreamID,
            Amount = model.Amount,
            Description = model.Description,
            CreatedAt = appTime.ToLocal(model.CreatedAtUtc)
        };
    }

    public Expense ToModel(ExpenseViewModel vm)
    {
        return new Expense
        {
            ID = vm.ID,
            DreamID = vm.DreamID,
            Amount = vm.Amount,
            Description = vm.Description,
        };
    }
}