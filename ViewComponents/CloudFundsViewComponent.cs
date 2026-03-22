using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.ViewComponents;

public class CloudFundsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly ICloudNameResolver _cloudNameResolver;

    public CloudFundsViewComponent(ApplicationDbContext context, ICloudNameResolver cloudNameResolver)
    {
        _context = context;
        _cloudNameResolver = cloudNameResolver;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Calculate totals for the current cloud
        var cloud = await _context.Cloud.FindAsync(_cloudNameResolver.CloudID);
        if (cloud == null)
        {
            return View(new CloudFundsViewModel());
        }

        var totalFundsAllocated = await _context.Fund
            .Where(f => f.Dream.CloudID == _cloudNameResolver.CloudID)
            .SumAsync(f => f.Amount);

        var model = new CloudFundsViewModel
        {
            TotalFundsAvailable = cloud.TotalFundsAvailable,
            TotalFundsSpent = cloud.TotalFundsSpent,
            TotalFundsRemaining = cloud.TotalFundsRemaining
        };

        return View(model);
    }
}