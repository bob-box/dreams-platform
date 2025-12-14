using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Models;
using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Services;

namespace Borderland.Dreams.Controllers;

[Authorize(Roles = Constants.AdminRole)]
public class AdminController(ILogger<AdminController> logger,
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    UserService userService
)
    : BaseController(context, authorizationService, userManager)
{
    private readonly ILogger<AdminController> _logger = logger;

    public async Task<IActionResult> ManageUsersInCloud()
    {
        var viewModels = await userService.GetUsersInCloud();
        return View(viewModels);
    }

    [HttpGet]
    [AjaxOnly]
    public async Task<IActionResult> ManageUsersInCloudSearch(string filter = "")
    {
        var viewModels = await userService.GetUsersInCloud(filter);
        // returns the table rows as a partial for AJAX updating
        return PartialView("_ManageUsersTable", viewModels);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<RedirectToActionResult> UpdateWallets(string action, int amount, string? userId)
    {
        await userService.UpdateWallet(userId ?? "", action, amount);

        return RedirectToAction(nameof(ManageUsersInCloud));
    }
}
