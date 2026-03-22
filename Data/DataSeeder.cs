using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Services;

namespace Borderland.Dreams.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        await AddRolesAsync(serviceProvider);
        await AddAdminUserAsync(serviceProvider);
        await AddUserAsync(serviceProvider);
        await AddTestCloudAsync(serviceProvider);
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

    private static async Task AddAdminUserAsync(IServiceProvider serviceProvider)
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
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Constants.AdminRole);
            }
            else
            {
                throw new Exception("Failed to create the admin user: " + string.Join(", ", result.Errors));
            }
        }
    }

    private static async Task AddUserAsync(IServiceProvider serviceProvider)
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
        }
    }

    public static async Task AddTestCloudAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var cloudExists = await context.Cloud.SingleOrDefaultAsync(x => x.Name == "test");
        if (cloudExists == null)
        {
            context.Cloud.Add(new Cloud { Name = "test" });
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