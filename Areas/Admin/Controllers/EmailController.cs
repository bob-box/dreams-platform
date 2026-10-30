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
    IEmailCampaignManagementService emailManagementService,
    ICloudNameResolver cloudNameResolver
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Campaigns()
    {
        if (!HasCloudContext())
            return NotFound();

        return View(await emailManagementService.GetCampaignsAsync());
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
    public async Task<IActionResult> Create(EmailCampaignViewModel model)
    {
        if (!HasCloudContext())
            return NotFound();

        if (!ModelState.IsValid)
        {
            await emailManagementService.PopulateCampaignOptionsAsync(model);
            return View(model);
        }

        var result = await emailManagementService.CreateCampaignAsync(model);
        if (result.Status == EmailCampaignManagementStatus.ValidationError)
        {
            AddErrorsToModelState(result.Errors);
            return View(result.ViewModel ?? model);
        }
        if (result.Status == EmailCampaignManagementStatus.Error)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "The email campaign could not be queued.");
            await emailManagementService.PopulateCampaignOptionsAsync(model);
            return View(model);
        }

        TempData["Success"] = model.ScheduledAt.HasValue
            ? "Email campaign scheduled."
            : "Email campaign queued for sending.";
        return RedirectToAction(nameof(Campaigns), new { cloudSlug = cloudNameResolver.CloudSlug });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!HasCloudContext())
            return NotFound();

        var result = await emailManagementService.GetForEditAsync(id);
        if (result.Status == EmailCampaignManagementStatus.NotFound)
            return NotFound();
        if (result.Status == EmailCampaignManagementStatus.Conflict)
            return Conflict(result.Message);

        ViewBag.CampaignId = id;
        return View(result.ViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmailCampaignViewModel model)
    {
        if (!HasCloudContext())
            return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.CampaignId = id;
            await emailManagementService.PopulateCampaignOptionsAsync(model);
            return View(model);
        }

        var result = await emailManagementService.UpdateCampaignAsync(id, model);
        if (result.Status == EmailCampaignManagementStatus.NotFound)
            return NotFound();
        if (result.Status == EmailCampaignManagementStatus.Conflict)
            return Conflict(result.Message);
        if (result.Status == EmailCampaignManagementStatus.ValidationError)
        {
            AddErrorsToModelState(result.Errors);
            ViewBag.CampaignId = id;
            return View(result.ViewModel ?? model);
        }
        if (result.Status == EmailCampaignManagementStatus.Error)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Campaigns), new { cloudSlug = cloudNameResolver.CloudSlug });
        }

        TempData["Success"] = "Email campaign updated.";
        return RedirectToAction(nameof(Campaigns), new { cloudSlug = cloudNameResolver.CloudSlug });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!HasCloudContext())
            return NotFound();

        var result = await emailManagementService.CancelCampaignAsync(id);
        if (result.Status == EmailCampaignManagementStatus.NotFound)
            return NotFound();

        if (result.Status == EmailCampaignManagementStatus.Conflict)
            TempData["Error"] = result.Message;
        else
            TempData["Success"] = "Email campaign cancelled.";

        return RedirectToAction(nameof(Campaigns), new { cloudSlug = cloudNameResolver.CloudSlug });
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
