using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class TagMapper()
{
    public TagViewModel ToViewModel(Tag model)
    {
        return new TagViewModel
        {
            ID = model.ID,
            Title = model.Title,
            CloudID = model.CloudID
        };
    }

    public Tag ToModel(TagViewModel vm)
    {
        return new Tag
        {
            ID = vm.ID,
            Title = vm.Title,
            CloudID = vm.CloudID
        };
    }
}