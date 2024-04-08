using AspNetCoreHero.ToastNotification.Abstractions;

using CsvHelper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    public class ClaimsInvestigationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IFtpService ftpService;
        private readonly INotyfService notifyService;
        private readonly IInvestigationReportService investigationReportService;
        private readonly IClaimPolicyService claimPolicyService;

        public ClaimsInvestigationController(ApplicationDbContext context,
            IEmpanelledAgencyService empanelledAgencyService,
            IFtpService ftpService,
            INotyfService notifyService,
            IInvestigationReportService investigationReportService,
            IClaimPolicyService claimPolicyService,
            IToastNotification toastNotification)
        {
            _context = context;
            this.claimPolicyService = claimPolicyService;
            this.empanelledAgencyService = empanelledAgencyService;
            this.ftpService = ftpService;
            this.notifyService = notifyService;
            this.investigationReportService = investigationReportService;
            this.toastNotification = toastNotification;
        }

        [Breadcrumb(" Claims")]
        public IActionResult Index()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return RedirectToAction("Draft");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Assign", FromAction = "Index")]
        public IActionResult Assign()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb(" Assess", FromAction = "Index")]
        public IActionResult Assessor()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb(" Assign(manual)", FromAction = "Index")]
        public IActionResult Assigner()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == currentUserEmail);
                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                return View(company.BulkUpload);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assigner(IFormFile postedFile, string uploadtype)
        {
            try
            {
                var userEmail = HttpContext.User.Identity.Name;
                if (postedFile != null && !string.IsNullOrWhiteSpace(userEmail))
                {
                    UploadType uploadType = (UploadType)Enum.Parse(typeof(UploadType), uploadtype, true);

                    if (uploadType == UploadType.FTP)
                    {
                        await ftpService.DownloadFtpFile(userEmail, postedFile);

                        notifyService.Custom($"Ftp download complete ", 3, "green", "far fa-file-powerpoint");
                    }

                    if (uploadType == UploadType.FILE && Path.GetExtension(postedFile.FileName) == ".zip")
                    {
                        await ftpService.UploadFile(userEmail, postedFile);

                        notifyService.Custom($"File upload complete", 3, "green", "far fa-file-powerpoint");
                    }
                    return RedirectToAction("Assigner", "ClaimsInvestigation");
                }
            }
            catch (Exception)
            {
                notifyService.Custom($"Upload Error. Pls try again", 3, "red", "far fa-file-powerpoint");
            }
            return RedirectToAction("Draft", "ClaimsInvestigation");
        }
        [Breadcrumb(" Assign(auto)", FromAction = "Index")]
        public IActionResult Draft()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == currentUserEmail);
                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                return View(company.BulkUpload);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Draft(IFormFile postedFile, string uploadtype)
        {
            try
            {
                var userEmail = HttpContext.User.Identity.Name;
                if (postedFile != null && !string.IsNullOrWhiteSpace(userEmail))
                {
                    UploadType uploadType = (UploadType)Enum.Parse(typeof(UploadType), uploadtype, true);

                    if (uploadType == UploadType.FTP)
                    {
                        await ftpService.DownloadFtpFile(userEmail, postedFile);

                        notifyService.Custom($"Ftp download complete ", 3, "green", "far fa-file-powerpoint");
                    }

                    if (uploadType == UploadType.FILE && Path.GetExtension(postedFile.FileName) == ".zip")
                    {

                        await ftpService.UploadFile(userEmail, postedFile);

                        notifyService.Custom($"File upload complete", 3, "green", "far fa-file-powerpoint");
                    }
                }
                return RedirectToAction("Draft", "ClaimsInvestigation");
            }
            catch (Exception ex)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb(" Empanelled Agencies", FromAction = "Assigner")]
        public async Task<IActionResult> EmpanelledVendors(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(Assigner));
                }

                var model = await empanelledAgencyService.GetEmpanelledVendors(selectedcase);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb(" Allocate (to agency)")]
        public async Task<IActionResult> AllocateToVendor(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(Assigner));
                }

                var claimsInvestigation = await empanelledAgencyService.GetAllocateToVendor(selectedcase);
                if (claimsInvestigation == null)
                {
                    return NotFound();
                }

                return View(claimsInvestigation);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Assessed")]
        public IActionResult Approved()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [Breadcrumb(title: "Active")]
        public IActionResult Active()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(title: "Draft")]
        public IActionResult Incomplete()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(title: "Review")]
        public IActionResult Review()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: "Report", FromAction = "Assessor")]
        public IActionResult GetInvestigateReport(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (selectedcase == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index));
                }
                var model = investigationReportService.GetInvestigateReport(currentUserEmail, selectedcase);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: "Previous Reports", FromAction = "GetInvestigateReport")]
        public IActionResult PreviousReports(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == 0)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index));
                }

                var model = investigationReportService.GetPreviousReport(id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }


        [Breadcrumb("Details", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index));
                }

                var model = await investigationReportService.GetClaimDetails(currentUserEmail, id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Detail", FromAction = "Assigner")]
        public async Task<IActionResult> Detail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index));
                }

                var model = await claimPolicyService.GetClaimDetail(id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Detail", FromAction = "Active")]
        public async Task<IActionResult> ActiveDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index));
                }

                var model = await claimPolicyService.GetClaimDetail(id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Detail", FromAction = "Index")]
        public async Task<IActionResult> ReadyDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index));
                }
                var model = await claimPolicyService.GetClaimDetail(id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Detail", FromAction = "Assign")]
        public async Task<IActionResult> AssignDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index));
                }

                var claimsInvestigation = await investigationReportService.GetAssignDetails(id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index));
                }

                return View(claimsInvestigation);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Agency detail", FromAction = "Draft")]
        public async Task<IActionResult> VendorDetail(string companyId, long id, string backurl, string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == 0 || companyId is null || selectedcase is null)
                {
                    notifyService.Error("OOPs !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendor = await _context.Vendor
                    .Include(v => v.ratings)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.PincodeServices)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.State)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.LineOfBusiness)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.InvestigationServiceType)
                    .FirstOrDefaultAsync(m => m.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                ViewBag.CompanyId = companyId;
                ViewBag.Backurl = backurl;
                ViewBag.Selectedcase = selectedcase;

                return View(vendor);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}