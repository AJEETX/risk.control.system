using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.AgencyAdmin;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Agency
{
    [Breadcrumb("User Profile ")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}, {AGENT.DISPLAY_NAME}")]
    public class AgencyUserProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAgencyUserService vendorUserService;
        private readonly INotyfService notifyService;
        private readonly ILogger<AgencyUserProfileController> logger;
        private readonly string portal_base_url = string.Empty;

        public AgencyUserProfileController(ApplicationDbContext context,
            IAgencyUserService vendorUserService,
             IHttpContextAccessor httpContextAccessor,
            INotyfService notifyService,
            ILogger<AgencyUserProfileController> logger)
        {
            _context = context;
            this.vendorUserService = vendorUserService;
            this.notifyService = notifyService;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = await _context.ApplicationUser.AsNoTracking()
                    .Include(u => u.PinCode)
                    .Include(u => u.Country)
                    .Include(u => u.State)
                    .Include(u => u.District)
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

                return View(vendorUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb("Edit Profile")]
        public async Task<IActionResult> Edit(long? userId)
        {
            try
            {
                if (userId == null || userId < 1)
                {
                    notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var agencyUser = await _context.ApplicationUser.AsNoTracking().Include(v => v.Vendor).Include(c => c.Country).FirstOrDefaultAsync(u => u.Id == userId);
                if (agencyUser == null)
                {
                    notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                return View(agencyUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {UserId} for {UserEmail}", userId, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadModel(model, userEmail);
                    return View(model);
                }
                if (id != model.Id.ToString())
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var result = await vendorUserService.UpdateUserAsync(id, model, userEmail, portal_base_url);

                if (!result.Success)
                {
                    notifyService.Error(result.Message ?? "Validation failed");

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }
                    await LoadModel(model, userEmail);

                    return View(model);
                }
                notifyService.Custom($"User profile edited successfully.", 3, "orange", "fas fa-user");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {UserId} for {UserEmail}", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPS !!!..Contact Admin");
            }
            return this.RedirectToAction<DashboardController>(x => x.Index());
        }

        [HttpGet]
        [Breadcrumb("Change Password ")]
        public async Task<IActionResult> ChangePassword()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (vendorUser != null)
                {
                    var model = new ChangePasswordViewModel { Id = vendorUser.Id };
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error for {UserEmail}", userEmail ?? "Anonymous");
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            notifyService.Error("OOPS !!!..Contact Admin");
            return this.RedirectToAction<DashboardController>(x => x.Index());
        }

        private async Task LoadModel(ApplicationUser model, string currentUserEmail)
        {
            var vendorUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var vendor = await _context.Vendor.AsNoTracking().Include(c => c.Country).FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);
            model.Vendor = vendor;
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;

            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }
    }
}