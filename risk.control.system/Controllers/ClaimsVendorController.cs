using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;

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
        public async Task<IActionResult> GetInvestigate(string selectedcase, bool uploaded = false)
        {
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be investigate.");
                return RedirectToAction(nameof(Index));
            }

            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }
            //POST FACE IMAGE AND DOCUMENT
            await vendorService.PostFaceId(userEmail, selectedcase);

            await vendorService.PostDocumentId(userEmail, selectedcase);

            var model = await vendorService.GetInvestigate(userEmail, selectedcase, uploaded);

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

            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }
            var model = await vendorService.GetInvestigateReportReview(currentUserEmail, selectedcase);
            return View(model);
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