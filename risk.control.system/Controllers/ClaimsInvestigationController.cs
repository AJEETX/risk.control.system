using CsvHelper;

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

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace risk.control.system.Controllers
{
    public class ClaimsInvestigationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IInvestigationReportService investigationReportService;
        private readonly IClaimPolicyService claimPolicyService;

        public ClaimsInvestigationController(ApplicationDbContext context,
            IEmpanelledAgencyService empanelledAgencyService,
            IInvestigationReportService investigationReportService,
            IClaimPolicyService claimPolicyService,
            IToastNotification toastNotification)
        {
            _context = context;
            this.claimPolicyService = claimPolicyService;
            this.empanelledAgencyService = empanelledAgencyService;
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

        [Breadcrumb(" Allocate(manual)", FromAction = "Index")]
        public IActionResult Assigner()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [Breadcrumb(" Assign", FromAction = "Index")]
        public IActionResult Draft()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
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

            var (claimsInvestigation, claimCase, vendorWithCaseCounts) = await empanelledAgencyService.GetEmpanelledVendors(selectedcase);

            ViewBag.CompanyId = claimCase.ClaimsInvestigation.PolicyDetail.ClientCompanyId;

            ViewBag.Selectedcase = selectedcase;

            return View(new ClaimsInvestigationVendorsModel { CaseLocation = claimCase, Vendors = vendorWithCaseCounts, ClaimsInvestigation = claimsInvestigation });
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

        [Breadcrumb(title: "Report", FromAction = "Approved")]
        public async Task<IActionResult> GetApprovedReport(string selectedcase)
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

            var model = await investigationReportService.GetApprovedReport(selectedcase);

            return View(model);
        }

        [Breadcrumb(title: "Invoice", FromAction = "GetApprovedReport")]
        public async Task<IActionResult> ShowInvoice(string id)
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
        public async Task<IActionResult> PrintInvoice(string id)
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

        [Breadcrumb("Details", FromAction = "CreatePolicy", FromController = typeof(InsurancePolicyController))]
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

            var model = await investigationReportService.GetClaimDetails(id);

            if (model.ClaimsInvestigation.CustomerDetail is not null)
            {
                var customerLatLong = model.ClaimsInvestigation.CustomerDetail.PinCode.Latitude + "," + model.ClaimsInvestigation.CustomerDetail.PinCode.Longitude;

                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=18&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Applicationsettings.GMAPData}";
                ViewBag.CustomerLocationUrl = url;
            }

            if (model.Location is not null)
            {
                var beneficiarylatLong = model.Location.PinCode.Latitude + "," + model.Location.PinCode.Longitude;
                var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=18&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key={Applicationsettings.GMAPData}";
                ViewBag.BeneficiaryLocationUrl = bUrl;
            }

            return View(model);
        }

        [Breadcrumb(title: " Detail", FromAction = "Active")]
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
            var customerLatLong = model.ClaimsInvestigation.CustomerDetail.PinCode.Latitude + "," + model.ClaimsInvestigation.CustomerDetail.PinCode.Longitude;

            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=18&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Applicationsettings.GMAPData}";
            ViewBag.CustomerLocationUrl = url;

            var beneficiarylatLong = model.Location.PinCode.Latitude + "," + model.Location.PinCode.Longitude;
            var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=18&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key={Applicationsettings.GMAPData}";
            ViewBag.BeneficiaryLocationUrl = bUrl;

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
        public async Task<IActionResult> VendorDetail(string companyId, string id, string backurl, string selectedcase)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (id == null || companyId is null || selectedcase is null)
            {
                toastNotification.AddErrorToastMessage("id null!");
                return RedirectToAction(nameof(Index));
            }

            var vendor = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
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