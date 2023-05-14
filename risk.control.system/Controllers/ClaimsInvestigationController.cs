using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class ClaimsInvestigationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly IToastNotification toastNotification;

        public ClaimsInvestigationController(ApplicationDbContext context, UserManager<VendorApplicationUser> userManager, IToastNotification toastNotification)
        {
            _context = context;
            this.userManager = userManager;
            this.toastNotification = toastNotification;
        }

        // GET: ClaimsInvestigation
        public async Task<IActionResult> Index()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.InvestigationServiceType)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole.Value.Contains(AppRoles.ClientCreator.ToString()))
            {
                var status = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains("CREATED"));
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseStatusId == status.InvestigationCaseSubStatusId);
            }
            else if (userRole.Value.Contains(AppRoles.ClientAssigner.ToString()))
            {
                var status = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains("ASSIGNED_TO_ASSIGNER"));
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseStatusId == status.InvestigationCaseSubStatusId);
            }
            else if (!userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) && !userRole.Value.Contains(AppRoles.ClientAdmin.ToString()))
            {
                return View(new List<ClaimsInvestigation> { });
            }
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompany = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            ViewBag.HasClientCompany = true;
            if (clientCompany == null)
            {
                ViewBag.HasClientCompany = false;
            }

            return View(await applicationDbContext.ToListAsync());
        }
        [HttpPost]
        public async Task<IActionResult> Assign(List<string> claims)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains("IN-PROGRESS"));
            if (status == null)
            {

                return RedirectToAction(nameof(Create));
            }
            if (claims is not null && claims.Count > 0)
            {
                var casesAssigned = _context.ClaimsInvestigation.Where(v => claims.Contains(v.ClaimsInvestigationCaseId));
                var user = User?.Claims.FirstOrDefault(u => u.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                foreach (var claimsInvestigation in casesAssigned)
                {
                    claimsInvestigation.Updated = DateTime.UtcNow;
                    claimsInvestigation.UpdatedBy = user;
                    claimsInvestigation.CurrentUserId = User?.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value;
                    claimsInvestigation.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains("IN-PROGRESS")).InvestigationCaseStatusId;
                    claimsInvestigation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains("ASSIGNED_TO_ASSIGNER")).InvestigationCaseSubStatusId;
                }
                _context.UpdateRange(casesAssigned);
                toastNotification.AddSuccessToastMessage("case(s) assigned successfully!");
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
        // GET: ClaimsInvestigation/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationServiceType)
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
            var model = new ClaimsInvestigation { LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId };

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompany = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            if (clientCompany == null)
            {
                model.HasClientCompany = false;
            }
            else
            {
                model.ClientCompanyId = clientCompany.ClientCompanyId;
            }
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name");
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.LineOfBusinessId == model.LineOfBusinessId), "InvestigationServiceTypeId", "Name", model.InvestigationServiceTypeId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name");
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name");
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            return View(model);
        }

        // POST: ClaimsInvestigation/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClaimsInvestigation claimsInvestigation)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains("INITIATED"));
            if (status == null)
            {

                return View(claimsInvestigation);
            }
            if (claimsInvestigation is not null)
            {
                var user = User?.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Email)?.Value;
                claimsInvestigation.Updated = DateTime.UtcNow;
                claimsInvestigation.UpdatedBy = user;
                claimsInvestigation.CurrentUserId = User?.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value;
                claimsInvestigation.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains("INITIATED")).InvestigationCaseStatusId;
                claimsInvestigation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains("CREATED")).InvestigationCaseSubStatusId;
                _context.Add(claimsInvestigation);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("case(s) created successfully!");
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.InvestigationServiceTypeId);
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
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.InvestigationServiceTypeId);
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

            if (claimsInvestigation is not null)
            {
                try
                {
                    var user = User?.Claims.FirstOrDefault(u => u.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                    claimsInvestigation.Updated = DateTime.UtcNow;
                    claimsInvestigation.UpdatedBy = user;
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
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.InvestigationServiceTypeId);
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
                .Include(c => c.ClientCompany)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationServiceType)
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
                var user = User?.Claims.FirstOrDefault(u => u.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                claimsInvestigation.Updated = DateTime.UtcNow;
                claimsInvestigation.UpdatedBy = user;
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
