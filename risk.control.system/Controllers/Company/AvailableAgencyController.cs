using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Company
{
    [Breadcrumb("Manage Agency(s)")]
    [Authorize(Roles = $"{MANAGER.DISPLAY_NAME}")]
    public class AvailableAgencyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAgencyCreateEditService agencyCreateEditService;
        private readonly INotyfService notifyService;
        private readonly IInvestigationDetailService investigationDetailService;
        private readonly ILogger<AvailableAgencyController> logger;
        private readonly string portal_base_url = string.Empty;

        public AvailableAgencyController(
            ApplicationDbContext context,
            IAgencyCreateEditService agencyCreateEditService,
            INotyfService notifyService,
            IInvestigationDetailService investigationDetailService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<AvailableAgencyController> logger)
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
            return RedirectToAction(nameof(AvailableVendors));
        }

        [Breadcrumb("Available Agencies")]
        public IActionResult AvailableVendors()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AvailableVendors(List<long> vendors)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("No agency selected !!!");
                    return RedirectToAction(nameof(AvailableVendors));
                }

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(AvailableVendors));
                }
                var company = await _context.ClientCompany.Include(c => c.EmpanelledVendors)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction(nameof(AvailableVendors));
                }
                var vendors2Empanel = await _context.Vendor.AsNoTracking().Where(v => vendors.Contains(v.VendorId)).ToListAsync();
                company.EmpanelledVendors.AddRange(vendors2Empanel);

                company.Updated = DateTime.Now;
                company.UpdatedBy = userEmail;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();

                notifyService.Custom($"Agency(s) empanelled successfully", 3, "green", "fas fa-thumbs-up");
                return RedirectToAction(nameof(AvailableVendors));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred depanelling Agencies. {UserEmail}", userEmail);
                notifyService.Error("Error occurred depanelling Agencies. Try again.");
                return RedirectToAction(nameof(AvailableVendors));
            }
        }

        [Breadcrumb(" Edit Agency", FromAction = "Details")]
        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Error getting Agency. Try again.");
                    return RedirectToAction(nameof(AvailableVendors));
                }

                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("Error getting Agency. Try again.");
                    return RedirectToAction(nameof(AvailableVendors));
                }
                vendor.SelectedByCompany = await _context.ApplicationUser.AnyAsync(u => u.Email.ToLower() == userEmail.ToLower() && u.IsSuperAdmin);
                var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "AvailableAgency", "Manager Agency(s)");
                var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "AvailableAgency", "Available Agencies") { Parent = agencysPage, };
                var agencyPage = new MvcBreadcrumbNode("Details", "AvailableAgency", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Edit", "AvailableAgency", $"Edit Agency") { Parent = agencyPage };

                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Agency {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error getting User. Try again.");
                return RedirectToAction(nameof(AvailableVendors));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long vendorId, Vendor model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadModel(model);
                    return View(model);
                }

                var userEmail = HttpContext.User?.Identity?.Name;

                var result = await agencyCreateEditService.EditAsync(userEmail, model, portal_base_url);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await LoadModel(model);
                    return View(model);
                }
                notifyService.Custom($"Agency <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {AgencyId}. {UserEmail}.", vendorId, User?.Identity?.Name);
                notifyService.Error("Error editing Agency. Try again.");
            }
            return RedirectToAction(nameof(Details), "AvailableAgency", new { id = vendorId });
        }

        [Breadcrumb("Agency Profile", FromAction = "AvailableVendors")]
        public async Task<IActionResult> Details(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("Error getting Agency");
                    return RedirectToAction(nameof(AvailableVendors));
                }

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
                    notifyService.Error("Error getting Agency");
                    return RedirectToAction(nameof(AvailableVendors));
                }
                var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

                var vendorAllCasesCount = await _context.Investigations.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted &&
                          (c.SubStatus == approvedStatus ||
                          c.SubStatus == rejectedStatus));

                var vendorUserCount = await _context.ApplicationUser.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted);

                // HACKY
                var currentCases = investigationDetailService.GetAgencyIdsLoad(new List<long> { vendor.VendorId });
                vendor.SelectedCountryId = vendorUserCount;
                vendor.SelectedStateId = currentCases.FirstOrDefault().CaseCount;
                vendor.SelectedDistrictId = vendorAllCasesCount;
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}.", id, userEmail);
                notifyService.Error("Error getting Agency. Try again.");
                return RedirectToAction(nameof(AvailableVendors));
            }
        }

        public async Task<IActionResult> Delete(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var vendor = await _context.Vendor
                    .Include(v => v.ratings)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.State)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.InvestigationServiceType)
                    .FirstOrDefaultAsync(m => m.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Vendor Not Found");
                    return RedirectToAction(nameof(AvailableVendors));
                }
                var agencySubStatuses = new[]{
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR };

                var hasClaims = _context.Investigations.Any(c => agencySubStatuses.Contains(c.SubStatus) && c.VendorId == id);
                var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "AvailableAgency", "Manager Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("AvailableVendors", "AvailableAgency", "Available Agencies") { Parent = agencysPage, };
                var editPage = new MvcBreadcrumbNode("Delete", "AvailableAgency", $"Delete Agency") { Parent = agencyPage };
                ViewData["BreadcrumbNode"] = editPage;
                vendor.HasClaims = hasClaims;
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}.", id, userEmail);
                notifyService.Error("Error getting agency. Try again.");
                return RedirectToAction(nameof(AvailableVendors));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long VendorId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var vendor = await _context.Vendor.FindAsync(VendorId);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Agency Not Found");
                    return RedirectToAction(nameof(AvailableVendors));
                }

                var vendorUser = await _context.ApplicationUser.Where(v => v.VendorId == VendorId).ToListAsync();
                foreach (var user in vendorUser)
                {
                    user.Updated = DateTime.Now;
                    user.UpdatedBy = userEmail;
                    user.Deleted = true;
                    _context.ApplicationUser.Update(user);
                }
                vendor.Updated = DateTime.Now;
                vendor.UpdatedBy = userEmail;
                vendor.Deleted = true;
                _context.Vendor.Update(vendor);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Agency <b>{vendor.Email}</b> deleted successfully.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(AvailableVendors));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting {AgencyId}. {UserEmail}.", VendorId, userEmail);
                notifyService.Error("Error deleting agency. Try again.");
                return RedirectToAction(nameof(AvailableVendors));
            }
        }

        private async Task LoadModel(Vendor model)
        {
            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }
    }
}