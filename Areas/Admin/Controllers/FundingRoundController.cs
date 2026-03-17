using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Models;
using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Middleware;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.ViewModels;
using Borderland.Dreams.Extensions;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class FundingRoundController(ILogger<FundingRoundController> logger,
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    // UserService userService,
    ICloudNameResolver cloudNameResolver
)
    : Dreams.Controllers.BaseController(context, authorizationService, userManager)
{
    private readonly ILogger<FundingRoundController> _logger = logger;

    // GET: Admin/FundingRound
    public async Task<IActionResult> Index()
    {
        var fundingRounds = await _context.FundingRound
            .Where(fr => fr.CloudID == cloudNameResolver.CloudID)
            .OrderBy(fr => fr.StartDate)
            .ToListAsync();

        var viewModels = fundingRounds.Select(fr => fr.ToViewModel()).ToList();
        return View(viewModels);
    }

    // GET: Admin/FundingRound/Create
    public IActionResult Create()
    {
        var viewModel = new FundingRoundViewModel
        {
            CloudID = cloudNameResolver.CloudID,
            StartDate = DateTime.Now.Date,
            EndDate = DateTime.Now.Date.AddDays(3).EndOfDay()
        };
        return View(viewModel);
    }

    // POST: Admin/FundingRound/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FundingRoundViewModel viewModel)
    {
        if (viewModel.CloudID != cloudNameResolver.CloudID)
        {
            return Forbid();
        }

        // Validate date ranges don't overlap
        var existingRounds = await _context.FundingRound
            .Where(fr => fr.CloudID == cloudNameResolver.CloudID)
            .ToListAsync();

        if (existingRounds.Any(fr =>
            (viewModel.StartDate <= fr.EndDate && viewModel.EndDate >= fr.StartDate)))
        {
            ModelState.AddModelError("", "Funding round dates cannot overlap with existing rounds.");
        }

        if (viewModel.EndDate <= viewModel.StartDate)
        {
            ModelState.AddModelError("EndDate", "End date must be after start date.");
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var fundingRound = viewModel.ToModel();
        _context.Add(fundingRound);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/FundingRound/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var fundingRound = await _context.FundingRound
            .FirstOrDefaultAsync(fr => fr.ID == id && fr.CloudID == cloudNameResolver.CloudID);

        if (fundingRound == null)
        {
            return NotFound();
        }

        return View(fundingRound.ToViewModel());
    }

    // POST: Admin/FundingRound/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FundingRoundViewModel viewModel)
    {
        if (id != viewModel.ID || viewModel.CloudID != cloudNameResolver.CloudID)
        {
            return NotFound();
        }

        var roundToUpdate = await _context.FundingRound.FindAsync(id);
        if (roundToUpdate == null)
        {
            return NotFound();
        }

        // Validate date ranges don't overlap (excluding current round)
        var existingRounds = await _context.FundingRound
            .Where(fr => fr.CloudID == cloudNameResolver.CloudID && fr.ID != id)
            .ToListAsync();

        if (existingRounds.Any(fr =>
            (viewModel.StartDate <= fr.EndDate && viewModel.EndDate >= fr.StartDate)))
        {
            ModelState.AddModelError("", "Funding round dates cannot overlap with existing rounds.");
        }

        if (viewModel.EndDate <= viewModel.StartDate)
        {
            ModelState.AddModelError("EndDate", "End date must be after start date.");
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            roundToUpdate.Title = viewModel.Title;
            roundToUpdate.StartDate = viewModel.StartDate;
            roundToUpdate.EndDate = viewModel.EndDate;
            roundToUpdate.MaxPercentagePerDream = viewModel.MaxPercentagePerDream;
            roundToUpdate.AllocationPerUser = viewModel.AllocationPerUser;

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!FundingRoundExists(viewModel.ID))
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

    // GET: Admin/FundingRound/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var fundingRound = await _context.FundingRound
            .FirstOrDefaultAsync(fr => fr.ID == id && fr.CloudID == cloudNameResolver.CloudID);

        if (fundingRound == null)
        {
            return NotFound();
        }

        return View(fundingRound.ToViewModel());
    }

    // POST: Admin/FundingRound/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var fundingRound = await _context.FundingRound
            .FirstOrDefaultAsync(fr => fr.ID == id && fr.CloudID == cloudNameResolver.CloudID);

        if (fundingRound != null)
        {
            _context.FundingRound.Remove(fundingRound);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool FundingRoundExists(int id)
    {
        return _context.FundingRound.Any(e => e.ID == id);
    }

    // to be used later for processing ended rounds in batch (e.g. via a scheduled job or admin action)
    // Code not tested!!
    public async Task ProcessEndedRound(int id)
    {
        var endedRound = await _context.FundingRound.FindAsync(id);
        if (endedRound == null)
        {
            throw new Exception("Funding round not found");
        }
        if (endedRound.ProcessedDate != null)
        {
            throw new InvalidOperationException("This funding round has already been processed.");
        }

        // Calculate total spent in this round
        var totalSpent = await _context.Fund
            .Where(f => f.FundingRoundID == endedRound.ID)
            .SumAsync(f => f.Amount);

        // Get all wallets in the cloud
        var wallets = await _context.Wallet
            .Where(w => w.CloudID == cloudNameResolver.CloudID)
            .ToListAsync();

        // Reset all wallet balances to 0
        foreach (var wallet in wallets)
        {
            wallet.Balance = 0;
        }

        // Lower the cloud's total funds available by the amount spent
        var cloud = await _context.Cloud.FindAsync(endedRound.CloudID);
        if (cloud != null)
        {
            cloud.TotalFundsAvailable -= totalSpent;
            if (cloud.TotalFundsAvailable < 0) cloud.TotalFundsAvailable = 0;
        }

        // Mark the round as processed to prevent double-processing
        endedRound.ProcessedDate = DateTime.Now;

        await _context.SaveChangesAsync();
    }
}
