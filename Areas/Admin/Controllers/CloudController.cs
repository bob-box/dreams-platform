using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class CloudController(
    ICloudService cloudService)
    : Controller
{
    public async Task<IActionResult> Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,TotalFundsAvailable")] CloudViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            await cloudService.Create(viewModel);
            return RedirectToAction(nameof(Index), "Home");
        }
        return View(viewModel);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var viewModel = await cloudService.GetById(id.Value);
        if (viewModel == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ID,Name,TotalFundsAvailable")] CloudViewModel viewModel)
    {
        if (id != viewModel.ID)
            return NotFound();

        if (!ModelState.IsValid)
            return View(viewModel);

        var result = await cloudService.Update(viewModel);
        if (result == CloudServiceStatus.NotFound)
            return NotFound();
            
        TempData["SuccessMessage"] = "Cloud updated successfully.";
        return RedirectToAction(nameof(Edit), new { id = viewModel.ID });
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var viewModel = await cloudService.GetById(id.Value);
        if (viewModel == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await cloudService.Delete(id);
        if (result == CloudServiceStatus.NotFound)
            return NotFound();

        return RedirectToAction(nameof(Index), "Home", routeValues: new { cloudName = "" });
    }
}