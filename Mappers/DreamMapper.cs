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
            CreatedDate = model.CreatedDate,
            Status = model.Status,
            BudgetItems = model.BudgetItems?.Select(x => x.ToViewModel()).ToList() ?? [],
            CloudID = model.CloudID,
            Funds = model.Funds?.Select(x => x.ToViewModel()).ToList() ?? [],
            Comments = model.Comments?.Select(x => x.ToViewModel()).ToList() ?? [],
            Expenses = model.Expenses?.Select(x => x.ToViewModel()).ToList() ?? [],
            Tags = model.Tags?.Select(x => x.ToViewModel()).ToList() ?? [],
            Dreamers = model.Dreamers?.Select(x => x.ToViewModel()).ToList() ?? []
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
            CreatedDate = vm.CreatedDate,
            Status = vm.Status,
            BudgetItems = vm.BudgetItems?.Select(x => x.ToModel()).ToList() ?? [],
            CloudID = vm.CloudID,
            Funds = vm.Funds?.Select(x => x.ToModel()).ToList() ?? [],
            Comments = vm.Comments?.Select(x => x.ToModel()).ToList() ?? [],
            Expenses = vm.Expenses?.Select(x => x.ToModel()).ToList() ?? [],
            Tags = vm.Tags?.Select(x => x.ToModel()).ToList() ?? [],
            Dreamers = vm.Dreamers?.Select(x => x.ToModel()).ToList() ?? []
        };
    }
}