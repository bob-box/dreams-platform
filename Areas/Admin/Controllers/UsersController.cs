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
    // GET: Admin/Users/Index
    public async Task<IActionResult> Index()
    {
        ViewData["CurrentUserId"] = _userManager.GetUserId(User);
        var viewModels = await userService.GetUsersInCloud();
        return View(viewModels);
    }

    // GET: Admin/Users/Filter
    [HttpGet]
    public async Task<IActionResult> Filter(string filter = "")
    {
        ViewData["CurrentUserId"] = _userManager.GetUserId(User);
        var viewModels = await userService.GetUsersInCloud(filter);
        return PartialView("_ManageUsersTable", viewModels);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(string userId, string role)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
        {
            return BadRequest(new { message = "User and role are required.", statusClass = "danger" });
        }

        var currentUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Challenge();

        try
        {
            await userService.UpdateRole(userId, currentUserId, role);
            return Ok(new { message = "User role updated.", statusClass = "success" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User not found.", statusClass = "danger" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message, statusClass = "warning" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message, statusClass = "danger" });
        }
    }

    // POST: Admin/Users/UpdateWallets
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateWallets(WalletAction action, int amount, string? userId)
    {
        if (amount <= 0)
            return BadRequest("Amount must be greater than zero.");

        await userService.UpdateWallet(userId ?? "", action, amount);

        return RedirectToAction(nameof(Index));
    }
}