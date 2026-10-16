using Borderland.Dreams.Authorization;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class DeletedDreamsController(
    IDreamService dreamService,
    ICloudNameResolver cloudNameResolver
) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (cloudNameResolver.CloudID <= 0)
            return NotFound();

        return View(await dreamService.GetDeletedDreams());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null || cloudNameResolver.CloudID <= 0)
            return NotFound();

        var result = await dreamService.GetDeletedDream(id.Value);
        if (result.Status == DreamServiceStatus.NotFound)
            return NotFound();

        return View(result.Dream);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var result = await dreamService.RestoreDeletedDream(id);
        if (result.Status == DreamServiceStatus.NotFound)
            return NotFound();
        if (result.Status == DreamServiceStatus.NotAllowed)
            return Forbid();

        return RedirectToAction(nameof(Index), new { cloudSlug = cloudNameResolver.CloudSlug });
    }
}
