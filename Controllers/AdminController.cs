using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Models;
using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Services;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Controllers;

[Authorize(Roles = Constants.AdminRole)]
public class AdminController(ILogger<AdminController> logger,
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    UserService userService,
    ICloudNameResolver cloudNameService)
    : BaseController(context, authorizationService, userManager)
{
    private readonly ILogger<AdminController> _logger = logger;

    public async Task<IActionResult> ManageUsersInCloud()
    {
        var viewModels = await userService.GetUsersInCloud(cloudNameService.CloudID);
        return View(viewModels);
    }

    // New: returns the table rows as a partial for AJAX updating
    [HttpGet]
    [AjaxOnly]
    public async Task<IActionResult> ManageUsersInCloudSearch(string? q)
    {
        var viewModels = await userService.GetUsersInCloud(cloudNameService.CloudID);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qTrim = q.Trim();
            viewModels = viewModels.Where(u =>
                (!string.IsNullOrEmpty(u.FullName) && u.FullName.IndexOf(qTrim, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(u.UserName) && u.UserName.IndexOf(qTrim, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(u.Email) && u.Email.IndexOf(qTrim, StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();
        }

        return PartialView("_ManageUsersTable", viewModels);
    }
}
