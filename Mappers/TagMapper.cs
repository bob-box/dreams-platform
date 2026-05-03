using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class TagMapper(IMapperFacade mapper, IAppTime appTime) : IEntityMapper<Tag, TagViewModel>
{
    public TagViewModel? ToViewModel(Tag model)
    {
        if (model == null) return null!;
        return new TagViewModel
        {
            ID = model.ID,
            Title = model.Title,
            CloudID = model.CloudID
        };
    }

    public Tag? ToModel(TagViewModel vm)
    {
        if (vm == null) return null!;
        return new Tag
        {
            ID = vm.ID,
            Title = vm.Title,
            CloudID = vm.CloudID
        };
    }
}