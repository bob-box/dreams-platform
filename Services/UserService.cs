using System.Security.Claims;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Borderland.Dreams.Services;

public class UserService(
    ILogger<UserService> logger,
    ApplicationDbContext _context,
    ICloudNameResolver cloudNameResolver,
    UserManager<User> userManager,
    IMapperFacade mapper
    ) : IUserService
{
    private static readonly string[] ManageableRoles = [Constants.AdminRole, Constants.MemberRole];

    public async Task<IEnumerable<UserViewModel>> GetAll(string filter = "")
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

        var roleLookup = await GetRoles(usersInCloud);

        var list = usersInCloud
            .Select(user =>
            {
                var vm = mapper.ToViewModel(user);
                vm.Role = roleLookup.TryGetValue(user.Id, out var role) ? role : Constants.MemberRole;
                return vm;
            })
            .ToList();

        return list;
    }

    private async Task<Dictionary<string, string>> GetRoles(IList<User> usersInCloud)
    {
        var roleLookup = new Dictionary<string, string>(); // userId → role
        var usersInCloudIds = usersInCloud.Select(u => u.Id).ToHashSet();

        foreach (var role in ManageableRoles)
        {
            var usersInRole = (await userManager.GetUsersInRoleAsync(role)).Where(u => usersInCloudIds.Contains(u.Id));
            foreach (var user in usersInRole)
            {
                if (!roleLookup.TryAdd(user.Id, role))
                {
                    // User is in multiple roles — log a warning, this shouldn't happen
                    logger.LogWarning("User {UserId} is assigned multiple roles, that's not expected", user.Id);
                }
            }
        }

        return roleLookup;
    }

    public async Task<int> Count()
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
            var wallet = user.Wallets.SingleOrDefault(w => w.CloudID == cloudId);
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
