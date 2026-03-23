using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.FilterAttributes;
using System.Security.Claims;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;
using Borderland.Dreams.Mappers;

namespace Borderland.Dreams.Controllers;

public class CloudController(ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    SignInManager<User> signInManager)
    : BaseController(context, authorizationService, userManager)
{
    private readonly SignInManager<User> _signInManager = signInManager;

    [Authorize(Roles = Constants.AdminRole)]
    public async Task<IActionResult> Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.AdminRole)]
    public async Task<IActionResult> Create([Bind("ID,Name,TotalFundsAvailable")] CloudViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var cloud = viewModel.ToModel();
            _context.Add(cloud);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), nameof(HomeController)[..^10]);
        }
        return View(viewModel);
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
}
