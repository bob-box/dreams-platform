using System.Security.Claims;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class CloudService(
    ApplicationDbContext context,
    ICloudNameResolver cloudNameService,
    UserManager<User> userManager,
    SignInManager<User> signInManager) : ICloudService
{
    public async Task<CloudViewModel?> GetById(int id)
    {
        var cloud = await context.Cloud.FindAsync(id);
        return cloud?.ToViewModel();
    }

    public async Task<List<CloudViewModel>> GetAll()
    {
        var clouds = await context.Cloud
            .Include(c => c.Dreams)
            .ToListAsync();

        return clouds.Select(c => c.ToViewModel()).ToList();
    }

    public async Task<JoinCloudResult> Join(int cloudId, string userId)
    {
        var cloud = await context.Cloud.FindAsync(cloudId);
        if (cloud == null)
            return new JoinCloudResult(JoinCloudStatus.CloudNotFound);

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return new JoinCloudResult(JoinCloudStatus.UserNotFound);

        var userClaims = await userManager.GetClaimsAsync(user);
        var claim = new Claim(Constants.JOINED_CLOUD_CLAIM, cloudId.ToString());
        var hasAlreadyJoined = userClaims.Any(c => c.Type == claim.Type && c.Value == claim.Value);

        if (hasAlreadyJoined)
            return new JoinCloudResult(JoinCloudStatus.AlreadyJoined);

        await userManager.AddClaimAsync(user, claim);
        await signInManager.RefreshSignInAsync(user);

        user.Wallets.Add(new Wallet(){ CloudID = cloudId });
        await context.SaveChangesAsync();

        return new JoinCloudResult(JoinCloudStatus.Success);
    }

    public async Task<bool> UserHasJoinedCloud(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        var userClaims = await userManager.GetClaimsAsync(user);
        var cloudId = cloudNameService.CloudID.ToString();
        return userClaims.Any(c =>
            c.Type == Constants.JOINED_CLOUD_CLAIM &&
            c.Value == cloudId);
    }
}