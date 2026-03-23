using System.Security.Claims;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Borderland.Dreams.Services;

public class UserService(
    ApplicationDbContext _context,
    ICloudNameResolver cloudNameResolver,
    UserManager<User> userManager)
{
    public async Task<IEnumerable<ManageUsersViewModel>> GetUsersInCloud(string filter = "")
    {
        int cloudId = cloudNameResolver.CloudID;

        var joinedCloudClaim = new Claim(Constants.JOINED_CLOUD_CLAIM, cloudId.ToString());
        var usersInCloud = await userManager.GetUsersForClaimAsync(joinedCloudClaim);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            usersInCloud = usersInCloud
            .Where(x =>
                x.UserName.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
                x.FullName.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
                x.Email.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        }
        var list = usersInCloud.Select(user => user.ToManageUsersViewModel(userManager.GetRolesAsync(user).Result.FirstOrDefault(), cloudId));

        return list;
    }

    public async Task<int> CountUsersInCloud()
    {
        int cloudId = cloudNameResolver.CloudID;

        var joinedCloudClaim = new Claim(Constants.JOINED_CLOUD_CLAIM, cloudId.ToString());
        var usersInCloud = await userManager.GetUsersForClaimAsync(joinedCloudClaim);
        return usersInCloud.Count;
    }

    public async Task UpdateWallet(string userId, WalletAction action, int amount)
    {
        int cloudId = cloudNameResolver.CloudID;
        IList<User> users = [];

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await userManager.FindByIdAsync(userId);
            users.Add(user);
        }
        else
        {
            var claim = new Claim(Constants.JOINED_CLOUD_CLAIM, cloudId.ToString());
            users = await userManager.GetUsersForClaimAsync(claim);
        }

        foreach (var user in users)
        {
            var wallet = user.Wallets.FirstOrDefault(w => w.CloudID == cloudId);
            if (wallet == null) return;

            switch (action)
            {
                case WalletAction.Set:
                    wallet.Balance = amount;
                    break;
                case WalletAction.Add:
                    wallet.Balance += amount;
                    break;
                default:
                    throw new ArgumentException("Invalid action");
            }
        }

        await _context.SaveChangesAsync();
        return;
    }
}
