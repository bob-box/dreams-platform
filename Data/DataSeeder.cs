using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Services;

namespace Borderland.Dreams.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        await AddRolesAsync(serviceProvider);
        string adminId = await AddAdminUserAsync(serviceProvider);
        string userId = await AddUserAsync(serviceProvider);
        int cloudId = await AddTestCloudAsync(serviceProvider);
        await AddUsersToCloudAsync(serviceProvider, cloudId, adminId, userId);
        var testTags = await AddTestTagsAsync(serviceProvider, cloudId);
        var firstTag = testTags.First();
        await AddTestDreamAsync(serviceProvider, userId, cloudId, firstTag);
        //await AddTestFundingRoundAsync(serviceProvider);
    }

    private static async Task AddRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var roles = new[] { Constants.AdminRole, Constants.MemberRole };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task<string> AddAdminUserAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        var adminEmail = "admin@dreams.com";
        var adminPassword = "password1";

        var userExist = await userManager.FindByEmailAsync(adminEmail);
        if (userExist == null)
        {
            var adminUser = new User
            {
                UserName = "admin",
                FullName = "Admin User",
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Constants.AdminRole);
                return adminUser.Id;
            }
            else
            {
                throw new Exception("Failed to create the admin user: " + string.Join(", ", result.Errors));
            }
        }

        return userExist.Id;
    }

    private static async Task<string> AddUserAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var userEmail = "user@dreams.com";
        var userPassword = "password1";
        var userExist = await userManager.FindByEmailAsync(userEmail);

        if (userExist == null)
        {
            var user = new User
            {
                UserName = "user",
                FullName = "Test User",
                Email = userEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, userPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, Constants.MemberRole);
            }
            else
            {
                throw new Exception("Failed to create the user: " + string.Join(", ", result.Errors));
            }
            return user.Id;
        }
        return userExist.Id;
    }

    public static async Task<int> AddTestCloudAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var cloudExists = await context.Cloud.SingleOrDefaultAsync(x => x.Name == "test");
        if (cloudExists == null)
        {
            var cloud = new Cloud { Name = "test" };
            context.Cloud.Add(cloud);
            await context.SaveChangesAsync();
            return cloud.ID;
        }

        return cloudExists.ID;
    }

    private static async Task AddUsersToCloudAsync(IServiceProvider serviceProvider, int cloudId, params string[] userIds)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var joinedCloudClaim = new Claim(Constants.JOINED_CLOUD_CLAIM, cloudId.ToString());

        foreach (var userId in userIds.Distinct())
        {
            var user = await userManager.FindByIdAsync(userId) ?? throw new Exception($"Failed to find user with id '{userId}'.");
            var claims = await userManager.GetClaimsAsync(user);
            var alreadyJoined = claims.Any(claim =>
                claim.Type == joinedCloudClaim.Type &&
                claim.Value == joinedCloudClaim.Value);

            if (!alreadyJoined)
            {
                var result = await userManager.AddClaimAsync(user, joinedCloudClaim);
                user.Wallets.Add(new Wallet(cloudId));
                await context.SaveChangesAsync();
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to add joined cloud claim: " + string.Join(", ", result.Errors.Select(error => error.Description)));
                }
            }
        }
    }

    private static async Task<List<Tag>> AddTestTagsAsync(IServiceProvider serviceProvider, int cloudId)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var testTagTitles = new[] { "test tag 1", "test tag 2" };

        var existingTags = await context.Tag
            .Where(tag => tag.CloudID == cloudId && testTagTitles.Contains(tag.Title))
            .ToListAsync();

        var existingTitles = existingTags
            .Select(tag => tag.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newTags = testTagTitles
            .Where(title => !existingTitles.Contains(title))
            .Select(title => new Tag { CloudID = cloudId, Title = title })
            .ToList();

        if (newTags.Count > 0)
        {
            await context.Tag.AddRangeAsync(newTags);
            await context.SaveChangesAsync();
        }

        return existingTags
            .Concat(newTags)        // combine existing + newly inserted in memory
            .OrderBy(tag => tag.Title)
            .ToList();
    }

    private static async Task AddTestDreamAsync(IServiceProvider serviceProvider, string userId, int cloudId, Tag tag)
    {
        var dreamName = "Test Dream";
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        var dream = await context.Dream
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.Name == dreamName && x.CloudID == cloudId);

        if (dream == null)
        {
            context.Dream.Add(new Dream
            {
                CloudID = cloudId,
                Name = dreamName,
                Description = "This is a test dream used for development and testing purposes.",
                CreatedDate = DateTime.UtcNow,
                Tags = [tag],
                Dreamers =
                [
                    new Dreamer { UserID = userId, IsMainDreamer = true }
                ]
            });
            await context.SaveChangesAsync();
            return;
        }

        if (!dream.Tags.Any(x => x.ID == tag.ID))
        {
            dream.Tags.Add(tag);
            await context.SaveChangesAsync();
        }
    }

    public static async Task AddTestFundingRoundAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var schedulerService = serviceProvider.GetRequiredService<FundingRoundSchedulerService>();

        var cloud = await context.Cloud.SingleOrDefaultAsync(x => x.Name == "test");
        if (cloud != null)
        {
            var existingRound = await context.FundingRound
                .SingleOrDefaultAsync(fr => fr.CloudID == cloud.ID && fr.Title == "Test Round");
            if (existingRound == null)
            {
                var fundingRound = new FundingRound
                {
                    CloudID = cloud.ID,
                    Title = "Test Round",
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    MaxPercentagePerDream = 10m, // 10% of per-user allocation
                    AllocationPerUser = 100m // each user receives 100 units
                };
                context.FundingRound.Add(fundingRound);
                await context.SaveChangesAsync();

                // Schedule Hangfire jobs for this funding round and save job IDs
                schedulerService.ScheduleFundingRoundJobs(fundingRound);
                await context.SaveChangesAsync();
            }
        }
    }
}