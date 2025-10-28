using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Services.Mappers;

public static class WalletMapper
{
    public static WalletViewModel ToViewModel(this Wallet model)
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

    public static Wallet ToModel(this WalletViewModel vm)
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