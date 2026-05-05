using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.ViewComponents;

public class FundButtonViewComponent(
    ICloudNameResolver cloudNameResolver,
    IFundingRoundService fundingRoundService,
    IDreamService dreamService
    ) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int dreamId)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Content(string.Empty);

        var activeRound = await fundingRoundService.GetActiveRound(cloudNameResolver.CloudID);
        if (activeRound == null)
            return Content(string.Empty);
        
        var result = await dreamService.GetQuick(dreamId);
        if (result.Status == DreamServiceStatus.NotFound || result.Dream?.IsOpenToFunding() == false)
            return Content(string.Empty);

        var model = new FundButtonViewModel
        {
            DreamId = dreamId
        };

        return View(model);
    }
}
