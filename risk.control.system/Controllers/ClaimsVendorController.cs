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
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;
using static risk.control.system.Helpers.Permissions;

namespace risk.control.system.Controllers
{
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
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Index));
            }

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
        public async Task<IActionResult> SelectVendorAgent(long selectedcase, string claimId)
        {
            if (selectedcase < 1 || string.IsNullOrWhiteSpace(claimId))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Index));
            }

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

            var vendorUsers = _context.VendorApplicationUser.Where(u => u.VendorId == claimsCaseLocation.VendorId && u.Active);

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
            if (string.IsNullOrWhiteSpace(selectedcase) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Index));
            }

            var userEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Id.ToString() == selectedcase);

            await claimsInvestigationService.AssignToVendorAgent(vendorUser.Email, vendorUser.VendorId, claimId);

            await mailboxService.NotifyClaimAssignmentToVendorAgent(userEmail, claimId, vendorUser.Email, vendorUser.VendorId, caseLocationId);

            toastNotification.AddSuccessToastMessage("claim case allocated to agency agent successfully!");

            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }

        [Breadcrumb(" Claims")]
        public ActionResult Index()
        {
            var activePage = new MvcBreadcrumbNode("Open", "ClaimsVendor", "Claims");
            var newPage = new MvcBreadcrumbNode("Index", "ClaimsVendor", "Allocate New") { Parent = activePage };
            ViewData["BreadcrumbNode"] = newPage;

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole.Value.Contains(AppRoles.Agent.ToString()))
            {
                return View("Agent");
            }
            return View();
        }

        [Breadcrumb(" Investigations")]
        public ActionResult Agent()
        {
            //var activePage = new MvcBreadcrumbNode("Open", "ClaimsVendor", "Claims");
            //var newPage = new MvcBreadcrumbNode("Index", "ClaimsVendor", "Allocate New") { Parent = activePage };
            //ViewData["BreadcrumbNode"] = newPage;
            return View();
        }

        [Breadcrumb(" Reports")]
        public async Task<IActionResult> GetInvestigate(string selectedcase)
        {
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be investigate.");
                return RedirectToAction(nameof(Index));
            }

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
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be review.");
                return RedirectToAction(nameof(Index));
            }

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
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be assess.");
                return RedirectToAction(nameof(ClaimReport));
            }

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
            if (string.IsNullOrWhiteSpace(remarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
            {
                toastNotification.AddAlertToastMessage("No Agent remarks entered!!!. Please enter remarks.");
                return RedirectToAction(nameof(GetInvestigate), new { selectedcase = claimId });
            }

            string userEmail = HttpContext?.User?.Identity.Name;

            await claimsInvestigationService.SubmitToVendorSupervisor(userEmail, caseLocationId, claimId, remarks);

            await mailboxService.NotifyClaimReportSubmitToVendorSupervisor(userEmail, claimId, caseLocationId);

            toastNotification.AddSuccessToastMessage("report submitted to supervisor successfully");

            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }

        [HttpPost]
        public async Task<IActionResult> ProcessReport(string supervisorRemarks, string supervisorRemarkType, string claimId, long caseLocationId)
        {
            if (string.IsNullOrWhiteSpace(supervisorRemarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
            {
                toastNotification.AddAlertToastMessage("No Supervisor remarks entered!!!. Please enter remarks.");
                return RedirectToAction(nameof(GetInvestigateReport), new { selectedcase = claimId });
            }
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
            if (string.IsNullOrWhiteSpace(supervisorRemarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
            {
                toastNotification.AddAlertToastMessage("No remarks entered!!!. Please enter remarks.");
                return RedirectToAction(nameof(GetInvestigate), new { selectedcase = claimId });
            }
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

        [Breadcrumb("New Reports")]
        public async Task<IActionResult> ClaimReport()
        {
            return View();
        }

        [Breadcrumb(" Review Reports")]
        public async Task<IActionResult> ClaimReportReview()
        {
            return View();
        }

        private async Task<List<string>> GetUserRoles(VendorApplicationUser user)
        {
            return new List<string>(await userManager.GetRolesAsync(user));
        }
    }
}