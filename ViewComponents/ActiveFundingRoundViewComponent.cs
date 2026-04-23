using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.ViewComponents;

public class ActiveFundingRoundViewComponent(
    ICloudNameResolver cloudNameResolver,
    IFundingRoundService fundingRoundService,
    IDreamService dreamService
    ) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int dreamId)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Content(string.Empty);
        }

        var activeRound = await fundingRoundService.GetActiveRound(cloudNameResolver.CloudID);
        if (activeRound == null)
        {
            return Content(string.Empty);
        }
        
        var dream = await dreamService.GetQuick(dreamId);
        if (dream == null || !dream.IsOpenForFunding())
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
