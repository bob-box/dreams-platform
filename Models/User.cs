using Borderland.Dreams.Enums;
using Microsoft.AspNetCore.Identity;

namespace Borderland.Dreams.Models;

public class User : IdentityUser
{
    [PersonalData]
    public string FullName { get; set; } = string.Empty;
    public int? WalletID { get; set; }
    public Wallet? Wallet { get; set; }
    public UserType UserType { get; set; } = UserType.User;
}
