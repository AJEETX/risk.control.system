using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using System.Security.Claims;

namespace risk.control.system.Controllers
{
    public class ClaimsVendorController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly IDashboardService dashboardService;
        private readonly IClaimsVendorService vendorService;
        private readonly IMailboxService mailboxService;
        private readonly IToastNotification toastNotification;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private static HttpClient httpClient = new();

        public ClaimsVendorController(
            IClaimsInvestigationService claimsInvestigationService,
            UserManager<VendorApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            IDashboardService dashboardService,
            IClaimsVendorService vendorService,
            IMailboxService mailboxService,
            IToastNotification toastNotification,
            ApplicationDbContext context)
        {
            this.claimsInvestigationService = claimsInvestigationService;
            this.userManager = userManager;
            this.dashboardService = dashboardService;
            this.vendorService = vendorService;
            this.mailboxService = mailboxService;
            this.toastNotification = toastNotification;
            this._context = context;
            this.webHostEnvironment = webHostEnvironment;
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
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }
            var claimsInvestigation = await vendorService.AllocateToVendorAgent(userEmail, selectedcase);

            if (claimsInvestigation == null)
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }
            return View(claimsInvestigation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase, string claimId, long caseLocationId)
        {
            if (string.IsNullOrWhiteSpace(selectedcase) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Index));
            }

            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }
            var vendorAgent = _context.VendorApplicationUser.FirstOrDefault(c => c.Id.ToString() == selectedcase);

            var claim = await claimsInvestigationService.AssignToVendorAgent(vendorAgent.Email, userEmail, vendorAgent.VendorId, claimId);

            await mailboxService.NotifyClaimAssignmentToVendorAgent(userEmail, claimId, vendorAgent.Email, vendorAgent.VendorId, caseLocationId);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # {0}] tasked to {1} successfully!", claim.PolicyDetail.ContractNumber, vendorAgent.Email));

            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }

        [HttpGet]
        [Breadcrumb("Allocate")]
        public async Task<IActionResult> SelectVendorAgent(string selectedcase)
        {
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Index));
            }

            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }
            var model = await vendorService.SelectVendorAgent(userEmail, selectedcase);

            return View(model);
        }

        [HttpGet]
        [Breadcrumb("ReAllocate")]
        public async Task<IActionResult> ReSelectVendorAgent(string selectedcase)
        {
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Index));
            }

            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }
            var model = await vendorService.ReSelectVendorAgent(userEmail, selectedcase);

            return View(model);
        }

        [Breadcrumb("Agency Workload")]
        public async Task<IActionResult> AgentLoad()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }
            var agents = await vendorService.GetAgentLoad(userEmail);
            return View(agents);
        }

        [Breadcrumb(" Claims")]
        public ActionResult Index()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var activePage = new MvcBreadcrumbNode("Open", "ClaimsVendor", "Claims");
            var newPage = new MvcBreadcrumbNode("Index", "ClaimsVendor", "Allocate") { Parent = activePage };
            ViewData["BreadcrumbNode"] = newPage;

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole.Value.Contains(AppRoles.Agent.ToString()))
            {
                return View("Agent");
            }
            return View();
        }

        [Breadcrumb(" Tasks")]
        public ActionResult Agent()
        {
            return View();
        }

        [Breadcrumb("Agent Report", FromAction = "Agent")]
        public async Task<IActionResult> GetInvestigate(string selectedcase)
        {
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be investigate.");
                return RedirectToAction(nameof(Index));
            }

            var currentUserEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }
            var model = await vendorService.GetInvestigate(currentUserEmail, selectedcase);

            return View(model);
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
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            var submittedToSupervisortStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.ClaimReport)
                .Include(c => c.District)
                .Include(c => c.State)
                .FirstOrDefault(c => (c.ClaimsInvestigationId == selectedcase
                && c.InvestigationCaseSubStatusId == submittedToSupervisortStatus.InvestigationCaseSubStatusId) || c.IsReviewCaseLocation
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

        [Breadcrumb("Agent Report")]
        public async Task<IActionResult> GetInvestigateReport(string selectedcase)
        {
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be investigate.");
                return RedirectToAction(nameof(Index));
            }

            var currentUserEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }

            var model = await vendorService.GetInvestigateReport(currentUserEmail, selectedcase);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(string remarks, string question1, string question2, string question3, string question4, string claimId, long caseLocationId)
        {
            if (string.IsNullOrWhiteSpace(remarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
            {
                toastNotification.AddAlertToastMessage("No Agent remarks entered!!!. Please enter remarks.");
                return RedirectToAction(nameof(GetInvestigate), new { selectedcase = claimId });
            }

            string userEmail = HttpContext?.User?.Identity.Name;

            //POST FACE IMAGE AND DOCUMENT

            await vendorService.PostFaceId(userEmail, claimId);

            await vendorService.PostDocumentId(userEmail, claimId);

            //END : POST FACE IMAGE AND DOCUMENT

            var claim = await claimsInvestigationService.SubmitToVendorSupervisor(userEmail, caseLocationId, claimId, remarks, question1, question2, question3, question4);

            await mailboxService.NotifyClaimReportSubmitToVendorSupervisor(userEmail, claimId, caseLocationId);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # {0}] investigation submitted to supervisor successfully !", claim.PolicyDetail.ContractNumber));

            return RedirectToAction(nameof(ClaimsVendorController.Agent), "ClaimsVendor");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReport(string supervisorRemarks, string supervisorRemarkType, string claimId, long caseLocationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(supervisorRemarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
                {
                    toastNotification.AddAlertToastMessage("No Supervisor remarks entered!!!. Please enter remarks.");
                    return RedirectToAction(nameof(GetInvestigateReport), new { selectedcase = claimId });
                }
                string userEmail = HttpContext?.User?.Identity.Name;
                var reportUpdateStatus = SupervisorRemarkType.OK;

                var success = await claimsInvestigationService.ProcessAgentReport(userEmail, supervisorRemarks, caseLocationId, claimId, reportUpdateStatus);

                if (success != null)
                {
                    await mailboxService.NotifyClaimReportSubmitToCompany(userEmail, claimId, caseLocationId);
                    toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # {0}] report submitted to Company successfully !", success.PolicyDetail.ContractNumber));
                }
                else
                {
                    toastNotification.AddSuccessToastMessage("Report sent to review successfully");
                }
                return RedirectToAction(nameof(ClaimsVendorController.ClaimReport));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReAllocateReport(string supervisorRemarks, string supervisorRemarkType, string claimId, long caseLocationId)
        {
            if (string.IsNullOrWhiteSpace(supervisorRemarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
            {
                toastNotification.AddAlertToastMessage("No remarks entered!!!. Please enter remarks.");
                return RedirectToAction(nameof(GetInvestigate), new { selectedcase = claimId });
            }
            string userEmail = HttpContext?.User?.Identity.Name;
            var reportUpdateStatus = SupervisorRemarkType.REVIEW;

            var success = await claimsInvestigationService.ProcessAgentReport(userEmail, supervisorRemarks, caseLocationId, claimId, reportUpdateStatus);

            if (success != null)
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

        [Breadcrumb(" Active")]
        public IActionResult Open()
        {
            return View();
        }

        [Breadcrumb(title: " Detail", FromAction = "Open")]
        public async Task<IActionResult> Detail(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }
            var model = await vendorService.GetClaimsDetails(currentUserEmail, id);
            return View(model);
        }

        [Breadcrumb("Agent Report")]
        public IActionResult ClaimReport()
        {
            return View();
        }

        [Breadcrumb(" Re Allocate")]
        public IActionResult ClaimReportReview()
        {
            return View();
        }
    }
}