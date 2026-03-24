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
using Borderland.Dreams.Services;
using Borderland.Dreams.Extensions;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class FundingRoundController(ILogger<FundingRoundController> logger,
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    UserService userService,
    ICloudNameResolver cloudNameResolver,
    FundingRoundSchedulerService schedulerService
)
    : Dreams.Controllers.BaseController(context, authorizationService, userManager)
{
    private readonly ILogger<FundingRoundController> _logger = logger;
    private readonly FundingRoundSchedulerService _schedulerService = schedulerService;

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
    public async Task<IActionResult> Create()
    {
        var userCount = await userService.CountUsersInCloud();

        var viewModel = new FundingRoundViewModel
        {
            CloudID = cloudNameResolver.CloudID,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(3).EndOfDay(),
            UsersInCloudCount = userCount
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

        ValidateDates(viewModel, existingRounds);

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var fundingRound = viewModel.ToModel();
        _context.Add(fundingRound);
        await _context.SaveChangesAsync();

        // Schedule Hangfire jobs for this funding round and save job IDs
        _schedulerService.ScheduleFundingRoundJobs(fundingRound);
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

        if (IsPassed(fundingRound))
        {
            TempData["Error"] = "Cannot edit a funding round that has already passed.";
            return RedirectToAction(nameof(Index));
        }
        ViewData["IsActive"] = IsActive(fundingRound);

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

        if (IsPassed(roundToUpdate))
        {
            ModelState.AddModelError("", "Cannot edit a funding round that has already passed.");
            ViewData["IsActive"] = IsActive(roundToUpdate);
            return View(viewModel);
        }
        var existingRounds = await _context.FundingRound
            .Where(fr => fr.CloudID == cloudNameResolver.CloudID && fr.ID != id)
            .ToListAsync();

        if (IsActive(roundToUpdate))
            return await EditActiveRoundAsync(viewModel, roundToUpdate, existingRounds);

        return await EditUpcomingRoundAsync(viewModel, roundToUpdate, existingRounds);
    }

    private async Task<IActionResult> EditActiveRoundAsync(
        FundingRoundViewModel viewModel,
        FundingRound roundToUpdate,
        List<FundingRound> existingRounds)
    {
        // Validate only the editable fields for an active round
        var now = DateTime.UtcNow;
        if (viewModel.EndDate <= now)
            ModelState.AddModelError("EndDate", "End date must be in the future.");
        else if (existingRounds.Any(fr => fr.StartDate <= viewModel.EndDate && fr.EndDate >= roundToUpdate.StartDate))
            ModelState.AddModelError("", "Funding round dates cannot overlap with existing rounds.");

        if (!ModelState.IsValid)
        {
            // Restore locked fields so the view renders correctly
            viewModel.Title = roundToUpdate.Title;
            viewModel.StartDate = roundToUpdate.StartDate;
            viewModel.AllocationPerUser = roundToUpdate.AllocationPerUser;
            return View(viewModel);
        }

        try
        {
            roundToUpdate.EndDate = viewModel.EndDate;
            roundToUpdate.MaxPercentagePerDream = viewModel.MaxPercentagePerDream;

            _schedulerService.ScheduleFundingRoundJobs(roundToUpdate); // updates job IDs on entity
            await _context.SaveChangesAsync(); // saves both entity changes and job IDs in one call
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!FundingRoundExists(viewModel.ID))
                return NotFound();
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> EditUpcomingRoundAsync(
        FundingRoundViewModel viewModel,
        FundingRound roundToUpdate,
        List<FundingRound> existingRounds)
    {
        ValidateDates(viewModel, existingRounds);
        if (!ModelState.IsValid)
            return View(viewModel);

        roundToUpdate.Title = viewModel.Title;
        roundToUpdate.StartDate = viewModel.StartDate;
        roundToUpdate.EndDate = viewModel.EndDate;
        roundToUpdate.MaxPercentagePerDream = viewModel.MaxPercentagePerDream;
        roundToUpdate.AllocationPerUser = viewModel.AllocationPerUser;

        _schedulerService.ScheduleFundingRoundJobs(roundToUpdate); // updates job IDs on entity

        try
        {
            await _context.SaveChangesAsync(); // saves both entity changes and job IDs in one call
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!FundingRoundExists(viewModel.ID))
                return NotFound();
            throw;
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

        if (IsActiveOrPassed(fundingRound))
        {
            TempData["Error"] = "Cannot delete a funding round that is active or has already passed.";
            return RedirectToAction(nameof(Index));
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
            if (IsActiveOrPassed(fundingRound))
            {
                TempData["Error"] = "Cannot delete a funding round that is active or has already passed.";
                return RedirectToAction(nameof(Index));
            }

            // Cancel scheduled jobs
            _schedulerService.CancelFundingRoundJobs(fundingRound);

            _context.FundingRound.Remove(fundingRound);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool FundingRoundExists(int id)
    {
        return _context.FundingRound.Any(e => e.ID == id);
    }

    private static bool IsActiveOrPassed(FundingRound fundingRound) =>
        fundingRound.StartDate <= DateTime.UtcNow;

    private static bool IsActive(FundingRound fundingRound) =>
        fundingRound.StartDate <= DateTime.UtcNow && fundingRound.EndDate >= DateTime.UtcNow;

    private static bool IsPassed(FundingRound fundingRound) =>
        fundingRound.EndDate < DateTime.UtcNow;

    private void ValidateDates(FundingRoundViewModel viewModel, List<FundingRound> existingRounds)
    {
        if (existingRounds.Any(fr =>
            (viewModel.StartDate <= fr.EndDate && viewModel.EndDate >= fr.StartDate)))
        {
            ModelState.AddModelError("", "Funding round dates cannot overlap with existing rounds.");
        }

        if (viewModel.EndDate <= viewModel.StartDate)
        {
            ModelState.AddModelError("EndDate", "End date must be after start date.");
        }

        var now = DateTime.UtcNow;
        var currentUtcMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);

        if (viewModel.StartDate < currentUtcMinute)
        {
            ModelState.AddModelError("StartDate", "Start date cannot be in the past.");
        }
    }
}
