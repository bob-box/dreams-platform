using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        await AddRolesAsync(serviceProvider);
        await AddAdminUserAsync(serviceProvider);
        await AddUserAsync(serviceProvider);
        await AddTestCloudAsync(serviceProvider);
    }

    private static async Task AddRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var roles = new[] { Constants.AdministratorsRole, Constants.MemberRole };
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
                await userManager.AddToRoleAsync(adminUser, Constants.AdministratorsRole);
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
}