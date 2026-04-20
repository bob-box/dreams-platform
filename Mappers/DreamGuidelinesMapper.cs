using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class DreamGuidelinesMapper(MapperFacade mapper, IAppTime appTime)
{
    public DreamGuidelinesViewModel ToViewModel(DreamGuidelines model)
    {
        if (model == null) return null!;
        return new DreamGuidelinesViewModel
        {
            ID = model.ID,
            Title = model.Title,
            Text = model.Text
        };
    }

    public DreamGuidelines ToModel(DreamGuidelinesViewModel vm)
    {
        if (vm == null) return null!;
        return new DreamGuidelines
        {
            ID = vm.ID,
            Title = vm.Title,
            Text = vm.Text
        };
    }
}