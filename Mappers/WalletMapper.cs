using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class WalletMapper(IMapperFacade mapper, IAppTime appTime)
{
    public WalletViewModel ToViewModel(Wallet model)
    {
        return new WalletViewModel
        {
            ID = model.ID,
            UserID = model.UserID,
            CloudID = model.CloudID,
            Balance = model.Balance
        };
    }

    public Wallet ToModel(WalletViewModel vm)
    {
        return new Wallet
        {
            ID = vm.ID,
            UserID = vm.UserID,
            CloudID = vm.CloudID,
            Balance = vm.Balance
        };
    }
}