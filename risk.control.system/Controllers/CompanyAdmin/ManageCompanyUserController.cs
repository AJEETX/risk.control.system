using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using risk.control.system.Services.Company;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.CompanyAdmin
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    [Breadcrumb("Manage Company")]
    public class ManageCompanyUserController : Controller
    {
        private readonly INotyfService _notifyService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IManageCompanyUserService _manageCompanyUserService;
        private readonly IErrorNotifyService _errorNotifyService;
        private readonly ILogger<ManageCompanyUserController> _logger;
        private readonly string _baseUrl;

        public ManageCompanyUserController(
            UserManager<ApplicationUser> userManager,
            IManageCompanyUserService manageCompanyUserService,
            IErrorNotifyService errorNotifyService,
            INotyfService notifyService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<ManageCompanyUserController> logger)
        {
            _userManager = userManager;
            _manageCompanyUserService = manageCompanyUserService;
            _errorNotifyService = errorNotifyService;
            _notifyService = notifyService;
            _logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Users));
        }

        [Breadcrumb("Manage Users ")]
        public IActionResult Users()
        {
            return View();
        }

        [Breadcrumb("Add User", FromAction = nameof(Users))]
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var model = await _manageCompanyUserService.GetUserCreationModelAsync(User.Identity.Name);
                if (model == null)
                {
                    _notifyService.Error("User or Company not found.");
                    return RedirectToAction("Index", "Dashboard");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user for {UserEmail}.", userEmail);
                _notifyService.Error("Error creating user. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, string emailSuffix)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _manageCompanyUserService.LoadModelAsync(model, User.Identity.Name);
                    return View(model);
                }

                var result = await _manageCompanyUserService.CreateUserAsync(model, emailSuffix, User.Identity.Name, _baseUrl);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _manageCompanyUserService.LoadModelAsync(model, User.Identity.Name);
                    return View(model);
                }

                _notifyService.Success(result.Message);
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user for {Company}. {UserEmail}.", emailSuffix, userEmail);
                _notifyService.Error("Error creating user. Try again");
            }
            return RedirectToAction(nameof(Users), ControllerName<ManageCompanyUserController>.Name);
        }

        [Breadcrumb("Edit User", FromAction = nameof(Users))]
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return RedirectToAction(nameof(Users));

            try
            {
                var model = await _manageCompanyUserService.GetUserForEditAsync(id.Value);
                if (model == null)
                {
                    _notifyService.Error("User not found.");
                    return RedirectToAction(nameof(Users));
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing {UserId} for {UserEmail}.", id, HttpContext.User?.Identity?.Name);
                _notifyService.Error("Error editing user. Try again");
            }
            return RedirectToAction(nameof(Users), ControllerName<ManageCompanyUserController>.Name);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser model)
        {
            var currentUser = User.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _manageCompanyUserService.LoadModelAsync(model, currentUser);
                    return View(model);
                }

                var result = await _manageCompanyUserService.UpdateUserAsync(id, model, currentUser, _baseUrl);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);
                    _errorNotifyService.ShowErrorNotification(ModelState);

                    await _manageCompanyUserService.LoadModelAsync(model, currentUser);
                    return View(model);
                }

                _notifyService.Custom(result.Message, 3, "orange", "fas fa-user-check");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}.", id);
                _notifyService.Error("An unexpected error occurred.");
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string userId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new { success = false, message = "Invalid user id" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Optional extra safety
                if (await _userManager.IsInRoleAsync(user, COMPANY_ADMIN.DISPLAY_NAME))
                {
                    return Json(new { success = false, message = "Company Admin cannot be deleted" });
                }

                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = errors });
                }

                return Json(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                // 🔥 log this properly in real apps
                _logger.LogError(ex, "Error deleting user {UserId}. {UserEmail}", userId, userEmail);

                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while deleting the user"
                });
            }
        }
    }
}