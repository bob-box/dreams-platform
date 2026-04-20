using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Models;

namespace Borderland.Dreams.ViewComponents;

public class CloudFundsViewComponent(
    ApplicationDbContext context,
    ICloudNameResolver cloudNameResolver,
    UserManager<User> userManager) : ViewComponent
{

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Calculate totals for the current cloud
        var cloud = await context.Cloud.FindAsync(cloudNameResolver.CloudID);
        if (cloud == null)
        {
            return View(new CloudFundsViewModel());
        }

        var totalFundsAllocated = await context.Fund
            .Where(f => f.Dream.CloudID == cloudNameResolver.CloudID)
            .SumAsync(f => f.Amount);

        var userWalletBalance = 0m;
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user != null)
        {
            userWalletBalance = await context.Wallet
                .Where(w => w.UserID == user.Id && w.CloudID == cloudNameResolver.CloudID)
                .Select(w => w.Balance)
                .SingleOrDefaultAsync();
        }

        var model = new CloudFundsViewModel
        {
            TotalFundsAvailable = cloud.TotalFundsAvailable,
            TotalFundsSpent = cloud.TotalFundsSpent,
            TotalFundsRemaining = cloud.TotalFundsRemaining,
            UserWalletBalance = userWalletBalance
        };

        return View(model);
    }
}