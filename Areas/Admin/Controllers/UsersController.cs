using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Models;
using Borderland.Dreams.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class UsersController(
    UserManager<User> userManager,
    IUserService userService
) : Controller
{
    // GET: Admin/Users/Index
    public async Task<IActionResult> Index()
    {
        ViewData["CurrentUserId"] = userManager.GetUserId(User);
        var viewModels = await userService.GetAll();
        return View(viewModels);
    }

    // GET: Admin/Users/Filter
    [HttpGet]
    public async Task<IActionResult> Filter(string filter = "")
    {
        ViewData["CurrentUserId"] = userManager.GetUserId(User);
        var viewModels = await userService.GetAll(filter);
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

        var currentUserId = userManager.GetUserId(User);
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