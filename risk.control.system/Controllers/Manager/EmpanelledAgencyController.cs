using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Manager
{
    [Breadcrumb("Manage Agency(s)")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class EmpanelledAgencyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAgencyCreateEditService agencyCreateEditService;
        private readonly INotyfService notifyService;
        private readonly IInvestigationDetailService investigationDetailService;
        private readonly ILogger<EmpanelledAgencyController> logger;
        private readonly string portal_base_url = string.Empty;

        public EmpanelledAgencyController(
            ApplicationDbContext context,
            IAgencyCreateEditService agencyCreateEditService,
            INotyfService notifyService,
            IInvestigationDetailService investigationDetailService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<EmpanelledAgencyController> logger)
        {
            _context = context;
            this.agencyCreateEditService = agencyCreateEditService;
            this.notifyService = notifyService;
            this.investigationDetailService = investigationDetailService;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Agencies));
        }

        [Breadcrumb("Empanelled Agencies")]
        public IActionResult Agencies()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agencies(List<long> vendors)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("OOPs !!!..Not Agency Found");
                    return RedirectToAction(nameof(Agencies), "EmpanelledAgency");
                }

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..User Not Found. Try again.");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var company = await _context.ClientCompany
                    .Include(c => c.EmpanelledVendors)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Company Not Found. Try again.");
                    return RedirectToAction(nameof(Agencies), "EmpanelledAgency");
                }
                var agenciesToDepanel = company.EmpanelledVendors.Where(v => vendors.Contains(v.VendorId)).ToList();
                foreach (var agency in agenciesToDepanel)
                {
                    company.EmpanelledVendors.Remove(agency);
                }
                company.Updated = DateTime.UtcNow;
                company.UpdatedBy = userEmail;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();
                notifyService.Custom($"Agency(s) De-panelled successfully.", 3, "orange", "far fa-hand-pointer");
                return RedirectToAction(nameof(Agencies), "EmpanelledAgency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred empanelling Agencies. {UserEmail}", userEmail);
                notifyService.Error("Error occurred empanelling Agencies. Try again.");
                return RedirectToAction(nameof(Agencies), "EmpanelledAgency");
            }
        }

        [Breadcrumb("Agency Profile", FromAction = "Agencies")]
        public async Task<IActionResult> Detail(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var userEmail = HttpContext.User?.Identity?.Name;

                var vendor = await _context.Vendor
                    .Include(v => v.ratings)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .FirstOrDefaultAsync(m => m.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var vendorUserCount = await _context.ApplicationUser.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted);
                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

                var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

                var vendorAllCasesCount = await _context.Investigations.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted &&
                          (c.SubStatus == approvedStatus ||
                          c.SubStatus == rejectedStatus));

                // HACKY
                var currentCases = investigationDetailService.GetAgencyIdsLoad(new List<long> { vendor.VendorId });
                vendor.SelectedCountryId = vendorUserCount;
                vendor.SelectedStateId = currentCases.FirstOrDefault().CaseCount;
                vendor.SelectedDistrictId = vendorAllCasesCount;
                if (superAdminUser.IsSuperAdmin)
                {
                    vendor.SelectedByCompany = true;
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {AgencyId} for {UserEmail}.", id, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting Agency. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(" Edit Agency", FromAction = "Detail")]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("OOPS !!!..Invalid Agency Id");
                    return RedirectToAction(nameof(Agencies), "EmpanelledAgency");
                }

                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Agency Not Found");
                    return RedirectToAction(nameof(Agencies), "EmpanelledAgency");
                }
                //vendor.SelectedByCompany = true; // THIS IS TO NOT SHOW EDIT PROFILE
                var claimsPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Empanelled Agencies") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Detail", "EmpanelledAgency", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("Edit", "EmpanelledAgency", $"Edit Agency") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {AgencyId} for {UserEmail}.", id, HttpContext.User?.Identity?.Name);
                notifyService.Error("Agency Not Found. Try again.");
                return RedirectToAction(nameof(Agencies), "EmpanelledAgency");
            }
        }

        private async Task Load(Vendor model)
        {
            var vendor = await _context.Vendor.Include(c => c.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long vendorId, Vendor model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await Load(model);
                    return View(model);
                }

                var result = await agencyCreateEditService.EditAsync(userEmail, model, portal_base_url);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await Load(model);
                    return View(model);
                }
                notifyService.Custom($"Agency <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {AgencyId} for {UserEmail}.", vendorId, userEmail);
                notifyService.Error("Error editing agency. Try again.");
            }
            return RedirectToAction(nameof(Detail), "EmpanelledAgency", new { id = vendorId });
        }
    }
}