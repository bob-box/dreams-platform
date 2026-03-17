using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Enums;
using Microsoft.AspNetCore.Authorization;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Services;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;
using Borderland.Dreams.Extensions;

namespace Borderland.Dreams.Controllers;

[Authorize]
public class DreamController(ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    DreamService dreamService,
    ICloudNameResolver cloudNameService)
    : BaseController(context, authorizationService, userManager)
{
    // GET: Dream
    [AllowAnonymous]
    public async Task<IActionResult> Index()//[FromRoute] string? cloudName)
    {
        var listQuery = dreamService.GetDreamsByCloudIdBaseQuery();
        listQuery = IsAuthorizedToViewDream(listQuery);

        bool userHasAlreadyJoinedTheCloud = User.Claims.Any(c => c.Type == Constants.JOINED_CLOUD_CLAIM && c.Value == cloudNameService.CloudID.ToString());
        ViewData["showJoinCloudButton"] = !userHasAlreadyJoinedTheCloud && User.Identity?.IsAuthenticated == true;

        var viewModels = listQuery.Select(d => d.ToViewModel()).ToList();
        if (viewModels.Any(vm => vm == null))
        {
            return Problem("One or more dreams could not be mapped to a view model.");
        }

        return View(viewModels);
    }

    // GET: Dream/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var dreamQuery = dreamService.GetDreamsByCloudIdBaseQuery();
        dreamQuery = IsAuthorizedToViewDream(dreamQuery);

        var dream = await dreamQuery.SingleOrDefaultAsync(m => m.ID == id);
        if (dream == null)
        {
            return Forbid();
        }

        var viewModel = dream.ToViewModel();
        return View(viewModel);
    }

    [HttpGet]
    [AjaxOnly]
    public async Task<IActionResult> GetFundingModal(int dreamId)
    {
        // Verify the dream exists and belongs to the current cloud
        var dream = await _context.Dream
            .FirstOrDefaultAsync(d => d.ID == dreamId && d.CloudID == cloudNameService.CloudID);

        if (dream == null)
        {
            return NotFound();
        }

        // Check for active funding round
        var now = DateTime.Now;
        var activeRound = await _context.FundingRound
            .FirstOrDefaultAsync(fr => fr.CloudID == cloudNameService.CloudID &&
                                     fr.StartDate <= now && fr.EndDate >= now);

        if (activeRound == null)
        {
            return BadRequest("No active funding round.");
        }

        // Get user's wallet (we still need balance to verify sufficiency)
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
        var wallet = user.Wallets.FirstOrDefault(w => w.CloudID == cloudNameService.CloudID);

        if (wallet == null)
        {
            return BadRequest("User wallet not found.");
        }

        var userFundsForThisDream = await _context.Fund
            .Where(f => f.DreamID == dreamId && f.UserID == user.Id).ToListAsync();

        decimal userFundedInCurrentRound = userFundsForThisDream.SingleOrDefault(f => f.FundingRoundID == activeRound.ID)?.Amount ?? 0m;
        decimal userTotalFundedAllRounds = userFundsForThisDream.Sum(f => f.Amount);
        decimal roundMaxAmount = activeRound.AllocationPerUser * (activeRound.MaxPercentagePerDream / 100m);

        var viewModel = new FundingModalViewModel
        {
            DreamId = dreamId,
            RoundMaxAmount = roundMaxAmount,
            UserFundedInCurrentRound = userFundedInCurrentRound,
            UserTotalFundedAllRounds = userTotalFundedAllRounds,
            UserWalletBalance = wallet.Balance
        };
        return PartialView("_FundingModal", viewModel);
    }

    // GET: Dream/Create
    public async Task<IActionResult> Create()
    {
        bool userHasAlreadyJoinedTheCloud = User.Claims.Any(c => c.Type == Constants.JOINED_CLOUD_CLAIM && c.Value == cloudNameService.CloudID.ToString());
        if (!userHasAlreadyJoinedTheCloud)
        {
            return Problem("You have not joined this cloud yet, so you are not allowed to create a dream here.");
        }
        var viewModel = new DreamViewModel();
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
        var dreamer = new DreamerViewModel
        {
            UserID = user.Id,
            IsMainDreamer = true,
            FullName = user.FullName,
            UserName = user.UserName
        };
        viewModel.Dreamers.Add(dreamer); // Add the main dreamer (current user)
        return View(viewModel);
    }

    // POST: Dream/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Description,BudgetItems,Dreamers")] DreamViewModel viewModel)
    {
        bool userHasAlreadyJoinedTheCloud = User.Claims.Any(c => c.Type == Constants.JOINED_CLOUD_CLAIM && c.Value == cloudNameService.CloudID.ToString());
        if (!userHasAlreadyJoinedTheCloud)
        {
            return Problem("You have not joined this cloud yet, so you are not allowed to create a dream here.");
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }
        var dream = viewModel.ToModel();

        dream.CloudID = cloudNameService.CloudID; // Set the CloudID from the current context
        dream.Status = DreamStatus.Draft;

        _context.Add(dream);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = dream.ID });
    }

    // GET: Dream/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var dreamQuery = dreamService.GetDreamsByCloudIdBaseQuery();
        var dreamFromDb = await dreamQuery.SingleOrDefaultAsync(m => m.ID == id);

        if (dreamFromDb == null)
        {
            return NotFound();
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, dreamFromDb.Dreamers, Operations.Update);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        await CheckIfUserCanEditDreamers(dreamFromDb.Dreamers);
        var viewModel = dreamFromDb.ToViewModel();
        return View(viewModel);
    }

    // POST: Dream/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Description,BudgetItems,Dreamers")] DreamViewModel viewModel)
    {
        if (id != viewModel.ID)
        {
            return NotFound();
        }

        // We need to fetch the dreams dreamers to check authorization
        var dreamToUpdate = await dreamService.GetDreamsByCloudIdBaseQuery()
                .SingleOrDefaultAsync(m => m.ID == id);
        if (dreamToUpdate == null)
        {
            return NotFound();
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, dreamToUpdate.Dreamers, Operations.Update);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }
        if (!ModelState.IsValid)
        {
            await CheckIfUserCanEditDreamers(dreamToUpdate.Dreamers);
            return View(viewModel);
        }

        try
        {
            // Update the dream
            dreamToUpdate.Name = viewModel.Name;
            dreamToUpdate.Description = viewModel.Description;
            dreamToUpdate.Dreamers = viewModel.Dreamers.Where(x => x != null).Select(x => x.ToModel()).ToList();
            dreamToUpdate.BudgetItems = viewModel.BudgetItems.Where(b => b != null).Select(x => x.ToModel()).ToList();

            if (!dreamToUpdate.Dreamers.Any(x => x.IsMainDreamer))
            {
                return BadRequest("A dream must have at least one main dreamer.");
            }

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!DreamExists(viewModel.ID, cloudNameService.CloudID))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        return RedirectToAction(nameof(Details), new { id = viewModel.ID });
    }

    // GET: Dream/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var dreamFromDb = await dreamService.GetDreamsByCloudIdBaseQuery()
            .SingleOrDefaultAsync(m => m.ID == id);

        if (dreamFromDb == null)
        {
            return NotFound();
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, dreamFromDb.Dreamers, Operations.Delete);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        return View(dreamFromDb);
    }

    // POST: Dream/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var dreamFromDb = await dreamService.GetDreamsByCloudIdBaseQuery()
            .SingleOrDefaultAsync(m => m.ID == id);

        if (dreamFromDb == null)
        {
            return NotFound();
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, dreamFromDb.Dreamers, Operations.Delete);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }
        _context.Dream.Remove(dreamFromDb);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> FindDreamer([FromBody] FindDreamerViewModel request)
    {
        var dreamID = request.DreamID;
        var username = request.Username;

        // Check data
        if (string.IsNullOrWhiteSpace(username) || dreamID < 0)
        {
            return BadRequest("Invalid data provided.");
        }

        // Check dream stuff
        var dreamFromDb = await dreamService.GetDreamsByCloudIdBaseQuery()
            .SingleOrDefaultAsync(d => d.ID == dreamID);

        if (dreamFromDb == null)
        {
            return NotFound("Dream not found or you do not have access to it.");
        }
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, dreamFromDb.Dreamers, Operations.EditDreamers);
        if (!authorizationResult.Succeeded)
        {
            return new ForbiddenResult("You are not allowed to add dreamers to this dream.");
        }

        // Check user stuff
        var userToAdd = await _userManager.FindByNameAsync(username);
        if (userToAdd == null)
        {
            return NotFound("User not found.");
        }
        if (userToAdd.EmailConfirmed == false)
        {
            return BadRequest("This user has not confirmed their email address and cannot be added as a dreamer.");
        }
        var currentUserId = _userManager.GetUserId(User);
        if (userToAdd.Id == currentUserId)
        {
            return BadRequest("This is your own dream, silly!");
        }
        if (dreamFromDb.Dreamers.Any(d => d.UserID == userToAdd.Id))
        {
            return BadRequest("This user is already a dreamer for this dream.");
        }
        var userToAddClaims = await _userManager.GetClaimsAsync(userToAdd);
        bool hasJoinedCloud = userToAddClaims.Any(c => c.Type == Constants.JOINED_CLOUD_CLAIM && c.Value == cloudNameService.CloudID.ToString());
        if (!hasJoinedCloud)
        {
            return BadRequest("This user has not joined the cloud and cannot be added as a dreamer.");
        }

        // All good, add the dreamer
        var newDreamer = new DreamerViewModel
        {
            UserID = userToAdd.Id,
            FullName = userToAdd.FullName,
            UserName = userToAdd.UserName ?? string.Empty,
            DreamID = dreamID,
            IsMainDreamer = false
        };

        ViewData["canEditDreamers"] = true; // Needed for the delete button to be shown
        return PartialView("~/Views/Shared/EditorTemplates/DreamerSingle.cshtml", newDreamer);
    }

    /// <summary>
    /// Renders a blank editor row for a budget item.
    /// </summary>
    [Authorize]
    public PartialViewResult NewBudgetItem()
    {
        return PartialView("~/Views/Shared/EditorTemplates/BudgetItemViewModel.cshtml", new BudgetItemViewModel());
    }

    private bool DreamExists(int id, int cloudID)
    {
        return _context.Dream.Any(e => e.ID == id && e.CloudID == cloudID);
    }

    private IQueryable<Dream> IsAuthorizedToViewDream(IQueryable<Dream> dreamQuery)
    {
        // Admins are allowed to see all dreams
        var isAdmin = User.IsInRole(Constants.AdminRole);

        // Do not show dreams that are in Draft or Hidden status
        // But dreamers are still allowed to see their own dreams :)
        if (!isAdmin)
        {
            var currentUserId = _userManager.GetUserId(User);
            dreamQuery = dreamQuery.Where(x => (x.Status != DreamStatus.Hidden && x.Status != DreamStatus.Draft)
                                || x.Dreamers.Any(d => d.UserID == currentUserId));
        }

        return dreamQuery;
    }

    private async Task CheckIfUserCanEditDreamers(IEnumerable<IDreamersForAuthorization> dreamers)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, dreamers, Operations.EditDreamers);
        ViewData["canEditDreamers"] = authorizationResult.Succeeded; // Needed for the delete button to be shown
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Fund(int dreamId, decimal amount)
    {
        if (amount <= 0)
        {
            return BadRequest("Amount must be positive.");
        }

        var dream = await _context.Dream
            .FirstOrDefaultAsync(d => d.ID == dreamId && d.CloudID == cloudNameService.CloudID);

        if (dream == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
        var wallet = user.Wallets.FirstOrDefault(w => w.CloudID == cloudNameService.CloudID);

        if (wallet == null || wallet.Balance < amount)
        {
            return BadRequest("Insufficient funds.");
        }

        // Check if there's an active funding round
        var now = DateTime.Now;
        var activeRound = await _context.FundingRound
            .FirstOrDefaultAsync(fr => fr.CloudID == cloudNameService.CloudID &&
                                     fr.StartDate <= now && fr.EndDate >= now);

        decimal maxPerDream = decimal.MaxValue;
        if (activeRound != null)
        {
            // Check percentage limit based on round allocation
            maxPerDream = activeRound.AllocationPerUser * (activeRound.MaxPercentagePerDream / 100m);
            // see if user already has a fund for this dream/round
            var existingFund = await _context.Fund
                .FirstOrDefaultAsync(f => f.DreamID == dreamId &&
                                          f.UserID == user.Id &&
                                          f.FundingRoundID == activeRound.ID);

            if (existingFund != null)
            {
                if (existingFund.Amount + amount > maxPerDream)
                {
                    return BadRequest($"Amount exceeds the maximum allowed per dream for this round ({activeRound.MaxPercentagePerDream}% of per-user allocation). You have already funded {existingFund.Amount:C}.");
                }
                // update existing fund instead of creating new
                existingFund.Amount += amount;
                wallet.Balance -= amount;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = dreamId });
            }

            if (amount > maxPerDream)
            {
                return BadRequest($"Amount exceeds the maximum allowed per dream for this round ({activeRound.MaxPercentagePerDream}% of per-user allocation).");
            }
        }

        var fund = new Fund
        {
            DreamID = dreamId,
            UserID = user.Id,
            Amount = amount,
            FundingRoundID = activeRound?.ID
        };

        _context.Fund.Add(fund);
        wallet.Balance -= amount;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = dreamId });
    }
}