using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Company;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Company
{
    [Breadcrumb("User Profile")]
    [Authorize(Roles = $"{COMPANY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class CompanyUserProfileController : Controller
    {
        private readonly INotyfService _notifyService;
        private readonly ICompanyUserService _companyUserService;
        private readonly IAccountService _accountService;
        private readonly IErrorNotifyService _errorNotifyService;
        private readonly ILogger<CompanyUserProfileController> _logger;
        private readonly string _baseUrl;

        public CompanyUserProfileController(
            ICompanyUserService companyUserService,
            IAccountService accountService,
            IErrorNotifyService errorNotifyService,
            INotyfService notifyService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<CompanyUserProfileController> logger)
        {
            _companyUserService = companyUserService;
            this._accountService = accountService;
            _errorNotifyService = errorNotifyService;
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
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id < 1)
                {
                    _notifyService.Error("USER NOT FOUND");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var companyUser = await _companyUserService.GetUserAsync(id);
                if (companyUser == null)
                {
                    _notifyService.Error("USER NOT FOUND");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                return View(companyUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId} Profile. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting user Profile. Try again.");
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
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _companyUserService.LoadModel(model, userEmail);
                    return View(model);
                }
                var result = await _companyUserService.UpdateAsync(id, model, userEmail, _baseUrl);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }

                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _companyUserService.LoadModel(model, userEmail);
                    return View(model); // 🔥 fields now highlight
                }
                _notifyService.Success(result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing user {UserId} Profile. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting user Profile. Try again.");
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
                var companyUser = await _companyUserService.GetChangePasswordUserAsync(userEmail);
                if (companyUser != null)
                {
                    var model = new ChangePasswordViewModel { Id = companyUser.Id };
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