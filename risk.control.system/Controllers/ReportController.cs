using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using SmartBreadcrumbs.Attributes;
using risk.control.system.Models.ViewModel;
using risk.control.system.Models;
using System.Text.RegularExpressions;
using risk.control.system.Helpers;
using risk.control.system.Services;
using NToastNotify;
using AspNetCoreHero.ToastNotification.Notyf;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using static risk.control.system.AppConstant.Applicationsettings;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly INotyfService notifyService;
        private readonly IToastNotification toastNotification;
        private readonly IHttpClientService httpClientService;
        private static HttpClient _httpClient = new();
        private Regex longLatRegex = new Regex("(?<lat>[-|+| ]\\d+.\\d+)\\s* \\/\\s*(?<lon>\\d+.\\d+)");

        public ReportController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
            INotyfService notifyService,
            IToastNotification toastNotification, IHttpClientService httpClientService)
        {
            this._context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.notifyService = notifyService;
            this.toastNotification = toastNotification;
            this.httpClientService = httpClientService;
        }

        [Breadcrumb(title: " Approved", FromController = typeof(ClaimsInvestigationController))]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult Index()
        {
            return View();
        }

        [Breadcrumb(title: " Approved", FromController = typeof(ClaimsInvestigationController))]
        [Authorize(Roles = MANAGER.DISPLAY_NAME)]
        public IActionResult ManagerIndex()
        {
            return View();
        }

        [Breadcrumb(title: " Rejected", FromController = typeof(ClaimsInvestigationController))]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult Rejected()
        {
            return View();
        }

        [Breadcrumb(title: " Rejected", FromController = typeof(ClaimsInvestigationController))]
        [Authorize(Roles = MANAGER.DISPLAY_NAME)]
        public IActionResult ManagerRejected()
        {
            return View();
        }
        [Breadcrumb(" Details")]
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

                var caseLogs = await _context.InvestigationTransaction
                    .Include(i => i.InvestigationCaseStatus)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.BeneficiaryDetail)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.InvestigationCaseStatus)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.InvestigationCaseSubStatus)
                    .Where(t => t.ClaimsInvestigationId == id)
                    .OrderByDescending(c => c.HopCount)?.ToListAsync();

                var claimsInvestigation = await _context.ClaimsInvestigation
                    .Include(c => c.AgencyReport)
                  .Include(c => c.AgencyReport.DocumentIdReport)
                  .Include(c => c.AgencyReport.DigitalIdReport)
                  .Include(c => c.AgencyReport.ReportQuestionaire)
                    .Include(c => c.ClaimMessages)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.ClientCompany)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.CaseEnabler)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.CostCentre)
                  .Include(c=>c.Vendor)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.PinCode)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.BeneficiaryRelation)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.District)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.State)
                  .Include(c => c.BeneficiaryDetail)
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

                var location = await _context.BeneficiaryDetail
                    
                    .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var invoice = _context.VendorInvoice.FirstOrDefault(i => i.AgencyReportId == claimsInvestigation.AgencyReport.AgencyReportId);

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

        [Breadcrumb(" Details", FromAction ="ManagerIndex")]
        public async Task<IActionResult> ApprovedDetail(string id)
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

                var caseLogs = await _context.InvestigationTransaction
                    .Include(i => i.InvestigationCaseStatus)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.BeneficiaryDetail)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.InvestigationCaseStatus)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.InvestigationCaseSubStatus)
                    .Where(t => t.ClaimsInvestigationId == id)
                    .OrderByDescending(c => c.HopCount)?.ToListAsync();

                var claimsInvestigation = await _context.ClaimsInvestigation
                    .Include(c => c.AgencyReport)
                  .Include(c => c.AgencyReport.DocumentIdReport)
                  .Include(c => c.AgencyReport.DigitalIdReport)
                  .Include(c => c.AgencyReport.ReportQuestionaire)
                    .Include(c => c.ClaimMessages)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.ClientCompany)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.CaseEnabler)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.CostCentre)
                  .Include(c=>c.Vendor)

                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.PinCode)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.BeneficiaryRelation)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.District)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.State)
                  .Include(c => c.BeneficiaryDetail)
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

                var location = await _context.BeneficiaryDetail
                    .Include(b=>b.BeneficiaryRelation)
                    .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var invoice = _context.VendorInvoice.FirstOrDefault(i => i.AgencyReportId == claimsInvestigation.AgencyReport.AgencyReportId);

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

        [Breadcrumb(" Details", FromAction = "ManagerRejected")]
        public async Task<IActionResult> ManagerRejectDetail(string id)
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

                var caseLogs = await _context.InvestigationTransaction
                    .Include(i => i.InvestigationCaseStatus)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.BeneficiaryDetail)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.InvestigationCaseStatus)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.InvestigationCaseSubStatus)
                    .Where(t => t.ClaimsInvestigationId == id)
                    .OrderByDescending(c => c.HopCount)?.ToListAsync();

                var claimsInvestigation = await _context.ClaimsInvestigation
                    .Include(c => c.AgencyReport)
                  .Include(c => c.AgencyReport.DocumentIdReport)
                  .Include(c => c.AgencyReport.DigitalIdReport)
                  .Include(c => c.AgencyReport.ReportQuestionaire)
                    .Include(c => c.ClaimMessages)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.ClientCompany)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.CaseEnabler)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.CostCentre)
                  .Include(c=>c.Vendor)

                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.PinCode)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.BeneficiaryRelation)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.District)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.State)
                  .Include(c => c.BeneficiaryDetail)
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

                var location = await _context.BeneficiaryDetail
                    .Include(c => c.BeneficiaryRelation)
                    
                    .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var invoice = _context.VendorInvoice.FirstOrDefault(i => i.AgencyReportId == claimsInvestigation.AgencyReport.AgencyReportId);

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
        [Breadcrumb(" Details", FromAction = "Rejected")]
        public async Task<IActionResult> RejectDetail(string id)
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

                var caseLogs = await _context.InvestigationTransaction
                    .Include(i => i.InvestigationCaseStatus)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.BeneficiaryDetail)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.InvestigationCaseStatus)
                    .Include(c => c.ClaimsInvestigation)
                    .ThenInclude(i => i.InvestigationCaseSubStatus)
                    .Where(t => t.ClaimsInvestigationId == id)
                    .OrderByDescending(c => c.HopCount)?.ToListAsync();

                var claimsInvestigation = await _context.ClaimsInvestigation
                    .Include(c => c.AgencyReport)
                  .Include(c => c.AgencyReport.DocumentIdReport)
                  .Include(c => c.AgencyReport.DigitalIdReport)
                  .Include(c => c.AgencyReport.ReportQuestionaire)
                    .Include(c => c.ClaimMessages)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.ClientCompany)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.CaseEnabler)
                  .Include(c => c.PolicyDetail)
                  .ThenInclude(c => c.CostCentre)
                  .Include(c=>c.Vendor)

                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.PinCode)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.BeneficiaryRelation)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.District)
                  .Include(c => c.BeneficiaryDetail)
                  .ThenInclude(c => c.State)
                  .Include(c => c.BeneficiaryDetail)
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

                var location = await _context.BeneficiaryDetail
                    .Include(c => c.BeneficiaryRelation)
                   
                    .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var invoice = _context.VendorInvoice.FirstOrDefault(i => i.AgencyReportId == claimsInvestigation.AgencyReport.AgencyReportId);

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

        [Breadcrumb(title: "Invoice", FromAction = "Detail")]
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
                if (1 > id)
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

                var claimsPage = new MvcBreadcrumbNode("Assessor", "ClaimsInvestigation", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Index", "Report", "Approved") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Detail", "Report", $"Details") { Parent = agencyPage, RouteValues = new { id = invoice.ClaimId } };
                var editPage = new MvcBreadcrumbNode("ShowInvoice", "Report", $"Invoice") { Parent = detailsPage, RouteValues = new { id = id } };
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
        public async Task<IActionResult> PrintInvoice(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail) || 1 > id)
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
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [HttpGet]
        public async Task<IActionResult> PrintReport(string id)
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
                var claim = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .Include(c => c.BeneficiaryDetail)
                    .Include(r => r.AgencyReport)
                    .FirstOrDefault(c => c.ClaimsInvestigationId == id);

                var policy = claim.PolicyDetail;
                var customer = claim.CustomerDetail;
                var beneficiary = claim.BeneficiaryDetail;

                string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var filename = "report" + id + ".pdf";

                var filePath = Path.Combine(webHostEnvironment.WebRootPath, "report", filename);

                ReportRunner.Run(webHostEnvironment.WebRootPath).Build(filePath); ;
                var memory = new MemoryStream();
                using var stream = new FileStream(filePath, FileMode.Open);
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                return File(memory, "application/pdf", filename);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrintPdfReport(string id)
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

                var claim = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .Include(c => c.BeneficiaryDetail)
                    .FirstOrDefault(c => c.ClaimsInvestigationId == id);

                var policy = claim.PolicyDetail;
                var customer = claim.CustomerDetail;
                var beneficiary = claim.BeneficiaryDetail;

                string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var filename = "report" + id + ".pdf";

                var filePath = Path.Combine(webHostEnvironment.WebRootPath, "report", filename);

                PdfReportRunner.Run(webHostEnvironment.WebRootPath).Build(filePath); ;
                var memory = new MemoryStream();
                using var stream = new FileStream(filePath, FileMode.Open);
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                notifyService.Success($"Policy {policy.ContractNumber} Report download success !!!");
                return File(memory, "application/pdf", filename);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        public async Task<RootObject> GetAddress(string lat, string lon)
        {
            var jsonData = await _httpClient.GetStringAsync("http://nominatim.openstreetmap.org/reverse?format=json&lat=" + lat + "&lon=" + lon);

            var rootObject = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObject>(jsonData);
            return rootObject;
        }
    }
}