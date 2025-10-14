using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.FilterAttributes;
using System.Security.Claims;

namespace Borderland.Dreams.Controllers;

public class CloudController(ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    SignInManager<User> signInManager)
    : BaseController(context, authorizationService, userManager)
{
    private readonly SignInManager<User> _signInManager = signInManager;

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
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(cloud);
                _context.Entry(cloud).Property(x => x.CreatedDate).IsModified = false; // Prevent modification of CreatedDate
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
        var cloud = await _context.Cloud.FindAsync(id);
        if (cloud == null)
        {
            return NotFound();
        }

        var userHasAlreadyJoinedTheCloud = User.Claims.Any(c => c.Type == Constants.JOINED_CLOUD_CLAIM && c.Value == cloud.ID.ToString());
        if (!userHasAlreadyJoinedTheCloud)
        {
            var user = await _userManager.GetUserAsync(User);
            var claim = new Claim(Constants.JOINED_CLOUD_CLAIM, cloud.ID.ToString());
            await _userManager.AddClaimAsync(user, claim);
            await _signInManager.RefreshSignInAsync(user); // Refresh the user's claims

            user.Wallets.Add(new Wallet(cloud.ID));
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index", "Dream");
    }

    private bool CloudExists(int id)
    {
        return _context.Cloud.Any(e => e.ID == id);
    }
}
