using AspNetCoreHero.ToastNotification.Abstractions;

using CsvHelper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

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
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public IActionResult Index()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return RedirectToAction("Incomplete");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Re + Assign")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public IActionResult Assign()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
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

        [Breadcrumb(" Assess", FromAction = "Index")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult Assessor()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
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
        [Breadcrumb(" Submitted", FromAction = "Index")]
        [Authorize(Roles = MANAGER.DISPLAY_NAME)]
        public IActionResult Manager()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
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

        [Breadcrumb(" Re + Assign")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public IActionResult Assigner()
        {
            try
            {
                bool userCanUpload = true;
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);
                if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
                {
                    var totalClaimsCreated = _context.ClaimsInvestigation.Include(c => c.PolicyDetail).Where(c => !c.Deleted && c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
                    if (totalClaimsCreated?.Count >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                    {
                        userCanUpload = false;
                        notifyService.Information($"Version limit = {totalClaimsCreated?.Count}");
                    }
                }
                
                return View(companyUser.ClientCompany.BulkUpload && userCanUpload);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
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
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (postedFile != null && !string.IsNullOrWhiteSpace(userEmail))
                {
                    UploadType uploadType = (UploadType)Enum.Parse(typeof(UploadType), uploadtype, true);

                    if (uploadType == UploadType.FTP)
                    {
                        var processed = await ftpService.DownloadFtpFile(userEmail, postedFile);
                        if (processed)
                        {
                            notifyService.Custom($"FTP download complete ", 3, "green", "fa fa-upload");
                        }
                        else
                        {
                            notifyService.Custom($"FTP Upload Error. Check limit", 3, "#001680", "fa fa-upload");
                        }
                    }

                    if (uploadType == UploadType.FILE && Path.GetExtension(postedFile.FileName) == ".zip")
                    {
                        var processed = await ftpService.UploadFile(userEmail, postedFile);
                        if (processed)
                        {
                            notifyService.Custom($"File upload complete", 3, "green", "fa fa-upload");
                        }
                        else
                        {
                            notifyService.Custom($"File Upload Error. Check limit", 3, "#001680", "fa fa-upload");
                        }
                    }
                    return RedirectToAction("Assigner", "ClaimsInvestigation");
                }
            }
            catch (Exception)
            {
                notifyService.Custom($"Upload Error. Pls try again", 3, "red", "fa fa-upload");
            }
            return RedirectToAction(nameof(ClaimsInvestigationController.Incomplete), "ClaimsInvestigation");
        }
        [Breadcrumb(" Assign(auto)")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public IActionResult Draft()
        {
            try
            {
                bool userCanUpload = true;
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ClientCompanyApplicationUser.Include(u=>u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);
                if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
                {
                    var totalClaimsCreated = _context.ClaimsInvestigation.Include(c => c.PolicyDetail).Where(c => !c.Deleted && c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
                    if (totalClaimsCreated?.Count >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                    {
                        userCanUpload = false;
                        notifyService.Information($"Version limit = {totalClaimsCreated?.Count}");
                    }
                }
                
                return View(companyUser.ClientCompany.BulkUpload && userCanUpload);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> Draft(IFormFile postedFile, string uploadtype)
        {
            try
            {
                var userEmail = HttpContext.User.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (postedFile != null && !string.IsNullOrWhiteSpace(userEmail))
                {
                    UploadType uploadType = (UploadType)Enum.Parse(typeof(UploadType), uploadtype, true);

                    if (uploadType == UploadType.FTP)
                    {
                        var processed = await ftpService.DownloadFtpFile(userEmail, postedFile);
                        if (processed)
                        {
                            notifyService.Custom($"FTP download complete ", 3, "green", "fa fa-upload");
                        }
                        else
                        {
                            notifyService.Information($"FTP Upload Error. Check limit <i class='fa fa-upload' ></i>", 3);
                        }
                    }

                    if (uploadType == UploadType.FILE && Path.GetExtension(postedFile.FileName) == ".zip")
                    {

                        var processed = await ftpService.UploadFile(userEmail, postedFile);
                        if (processed)
                        {
                            notifyService.Custom($"File upload complete", 3, "green", "fa fa-upload");
                        }
                        else
                        {
                            notifyService.Custom($"File Upload Error. Check limit", 3, "red", "fa fa-upload");
                        }

                    }
                }
                return RedirectToAction("Draft", "ClaimsInvestigation");
            }
            catch (Exception ex)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb(" Empanelled Agencies", FromAction = "Assigner")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> EmpanelledVendors(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
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
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb(" Allocate (to agency)")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> AllocateToVendor(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
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
                    notifyService.Error("OOPs !!!..Contact Admin");
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

        [Breadcrumb(" Assessed")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult Approved()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
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

        [Breadcrumb(title: "Active")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public IActionResult Active()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
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

        [Breadcrumb(title: "Active")]
        [Authorize(Roles = MANAGER.DISPLAY_NAME)]
        public IActionResult ManagerActive()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
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
        [Breadcrumb(title: "Draft")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public IActionResult Incomplete()
        {
            try
            {
                bool userCanCreate = true;
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser == null || companyUser.UserRole != AppConstant.CompanyRole.CREATOR)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
                {
                    var totalClaimsCreated = _context.ClaimsInvestigation.Include(c => c.PolicyDetail).Where(c => !c.Deleted && c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
                    if (totalClaimsCreated?.Count >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                    {
                        userCanCreate = false;
                        notifyService.Information($"Version limit = {totalClaimsCreated?.Count}");
                    }
                }

                return View(userCanCreate);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(title: "Review")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult Review()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
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
        [Breadcrumb(title: "Review")]
        [Authorize(Roles = MANAGER.DISPLAY_NAME)]
        public IActionResult ManagerReview()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
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

        [Breadcrumb(title: " Detail", FromAction = "Review")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> ReviewDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await claimPolicyService.GetClaimDetail(id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: "Report", FromAction = "Assessor")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult GetInvestigateReport(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
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
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: "Previous Reports", FromAction = "GetInvestigateReport")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult PreviousReports(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == 0)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = investigationReportService.GetPreviousReport(id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }


        [Breadcrumb("Details", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await investigationReportService.GetClaimDetails(currentUserEmail, id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb("Details", FromAction = "Draft", FromController = typeof(ClaimsInvestigationController))]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> DetailsAuto(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await investigationReportService.GetClaimDetails(currentUserEmail, id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb("Details", FromAction = "Assigner", FromController = typeof(ClaimsInvestigationController))]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> DetailsManual(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await investigationReportService.GetClaimDetails(currentUserEmail, id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Detail", FromAction = "Assigner")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> Detail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
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
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> ActiveDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await claimPolicyService.GetClaimDetail(id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Detail")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> ReadyDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await claimPolicyService.GetClaimDetail(id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Detail", FromAction = "Assign")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> AssignDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
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
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Agency detail", FromAction = "Draft")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> VendorDetail(string companyId, long id, string backurl, string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == 0 || companyId is null || selectedcase is null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
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
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}