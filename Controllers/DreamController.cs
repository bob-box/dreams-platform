using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Extensions;
using Microsoft.AspNetCore.Authorization;
using Borderland.Dreams.Authorization;

namespace Borderland.Dreams.Controllers;

public class DreamController(ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager)
    : BaseController(context, authorizationService, userManager)
{
    // GET: Dream
    public async Task<IActionResult> Index()
    {
        var listQuery = _context.Dream
            .Include(d => d.Budget)
            .Include(d => d.Dreamers).ThenInclude(x => x.User)
            .Where(x => x.CloudID == GetCloudID());

        var isAuthorizedToViewAllDreams = User.IsInRole(Constants.ManagersRole) ||
                       User.IsInRole(Constants.AdministratorsRole);

        // Hide dreams in draft status
        // Admins is allowed to see all dreams, no matter what status they have.
        // Main dreamer and co-dreamers are also allowed to see their dreams :)
        if (!isAuthorizedToViewAllDreams)
        {
            var currentUserId = _userManager.GetUserId(User);
            listQuery = listQuery.Where(x => x.Status >= DreamStatus.Active
                                || x.Dreamers.Any(d => d.User.Id == currentUserId));
        }

        return View(await listQuery.ToListAsync());
    }

    // GET: Dream/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var isAuthorizedToViewAllDreams = User.IsInRole(Constants.ManagersRole) ||
                       User.IsInRole(Constants.AdministratorsRole);

        
        var dreamQuery = _context.Dream
            .Include(d => d.Budget)
            .Include(d => d.Dreamers).ThenInclude(d => d.User)
            .Where(x => x.CloudID == GetCloudID());

        // Admins is allowed to see all dreams
        // Main dreamer and co-dreamers are also allowed to see their dreams :)
        if (!isAuthorizedToViewAllDreams)
        {
            var currentUserId = _userManager.GetUserId(User);
            dreamQuery = dreamQuery.Where(x => x.Status >= DreamStatus.Active
                                || x.Dreamers.Any(d => d.User.Id == currentUserId));
        }

        var dream = await dreamQuery.SingleOrDefaultAsync(m => m.ID == id);
        if (dream == null)
        {
            return NotFound();
        }

        return View(dream);
    }

    // GET: Dream/Create
    public IActionResult Create()
    {
        ViewData["BudgetID"] = new SelectList(_context.Set<DreamBudget>(), "ID", "ID");
        return View();
    }

    // POST: Dream/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Description")] Dream dream)
    {
        if (ModelState.IsValid)
        {
            dream.Dreamers.Add(new Dreamer
            {
                User = await _userManager.GetUserAsync(User),
                IsMainDreamer = true,
                Dream = dream
            });
            dream.CloudID = GetCloudID(); // Set the CloudID from the current context
            dream.Status = DreamStatus.Draft;

            _context.Add(dream);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["BudgetID"] = new SelectList(_context.Set<DreamBudget>(), "ID", "ID", dream.BudgetID);
        return View(dream);
    }

    // GET: Dream/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var dream = await _context.Dream
            .Where(x => x.CloudID == GetCloudID())
            .SingleOrDefaultAsync(m => m.ID == id);

        if (dream == null)
        {
            return NotFound();
        }
        ViewData["BudgetID"] = new SelectList(_context.Set<DreamBudget>(), "ID", "ID", dream.BudgetID);
        return View(dream);
    }

    // POST: Dream/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Description")] Dream dream)
    {
        if (id != dream.ID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(dream);
                _context.Entry(dream).Property(x => x.CloudID).IsModified = false; // Prevent modification of CloudID
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DreamExists(dream.ID))
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
        ViewData["BudgetID"] = new SelectList(_context.Set<DreamBudget>(), "ID", "ID", dream.BudgetID);
        return View(dream);
    }

    // GET: Dream/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var dream = await _context.Dream
            .Include(d => d.Budget)
            .Where(x => x.CloudID == GetCloudID())
            .SingleOrDefaultAsync(m => m.ID == id);
        if (dream == null)
        {
            return NotFound();
        }

        return View(dream);
    }

    // POST: Dream/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var dream = await _context.Dream
            .Include(x => x.Dreamers).ThenInclude(d => d.User)
            .Where(x => x.CloudID == GetCloudID())
            .SingleOrDefaultAsync(m => m.ID == id);

        if (dream != null)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(
                                                        User, dream,
                                                        DreamOperations.Delete);
            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }
            _context.Dream.Remove(dream);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool DreamExists(int id)
    {
        return _context.Dream.Any(e => e.ID == id && e.CloudID == GetCloudID());
    }
}