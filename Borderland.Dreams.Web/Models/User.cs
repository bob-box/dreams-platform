using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Borderland.Dreams.Models;

public class User : IdentityUser, IAutoPopulateCreatedAtOnAdd
{       
    [DataType(DataType.DateTime)]
    public DateTime CreatedAtUtc { get; set; }

    [PersonalData]
    public string Name { get; set; } = string.Empty;
    public ICollection<Wallet> Wallets { get; set; } = [];
}
