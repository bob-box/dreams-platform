using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Services.Mappers;

public static class DreamerMapper
{
    public static DreamerViewModel ToViewModel(this Dreamer model)
    {
        if (model == null) return null!;
        return new DreamerViewModel
        {
            ID = model.ID,
            DreamID = model.DreamID,
            UserID = model.UserID,
            IsMainDreamer = model.IsMainDreamer,
            User = model.User?.ToViewModel()
        };
    }

    public static Dreamer ToModel(this DreamerViewModel vm)
    {
        if (vm == null) return null!;
        return new Dreamer
        {
            ID = vm.ID,
            DreamID = vm.DreamID,
            UserID = vm.UserID,
            IsMainDreamer = vm.IsMainDreamer,
            User = vm.User?.ToModel()
        };
    }
}