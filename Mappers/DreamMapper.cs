using Borderland.Dreams.Extensions;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public static class DreamMapper
{
    public static DreamViewModel ToViewModel(this Dream model)
    {
        if (model == null) return null!;
        return new DreamViewModel
        {
            ID = model.ID,
            Name = model.Name,
            Description = model.Description,
            CreatedAt = model.CreatedAt,
            Status = model.Status,
            BudgetItems = model.BudgetItems.MapList(x => x.ToViewModel()),
            CloudID = model.CloudID,
            Funds = model.Funds.MapList(x => x.ToViewModel()),
            Comments = model.Comments.MapList(x => x.ToViewModel()),
            Expenses = model.Expenses.MapList(x => x.ToViewModel()),
            Tags = model.Tags.MapList(x => x.ToViewModel()),
            Dreamers = model.Dreamers.MapList(x => x.ToViewModel())
        };
    }

    public static Dream ToModel(this DreamViewModel vm)
    {
        if (vm == null) return null!;
        return new Dream
        {
            ID = vm.ID,
            Name = vm.Name,
            Description = vm.Description,
            CreatedAt = vm.CreatedAt,
            Status = vm.Status,
            BudgetItems = vm.BudgetItems.MapList(x => x.ToModel()),
            CloudID = vm.CloudID,
            Funds = vm.Funds.MapList(x => x.ToModel()),
            Comments = vm.Comments.MapList(x => x.ToModel()),
            Expenses = vm.Expenses.MapList(x => x.ToModel()),
            Tags = vm.Tags.MapList(x => x.ToModel()),
            Dreamers = vm.Dreamers.MapList(x => x.ToModel())
        };
    }
}