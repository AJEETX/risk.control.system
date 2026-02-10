using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Manager
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
            return RedirectToAction(nameof(Agencies));
        }

        [Breadcrumb("Available Agencies")]
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
                    notifyService.Error("No agency selected !!!");
                    return RedirectToAction(nameof(Agencies));
                }

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(Agencies));
                }
                var company = await _context.ClientCompany.Include(c => c.EmpanelledVendors)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction(nameof(Agencies));
                }
                var vendors2Empanel = await _context.Vendor.AsNoTracking().Where(v => vendors.Contains(v.VendorId)).ToListAsync();
                company.EmpanelledVendors.AddRange(vendors2Empanel);

                company.Updated = DateTime.UtcNow;
                company.UpdatedBy = userEmail;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();

                notifyService.Custom($"Agency(s) empanelled successfully", 3, "green", "fas fa-thumbs-up");
                return RedirectToAction(nameof(Agencies));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred depanelling Agencies. {UserEmail}", userEmail);
                notifyService.Error("Error occurred depanelling Agencies. Try again.");
                return RedirectToAction(nameof(Agencies));
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
                    return RedirectToAction(nameof(Agencies));
                }

                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("Error getting Agency. Try again.");
                    return RedirectToAction(nameof(Agencies));
                }
                vendor.SelectedByCompany = await _context.ApplicationUser.AnyAsync(u => u.Email.ToLower() == userEmail.ToLower() && u.IsSuperAdmin);
                var agencysPage = new MvcBreadcrumbNode("Agencies", "AvailableAgency", "Manager Agency(s)");
                var agency2Page = new MvcBreadcrumbNode("Agencies", "AvailableAgency", "Available Agencies") { Parent = agencysPage, };
                var agencyPage = new MvcBreadcrumbNode("Details", "AvailableAgency", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Edit", "AvailableAgency", $"Edit Agency") { Parent = agencyPage };

                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Agency {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error getting User. Try again.");
                return RedirectToAction(nameof(Agencies));
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

        [Breadcrumb("Agency Profile", FromAction = "Agencies")]
        public async Task<IActionResult> Details(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("Error getting Agency");
                    return RedirectToAction(nameof(Agencies));
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
                    return RedirectToAction(nameof(Agencies));
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
                return RedirectToAction(nameof(Agencies));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (id < 1)
                return BadRequest(new { message = "Invalid Agency Id." });

            try
            {
                var vendor = await _context.Vendor.FindAsync(id);
                if (vendor == null)
                    return NotFound(new { message = "Agency not found." });

                var vendorUsers = await _context.ApplicationUser
                    .Where(v => v.VendorId == id)
                    .ToListAsync();

                foreach (var user in vendorUsers)
                {
                    user.Updated = DateTime.UtcNow;
                    user.UpdatedBy = userEmail;
                    user.Deleted = true;
                }

                vendor.Updated = DateTime.UtcNow;
                vendor.UpdatedBy = userEmail;
                vendor.Deleted = true;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Agency {vendor.Email} deleted successfully."
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting {AgencyId}. {UserEmail}.", id, userEmail);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting agency. Please try again."
                });
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