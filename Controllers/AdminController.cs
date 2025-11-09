using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Models;
using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Services;
using Borderland.Dreams.Middleware;

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
}
