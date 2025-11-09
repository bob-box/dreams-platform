using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public static class UserMapper
{
    public static UserViewModel ToViewModel(this User model)
    {
        if (model == null) return null!;
        return new UserViewModel
        {
            Id = model.Id,
            UserName = model.UserName,
            FullName = model.FullName,
            UserType = model.UserType,
            Email = model.Email
        };
    }

    public static User ToModel(this UserViewModel vm)
    {
        if (vm == null) return null!;
        return new User
        {
            Id = vm.Id,
            UserName = vm.UserName,
            FullName = vm.FullName,
            UserType = vm.UserType,
            Email = vm.Email
        };
    }
}