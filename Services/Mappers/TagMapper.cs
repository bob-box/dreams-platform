using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Services.Mappers;

public static class TagMapper
{
    public static TagViewModel ToViewModel(this Tag model)
    {
        if (model == null) return null!;
        return new TagViewModel
        {
            ID = model.ID,
            Title = model.Title
        };
    }

    public static Tag ToModel(this TagViewModel vm)
    {
        if (vm == null) return null!;
        return new Tag
        {
            ID = vm.ID,
            Title = vm.Title
        };
    }
}