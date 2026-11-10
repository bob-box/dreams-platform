using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Ganss.Xss;

namespace Borderland.Dreams.Mappers;

public class DreamMapper(IMapperFacade mapper, IAppTime appTime, HtmlSanitizer htmlSanitizer)
{
    public DreamViewModel ToViewModel(Dream model)
    {
        return new DreamViewModel
        {
            ID = model.ID,
            Name = model.Name,
            Description = htmlSanitizer.Sanitize(model.Description),
            CreatedAt = appTime.ToLocal(model.CreatedAtUtc),
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

    public Dream ToModel(DreamViewModel vm)
    {
        return new Dream
        {
            ID = vm.ID,
            Name = vm.Name,
            Description = htmlSanitizer.Sanitize(vm.Description),
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