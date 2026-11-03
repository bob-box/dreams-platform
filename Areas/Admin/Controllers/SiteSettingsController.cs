using Borderland.Dreams.Authorization;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class SiteSettingsController(IEmailManagementService emailManagementService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() =>
        View(new SiteSettingsViewModel
        {
            Email = await emailManagementService.GetSettingsAsync()
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Email.IsApiKeyConfigured =
                (await emailManagementService.GetSettingsAsync()).IsApiKeyConfigured;
            return View(model);
        }

        await emailManagementService.SaveSettingsAsync(model.Email);
        TempData["Success"] = "Site-wide settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
