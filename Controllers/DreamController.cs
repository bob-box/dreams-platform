using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;

namespace Borderland.Dreams.Controllers
{
    public class DreamController : BaseController
    {
        public DreamController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Dream
        public async Task<IActionResult> Index()
        {
            var list = await _context.Dream
                .Include(d => d.Budget)
                .Where(x => x.CloudID == GetCloudID())
                .ToListAsync();

            return View(list);
        }

        // GET: Dream/Details/5
        public async Task<IActionResult> Details(int? id)
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
        public async Task<IActionResult> Create([Bind("ID,Name,Description,CreatedDate,Status,BudgetID")] Dream dream)
        {
            if (ModelState.IsValid)
            {
                dream.CloudID = GetCloudID(); // Set the CloudID from the current context
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
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Description,CreatedDate,Status,BudgetID")] Dream dream)
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
                .Where(x => x.CloudID == GetCloudID())
                .SingleOrDefaultAsync(m => m.ID == id);

            if (dream != null)
            {
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
}
