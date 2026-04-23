using Borderland.Dreams.Extensions;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class DreamMapper(MapperFacade mapper, IAppTime appTime)
{
    public DreamViewModel? ToViewModel(Dream model)
    {
        if (model == null) return null!;
        return new DreamViewModel
        {
            ID = model.ID,
            Name = model.Name,
            Description = model.Description,
            CreatedAt = appTime.ToLocal(model.CreatedAt),
            Status = model.Status,
            BudgetItems = mapper.ToViewModels(model.BudgetItems),
            CloudID = model.CloudID,
            Funds = mapper.ToViewModels(model.Funds),
            Comments = mapper.ToViewModels(model.Comments),
            Expenses = mapper.ToViewModels(model.Expenses),
            Tags = mapper.ToViewModels(model.Tags),
            Dreamers = mapper.ToViewModels(model.Dreamers)
        };
    }

    public Dream? ToModel(DreamViewModel vm)
    {
        if (vm == null) return null!;
        return new Dream
        {
            ID = vm.ID,
            Name = vm.Name,
            Description = vm.Description,
            Status = vm.Status,
            BudgetItems = mapper.ToModels(vm.BudgetItems),
            CloudID = vm.CloudID,
            Funds = mapper.ToModels(vm.Funds),
            Comments = mapper.ToModels(vm.Comments),
            Expenses = mapper.ToModels(vm.Expenses),
            Tags = mapper.ToModels(vm.Tags),
            Dreamers = mapper.ToModels(vm.Dreamers)
        };
    }
}