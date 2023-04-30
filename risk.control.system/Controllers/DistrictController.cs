using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class DistrictController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DistrictController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: District
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.District.Include(d => d.Country).Include(d => d.State);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: District/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.District == null)
            {
                return NotFound();
            }

            var district = await _context.District
                .Include(d => d.Country)
                .Include(d => d.State)
                .FirstOrDefaultAsync(m => m.DistrictId == id);
            if (district == null)
            {
                return NotFound();
            }

            return View(district);
        }

        // GET: District/Create
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View();
        }

        // POST: District/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(District district)
        {
            if (district is not null)
            {
                _context.Add(district);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return Problem();
        }

        // GET: District/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.District == null)
            {
                return NotFound();
            }

            var district = await _context.District.FindAsync(id);
            if (district == null)
            {
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", district.CountryId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", district.StateId);
            return View(district);
        }

        // POST: District/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, District district)
        {
            if (id != district.DistrictId)
            {
                return NotFound();
            }

            if (district is not null)
            {
                try
                {
                    _context.Update(district);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DistrictExists(district.DistrictId))
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
            return Problem();
        }

        // GET: District/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.District == null)
            {
                return NotFound();
            }

            var district = await _context.District
                .Include(d => d.Country)
                .Include(d => d.State)
                .FirstOrDefaultAsync(m => m.DistrictId == id);
            if (district == null)
            {
                return NotFound();
            }

            return View(district);
        }

        // POST: District/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.District == null)
            {
                return Problem("Entity set 'ApplicationDbContext.District'  is null.");
            }
            var district = await _context.District.FindAsync(id);
            if (district != null)
            {
                _context.District.Remove(district);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DistrictExists(string id)
        {
          return (_context.District?.Any(e => e.DistrictId == id)).GetValueOrDefault();
        }
    }
}
