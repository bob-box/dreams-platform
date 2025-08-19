using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;

namespace Borderland.Dreams.Controllers
{
    public class DreamController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DreamController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Dream
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Dream.Include(d => d.Budget);
            return View(await applicationDbContext.ToListAsync());
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
                .FirstOrDefaultAsync(m => m.ID == id);
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

            var dream = await _context.Dream.FindAsync(id);
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
                .FirstOrDefaultAsync(m => m.ID == id);
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
            var dream = await _context.Dream.FindAsync(id);
            if (dream != null)
            {
                _context.Dream.Remove(dream);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DreamExists(int id)
        {
            return _context.Dream.Any(e => e.ID == id);
        }
    }
}
