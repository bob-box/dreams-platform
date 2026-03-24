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
            Name = model.Name,
            Email = model.Email
        };
    }

    public static ManageUsersViewModel ToManageUsersViewModel(this User model, string role, int cloudId)
    {
        if (model == null) return null!;
        return new ManageUsersViewModel
        {
            UserId = model.Id,
            Email = model.Email,
            Name = model.Name,
            Wallet = model.Wallets.FirstOrDefault(w => w.CloudID == cloudId),
            Role = role
        };
    }
}