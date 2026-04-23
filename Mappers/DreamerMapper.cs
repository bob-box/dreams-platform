using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class DreamerMapper(MapperFacade mapper, IAppTime appTime)
{
    public DreamerViewModel? ToViewModel(Dreamer model)
    {
        if (model == null) return null!;
        return new DreamerViewModel
        {
            ID = model.ID,
            DreamID = model.DreamID,
            UserID = model.UserID,
            IsMainDreamer = model.IsMainDreamer,
            Name = model.User.Name,
        };
    }

    public Dreamer? ToModel(DreamerViewModel vm)
    {
        if (vm == null) return null!;
        return new Dreamer
        {
            ID = vm.ID,
            DreamID = vm.DreamID,
            UserID = vm.UserID,
            IsMainDreamer = vm.IsMainDreamer,
        };
    }
}