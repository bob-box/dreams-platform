using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Models;
using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Middleware;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class CloudController(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    ICloudCache cloudCache)
    : Dreams.Controllers.BaseController(context, authorizationService, userManager)
{
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cloud = await _context.Cloud.FindAsync(id);
        if (cloud == null)
        {
            return NotFound();
        }

        var viewModel = cloud.ToViewModel();
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

        var cloud = await _context.Cloud.FindAsync(id);
        if (cloud == null)
            return NotFound();

        // Cache the old name before updating, in case it changed
        var oldName = cloud.Name;

        cloud.Name = viewModel.Name;
        cloud.TotalFundsAvailable = viewModel.TotalFundsAvailable;

        try
        {
            await _context.SaveChangesAsync();

            // Invalidate old cache entry if name changed
            if (!oldName.Equals(viewModel.Name, StringComparison.OrdinalIgnoreCase))
                cloudCache.Remove(oldName);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Cloud.AnyAsync(e => e.ID == id))
                return NotFound();
            throw;
        }

        return RedirectToAction(nameof(Index), "Home");
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cloud = await _context.Cloud.FindAsync(id);
        if (cloud == null)
        {
            return NotFound();
        }

        var viewModel = cloud.ToViewModel();
        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cloud = await _context.Cloud.FindAsync(id);
        if (cloud == null)
            return NotFound();

        _context.Cloud.Remove(cloud);
        cloudCache.Remove(cloud.Name);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), "Home", routeValues: new { cloudName = "" });
    }
}