using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class ClaimsInvestigationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClaimsInvestigationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ClaimsInvestigation
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ClaimsInvestigation.
                Include(c => c.BeneficiaryRelation)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ClaimsInvestigation/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationCaseId == id);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }

            return View(claimsInvestigation);
        }

        // GET: ClaimsInvestigation/Create
        public IActionResult Create()
        {
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name");
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name");
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            return View();
        }

        // POST: ClaimsInvestigation/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClaimsInvestigation claimsInvestigation)
        {
            if (ModelState.IsValid)
            {
                _context.Add(claimsInvestigation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", claimsInvestigation.BeneficiaryRelationId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.CostCentreId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.DistrictId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.LineOfBusinessId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.StateId);
            return View(claimsInvestigation);
        }

        // GET: ClaimsInvestigation/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(id);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", claimsInvestigation.BeneficiaryRelationId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.CostCentreId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.DistrictId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.LineOfBusinessId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.StateId);
            return View(claimsInvestigation);
        }

        // POST: ClaimsInvestigation/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ClaimsInvestigation claimsInvestigation)
        {
            if (id != claimsInvestigation.ClaimsInvestigationCaseId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(claimsInvestigation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClaimsInvestigationExists(claimsInvestigation.ClaimsInvestigationCaseId))
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
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", claimsInvestigation.BeneficiaryRelationId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.CostCentreId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.DistrictId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.LineOfBusinessId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.StateId);
            return View(claimsInvestigation);
        }

        // GET: ClaimsInvestigation/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationCaseId == id);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }

            return View(claimsInvestigation);
        }

        // POST: ClaimsInvestigation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.ClaimsInvestigation == null)
            {
                return Problem("Entity set 'ApplicationDbContext.ClaimsInvestigation'  is null.");
            }
            var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(id);
            if (claimsInvestigation != null)
            {
                _context.ClaimsInvestigation.Remove(claimsInvestigation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClaimsInvestigationExists(string id)
        {
            return (_context.ClaimsInvestigation?.Any(e => e.ClaimsInvestigationCaseId == id)).GetValueOrDefault();
        }
    }
}
