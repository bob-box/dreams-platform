using Borderland.Dreams.Authorization;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class TagController(
    ITagService tagService
) : Controller
{
    // GET: Admin/Tag
    public async Task<IActionResult> Index()
    {
        var tags = await tagService.GetAll();
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

        await tagService.Create(viewModel);
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/Tag/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var viewModel = await tagService.GetById(id.Value);
        if (viewModel == null)
            return NotFound();

        return View(viewModel);
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

        var updated = await tagService.Update(viewModel);
        if (!updated)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/Tag/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var viewModel = await tagService.GetById(id.Value);
        if (viewModel == null)
            return NotFound();

        return View(viewModel);
    }

    // POST: Admin/Tag/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var deleted = await tagService.Delete(id);
        if (!deleted)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }
}
