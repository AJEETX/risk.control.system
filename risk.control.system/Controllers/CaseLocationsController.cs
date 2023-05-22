using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class CaseLocationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public CaseLocationsController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: CaseLocations
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.CaseLocation.Include(c => c.District).Include(c => c.State);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: CaseLocations/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null || _context.CaseLocation == null)
            {
                return NotFound();
            }

            var caseLocation = await _context.CaseLocation
                .Include(c => c.PincodeServices)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(m => m.CaseLocationId == id);
            if (caseLocation == null)
            {
                return NotFound();
            }

            return View(caseLocation);
        }

        // GET: CaseLocations/Create
        public IActionResult Create(string id)
        {
            var claim = _context.ClaimsInvestigation
                .Include(i => i.CaseLocations)
                .ThenInclude(c => c.District)
                                .Include(i => i.CaseLocations)
                .ThenInclude(c => c.State)
                .Include(i => i.CaseLocations)
                .ThenInclude(c => c.Country)
                                .Include(i => i.CaseLocations)
                .ThenInclude(c => c.PincodeServices)
                .FirstOrDefault(v => v.ClaimsInvestigationId == id);

            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            var model = new CaseLocation { SelectedMultiPincodeId = new List<string>(), ClaimsInvestigation = claim, PincodeServices = new List<VerifyPinCode>() };

            return View(model);
        }

        // POST: CaseLocations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CaseLocation caseLocation)
        {
            if (caseLocation is not null)
            {
                var selectedPinCodes = await _context.PinCode.Where(p => caseLocation.SelectedMultiPincodeId.Contains(p.PinCodeId)).ToListAsync();
                var selectedVerifyPinCodes = selectedPinCodes.Select(p =>
                    new VerifyPinCode
                    {
                        Name = p.Name,
                        Pincode = p.Code,
                        CaseLocationId = caseLocation.CaseLocationId,
                        CaseLocation = caseLocation,
                    }).ToList();

                var existingCaseLocations = _context.CaseLocation
                    .Include(c=>c.PincodeServices)
                    .Where(c => c.ClaimsInvestigationId == caseLocation.ClaimsInvestigationId);

                var existingVerifyPincodes= _context.VerifyPinCode
                    .Where(v=> existingCaseLocations.Any(e=>e.CaseLocationId == v.CaseLocationId))?.ToList();

                if(existingVerifyPincodes is not null && existingVerifyPincodes.Any())
                {
                    var existingPicodes= existingVerifyPincodes.Select(e=>e.Pincode);
                }
                //var existingPinCodeServices = _c

                caseLocation.PincodeServices = selectedVerifyPinCodes;
                _context.Add(caseLocation);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("verification location created successfully!");
                return RedirectToAction(nameof(ClaimsInvestigationController.CaseLocation), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", caseLocation.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "DistrictId", caseLocation.DistrictId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "StateId", caseLocation.StateId);
            return View(caseLocation);
        }

        // GET: CaseLocations/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null || _context.CaseLocation == null)
            {
                return NotFound();
            }

            var caseLocation = await _context.CaseLocation.FindAsync(id);
            if (caseLocation == null)
            {
                return NotFound();
            }

            var services = _context.CaseLocation
                .Include(v => v.ClaimsInvestigation)
                .Include(v => v.PincodeServices)
                .First(v => v.CaseLocationId == id);

            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", caseLocation.DistrictId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", caseLocation.StateId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", caseLocation.CountryId);

            var selected = services.PincodeServices.Select(s => s.Pincode).ToList();
            services.SelectedMultiPincodeId = _context.PinCode.Where(p => selected.Contains(p.Code)).Select(p => p.PinCodeId).ToList();

            ViewBag.PinCodeId = _context.PinCode.Where(p => p.District.DistrictId == caseLocation.DistrictId)
                .Select(x => new SelectListItem
                {
                    Text = x.Name + " - " + x.Code,
                    Value = x.PinCodeId.ToString()
                }).ToList();

            return View(services);
        }

        // POST: CaseLocations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, CaseLocation caseLocation)
        {
            if (id != caseLocation.CaseLocationId)
            {
                return NotFound();
            }

            if (caseLocation is not null)
            {
                try
                {
                    if (caseLocation.SelectedMultiPincodeId.Count > 0)
                    {
                        var existingVerifyPincodes = _context.CaseLocation.Where(s => s.CaseLocationId == caseLocation.CaseLocationId);
                        _context.CaseLocation.RemoveRange(existingVerifyPincodes);

                        var pinCodeDetails = _context.PinCode.Where(p => caseLocation.SelectedMultiPincodeId.Contains(p.PinCodeId));

                        var pinCodesWithId = pinCodeDetails.Select(p => new VerifyPinCode
                        {
                            Pincode = p.Code,
                            Name = p.Name,
                            CaseLocationId = caseLocation.CaseLocationId
                        }).ToList();

                        _context.VerifyPinCode.AddRange(pinCodesWithId);

                        caseLocation.PincodeServices = pinCodesWithId;
                        _context.Update(caseLocation);
                        await _context.SaveChangesAsync();
                        toastNotification.AddSuccessToastMessage("verification location edited successfully!");
                        return RedirectToAction(nameof(ClaimsInvestigationController.CaseLocation), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });
                    }

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CaseLocationExists(caseLocation.CaseLocationId))
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
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "DistrictId", caseLocation.DistrictId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "StateId", caseLocation.StateId);
            return View(caseLocation);
        }

        // GET: CaseLocations/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null || _context.CaseLocation == null)
            {
                return NotFound();
            }

            var caseLocation = await _context.CaseLocation
                .Include(c => c.PincodeServices)
                .Include(c => c.District)
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.CaseLocationId == id);
            if (caseLocation == null)
            {
                return NotFound();
            }

            return View(caseLocation);
        }

        // POST: CaseLocations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.CaseLocation == null)
            {
                return Problem("Entity set 'ApplicationDbContext.CaseLocation'  is null.");
            }
            var caseLocation = await _context.CaseLocation.FindAsync(id);
            if (caseLocation != null)
            {
                _context.CaseLocation.Remove(caseLocation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });
        }

        private bool CaseLocationExists(long id)
        {
            return (_context.CaseLocation?.Any(e => e.CaseLocationId == id)).GetValueOrDefault();
        }
    }
}
