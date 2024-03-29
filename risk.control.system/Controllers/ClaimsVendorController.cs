using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
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
        [Breadcrumb("Agents", FromAction = "Allocate")]
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
        [Breadcrumb("ReAllocate", FromAction = "ClaimReport")]
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

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole.Value.Contains(AppRoles.Agent.ToString()))
            {
                return RedirectToAction("Agent");
            }
            return RedirectToAction("Allocate");
        }

        [Breadcrumb(" Allocate")]
        public ActionResult Allocate()
        {
            return View();
        }

        [Breadcrumb(" Tasks")]
        public ActionResult Agent()
        {
            return View();
        }

        [Breadcrumb(title: " Completed")]
        public IActionResult Completed()
        {
            return View();
        }

        [Breadcrumb(" Detail", FromAction = "Completed")]
        public async Task<IActionResult> CompletedDetail(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
              .Include(c => c.ClaimMessages)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.ClientCompany)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CaseEnabler)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CostCentre)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.InvestigationCaseSubStatus)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.BeneficiaryRelation)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.District)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.State)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.Country)
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
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = await _context.CaseLocation
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DocumentIdReport)
                .Include(l => l.Vendor)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ReportQuestionaire)
                .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var invoice = _context.VendorInvoice.FirstOrDefault(i => i.ClaimReportId == location.ClaimReport.ClaimReportId);

            var model = new ClaimTransactionModel
            {
                ClaimsInvestigation = claimsInvestigation,
                Log = caseLogs,
                Location = location,
                VendorInvoice = invoice,
                TimeTaken = caseLogs.GetElapsedTime()
            };

            return View(model);
        }

        [Breadcrumb(title: "Invoice", FromAction = "CompletedDetail")]
        public async Task<IActionResult> ShowInvoice(long id)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (id == null)
            {
                toastNotification.AddAlertToastMessage("NOT FOUND !!!..");
                return RedirectToAction(nameof(Index));
            }
            var invoice = await _context.VendorInvoice
                .Where(x => x.VendorInvoiceId.Equals(id))
                .Include(x => x.ClientCompany)
                .ThenInclude(c => c.District)
                .Include(c => c.ClientCompany)
                .ThenInclude(c => c.State)
                .Include(c => c.ClientCompany)
                .ThenInclude(c => c.Country)
                .Include(c => c.ClientCompany)
                .ThenInclude(c => c.PinCode)
                .Include(x => x.Vendor)
                .ThenInclude(v => v.State)
                .Include(v => v.Vendor)
                .ThenInclude(v => v.District)
                .Include(v => v.Vendor)
                .ThenInclude(v => v.Country)
                .Include(i => i.InvestigationServiceType)
                .FirstOrDefaultAsync();

            return View(invoice);
        }

        [Breadcrumb(title: "Print", FromAction = "ShowInvoice")]
        public async Task<IActionResult> PrintInvoice(long id)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var invoice = await _context.VendorInvoice
                .Where(x => x.VendorInvoiceId.Equals(id))
                .Include(x => x.ClientCompany)
                .ThenInclude(c => c.District)
                .Include(c => c.ClientCompany)
                .ThenInclude(c => c.State)
                .Include(c => c.ClientCompany)
                .ThenInclude(c => c.Country)
                .Include(c => c.ClientCompany)
                .ThenInclude(c => c.PinCode)
                .Include(x => x.Vendor)
                .ThenInclude(v => v.State)
                .Include(v => v.Vendor)
                .ThenInclude(v => v.District)
                .Include(v => v.Vendor)
                .ThenInclude(v => v.Country)
                .Include(i => i.InvestigationServiceType)
                .FirstOrDefaultAsync();

            return View(invoice);
        }

        [Breadcrumb("Submit", FromAction = "Agent")]
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
            //await vendorService.PostFaceId(userEmail, selectedcase);

            //await vendorService.PostDocumentId(userEmail, selectedcase);

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

        [Breadcrumb("Submit", FromAction= "ClaimReport")]
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

        [Breadcrumb(title: " Detail", FromAction = "Allocate")]
        public async Task<IActionResult> CaseDetail(string id)
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