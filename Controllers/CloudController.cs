using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Controllers;

public class CloudController(ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    ICloudService cloudService)
    : BaseController(context, authorizationService, userManager)
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    /// <summary>
    /// Called when a User joins a cloud. 
    /// Adds a claim to the user and creates a wallet in that cloud.
    /// </summary>
    /// <param name="id">The cloud ID.</param>
    public async Task<IActionResult> Join(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var result = await cloudService.Join(id, userId);
        if (result.Status == JoinCloudStatus.CloudNotFound)
            return NotFound();
        if (result.Status == JoinCloudStatus.UserNotFound)
            return Challenge();

        return RedirectToAction("Index", "Dream");
    }
}
