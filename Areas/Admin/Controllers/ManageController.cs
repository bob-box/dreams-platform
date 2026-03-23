using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Models;
using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Services;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Middleware;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class ManageController(ILogger<ManageController> logger,
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    UserService userService,
    ICloudCache cloudCache)
    : Dreams.Controllers.BaseController(context, authorizationService, userManager)
{
    private readonly ILogger<ManageController> _logger = logger;

    // GET: Admin/Manage/UsersInCloud
    public async Task<IActionResult> UsersInCloud()
    {
        var viewModels = await userService.GetUsersInCloud();
        return View(viewModels);
    }

    // GET: Admin/Manage/UsersInCloudSearch
    [HttpGet]
    [AjaxOnly]
    public async Task<IActionResult> UsersInCloudSearch(string filter = "")
    {
        var viewModels = await userService.GetUsersInCloud(filter);
        // returns the table rows as a partial for AJAX updating
        return PartialView("_ManageUsersTable", viewModels);
    }

    // POST: Admin/Manage/UpdateWallets
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<RedirectToActionResult> UpdateWallets(string action, int amount, string? userId)
    {
        await userService.UpdateWallet(userId ?? "", action, amount);

        return RedirectToAction(nameof(UsersInCloud));
    }

    public async Task<IActionResult> EditCloud(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cloud = await _context.Cloud.FindAsync(id);
        if (cloud == null)
        {
            return NotFound();
        }

        var viewModel = cloud.ToViewModel();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCloud(int id, [Bind("ID,Name,TotalFundsAvailable")] CloudViewModel viewModel)
    {
        if (id != viewModel.ID)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var existingCloud = await _context.Cloud.FindAsync(id);
        if (existingCloud == null)
        {
            return NotFound();
        }

        existingCloud.TotalFundsAvailable = viewModel.TotalFundsAvailable;
        existingCloud.Name = viewModel.Name;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Cloud.AnyAsync(e => e.ID == viewModel.ID))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return RedirectToAction(nameof(Index), nameof(Dreams.Controllers.HomeController)[..^10]);
    }

    public async Task<IActionResult> DeleteCloud(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cloud = await _context.Cloud.SingleOrDefaultAsync(m => m.ID == id);
        if (cloud == null)
        {
            return NotFound();
        }

        var viewModel = cloud.ToViewModel();
        return View(viewModel);
    }

    [HttpPost, ActionName("DeleteCloud")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCloudConfirmed(int id)
    {
        var cloud = await _context.Cloud.FindAsync(id);
        if (cloud != null)
        {
            _context.Cloud.Remove(cloud);
            cloudCache.Remove(cloud.Name);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), nameof(Dreams.Controllers.HomeController)[..^10], routeValues: new { cloudName = "" });
    }
}
