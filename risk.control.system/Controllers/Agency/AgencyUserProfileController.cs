using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.AgencyAdmin;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Agency
{
    [Breadcrumb("User Profile ")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}, {AGENT.DISPLAY_NAME}")]
    public class AgencyUserProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAgencyUserService _agencyUserService;
        private readonly IErrorNotifyService _errorNotifyService;
        private readonly IAccountService _accountService;
        private readonly INotyfService _notifyService;
        private readonly ILogger<AgencyUserProfileController> _logger;
        private readonly string _baseUrl;

        public AgencyUserProfileController(ApplicationDbContext context,
            IAgencyUserService agencyUserService,
            IErrorNotifyService errorNotifyService,
            IAccountService accountService,
             IHttpContextAccessor httpContextAccessor,
            INotyfService notifyService,
            ILogger<AgencyUserProfileController> logger)
        {
            _context = context;
            _agencyUserService = agencyUserService;
            _errorNotifyService = errorNotifyService;
            _accountService = accountService;
            _notifyService = notifyService;
            _logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [Breadcrumb("Edit Profile", FromAction = nameof(DashboardController.Index), FromController = typeof(DashboardController))]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                if (id < 1)
                {
                    _notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var agencyUser = await _agencyUserService.GetUserAsync(id);
                if (agencyUser == null)
                {
                    _notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                return View(agencyUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {UserId} for {UserEmail}", id, HttpContext.User?.Identity?.Name ?? "Anonymous");
                _notifyService.Error("OOPS !!!..Contact Admin");
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
                    _notifyService.Error("Please correct the errors");
                    await _agencyUserService.LoadModel(model, userEmail);
                    return View(model);
                }
                if (id != model.Id.ToString())
                {
                    _notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var result = await _agencyUserService.UpdateUserAsync(id, model, userEmail, _baseUrl);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _agencyUserService.LoadModel(model, userEmail);

                    return View(model);
                }
                _notifyService.Custom($"User profile edited successfully.", 3, "orange", "fas fa-user");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing {UserId} for {UserEmail}", id, userEmail ?? "Anonymous");
                _notifyService.Error("OOPS !!!..Contact Admin");
            }
            return this.RedirectToAction<DashboardController>(x => x.Index());
        }

        [HttpGet]
        [Breadcrumb("Change Password", FromAction = nameof(DashboardController.Index), FromController = typeof(DashboardController))]
        public async Task<IActionResult> ChangePassword()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var vendorUser = await _agencyUserService.GetChangePasswordUserAsync(userEmail);
                if (vendorUser != null)
                {
                    var model = new ChangePasswordViewModel { Id = vendorUser.Id };
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error for {UserEmail}", userEmail ?? "Anonymous");
                _notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            _notifyService.Error("OOPS !!!..Contact Admin");
            return this.RedirectToAction<DashboardController>(x => x.Index());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _notifyService.Custom($"Password update Error", 3, "red", "fa fa-lock");

                return View(model);
            }
            try
            {
                var result = await _accountService.ChangePasswordAsync(model, User, HttpContext.User.Identity.IsAuthenticated, _baseUrl);

                if (!result.Success)
                {
                    _notifyService.Error(result.Message);

                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    return View(model);
                }

                _notifyService.Custom($"Password update successful", 3, "orange", "fa fa-unlock");
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while changing password for {UserEmail}", User.Identity.Name ?? "Anonymous");
                _notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(AccountController.Login), ControllerName<AccountController>.Name);
            }
        }
    }
}