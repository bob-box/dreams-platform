using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Borderland.Dreams.Models;

public class User : IdentityUser
{
    [PersonalData]
    public string FullName { get; set; } = string.Empty;
    public List<Wallet> Wallets { get; set; } = new List<Wallet>();
    
    [ProtectedPersonalData]
    [Required]
    public override string? UserName
    {
        get => base.UserName!;
        set => base.UserName = value;
    }
}
