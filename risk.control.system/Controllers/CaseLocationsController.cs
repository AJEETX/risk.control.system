using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

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
        [Breadcrumb("Location", FromController = typeof(ClaimsInvestigationController), FromAction = "Draft")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.CaseLocation.Include(c => c.District).Include(c => c.State);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: CaseLocations/Details/5

        public async Task<IActionResult> AssignerDetails(long? id)
        {
            if (id == null || _context.CaseLocation == null)
            {
                return NotFound();
            }

            var caseLocation = await _context.CaseLocation
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(m => m.CaseLocationId == id);
            if (caseLocation == null)
            {
                return NotFound();
            }

            return View(caseLocation);
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null || _context.CaseLocation == null)
            {
                return NotFound();
            }

            var caseLocation = await _context.CaseLocation
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
        //[Breadcrumb("Create", FromController = typeof(ClaimsInvestigationController), FromAction = "Details")]
        [Breadcrumb("Location", FromController = typeof(ClaimsInvestigationController), FromAction = "Draft")]
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
                .FirstOrDefault(v => v.ClaimsInvestigationId == id);

            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");

            var model = new CaseLocation { ClaimsInvestigation = claim };

            var activeClaims = new MvcBreadcrumbNode("Index", "ClaimsInvestigation", "Claims");
            var incompleteClaims = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Draft") { Parent = activeClaims };

            var incompleteClaim = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", "Details") { Parent = incompleteClaims, RouteValues = new { id = id } };

            var locationPage = new MvcBreadcrumbNode("Add", "CaseLocations", "Add Beneficiary") { Parent = incompleteClaim, RouteValues = new { id = id } };

            ViewData["BreadcrumbNode"] = locationPage;
            return View(model);
        }

        // POST: CaseLocations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CaseLocation caseLocation)
        {
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
               i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);

            if (caseLocation is not null)
            {
                caseLocation.Updated = DateTime.UtcNow;
                caseLocation.UpdatedBy = HttpContext.User?.Identity?.Name;
                caseLocation.InvestigationCaseSubStatusId = createdStatus.InvestigationCaseSubStatusId;

                IFormFile? customerDocument = Request.Form?.Files?.FirstOrDefault();
                if (customerDocument is not null)
                {
                    var messageDocumentFileName = Path.GetFileNameWithoutExtension(customerDocument.FileName);
                    var extension = Path.GetExtension(customerDocument.FileName);
                    caseLocation.ProfileImage = customerDocument;
                    using var dataStream = new MemoryStream();
                    await caseLocation.ProfileImage.CopyToAsync(dataStream);
                    caseLocation.ProfilePicture = dataStream.ToArray();
                }

                _context.Add(caseLocation);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("Location added successfully!");
                return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", caseLocation.CountryId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", caseLocation.BeneficiaryRelationId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", caseLocation.DistrictId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "StateId", caseLocation.StateId);
            return View(caseLocation);
        }

        // GET: CaseLocations/Edit/5
        [Breadcrumb("Edit ")]
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
                .Include(v => v.District)
                .First(v => v.CaseLocationId == id);

            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", caseLocation.DistrictId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", caseLocation.StateId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", caseLocation.CountryId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", caseLocation.BeneficiaryRelationId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", caseLocation.PinCodeId);

            var activeClaims = new MvcBreadcrumbNode("Index", "ClaimsInvestigation", "Claims");
            var incompleteClaims = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Draft") { Parent = activeClaims };

            var incompleteClaim = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", "Details") { Parent = incompleteClaims, RouteValues = new { id = id } };

            var locationPage = new MvcBreadcrumbNode("Add", "CaseLocations", "Edit Beneficiary") { Parent = incompleteClaim, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = locationPage;
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
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
               i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            if (caseLocation is not null)
            {
                try
                {
                    {
                        caseLocation.Updated = DateTime.UtcNow;
                        caseLocation.InvestigationCaseSubStatusId = createdStatus.InvestigationCaseSubStatusId;
                        caseLocation.UpdatedBy = HttpContext.User?.Identity?.Name;
                        IFormFile? customerDocument = Request.Form?.Files?.FirstOrDefault();
                        if (customerDocument is not null)
                        {
                            var messageDocumentFileName = Path.GetFileNameWithoutExtension(customerDocument.FileName);
                            var extension = Path.GetExtension(customerDocument.FileName);
                            caseLocation.ProfileImage = customerDocument;
                            using var dataStream = new MemoryStream();
                            await caseLocation.ProfileImage.CopyToAsync(dataStream);
                            caseLocation.ProfilePicture = dataStream.ToArray();
                        }

                        var existingLocation = _context.CaseLocation.AsNoTracking().Where(c =>
                        c.CaseLocationId == caseLocation.CaseLocationId && c.CaseLocationId == id).FirstOrDefault();
                        if (existingLocation != null)
                        {
                            if (existingLocation.ProfilePicture != null)
                            {
                                caseLocation.ProfilePicture = existingLocation.ProfilePicture;
                            }
                        }

                        _context.Update(caseLocation);
                        await _context.SaveChangesAsync();
                        toastNotification.AddSuccessToastMessage("verification location edited successfully!");
                        return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });
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
            }
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", caseLocation.DistrictId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", caseLocation.BeneficiaryRelationId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", caseLocation.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", caseLocation.StateId);
            return View(caseLocation);
        }

        // GET: CaseLocations/Delete/5
        [Breadcrumb("Delete ")]
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null || _context.CaseLocation == null)
            {
                return NotFound();
            }

            var caseLocation = await _context.CaseLocation
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
                caseLocation.Updated = DateTime.UtcNow;
                caseLocation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.CaseLocation.Remove(caseLocation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });
        }

        private bool CaseLocationExists(long id)
        {
            return (_context.CaseLocation?.Any(e => e.CaseLocationId == id)).GetValueOrDefault();
        }
    }
}