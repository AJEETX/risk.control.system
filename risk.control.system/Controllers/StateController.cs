using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("General Setup")]
    public class StateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public StateController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: RiskCaseStatus
        public async Task<IActionResult> Index()
        {
            return RedirectToAction("Profile");
        }
        [Breadcrumb("State")]
        public async Task<IActionResult> Profile()
        {
            var statesResult = await _context.State.Include(s => s.Country).ToListAsync();
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View(statesResult);
        }
        // GET: RiskCaseStatus/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long id)
        {
            if (id == null || _context.State == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            var state = await _context.State.Include(s => s.Country)
                .FirstOrDefaultAsync(m => m.StateId == id);
            if (state == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            return View(state);
        }

        [Breadcrumb("Add New", FromAction ="Profile")]
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(State state)
        {
            state.Updated = DateTime.Now;
            state.UpdatedBy = HttpContext.User?.Identity?.Name;
            _context.Add(state);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("state created successfully!");
            return RedirectToAction(nameof(Index));
        }

        // GET: RiskCaseStatus/Edit/5
        [Breadcrumb("Edit", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id == null || _context.State == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            var state = await _context.State.Include(s => s.Country).FirstOrDefaultAsync(c => c.StateId == id);
            if (state == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", state.CountryId);

            return View(state);
        }

        // POST: RiskCaseStatus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, State state)
        {
            if (id != state.StateId)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            if (state is not null)
            {
                try
                {
                    state.Updated = DateTime.Now;
                    state.UpdatedBy = HttpContext.User?.Identity?.Name;
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
                toastNotification.AddSuccessToastMessage("state edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            toastNotification.AddErrorToastMessage("Error to edit state!");
            return View(state);
        }

        // GET: RiskCaseStatus/Delete/5
        [Breadcrumb("Delete", FromAction = "Profile")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id == null || _context.State == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            var state = await _context.State
                .FirstOrDefaultAsync(m => m.StateId == id);
            if (state == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            return View(state);
        }

        // POST: RiskCaseStatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.State == null)
            {
                return Problem("Entity set 'ApplicationDbContext.State'  is null.");
            }
            var state = await _context.State.FindAsync(id);
            if (state != null)
            {
                state.Updated = DateTime.Now;
                state.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.State.Remove(state);
            }

            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("state deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool StateExists(long id)
        {
            return (_context.State?.Any(e => e.StateId == id)).GetValueOrDefault();
        }
    }
}