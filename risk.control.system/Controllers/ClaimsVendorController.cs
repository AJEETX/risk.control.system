using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Controllers
{
    public class ClaimsVendorController : Controller
    {
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly IToastNotification toastNotification;
        private readonly ApplicationDbContext _context;

        public ClaimsVendorController(IClaimsInvestigationService claimsInvestigationService,IMailboxService mailboxService, IToastNotification toastNotification,
            ApplicationDbContext context)
        {
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.toastNotification = toastNotification;
            this._context = context;
        }
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase)
        {

            if (_context.ClaimsInvestigation == null)
            {
                return NotFound();
            }
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.Vendor)
                .Include(c => c.Vendors)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.District)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.State)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Country)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationServiceType)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .FirstOrDefault(m => m.ClaimsInvestigationId == selectedcase && m.CaseLocations.Any(c=>c.VendorId == vendorUser.VendorId));
            var onlyAllocatedCaseLocations = claimsInvestigation.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId)?.ToList();
            claimsInvestigation.CaseLocations.RemoveAll((a)=> true);
            claimsInvestigation.CaseLocations = onlyAllocatedCaseLocations;

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            return View(claimsInvestigation);
        }
        [HttpPost]
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase, string claimId, long caseLocationId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            await claimsInvestigationService.AssignToVendorAgent(HttpContext?.User?.Identity?.Name, selectedcase);
            //TO-DO :: send notification to  


            await claimsInvestigationService.AllocateToVendor(userEmail, claimId, selectedcase, caseLocationId);

            await mailboxService.NotifyClaimAllocationToVendor(userEmail, claimId, selectedcase, caseLocationId);

            toastNotification.AddSuccessToastMessage("claim case allocated to vendor successfully!");

            return RedirectToAction(nameof(ClaimsInvestigationController.Index), "ClaimsInvestigation");
        }
        public async Task<IActionResult> Index()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .Include(c => c.Vendor)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.InvestigationServiceType)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State);

            ViewBag.HasClientCompany = true;
            ViewBag.HasVendorCompany = true;
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (companyUser == null && vendorUser == null)
            {
                ViewBag.HasClientCompany = false;
                ViewBag.HasVendorCompany = false;
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }
            else if (companyUser != null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
                ViewBag.HasVendorCompany = false;
            }
            else if (companyUser != null && vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId && i.VendorId == vendorUser.VendorId);
            }
            else if (companyUser == null && vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.CaseLocations.Any(c=>c.VendorId == vendorUser.VendorId));
            }
            // SHOWING DIFFERRENT PAGES AS PER ROLES
            if (userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) || userRole.Value.Contains(AppRoles.ClientAdmin.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId ||
                a.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId);
                return View(await applicationDbContext.ToListAsync());
            }
            if (userRole.Value.Contains(AppRoles.ClientCreator.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId);
                return View(await applicationDbContext.ToListAsync());
            }
            else if (userRole.Value.Contains(AppRoles.ClientAssigner.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId);
                return View("Assigner", await applicationDbContext.ToListAsync());
            }
            else if (userRole.Value.Contains(AppRoles.VendorAdmin.ToString()) || userRole.Value.Contains(AppRoles.VendorSupervisor.ToString()) || userRole.Value.Contains(AppRoles.VendorAgent.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId);
            }
            else if (userRole.Value.Contains(AppRoles.VendorAgent.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId);
                return View("VendorAgent", await applicationDbContext.ToListAsync());
            }

            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> Open()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
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
            ViewBag.HasClientCompany = true;

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            if (userRole.Value.Contains(AppRoles.VendorAdmin.ToString()) || userRole.Value.Contains(AppRoles.VendorSupervisor.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a =>
                openStatusesIds.Contains(a.InvestigationCaseStatusId) && a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId);
            }
            else if (!userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) && !userRole.Value.Contains(AppRoles.ClientAdmin.ToString()))
            {
                return View(new List<ClaimsInvestigation> { });
            }
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompany = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            if (clientCompany == null)
            {
                ViewBag.HasClientCompany = false;
            }
            else
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == clientCompany.ClientCompanyId);
            }
            return View(await applicationDbContext.ToListAsync());
        }
    }
}
