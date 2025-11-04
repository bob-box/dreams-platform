using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Services.Mappers;

public static class DreamGuidelinesMapper
{
    public static DreamGuidelinesViewModel ToViewModel(this DreamGuidelines model)
    {
        if (model == null) return null!;
        return new DreamGuidelinesViewModel
        {
            ID = model.ID,
            Title = model.Title,
            Text = model.Text
        };
    }

    public static DreamGuidelines ToModel(this DreamGuidelinesViewModel vm)
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