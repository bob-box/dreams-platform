namespace Borderland.Dreams.ViewModels;

public class UserViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public ICollection<WalletViewModel> Wallets { get; set; }
    public string Role { get; set; }
}