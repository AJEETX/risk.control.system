using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb(" Claims")]
    public class ClaimsVendorController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly IDashboardService dashboardService;
        private readonly IMailboxService mailboxService;
        private readonly IToastNotification toastNotification;
        private readonly ApplicationDbContext _context;

        public ClaimsVendorController(
            IClaimsInvestigationService claimsInvestigationService,
            UserManager<VendorApplicationUser> userManager,
            IDashboardService dashboardService,
            IMailboxService mailboxService,
            IToastNotification toastNotification,
            ApplicationDbContext context)
        {
            this.claimsInvestigationService = claimsInvestigationService;
            this.userManager = userManager;
            this.dashboardService = dashboardService;
            this.mailboxService = mailboxService;
            this.toastNotification = toastNotification;
            this._context = context;
            UserList = new List<UsersViewModel>();
        }

        [Breadcrumb(" Allocate To Agent")]
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase)
        {
            if (_context.ClaimsInvestigation == null)
            {
                return NotFound();
            }
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);

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
                .FirstOrDefault(m => m.ClaimsInvestigationId == selectedcase && m.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));
            claimsInvestigation.CaseLocations = claimsInvestigation.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId
                        && c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId)?.ToList();

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            return View(claimsInvestigation);
        }

        [HttpPost]
        public async Task<IActionResult> SelectVendorAgent(long selectedcase, string claimId, long caseLocationId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var claimsCaseLocation = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.Vendor)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.CaseLocationId == selectedcase && c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId);

            var claimsCaseToAllocateToVendorAgent = _context.ClaimsInvestigation
                .Include(c => c.CaseLocations)
                .Include(c => c.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .Include(c => c.Vendors)
                .FirstOrDefault(v => v.ClaimsInvestigationId == claimId);
            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Agent.ToString()));

            var vendorUsers = _context.VendorApplicationUser.Where(u => u.VendorId == claimsCaseLocation.VendorId);

            List<VendorUserClaim> agents = new List<VendorUserClaim>();
            var result = dashboardService.CalculateAgentCaseStatus(userEmail);

            foreach (var vendorUser in vendorUsers)
            {
                var isTrue = await userManager.IsInRoleAsync(vendorUser, agentRole?.Name);
                if (isTrue)
                {
                    int claimCount = 0;
                    if (result.TryGetValue(vendorUser.Email, out claimCount))
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = claimCount,
                        };
                        agents.Add(agentData);
                    }
                    else
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = 0,
                        };
                        agents.Add(agentData);
                    }
                }
            }

            var model = new ClaimsInvestigationVendorAgentModel
            {
                CaseLocation = claimsCaseLocation,
                ClaimsInvestigation = claimsCaseToAllocateToVendorAgent,
                VendorUserClaims = agents
            };
            return View(model);
        }

        [Breadcrumb("Agent Workload")]
        public async Task<IActionResult> AgentLoad()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Agent.ToString()));
            List<VendorUserClaim> agents = new List<VendorUserClaim>();

            var vendor = _context.Vendor
                .Include(c => c.VendorApplicationUser)
                .FirstOrDefault(c => c.VendorId == vendorUser.VendorId);

            var users = vendor.VendorApplicationUser.AsQueryable();
            var result = dashboardService.CalculateAgentCaseStatus(userEmail);

            foreach (var user in users)
            {
                var isAgent = await userManager.IsInRoleAsync(user, agentRole?.Name);
                if (isAgent)
                {
                    int claimCount = 0;
                    if (result.TryGetValue(user.Email, out claimCount))
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = user,
                            CurrentCaseCount = claimCount,
                        };
                        agents.Add(agentData);
                    }
                    else
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = user,
                            CurrentCaseCount = 0,
                        };
                        agents.Add(agentData);
                    }
                }
            }
            return View(agents);
        }

        [HttpPost]
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase, string claimId, long caseLocationId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Id.ToString() == selectedcase);

            await claimsInvestigationService.AssignToVendorAgent(vendorUser.Email, vendorUser.VendorId, claimId);

            await mailboxService.NotifyClaimAssignmentToVendorAgent(userEmail, claimId, vendorUser.Email, vendorUser.VendorId, caseLocationId);

            toastNotification.AddSuccessToastMessage("claim case allocated to agency agent successfully!");

            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }

        public async Task<IActionResult> Index()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.ClaimReport)
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
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

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            //var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext
                    .Include(a => a.LineOfBusiness)
                    .Where(i => i.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));
            }
            // SHOWING DIFFERRENT PAGES AS PER ROLES
            if (userRole.Value.Contains(AppRoles.AgencyAdmin.ToString()) || userRole.Value.Contains(AppRoles.Supervisor.ToString()))
            {
                var claimsAssigned = new List<ClaimsInvestigation>();

                applicationDbContext = applicationDbContext.Where(a =>
                a.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId);
                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId
                    && !c.IsReviewCaseLocation)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsAssigned.Add(item);
                    }
                }
                return View(claimsAssigned);
            }
            else if (userRole.Value.Contains(AppRoles.Agent.ToString()))
            {
                var claimsAssigned = new List<ClaimsInvestigation>();

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId
                        && c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId
                        && c.AssignedAgentUserEmail == currentUserEmail)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsAssigned.Add(item);
                    }
                }
                return View("Agent", claimsAssigned);
            }

            return View(await applicationDbContext.ToListAsync());
        }

        [Breadcrumb(" Report")]
        public async Task<IActionResult> GetInvestigate(string selectedcase)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.LineOfBusiness)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase
                && c.CurrentUserEmail == currentUserEmail);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.ClaimReport)
                .Include(c => c.District)
                .Include(c => c.State)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase
                && c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId
                    );
            return View(new ClaimsInvestigationVendorsModel { CaseLocation = claimCase, ClaimsInvestigation = claimsInvestigation });
        }

        [Breadcrumb(" Review Report")]
        public async Task<IActionResult> GetInvestigateReportReview(string selectedcase)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.LineOfBusiness)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            var submittedToSupervisortStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.ClaimReport)
                .Include(c => c.District)
                .Include(c => c.State)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase
                && c.InvestigationCaseSubStatusId == submittedToSupervisortStatus.InvestigationCaseSubStatusId || c.IsReviewCaseLocation
                    );
            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Agent.ToString()));

            var vendorUsers = _context.VendorApplicationUser.Where(u => u.VendorId == claimCase.VendorId);

            List<VendorUserClaim> agents = new List<VendorUserClaim>();
            var result = dashboardService.CalculateAgentCaseStatus(currentUserEmail);

            foreach (var vendorUser in vendorUsers)
            {
                var isTrue = await userManager.IsInRoleAsync(vendorUser, agentRole?.Name);
                if (isTrue)
                {
                    int claimCount = 0;
                    if (result.TryGetValue(vendorUser.Email, out claimCount))
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = claimCount,
                        };
                        agents.Add(agentData);
                    }
                    else
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = 0,
                        };
                        agents.Add(agentData);
                    }
                }
            }
            return View(new ClaimsInvestigationVendorAgentModel
            {
                CaseLocation = claimCase,
                ClaimsInvestigation = claimsInvestigation,
                VendorUserClaims = agents
            }
            );
        }

        [Breadcrumb("  Report")]
        public async Task<IActionResult> GetInvestigateReport(string selectedcase)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .Include(c => c.LineOfBusiness)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            var submittedToSupervisortStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.ClaimReport)
                .Include(c => c.District)
                .Include(c => c.State)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase
                && c.InvestigationCaseSubStatusId == submittedToSupervisortStatus.InvestigationCaseSubStatusId || c.IsReviewCaseLocation
                    );
            return View(new ClaimsInvestigationVendorsModel { CaseLocation = claimCase, ClaimsInvestigation = claimsInvestigation });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReport(string remarks, string claimId, long caseLocationId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;

            await claimsInvestigationService.SubmitToVendorSupervisor(userEmail, caseLocationId, claimId, remarks);

            await mailboxService.NotifyClaimReportSubmitToVendorSupervisor(userEmail, claimId, caseLocationId);

            toastNotification.AddSuccessToastMessage("report submitted to supervisor successfully");

            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }

        [HttpPost]
        public async Task<IActionResult> ProcessReport(string supervisorRemarks, string supervisorRemarkType, string claimId, long caseLocationId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;
            var reportUpdateStatus = SupervisorRemarkType.OK;

            var success = await claimsInvestigationService.Process(userEmail, supervisorRemarks, caseLocationId, claimId, reportUpdateStatus);

            if (success)
            {
                await mailboxService.NotifyClaimReportSubmitToCompany(userEmail, claimId, caseLocationId);
                toastNotification.AddSuccessToastMessage("report submitted to Company successfully");
            }
            else
            {
                toastNotification.AddSuccessToastMessage("Report sent to review successfully");
            }
            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }

        [HttpPost]
        public async Task<IActionResult> ReAllocateReport(string supervisorRemarks, string supervisorRemarkType, string claimId, long caseLocationId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;
            var reportUpdateStatus = SupervisorRemarkType.REVIEW;

            var success = await claimsInvestigationService.Process(userEmail, supervisorRemarks, caseLocationId, claimId, reportUpdateStatus);

            if (success)
            {
                await mailboxService.NotifyClaimReportSubmitToCompany(userEmail, claimId, caseLocationId);
                toastNotification.AddSuccessToastMessage("report submitted to Company successfully");
            }
            else
            {
                toastNotification.AddSuccessToastMessage("Report sent to review successfully");
            }
            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }

        [Breadcrumb(" Active Claims")]
        public IActionResult Open()
        {
            return View();
        }

        [Breadcrumb(" Report")]
        public async Task<IActionResult> ClaimReport()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.ClaimReport)
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.ClaimReport)
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

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submittedToVendorSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));
            }
            // SHOWING DIFFERRENT PAGES AS PER ROLES
            var claimsSubmitted = new List<ClaimsInvestigation>();
            if (userRole.Value.Contains(AppRoles.AgencyAdmin.ToString()) || userRole.Value.Contains(AppRoles.Supervisor.ToString()))
            {
                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId
                        && c.InvestigationCaseSubStatusId == submittedToVendorSupervisorStatus.InvestigationCaseSubStatusId
                        && !c.IsReviewCaseLocation
                        )?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsSubmitted.Add(item);
                    }
                }
            }
            //else if (userRole.Value.Contains(AppRoles.Agent.ToString()))
            //{
            //    foreach (var item in applicationDbContext)
            //    {
            //        item.CaseLocations = item.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId
            //            && c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId && c.AssignedAgentUserEmail == currentUserEmail)?.ToList();
            //        if (item.CaseLocations.Any())
            //        {
            //            claimsSubmitted.Add(item);
            //        }
            //    }
            //}

            return View(claimsSubmitted);
        }

        [Breadcrumb(" Review")]
        public async Task<IActionResult> ClaimReportReview()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.ClaimReport)
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.ClaimReport)
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

            var allocatedToVendorSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var submittedToVendorSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
            i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));
            }
            // SHOWING DIFFERRENT PAGES AS PER ROLES
            var claimsSubmitted = new List<ClaimsInvestigation>();
            if (userRole.Value.Contains(AppRoles.AgencyAdmin.ToString()) || userRole.Value.Contains(AppRoles.Supervisor.ToString()))
            {
                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId && c.IsReviewCaseLocation == true
                        && (c.InvestigationCaseSubStatusId == allocatedToVendorSupervisorStatus.InvestigationCaseSubStatusId
                        || c.InvestigationCaseSubStatusId == submittedToVendorSupervisorStatus.InvestigationCaseSubStatusId))?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsSubmitted.Add(item);
                    }
                }
            }

            return View(claimsSubmitted);
        }

        private async Task<List<string>> GetUserRoles(VendorApplicationUser user)
        {
            return new List<string>(await userManager.GetRolesAsync(user));
        }
    }
}