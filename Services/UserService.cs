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
    private static readonly string[] ManageableRoles = [Constants.MemberRole, Constants.AdminRole];

    public async Task<IEnumerable<ManageUsersViewModel>> GetUsersInCloud(string filter = "")
    {
        int cloudId = cloudNameResolver.CloudID;

        var joinedCloudClaim = new Claim(Constants.JOINED_CLOUD_CLAIM, cloudId.ToString());
        var usersInCloud = await userManager.GetUsersForClaimAsync(joinedCloudClaim);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            usersInCloud = usersInCloud
            .Where(x =>
                x.Name?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) == true ||
                x.Email?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) == true)
            .ToList();
        }

        var list = new List<ManageUsersViewModel>(usersInCloud.Count);
        foreach (var user in usersInCloud)
        {
            var roles = await userManager.GetRolesAsync(user);
            list.Add(user.ToManageUsersViewModel(roles.FirstOrDefault() ?? Constants.MemberRole, cloudId));
        }

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
            var user = await userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");
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

    public async Task UpdateRole(string userId, string currentUserId, string role)
    {
        if (!ManageableRoles.Contains(role, StringComparer.Ordinal))
            throw new ArgumentException("Invalid role.");

        if (string.Equals(userId, currentUserId, StringComparison.Ordinal))
            throw new InvalidOperationException("You cannot change your own role from this page.");

        int cloudId = cloudNameResolver.CloudID;
        var joinedCloudClaim = new Claim(Constants.JOINED_CLOUD_CLAIM, cloudId.ToString());
        var user = await userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");

        var userClaims = await userManager.GetClaimsAsync(user);
        var hasJoinedCurrentCloud = userClaims.Any(claim =>
            claim.Type == joinedCloudClaim.Type &&
            claim.Value == joinedCloudClaim.Value);

        if (!hasJoinedCurrentCloud)
            throw new InvalidOperationException("This user is not part of the current cloud.");

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Contains(role, StringComparer.Ordinal))
            return;

        var rolesToRemove = currentRoles
            .Where(currentRole => ManageableRoles.Contains(currentRole, StringComparer.Ordinal))
            .ToList();

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
                throw new InvalidOperationException(string.Join(", ", removeResult.Errors.Select(error => error.Description)));
        }

        var addResult = await userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
            throw new InvalidOperationException(string.Join(", ", addResult.Errors.Select(error => error.Description)));
    }
}
