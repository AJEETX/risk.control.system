using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Agency;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Breadcrumb("Admin Settings ")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME}")]
    public class AgencyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAgencyCreateEditService agencyCreateEditService;
        private readonly INotyfService notifyService;
        private readonly ILogger<AgencyController> logger;
        private string portal_base_url = string.Empty;

        public AgencyController(ApplicationDbContext context,
            IAgencyCreateEditService agencyCreateEditService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AgencyController> logger)
        {
            _context = context;
            this.agencyCreateEditService = agencyCreateEditService;
            this.notifyService = notifyService;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        [Breadcrumb("Agency Profile ", FromAction = nameof(Index))]
        public async Task<IActionResult> Profile()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var vendorUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);

                var vendor = await _context.Vendor
                    .Include(v => v.ratings)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .FirstOrDefaultAsync(m => m.VendorId == vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Error("Agency Not Found! Contact ");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Agency for {UserEmail}", userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!...Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb("Edit Agency", FromAction = nameof(Profile))]
        public async Task<IActionResult> Edit()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (vendorUser == null)
                {
                    notifyService.Error("User Not found !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Custom($"Agency Not found.", 3, "red", "fas fa-building");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                if (vendorUser.IsVendorAdmin)
                {
                    vendor.SelectedByCompany = true;
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Agency for {UserEmail}", userEmail ?? "Anonymous");
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vendor model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid)
            {
                notifyService.Error("Please correct the errors");
                await Load(model);
                return View(model);
            }

            try
            {
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
                return RedirectToAction(nameof(AgencyController.Profile), "Agency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Agency for {UserEmail}", userEmail ?? "Anonymous");
                notifyService.Error("OOPS !!!..Error Editing Agency. Try again.");
                return RedirectToAction(nameof(AgencyController.Profile), "Agency");
            }
        }

        private async Task Load(Vendor model)
        {
            var country = await _context.Country.AsNoTracking().FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            if (vendorUser.IsVendorAdmin)
            {
                model.SelectedByCompany = true;
            }
            else
            {
                model.SelectedByCompany = false;
            }
        }
    }
}