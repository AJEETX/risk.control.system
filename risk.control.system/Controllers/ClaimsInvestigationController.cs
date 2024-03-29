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
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return RedirectToAction("Draft");
        }

        [Breadcrumb(" Assign", FromAction = "Index")]
        public IActionResult Assign()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [Breadcrumb(" Assess", FromAction = "Index")]
        public IActionResult Assessor()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [Breadcrumb(" Assign(manual)", FromAction = "Index")]
        public IActionResult Assigner()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == currentUserEmail);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            return View(company.BulkUpload);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assigner(IFormFile postedFile, string uploadtype)
        {
            var userEmail = HttpContext.User.Identity.Name;
            if (postedFile != null && !string.IsNullOrWhiteSpace(userEmail))
            {
                UploadType uploadType = (UploadType)Enum.Parse(typeof(UploadType), uploadtype, true);

                if (uploadType == UploadType.FTP)
                {
                    await ftpService.DownloadFtpFile(userEmail, postedFile);

                    notifyService.Custom($"Ftp download complete ", 3, "green", "far fa-file-powerpoint");

                    return RedirectToAction("Draft", "ClaimsInvestigation");
                }

                if (uploadType == UploadType.FILE && Path.GetExtension(postedFile.FileName) == ".zip")
                {
                    try
                    {
                        await ftpService.UploadFile(userEmail, postedFile);

                        notifyService.Custom($"File upload complete", 3, "green", "far fa-file-powerpoint");

                        return RedirectToAction("Assigner", "ClaimsInvestigation");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            notifyService.Custom($"Upload Error. Pls try again", 3, "red", "far fa-file-powerpoint");

            return RedirectToAction("Draft", "ClaimsInvestigation");
        }
        [Breadcrumb(" Assign(auto)", FromAction = "Index")]
        public IActionResult Draft()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u=>u.Email == currentUserEmail);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            return View(company.BulkUpload);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Draft(IFormFile postedFile, string uploadtype)
        {
            var userEmail = HttpContext.User.Identity.Name;
            if (postedFile != null && !string.IsNullOrWhiteSpace(userEmail))
            {
                UploadType uploadType = (UploadType)Enum.Parse(typeof(UploadType), uploadtype, true);

                if (uploadType == UploadType.FTP)
                {
                    await ftpService.DownloadFtpFile(userEmail, postedFile);

                    notifyService.Custom($"Ftp download complete ", 3, "green", "far fa-file-powerpoint");

                    return RedirectToAction("Draft", "ClaimsInvestigation");
                }

                if (uploadType == UploadType.FILE && Path.GetExtension(postedFile.FileName) == ".zip")
                {
                    try
                    {
                        await ftpService.UploadFile(userEmail, postedFile);

                        notifyService.Custom($"File upload complete", 3, "green", "far fa-file-powerpoint");

                        return RedirectToAction("Draft", "ClaimsInvestigation");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            notifyService.Custom($"Upload Error. Pls try again", 3, "red", "far fa-file-powerpoint");

            return RedirectToAction("Draft", "ClaimsInvestigation");
        }

        [HttpGet]
        [Breadcrumb(" Empanelled Agencies", FromAction = "Assigner")]
        public async Task<IActionResult> EmpanelledVendors(string selectedcase)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Assigner));
            }

            var model = await empanelledAgencyService.GetEmpanelledVendors(selectedcase);

            return View(model);
        }

        [HttpGet]
        [Breadcrumb(" Allocate (to agency)")]
        public async Task<IActionResult> AllocateToVendor(string selectedcase)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Assigner));
            }

            var claimsInvestigation = await empanelledAgencyService.GetAllocateToVendor(selectedcase);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }

            return View(claimsInvestigation);
        }

        [Breadcrumb(" Assessed")]
        public IActionResult Approved()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [Breadcrumb(title: "Active")]
        public IActionResult Active()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [Breadcrumb(title: "Report", FromAction = "Assessor")]
        public IActionResult GetInvestigateReport(string selectedcase)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (selectedcase == null)
            {
                toastNotification.AddAlertToastMessage("NOT FOUND !!!..");
                return RedirectToAction(nameof(Index));
            }
            var model = investigationReportService.GetInvestigateReport(currentUserEmail, selectedcase);

            return View(model);
        }

        [Breadcrumb(title: "Previous Reports", FromAction = "GetInvestigateReport")]
        public IActionResult PreviousReports(long id)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (id == 0)
            {
                toastNotification.AddAlertToastMessage("NOT FOUND !!!..");
                return RedirectToAction(nameof(Index));
            }

            var model = investigationReportService.GetPreviousReport(id);

            return View(model);
        }


        [Breadcrumb("Details", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public async Task<IActionResult> Details(string id)
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

            var model = await investigationReportService.GetClaimDetails(currentUserEmail, id);

            return View(model);
        }

        [Breadcrumb(title: " Detail", FromAction = "Assigner")]
        public async Task<IActionResult> Detail(string id)
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

            var model = await claimPolicyService.GetClaimDetail(id);

            return View(model);
        }

        [Breadcrumb(title: " Detail", FromAction = "Active")]
        public async Task<IActionResult> ActiveDetail(string id)
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

            var model = await claimPolicyService.GetClaimDetail(id);

            return View(model);
        }

        [Breadcrumb(title: " Detail", FromAction = "Index")]
        public async Task<IActionResult> ReadyDetail(string id)
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
            var model = await claimPolicyService.GetClaimDetail(id);

            return View(model);
        }

        [Breadcrumb(title: " Detail", FromAction = "Assign")]
        public async Task<IActionResult> AssignDetail(string id)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (id == null)
            {
                toastNotification.AddErrorToastMessage("detail not found!");
                return RedirectToAction(nameof(Index));
            }

            var claimsInvestigation = await investigationReportService.GetAssignDetails(id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }

            return View(claimsInvestigation);
        }

        [Breadcrumb(title: " Agency detail", FromAction = "Draft")]
        public async Task<IActionResult> VendorDetail(string companyId, long id, string backurl, string selectedcase)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (id == 0 || companyId is null || selectedcase is null)
            {
                toastNotification.AddErrorToastMessage("id null!");
                return RedirectToAction(nameof(Index));
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
                return NotFound();
            }
            ViewBag.CompanyId = companyId;
            ViewBag.Backurl = backurl;
            ViewBag.Selectedcase = selectedcase;

            return View(vendor);
        }
    }
}