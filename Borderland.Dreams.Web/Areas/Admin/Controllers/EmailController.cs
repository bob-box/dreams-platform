using Borderland.Dreams.Authorization;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class EmailController(
    IEmailManagementService emailManagementService,
    ICloudNameResolver cloudNameResolver
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!HasCloudContext())
            return NotFound();

        return View(await emailManagementService.GetEmailsAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!HasCloudContext())
            return NotFound();

        return View(await emailManagementService.BuildCreateViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmailViewModel model)
    {
        if (!HasCloudContext())
            return NotFound();

        if (!ModelState.IsValid)
        {
            await emailManagementService.PopulateEmailOptionsAsync(model);
            return View(model);
        }

        var result = await emailManagementService.CreateEmailAsync(model);
        if (result.Status == EmailManagementStatus.ValidationError)
        {
            AddErrorsToModelState(result.Errors);
            return View(result.ViewModel ?? model);
        }
        if (result.Status == EmailManagementStatus.Error)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "The email could not be queued.");
            await emailManagementService.PopulateEmailOptionsAsync(model);
            return View(model);
        }

        TempData["Success"] = model.ScheduledAt.HasValue
            ? "Email scheduled."
            : "Email queued for sending.";
        return RedirectToAction(nameof(Index), new { cloudSlug = cloudNameResolver.CloudSlug });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!HasCloudContext())
            return NotFound();

        var result = await emailManagementService.GetForEditAsync(id);
        if (result.Status == EmailManagementStatus.NotFound)
            return NotFound();
        if (result.Status == EmailManagementStatus.Conflict)
            return Conflict(result.Message);

        ViewBag.EmailId = id;
        return View(result.ViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmailViewModel model)
    {
        if (!HasCloudContext())
            return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.EmailId = id;
            await emailManagementService.PopulateEmailOptionsAsync(model);
            return View(model);
        }

        var result = await emailManagementService.UpdateEmailAsync(id, model);
        if (result.Status == EmailManagementStatus.NotFound)
            return NotFound();
        if (result.Status == EmailManagementStatus.Conflict)
            return Conflict(result.Message);
        if (result.Status == EmailManagementStatus.ValidationError)
        {
            AddErrorsToModelState(result.Errors);
            ViewBag.EmailId = id;
            return View(result.ViewModel ?? model);
        }
        if (result.Status == EmailManagementStatus.Error)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index), new { cloudSlug = cloudNameResolver.CloudSlug });
        }

        TempData["Success"] = "Email updated.";
        return RedirectToAction(nameof(Index), new { cloudSlug = cloudNameResolver.CloudSlug });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!HasCloudContext())
            return NotFound();

        var result = await emailManagementService.CancelEmailAsync(id);
        if (result.Status == EmailManagementStatus.NotFound)
            return NotFound();

        if (result.Status == EmailManagementStatus.Conflict)
            TempData["Error"] = result.Message;
        else
            TempData["Success"] = "Email cancelled.";

        return RedirectToAction(nameof(Index), new { cloudSlug = cloudNameResolver.CloudSlug });
    }

    private bool HasCloudContext() => cloudNameResolver.CloudID > 0;

    private void AddErrorsToModelState(Dictionary<string, List<string>>? errors)
    {
        if (errors == null)
            return;

        foreach (var (key, messages) in errors)
        {
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
        }
    }
}
