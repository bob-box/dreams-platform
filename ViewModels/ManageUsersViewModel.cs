using Borderland.Dreams.Models;

namespace Borderland.Dreams.ViewModels;

public class ManageUsersViewModel
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public Wallet Wallet { get; set; }
    public string Role { get; set; }
}
