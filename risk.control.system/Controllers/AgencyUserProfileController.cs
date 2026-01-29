using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("User Profile ")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}, {AGENT.DISPLAY_NAME}")]
    public class AgencyUserProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVendorUserService vendorUserService;
        private readonly IAccountService accountService;
        private readonly INotyfService notifyService;
        private readonly ILogger<AgencyUserProfileController> logger;
        private readonly string portal_base_url = string.Empty;

        public AgencyUserProfileController(ApplicationDbContext context,
            IVendorUserService vendorUserService,
            IAccountService accountService,
             IHttpContextAccessor httpContextAccessor,
            INotyfService notifyService,
            ILogger<AgencyUserProfileController> logger)
        {
            _context = context;
            this.vendorUserService = vendorUserService;
            this.accountService = accountService;
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
                var vendorUser = await _context.ApplicationUser
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
                if (userId == null || _context.ApplicationUser == null)
                {
                    notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var vendorApplicationUser = await _context.ApplicationUser.Include(v => v.Vendor).Include(c => c.Country).FirstOrDefaultAsync(u => u.Id == userId);
                if (vendorApplicationUser == null)
                {
                    notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == vendorApplicationUser.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == vendorApplicationUser.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == vendorApplicationUser.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", vendorApplicationUser.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", vendorApplicationUser.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", vendorApplicationUser.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", vendorApplicationUser.PinCodeId);

                return View(vendorApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {UserId} for {UserName}", userId, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    return View(model);
                }
                if (id != model.Id.ToString())
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var result = await vendorUserService.UpdateUserAsync(id, model, User?.Identity?.Name, portal_base_url);

                if (!result.Success)
                {
                    notifyService.Error(result.Message ?? "Validation failed");

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }

                    return View(model);
                }
                notifyService.Custom($"User profile edited successfully.", 3, "orange", "fas fa-user");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {UserId} for {UserName}", id, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPS !!!..Contact Admin");
            }
            return this.RedirectToAction<DashboardController>(x => x.Index());
        }

        [HttpGet]
        [Breadcrumb("Change Password ")]
        public async Task<IActionResult> ChangePassword()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (vendorUser != null)
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error for {UserName}", HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            notifyService.Error("OOPS !!!..Contact Admin");
            return this.RedirectToAction<DashboardController>(x => x.Index());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await accountService.ChangePasswordAsync(model, User, HttpContext.User.Identity.IsAuthenticated, portal_base_url);

                if (!result.Success)
                {
                    notifyService.Error(result.Message);

                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    return View(model);
                }

                return View("ChangePasswordConfirmation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error for changing password by {UserName}", HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpGet]
        [Breadcrumb("Password Change Succees")]
        public IActionResult ChangePasswordConfirmation()
        {
            notifyService.Custom($"Password edited successfully.", 3, "orange", "fas fa-user");
            return View();
        }
    }
}