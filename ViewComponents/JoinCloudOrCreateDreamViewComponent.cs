using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.ViewComponents;

public partial class JoinCloudOrCreateDreamViewComponent(
    ICloudService cloudService,
    UserManager<User> userManager,
    ICloudNameResolver cloudNameResolver,
    IAppTime appTime
    ) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var currentUserId = userManager.GetUserId(HttpContext.User);

        var userHasAlreadyJoinedTheCloud =
            !string.IsNullOrWhiteSpace(currentUserId) &&
            await cloudService.UserHasJoinedCloud(currentUserId);

        var cloudViewModel = await cloudService.GetById(cloudNameResolver.CloudID);

        var model = new JoinCloudOrCreateDreamViewModel
        {
            ShowJoinCloudButton = !userHasAlreadyJoinedTheCloud,
            CloudId = cloudNameResolver.CloudID,
            IsDreamingPeriodActive = cloudViewModel?.IsDreamingPeriodActive(appTime) ?? false
        };

        return View(model);
    }
}