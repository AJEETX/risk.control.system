﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class CostCentreController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public CostCentreController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: CostCentre
        public async Task<IActionResult> Index(string searchString)
        {
            return _context.CostCentre != null ?
                        View(await _context.CostCentre.ToListAsync()) :
                        Problem("Entity set 'ApplicationDbContext.CostCentre'  is null.");
        }

        // GET: CostCentre/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.CostCentre == null)
            {
                return NotFound();
            }

            var costCentre = await _context.CostCentre
                .FirstOrDefaultAsync(m => m.CostCentreId == id);
            if (costCentre == null)
            {
                return NotFound();
            }

            return View(costCentre);
        }

        // GET: CostCentre/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CostCentre/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CostCentre costCentre)
        {
            if (costCentre is not null)
            {
                _context.Add(costCentre);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("cost centre created successfully!");
                return RedirectToAction(nameof(Index));
            }
            return View(costCentre);
        }

        // GET: CostCentre/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.CostCentre == null)
            {
                return NotFound();
            }

            var costCentre = await _context.CostCentre.FindAsync(id);
            if (costCentre == null)
            {
                return NotFound();
            }
            return View(costCentre);
        }

        // POST: CostCentre/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("CostCentreId,Name,Code,Created,Updated,UpdatedBy")] CostCentre costCentre)
        {
            if (id != costCentre.CostCentreId)
            {
                return NotFound();
            }

            if (costCentre is not null)
            {
                try
                {
                    _context.Update(costCentre);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CostCentreExists(costCentre.CostCentreId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                toastNotification.AddSuccessToastMessage("cost centre edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            return View(costCentre);
        }

        // GET: CostCentre/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.CostCentre == null)
            {
                return NotFound();
            }

            var costCentre = await _context.CostCentre
                .FirstOrDefaultAsync(m => m.CostCentreId == id);
            if (costCentre == null)
            {
                return NotFound();
            }

            return View(costCentre);
        }

        // POST: CostCentre/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.CostCentre == null)
            {
                return Problem("Entity set 'ApplicationDbContext.CostCentre'  is null.");
            }
            var costCentre = await _context.CostCentre.FindAsync(id);
            if (costCentre != null)
            {
                _context.CostCentre.Remove(costCentre);
            }

            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("cost centre deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool CostCentreExists(string id)
        {
            return (_context.CostCentre?.Any(e => e.CostCentreId == id)).GetValueOrDefault();
        }
    }
}