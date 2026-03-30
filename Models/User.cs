using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Borderland.Dreams.Models;

public class User : IdentityUser
{
    [PersonalData]
    public string Name { get; set; } = string.Empty;
    public ICollection<Wallet> Wallets { get; set; } = [];
}
