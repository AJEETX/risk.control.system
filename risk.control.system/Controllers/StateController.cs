using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class StateController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StateController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RiskCaseStatus
        public async Task<IActionResult> Index(string sortOrder,string currentFilter, string searchString, int? currentPage, int pageSize = 10)
        {
            ViewBag.CountrySortParm = String.IsNullOrEmpty(sortOrder) ? "country_desc" : "";
            ViewBag.StateSortParm = sortOrder == "State" ? "state_desc" : "State";
            if (searchString != null)
            {
                currentPage = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;
            var states = _context.State.Include(s => s.Country).AsQueryable();
            if (!String.IsNullOrEmpty(searchString))
            {
                states = states.Where(s =>
                 s.Name.ToLower().Contains(searchString.Trim().ToLower()) ||
                 s.Country.Name.ToLower().Contains(searchString.Trim().ToLower()) ||
                 s.Code.ToLower().Contains(searchString.Trim().ToLower())
                 );
            }

            switch (sortOrder)
            {
                case "country_desc":
                    states = states.OrderByDescending(s => s.Country.Name);
                    break;
                case "state_desc":
                    states = states.OrderBy(s => s.Name);
                    break;
                default:
                    states = states.OrderBy(s => s.Name);
                    break;
            }
            int pageNumber = (currentPage ?? 1);
            ViewBag.TotalPages = (int)Math.Ceiling(decimal.Divide(states.Count(), pageSize));
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.ShowPrevious = pageNumber > 1;
            ViewBag.ShowNext = pageNumber < (int)Math.Ceiling(decimal.Divide(states.Count(), pageSize));
            ViewBag.ShowFirst = pageNumber != 1;
            ViewBag.ShowLast = pageNumber != (int)Math.Ceiling(decimal.Divide(states.Count(), pageSize));

            var statesResult = await states.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return _context.State != null ?
                        View(statesResult) :
                        Problem("Entity set 'ApplicationDbContext.State'  is null.");
        }

        // GET: RiskCaseStatus/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.State == null)
            {
                return NotFound();
            }

            var state = await _context.State.Include(s => s.Country)
                .FirstOrDefaultAsync(m => m.StateId == id);
            if (state == null)
            {
                return NotFound();
            }

            return View(state);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string countryId, string stateName, string stateCode)
        {
            _context.Add(new State
            {
                Name = stateName.Trim().ToUpper(),
                CountryId = countryId,
                Code = stateCode.Trim().ToUpper()
            });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: RiskCaseStatus/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.State == null)
            {
                return NotFound();
            }

            var state = await _context.State.Include(s => s.Country).FirstOrDefaultAsync(c => c.StateId == id);
            if (state == null)
            {
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            return View(state);
        }

        // POST: RiskCaseStatus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, State state)
        {
            if (id != state.StateId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(state);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StateExists(state.StateId))
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
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            return View(state);
        }

        // GET: RiskCaseStatus/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.State == null)
            {
                return NotFound();
            }

            var state = await _context.State
                .FirstOrDefaultAsync(m => m.StateId == id);
            if (state == null)
            {
                return NotFound();
            }

            return View(state);
        }

        // POST: RiskCaseStatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.State == null)
            {
                return Problem("Entity set 'ApplicationDbContext.State'  is null.");
            }
            var state = await _context.State.FindAsync(id);
            if (state != null)
            {
                _context.State.Remove(state);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StateExists(string id)
        {
            return (_context.State?.Any(e => e.StateId == id)).GetValueOrDefault();
        }
    }
}
