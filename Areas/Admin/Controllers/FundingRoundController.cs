using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Models;
using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class FundingRoundController(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    IFundingRoundService fundingRoundService
)
    : Dreams.Controllers.BaseController(context, authorizationService, userManager)
{
    // GET: Admin/FundingRound
    public async Task<IActionResult> Index()
    {
        var viewModels = await fundingRoundService.GetAll();
        return View(viewModels);
    }

    // GET: Admin/FundingRound/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = await fundingRoundService.BuildCreateViewModel();
        return View(viewModel);
    }

    // POST: Admin/FundingRound/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FundingRoundViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await fundingRoundService.Create(viewModel);
        if (result.Status == FundingRoundServiceStatus.Error)
        {
            AddErrorsToModelState(ModelState, result.Errors);
            return View(viewModel);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/FundingRound/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var loadResult = await fundingRoundService.GetForEdit(id.Value);
        if (loadResult.Status == FundingRoundServiceStatus.NotFound)
            return NotFound();

        if (loadResult.Status == FundingRoundServiceStatus.Error)
        {
            TempData["Error"] = loadResult.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(loadResult.ViewModel);
    }

    // POST: Admin/FundingRound/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FundingRoundViewModel viewModel)
    {
        if (id != viewModel.ID)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
            return View(viewModel);

        var result = await fundingRoundService.Update(viewModel);
        if (result.Status == FundingRoundServiceStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == FundingRoundServiceStatus.Error)
        {
            AddErrorsToModelState(ModelState, result.Errors);
            return View(viewModel);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/FundingRound/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var loadResult = await fundingRoundService.GetForDelete(id.Value);
        if (loadResult.Status == FundingRoundServiceStatus.NotFound)
            return NotFound();

        if (loadResult.Status == FundingRoundServiceStatus.Error)
        {
            TempData["Error"] = loadResult.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(loadResult.ViewModel);
    }

    // POST: Admin/FundingRound/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await fundingRoundService.Delete(id);
        if (result.Status == FundingRoundServiceStatus.NotFound)
            return NotFound();

        if (result.Status == FundingRoundServiceStatus.Error)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index));
    }

    private static void AddErrorsToModelState(
        ModelStateDictionary modelState,
        Dictionary<string, List<string>>? errors)
    {
        if (errors == null)
            return;

        foreach (var (key, messages) in errors)
        {
            foreach (var message in messages)
            {
                modelState.AddModelError(key, message);
            }
        }
    }
}
