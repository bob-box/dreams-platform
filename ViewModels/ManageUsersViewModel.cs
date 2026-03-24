using Borderland.Dreams.Models;

namespace Borderland.Dreams.ViewModels;

public class ManageUsersViewModel
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public Wallet Wallet { get; set; }
    public string Role { get; set; }
}
