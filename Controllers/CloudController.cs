using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.FilterAttributes;

namespace Borderland.Dreams.Controllers;

public class CloudController(ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager)
    : BaseController(context, authorizationService, userManager)
{
    public async Task<IActionResult> Index()
    {
        return View(await _context.Cloud.ToListAsync());
    }

    [AccessForRole(Constants.AdminRole)]
    public async Task<IActionResult> Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AccessForRole(Constants.AdminRole)]
    public async Task<IActionResult> Create([Bind("ID,Name")] Cloud cloud)
    {
        if (ModelState.IsValid)
        {
            _context.Add(cloud);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(cloud);
    }

    [AccessForRole(Constants.AdminRole)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cloud = await _context.Cloud.FindAsync(id);

        return cloud == null ? NotFound() : View(cloud);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AccessForRole(Constants.AdminRole)]
    public async Task<IActionResult> Edit(int id, [Bind("ID,Name")] Cloud cloud)
    {
        if (id != cloud.ID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(cloud);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CloudExists(cloud.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(cloud);
    }

    [AccessForRole(Constants.AdminRole)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cloud = await _context.Cloud.SingleOrDefaultAsync(m => m.ID == id);

        return cloud == null ? NotFound() : View(cloud);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [AccessForRole(Constants.AdminRole)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cloud = await _context.Cloud.FindAsync(id);
        if (cloud != null)
        {
            _context.Cloud.Remove(cloud);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CloudExists(int id)
    {
        return _context.Cloud.Any(e => e.ID == id);
    }
}
