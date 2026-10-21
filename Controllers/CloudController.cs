using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Interfaces;
using Ganss.Xss;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.ViewModels;
using Borderland.Dreams.Middleware;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Controllers;

public class CloudController(
    UserManager<User> userManager,
    ApplicationDbContext context,
    ICloudService cloudService,
    IDreamService dreamService,
    ICloudNameResolver cloudNameResolver,
    HtmlSanitizer sanitizer
) : Controller
{

    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? searchText,
        DreamStatus? statusFilter,
        int? tagFilter,
        DreamSortOrder sortOrder = DreamSortOrder.NameAsc
    )
    {
        var currentUserId = userManager.GetUserId(User);

        var filterViewModel = await dreamService.BuildDreamFilterViewModel(
            searchText,
            statusFilter,
            tagFilter,
            sortOrder,
            currentUserId,
            User.IsInRole(Constants.AdminRole));

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_DreamTable", filterViewModel);

        return View(filterViewModel);
    }

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
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var result = await cloudService.Join(id, userId);
        if (result.Status == JoinCloudStatus.CloudNotFound)
            return NotFound();
        if (result.Status == JoinCloudStatus.UserNotFound)
            return Challenge();

        return RedirectToAction("Index");
    }

    [AllowAnonymous]
    public async Task<IActionResult> Info()
    {
        if (cloudNameResolver.CloudID <= 0)
            return NotFound();

        var cloudInfo = await context.CloudInfo
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.CloudID == cloudNameResolver.CloudID);

        var content = cloudInfo?.Content ?? "";
        return View(new CloudInfoViewModel
        {
            Content = sanitizer.Sanitize(content),
        });
    }

    [Authorize(Roles = Constants.AdminRole)]
    public async Task<IActionResult> EditInfo()
    {
        if (cloudNameResolver.CloudID <= 0)
            return NotFound();

        var cloudInfo = await context.CloudInfo
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.CloudID == cloudNameResolver.CloudID);

        return View(new CloudInfoViewModel { Content = cloudInfo?.Content ?? "" });
    }

    [HttpPost]
    [Authorize(Roles = Constants.AdminRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditInfo(CloudInfoViewModel viewModel)
    {
        if (cloudNameResolver.CloudID <= 0)
            return NotFound();

        if (!ModelState.IsValid)
            return View(viewModel);

        var cloudInfo = await context.CloudInfo
            .SingleOrDefaultAsync(x => x.CloudID == cloudNameResolver.CloudID);

        if (cloudInfo == null)
        {
            cloudInfo = new CloudInfo
            {
                CloudID = cloudNameResolver.CloudID,
                Content = ""
            };
            context.CloudInfo.Add(cloudInfo);
        }

        cloudInfo.Content = sanitizer.Sanitize(viewModel.Content);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Info));
    }
}
