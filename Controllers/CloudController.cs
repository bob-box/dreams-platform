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
    public class CloudController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CloudController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cloud
        public async Task<IActionResult> Index()
        {
            return View(await _context.Cloud.ToListAsync());
        }

        // GET: Cloud/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cloud = await _context.Cloud
                .FirstOrDefaultAsync(m => m.ID == id);
            if (cloud == null)
            {
                return NotFound();
            }

            return View(cloud);
        }

        // GET: Cloud/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Cloud/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,CreatedDate")] Cloud cloud)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cloud);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cloud);
        }

        // GET: Cloud/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cloud = await _context.Cloud.FindAsync(id);
            if (cloud == null)
            {
                return NotFound();
            }
            return View(cloud);
        }

        // POST: Cloud/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,CreatedDate")] Cloud cloud)
        {
            if (id != cloud.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cloud);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CloudExists(cloud.ID))
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
            return View(cloud);
        }

        // GET: Cloud/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cloud = await _context.Cloud
                .FirstOrDefaultAsync(m => m.ID == id);
            if (cloud == null)
            {
                return NotFound();
            }

            return View(cloud);
        }

        // POST: Cloud/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cloud = await _context.Cloud.FindAsync(id);
            if (cloud != null)
            {
                _context.Cloud.Remove(cloud);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CloudExists(int id)
        {
            return _context.Cloud.Any(e => e.ID == id);
        }
    }
}
