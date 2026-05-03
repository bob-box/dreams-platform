using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class UserMapper(IMapperFacade mapper, IAppTime appTime)
{
    public UserViewModel? ToViewModel(User model)
    {
        if (model == null) return null!;
        return new UserViewModel
        {
            Id = model.Id,
            Name = model.Name,
            Email = model.Email,
            Wallets = mapper.ToViewModels(model.Wallets),
        };
    }
}