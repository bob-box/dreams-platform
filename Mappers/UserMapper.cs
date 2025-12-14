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
            Email = model.Email
        };
    }

    public static ManageUsersViewModel ToManageUsersViewModel(this User model, string role, int cloudId)
    {
        if (model == null) return null!;
        return new ManageUsersViewModel
        {
            UserId = model.Id,
            UserName = model.UserName,
            Email = model.Email,
            FullName = model.FullName,
            Wallet = model.Wallets.FirstOrDefault(w => w.CloudID == cloudId),
            Role = role
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
            Email = vm.Email
        };
    }
}