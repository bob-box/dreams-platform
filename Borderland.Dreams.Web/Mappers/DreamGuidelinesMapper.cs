using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class DreamGuidelinesMapper()
{
    public DreamGuidelinesViewModel ToViewModel(DreamGuidelines model)
    {
        return new DreamGuidelinesViewModel
        {
            ID = model.ID,
            Title = model.Title,
            Text = model.Text
        };
    }

    public DreamGuidelines ToModel(DreamGuidelinesViewModel vm)
    {
        return new DreamGuidelines
        {
            ID = vm.ID,
            Title = vm.Title,
            Text = vm.Text
        };
    }
}