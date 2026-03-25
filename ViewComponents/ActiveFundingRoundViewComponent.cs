using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.ViewComponents;

public class ActiveFundingRoundViewComponent(ApplicationDbContext context, ICloudNameResolver cloudNameResolver) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int dreamId)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Content(string.Empty);
        }

        var now = DateTime.UtcNow;
        var activeRound = await context.FundingRound
            .FirstOrDefaultAsync(fr => fr.CloudID == cloudNameResolver.CloudID &&
                                        fr.StartDate <= now && fr.EndDate >= now);

        if (activeRound == null)
        {
            return Content(string.Empty);
        }

        var model = new ActiveFundingRoundViewModel
        {
            DreamId = dreamId
        };

        return View(model);
    }
}
