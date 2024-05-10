using AspNetCoreHero.ToastNotification.Abstractions;
using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Authorization;
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

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR,AGENT")]
    public class ClaimsVendorController : Controller
    {
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly IDashboardService dashboardService;
        private readonly IClaimsVendorService vendorService;
        private readonly IMailboxService mailboxService;
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ClaimsVendorController(
            IClaimsInvestigationService claimsInvestigationService,
            UserManager<VendorApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            IDashboardService dashboardService,
            IClaimsVendorService vendorService,
            IMailboxService mailboxService,
            INotyfService notifyService,
            ApplicationDbContext context)
        {
            this.claimsInvestigationService = claimsInvestigationService;
            this.userManager = userManager;
            this.dashboardService = dashboardService;
            this.vendorService = vendorService;
            this.mailboxService = mailboxService;
            this.notifyService = notifyService;
            this._context = context;
            this.webHostEnvironment = webHostEnvironment;
        }

        [Breadcrumb(" Allocate To Agent")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(AllocateToVendorAgent));
                }

                var userEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var claimsInvestigation = await vendorService.AllocateToVendorAgent(userEmail, selectedcase);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("OOPs !!!..NOT FOUND");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View(claimsInvestigation);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [HttpGet]
        [Breadcrumb("Agents", FromAction = "Allocate")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> SelectVendorAgent(string selectedcase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(SelectVendorAgent),new { selectedcase = selectedcase });
                }

                var userEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorService.SelectVendorAgent(userEmail, selectedcase);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [HttpGet]
        [Breadcrumb("ReAllocate", FromAction = "ClaimReport")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> ReSelectVendorAgent(string selectedcase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(SelectVendorAgent), new { selectedcase = selectedcase });
                }

                var userEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorService.ReSelectVendorAgent(userEmail, selectedcase);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Agency Workload")]
        [Authorize(Roles = AGENCY_ADMIN.DISPLAY_NAME)]
        public async Task<IActionResult> AgentLoad()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var agents = await vendorService.GetAgentLoad(userEmail);
                return View(agents);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [Breadcrumb(" Claims")]
        public ActionResult Index()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                if (userRole.Value.Contains(AppRoles.AGENT.ToString()))
                {
                    return RedirectToAction("Agent");
                }
                return RedirectToAction("Allocate");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [Breadcrumb(" Allocate")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public ActionResult Allocate()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Tasks")]
        [Authorize(Roles = AGENT.DISPLAY_NAME)]
        public ActionResult Agent()
        {
            return View();
        }

        [Breadcrumb(title: " Completed")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public IActionResult Completed()
        {
            return View();
        }
        [Breadcrumb(title: " Submitted")]
        [Authorize(Roles = "AGENT")]
        public IActionResult Submitted()
        {
            return View();
        }
        [Breadcrumb(title: " Detail",FromAction = "Submitted")]
        [Authorize(Roles = "AGENT")]
        public async Task<IActionResult> SubmittedDetail(string id)
        {
            if (id == null)
            {
                notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
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
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
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
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Details", FromAction = "Completed")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]

        public async Task<IActionResult> CompletedDetail(string id)
        {
            if (id == null)
            {
                notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
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
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
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
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: "Invoice", FromAction = "CompletedDetail")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> ShowInvoice(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
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

                var claimsPage = new MvcBreadcrumbNode("Index", "ClaimsVendor", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Completed", "ClaimsVendor", "Completed") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("CompletedDetail", "ClaimsVendor", $"Details") { Parent = agencyPage, RouteValues = new { id = invoice.ClaimId } };
                var editPage = new MvcBreadcrumbNode("ShowInvoice", "ClaimsVendor", $"Invoice") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(invoice);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [Breadcrumb(title: "Print", FromAction = "ShowInvoice")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> PrintInvoice(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail) || 1 > id)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
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
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Submit", FromAction = "Agent")]
        [Authorize(Roles = AGENT.DISPLAY_NAME)]
        public async Task<IActionResult> GetInvestigate(string selectedcase, bool uploaded = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be investigate.");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var userEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                var model = await vendorService.GetInvestigate(userEmail, selectedcase, uploaded);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [Breadcrumb(" Review Report")]
        public async Task<IActionResult> GetInvestigateReportReview(string selectedcase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be review.");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorService.GetInvestigateReportReview(currentUserEmail, selectedcase);
                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Submit", FromAction= "ClaimReport")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]

        public async Task<IActionResult> GetInvestigateReport(string selectedcase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case.");
                    return RedirectToAction(nameof(ClaimReport));
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await vendorService.GetInvestigateReport(currentUserEmail, selectedcase);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [Breadcrumb(" Active")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public IActionResult Open()
        {
            return View();
        }

        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        [Breadcrumb(title: " Details", FromAction = "Allocate")]
        public async Task<IActionResult> CaseDetail(string id)
        {
            try
            {
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorService.GetClaimsDetails(currentUserEmail, id);
                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Details", FromAction = "Open")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> Detail(string id)
        {
            try
            {
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorService.GetClaimsDetails(currentUserEmail, id);
                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Verify(report)")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public IActionResult ClaimReport()
        {
            return View();
        }

        [Breadcrumb(" Re Allocate")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public IActionResult ClaimReportReview()
        {
            return View();
        }
    }
}