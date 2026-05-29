using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Enums;
using Microsoft.AspNetCore.Authorization;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;
using Borderland.Dreams.Extensions;

namespace Borderland.Dreams.Controllers;

[Authorize]
public class DreamController(
    UserManager<User> userManager,
    IDreamService dreamService,
    IDreamFundingService dreamFundingService,
    ICloudService cloudService,
    ITagService tagService,
    IAppTime appTime,
    ICloudNameResolver cloudNameService
) : Controller
{
    // GET: Dream
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? searchText,
        DreamStatus? statusFilter,
        int? tagFilter,
        DreamSortOrder sortOrder = DreamSortOrder.NameAsc
    )
    {
        var currentUserId = GetCurrentUserId();

        var filterViewModel = await dreamService.BuildDreamFilterViewModel(
            searchText,
            statusFilter,
            tagFilter,
            sortOrder,
            currentUserId,
            User.IsInRole(Constants.AdminRole));

        return View(filterViewModel);
    }

    // GET: Dream/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var result = await dreamService.GetFull(id.Value);
        if (result.Status == DreamServiceStatus.NotFound) return NotFound();
        if (result.Status == DreamServiceStatus.NotAllowed) return Forbid();

        // Expose whether editing is allowed (based on cloud's DreamEditDeadline) to the view
        var cloud = await cloudService.GetById(result.Dream!.CloudID);
        ViewBag.IsEditAllowed = User.IsInRole(Constants.AdminRole) || cloud.IsBeforeEditDeadline(appTime);

        return View(result.Dream);
    }

    // GET: Dream/Create
    public async Task<IActionResult> Create()
    {
        if (await GetCreateDreamGuardResult() is { } guardResult)
            return guardResult;

        var viewModel = new DreamViewModel();
        var user = await userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
        var dreamer = new DreamerViewModel
        {
            UserID = user.Id,
            IsMainDreamer = true,
            Name = user.Name,
        };
        viewModel.Dreamers.Add(dreamer); // Add the main dreamer (current user)
        ViewBag.AvailableTags = await tagService.GetAll();

        return View(viewModel);
    }

    // POST: Dream/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Description,BudgetItems,Dreamers,SelectedTagIds")] DreamViewModel viewModel)
    {
        if (await GetCreateDreamGuardResult() is { } guardResult)
            return guardResult;

        if (!ModelState.IsValid)
        {
            ViewBag.AvailableTags = await tagService.GetAll();
            return View(viewModel);
        }
        var dreamId = await dreamService.Create(viewModel);
        return RedirectToAction(nameof(Details), new { id = dreamId });
    }

    // GET: Dream/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        // Check cloud edit deadline
        var cloud = await cloudService.GetById(cloudNameService.CloudID);
        if (!User.IsInRole(Constants.AdminRole) || !cloud.IsBeforeEditDeadline(appTime))
        {
            return BadRequest("The editing deadline for this cloud has passed.");
        }

        var result = await dreamService.GetFull(id.Value);
        if (result.Status == DreamServiceStatus.NotFound) return NotFound();
        if (result.Status == DreamServiceStatus.NotAllowed) return Forbid();

        result.Dream!.SelectedTagIds = result.Dream.Tags.Select(t => t.ID).ToList();
        ViewBag.AvailableTags = await tagService.GetAll();

        return View(result.Dream);
    }

    // POST: Dream/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Description,BudgetItems,Dreamers,SelectedTagIds")] DreamViewModel viewModel)
    {
        if (id != viewModel.ID)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.AvailableTags = await tagService.GetAll();
            return View(viewModel);
        }

        // Check cloud edit deadline before attempting update
        var cloud = await cloudService.GetById(viewModel.CloudID);
        if (!User.IsInRole(Constants.AdminRole) || !cloud.IsBeforeEditDeadline(appTime))
        {
            return BadRequest("The editing deadline for this cloud has passed.");
        }

        try
        {
            var result = await dreamService.Update(viewModel);
            if (result.Status == DreamServiceStatus.NotFound) return NotFound();
            if (result.Status == DreamServiceStatus.NotAllowed) return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!dreamService.DreamExists(viewModel.ID, cloudNameService.CloudID))
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
            return NotFound();

        var result = await dreamService.GetForDelete(id.Value);
        if (result.Status == DreamServiceStatus.NotFound) return NotFound();
        if (result.Status == DreamServiceStatus.NotAllowed) return Forbid();

        return View(result.Dream);
    }

    // POST: Dream/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await dreamService.Delete(id);
        if (result.Status == DreamServiceStatus.NotFound) return NotFound();
        if (result.Status == DreamServiceStatus.NotAllowed) return Forbid();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> FindDreamer([FromBody] FindDreamerViewModel request)
    {
        var dreamID = request.DreamID;
        var email = request.Email;

        // Check data
        if (string.IsNullOrWhiteSpace(email) || dreamID <= 0)
        {
            return BadRequest("Invalid data provided.");
        }

        // Check dream stuff
        var result = await dreamService.GetFull(dreamID);
        if (result.Status == DreamServiceStatus.NotFound)
            return NotFound("Dream not found or you do not have access to it.");
        if (result.Status == DreamServiceStatus.NotAllowed)
            return new ForbiddenResult("You are not allowed to add dreamers to this dream.");

        // Check user stuff
        var userToAdd = await userManager.FindByEmailAsync(email);
        if (userToAdd == null)
        {
            return NotFound("User not found.");
        }
        if (userToAdd.EmailConfirmed == false)
        {
            return BadRequest("This user has not confirmed their email address and cannot be added as a dreamer.");
        }
        if (result.Dream!.Dreamers.Any(d => d.UserID == userToAdd.Id))
        {
            return BadRequest("This user is already a dreamer for this dream.");
        }
        var userToAddClaims = await userManager.GetClaimsAsync(userToAdd);
        bool hasJoinedCloud = userToAddClaims.Any(c => c.Type == Constants.JOINED_CLOUD_CLAIM && c.Value == cloudNameService.CloudID.ToString());
        if (!hasJoinedCloud)
        {
            return BadRequest("This user has not joined the cloud and cannot be added as a dreamer.");
        }

        // All good, add the dreamer
        var newDreamer = new DreamerViewModel
        {
            UserID = userToAdd.Id,
            Name = userToAdd.Name,
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

    [HttpGet]
    public async Task<IActionResult> GetFundingModal(int dreamId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("User not authenticated.");

        var result = await dreamFundingService.GetFundingModal(dreamId, userId);
        return result.Status switch
        {
            FundDreamServiceStatus.Success => PartialView("_FundingModal", result.ViewModel),
            FundDreamServiceStatus.CantFundDream => BadRequest(result.Message),
            FundDreamServiceStatus.NoActiveFundingRound => BadRequest(result.Message),
            FundDreamServiceStatus.WalletNotFound => BadRequest(result.Message),
            _ => BadRequest("Unable to load funding data." + result.Message)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Fund(int dreamId, decimal amount)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("User not authenticated.");

        var result = await dreamFundingService.FundDream(dreamId, amount, userId);
        return result.Status switch
        {
            FundDreamServiceStatus.Success => RedirectToAction(nameof(Details), new { id = dreamId }),
            FundDreamServiceStatus.CantFundDream => BadRequest(),
            FundDreamServiceStatus.InvalidAmount => BadRequest(result.Message),
            FundDreamServiceStatus.WalletNotFound => BadRequest(result.Message),
            FundDreamServiceStatus.InsufficientFunds => BadRequest(result.Message),
            FundDreamServiceStatus.ExceedsRoundLimit => BadRequest(result.Message),
            FundDreamServiceStatus.NoActiveFundingRound => BadRequest(result.Message),
            _ => BadRequest("Unable to fund dream." + result.Message)
        };
    }


    private async Task<bool> CurrentUserHasJoinedCloud()
    {
        var userId = GetCurrentUserId();
        return !string.IsNullOrWhiteSpace(userId)
            && await cloudService.UserHasJoinedCloud(userId);
    }

    private string? GetCurrentUserId() => userManager.GetUserId(User);

    private async Task<IActionResult?> GetCreateDreamGuardResult()
    {
        if (!await CurrentUserHasJoinedCloud())
            return Problem("You have not joined this cloud yet, so you are not allowed to create a dream here.");

        if (!User.IsInRole(Constants.AdminRole) && !await cloudService.IsDreamingPeriodActive(cloudNameService.CloudID))
            return Problem("The dreaming period is not active, so you are not allowed to create a dream here.");

        return null;
    }
}