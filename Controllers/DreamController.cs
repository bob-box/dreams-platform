using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Enums;
using Microsoft.AspNetCore.Authorization;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Services;

namespace Borderland.Dreams.Controllers;

[Authorize]
public class DreamController(ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    DreamService dreamService)
    : BaseController(context, authorizationService, userManager)
{
    // GET: Dream
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        // TODO : put this in a filter?
        if (!TryGetCloudID(out int cloudID))
        {
            return RedirectToAction("Index", "Home", new { cloudName = "" });
        }

        var listQuery = dreamService.GetDreamsByCloudIdBaseQuery(cloudID);
        listQuery = IsAuthorizedToViewDream(listQuery);

        bool userHasAlreadyJoinedTheCloud = User.Claims.Any(c => c.Type == Constants.JOINED_CLOUD_CLAIM && c.Value == cloudID.ToString());
        ViewData["showJoinCloudButton"] = !userHasAlreadyJoinedTheCloud && User.Identity?.IsAuthenticated == true;
        ViewData["CloudID"] = cloudID;

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

        if (!TryGetCloudID(out int cloudID))
        {
            return RedirectToAction("Index", "Home", new { cloudName = "" });
        }

        var dreamQuery = dreamService.GetDreamsByCloudIdBaseQuery(cloudID);
        dreamQuery = IsAuthorizedToViewDream(dreamQuery);

        var dream = await dreamQuery.SingleOrDefaultAsync(m => m.ID == id);
        var viewModel = dream.ToViewModel();

        return viewModel == null ? NotFound() : View(viewModel);
    }

    // GET: Dream/Create
    public IActionResult Create()
    {
        if (!TryGetCloudID(out int cloudID))
        {
            return RedirectToAction("Index", "Home", new { cloudName = "" });
        }
        bool userHasAlreadyJoinedTheCloud = User.Claims.Any(c => c.Type == Constants.JOINED_CLOUD_CLAIM && c.Value == cloudID.ToString());
        if (!userHasAlreadyJoinedTheCloud)
        {
            return Problem("You have not joined this cloud yet, so you are not allowed to create a dream here.");
        }

        return View(new Dream());
    }

    // POST: Dream/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Description,BudgetItems")] Dream dream)
    {
        if (!TryGetCloudID(out int cloudID))
        {
            return RedirectToAction("Index", "Home", new { cloudName = "" });
        }

        bool userHasAlreadyJoinedTheCloud = User.Claims.Any(c => c.Type == Constants.JOINED_CLOUD_CLAIM && c.Value == cloudID.ToString());
        if (!userHasAlreadyJoinedTheCloud)
        {
            return Problem("You have not joined this cloud yet, so you are not allowed to create a dream here.");
        }

        if (!ModelState.IsValid)
        {
            return View(dream);
        }

        dream.Dreamers.Add(new Dreamer
        {
            User = await _userManager.GetUserAsync(User),
            IsMainDreamer = true,
            DreamID = dream.ID
        });
        dream.CloudID = cloudID; // Set the CloudID from the current context
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
        if (!TryGetCloudID(out int cloudID))
        {
            return RedirectToAction("Index", "Home", new { cloudName = "" });
        }

        var dreamQuery = dreamService.GetDreamsByCloudIdBaseQuery(cloudID);
        var dream = await dreamQuery.SingleOrDefaultAsync(m => m.ID == id);

        if (dream == null)
        {
            return NotFound();
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, dream, Operations.Update);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        CheckIfUserCanEditDreamers(dream);
        return View(dream);
    }

    // POST: Dream/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Description,BudgetItems,Dreamers")] Dream dream)
    {
        if (id != dream.ID)
        {
            return NotFound();
        }
        if (!TryGetCloudID(out var cloudID))
        {
            return RedirectToAction("Index", "Home", new { cloudName = "" });
        }

        // We need to fetch the dreams dreamers to check authorization
        var dreamFromDb = await dreamService.GetDreamsByCloudIdBaseQuery(cloudID)
                .SingleOrDefaultAsync(m => m.ID == id);

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, dreamFromDb, Operations.Update);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }
        if (!ModelState.IsValid)
        {
            CheckIfUserCanEditDreamers(dreamFromDb);
            return View(dream);
        }

        try
        {
            // Update the dream
            dreamFromDb.Name = dream.Name;
            dreamFromDb.Description = dream.Description;
            dreamFromDb.Dreamers = dream.Dreamers.Where(x => x != null).ToList();
            dreamFromDb.BudgetItems = dream.BudgetItems.Where(b => b != null).ToList();

            if (!dreamFromDb.Dreamers.Any(x => x.IsMainDreamer))
            {
                return BadRequest("A dream must have at least one main dreamer.");
            }
    
            _context.Entry(dreamFromDb).State = EntityState.Modified;

            // Ensure some properties are not modified
            foreach (var item in dreamFromDb.BudgetItems)
            {
                _context.Entry(item).Property(x => x.CreatedDate).IsModified = false;
                _context.Entry(item).Property(x => x.DreamID).IsModified = false;
            }

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!DreamExists(dream.ID, cloudID))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        return RedirectToAction(nameof(Details), new { id = dream.ID });
    }

    // GET: Dream/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        if (!TryGetCloudID(out int cloudID))
        {
            return RedirectToAction("Index", "Home", new { cloudName = "" });
        }

        var dream = await dreamService.GetDreamsByCloudIdBaseQuery(cloudID)
            .SingleOrDefaultAsync(m => m.ID == id);

        if (dream == null)
        {
            return NotFound();
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, dream, Operations.Delete);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        return View(dream);
    }

    // POST: Dream/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!TryGetCloudID(out int cloudID))
        {
            return RedirectToAction("Index", "Home", new { cloudName = "" });
        }

        var dream = await dreamService.GetDreamsByCloudIdBaseQuery(cloudID)
            .SingleOrDefaultAsync(m => m.ID == id);

        if (dream == null)
        {
            return NotFound();
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, dream, Operations.Delete);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }
        _context.Dream.Remove(dream);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> FindDreamer([FromBody] FindDreamerViewModel request)
    {
        var dreamID = request.DreamID;
        var username = request.Username;

        // Check data
        if (string.IsNullOrWhiteSpace(username) || dreamID <= 0)
        {
            return BadRequest("Invalid data provided.");
        }
        if (!TryGetCloudID(out int cloudID))
        {
            return BadRequest("Cloud context is missing.");
        }

        // Check dream stuff
        var dream = await dreamService.GetDreamsByCloudIdBaseQuery(cloudID)
            .SingleOrDefaultAsync(d => d.ID == dreamID);

        if (dream == null)
        {
            return NotFound("Dream not found or you do not have access to it.");
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, dream, Operations.EditDreamers);
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
        if (dream.Dreamers.Any(d => d.UserID == userToAdd.Id))
        {
            return BadRequest("This user is already a dreamer for this dream.");
        }
        
        var currentUserId = _userManager.GetUserId(User);
        if (userToAdd.Id == currentUserId)
        {
            return BadRequest("This is your own dream, silly!");
        }
        
        var userClaims = await _userManager.GetClaimsAsync(userToAdd);
        bool hasJoinedCloud = userClaims.Any(c => c.Type == Constants.JOINED_CLOUD_CLAIM && c.Value == cloudID.ToString());
        if (!hasJoinedCloud)
        {
            return BadRequest("This user has not joined the cloud and cannot be added as a dreamer.");
        }

        // All good, add the dreamer
        var newDreamer = new Dreamer
        {
            UserID = userToAdd.Id,
            User = userToAdd,
            DreamID = dream.ID,
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
        return PartialView("~/Views/Shared/EditorTemplates/BudgetItem.cshtml", new BudgetItem());
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

    private void CheckIfUserCanEditDreamers(Dream dreamFromDb)
    {
        var authorizationResult = _authorizationService.AuthorizeAsync(User, dreamFromDb, Operations.EditDreamers).Result;
        ViewData["canEditDreamers"] = authorizationResult.Succeeded; // Needed for the delete button to be shown
    }
}