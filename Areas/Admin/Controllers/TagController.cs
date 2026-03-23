using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class TagController(ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    ICloudNameResolver cloudNameResolver)
    : Dreams.Controllers.BaseController(context, authorizationService, userManager)
{
    // GET: Admin/Tag
    public async Task<IActionResult> Index()
    {
        var tags = await _context.Tag
            .Where(t => t.CloudID == cloudNameResolver.CloudID)
            .OrderBy(t => t.Title)
            .Select(t => t.ToViewModel())
            .ToListAsync();
        return View(tags);
    }

    // GET: Admin/Tag/Create
    public IActionResult Create() => View(new TagViewModel());

    // POST: Admin/Tag/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title")] TagViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(viewModel);

        var tag = new Tag
        {
            Title = viewModel.Title,
            CloudID = cloudNameResolver.CloudID
        };

        _context.Tag.Add(tag);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/Tag/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var tag = await GetTagInCloudAsync(id.Value);
        if (tag == null)
            return NotFound();

        return View(tag.ToViewModel());
    }

    // POST: Admin/Tag/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ID,Title")] TagViewModel viewModel)
    {
        if (id != viewModel.ID)
            return NotFound();

        if (!ModelState.IsValid)
            return View(viewModel);

        var tag = await GetTagInCloudAsync(id);
        if (tag == null)
            return NotFound();

        tag.Title = viewModel.Title;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/Tag/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var tag = await GetTagInCloudAsync(id.Value);
        if (tag == null)
            return NotFound();

        return View(tag.ToViewModel());
    }

    // POST: Admin/Tag/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var tag = await GetTagInCloudAsync(id);
        if (tag == null)
            return NotFound();

        _context.Tag.Remove(tag);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<Tag?> GetTagInCloudAsync(int id) =>
    await _context.Tag
        .FirstOrDefaultAsync(t => t.ID == id && t.CloudID == cloudNameResolver.CloudID);
}
