using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Models;
using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Services;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class UsersController(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    UserService userService)
    : Dreams.Controllers.BaseController(context, authorizationService, userManager)
{
    // GET: Admin/Users/UsersInCloud
    public async Task<IActionResult> UsersInCloud()
    {
        var viewModels = await userService.GetUsersInCloud();
        return View(viewModels);
    }

    // GET: Admin/Users/UsersInCloudSearch
    [HttpGet]
    [AjaxOnly]
    public async Task<IActionResult> UsersInCloudSearch(string filter = "")
    {
        var viewModels = await userService.GetUsersInCloud(filter);
        return PartialView("_ManageUsersTable", viewModels);
    }

    // POST: Admin/Users/UpdateWallets
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateWallets(WalletAction action, int amount, string? userId)
    {
        if (amount <= 0)
            return BadRequest("Amount must be greater than zero.");

        await userService.UpdateWallet(userId ?? "", action, amount);

        return RedirectToAction(nameof(UsersInCloud));
    }
}