using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class WalletMapper(MapperFacade mapper, IAppTime appTime)
{
    public WalletViewModel? ToViewModel(Wallet model)
    {
        if (model == null) return null!;
        return new WalletViewModel
        {
            ID = model.ID,
            UserID = model.UserID,
            CloudID = model.CloudID,
            Balance = model.Balance
        };
    }

    public Wallet? ToModel(WalletViewModel vm)
    {
        if (vm == null) return null!;
        return new Wallet
        {
            ID = vm.ID,
            UserID = vm.UserID,
            CloudID = vm.CloudID,
            Balance = vm.Balance
        };
    }
}