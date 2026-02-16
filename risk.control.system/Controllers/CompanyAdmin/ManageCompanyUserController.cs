using System.ComponentModel.DataAnnotations;
using System.Reflection;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Company;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.CompanyAdmin
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    [Breadcrumb("Manage Company")]
    public class ManageCompanyUserController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ICompanyUserService companyUserService;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<ManageCompanyUserController> logger;
        private readonly string portal_base_url;

        public ManageCompanyUserController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICompanyUserService companyUserService,
            INotyfService notifyService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<ManageCompanyUserController> logger)
        {
            this._context = context;
            this.userManager = userManager;
            this.companyUserService = companyUserService;
            this.notifyService = notifyService;
            this.featureManager = featureManager;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
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
                var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var company = await _context.ClientCompany.AsNoTracking().Include(c => c.Country).FirstOrDefaultAsync(v => v.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var usersInCompany = _context.ApplicationUser.AsNoTracking().Where(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
                bool isManagerTaken = false;

                foreach (var user in usersInCompany)
                {
                    if (await userManager.IsInRoleAsync(user, MANAGER.DISPLAY_NAME))
                    {
                        isManagerTaken = true;
                        break;
                    }
                }

                var availableRoles = RoleGroups.CompanyAppRoles
                    .Where(r => r != AppRoles.COMPANY_ADMIN && r != AppRoles.MANAGER || !isManagerTaken) // Exclude MANAGER if already taken
                    .Select(r => new SelectListItem
                    {
                        Value = r.ToString(),
                        Text = r.GetType()
                                .GetMember(r.ToString())
                                .First()
                                .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                    })
                    .ToList();

                var model = new ApplicationUser { Country = company.Country, ClientCompany = company, CountryId = company.CountryId, AvailableRoles = availableRoles };
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating user for {UserEmail}.", userEmail);
                notifyService.Error("Error creating user. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        private async Task LoadModel(ApplicationUser model, string userEmail)
        {
            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);
            var company = await _context.ClientCompany.AsNoTracking().Include(c => c.Country).FirstOrDefaultAsync(v => v.ClientCompanyId == companyUser.ClientCompanyId);
            var usersInCompany = _context.ApplicationUser.AsNoTracking().Where(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
            bool isManagerTaken = false;

            foreach (var user in usersInCompany)
            {
                if (await userManager.IsInRoleAsync(user, MANAGER.DISPLAY_NAME))
                {
                    isManagerTaken = true;
                    break;
                }
            }

            var availableRoles = RoleGroups.CompanyAppRoles
                .Where(r => r != AppRoles.COMPANY_ADMIN && r != AppRoles.MANAGER || !isManagerTaken) // Exclude MANAGER if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();

            model.ClientCompany = company;
            model.Country = company.Country;
            model.CountryId = company.CountryId;

            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.AvailableRoles = availableRoles;
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
                    notifyService.Error("Please correct the errors");
                    await LoadModel(model, userEmail);
                    return View(model);
                }
                var result = await companyUserService.CreateAsync(model, emailSuffix, userEmail, portal_base_url);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }

                    notifyService.Error("Correct the highlighted errors.");
                    await LoadModel(model, userEmail);
                    return View(model); // 🔥 fields now highlight
                }

                notifyService.Success(result.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating user for {Company}. {UserEmail}.", emailSuffix, userEmail);
                notifyService.Error("Error creating user. Try again");
            }
            return RedirectToAction(nameof(Users), ControllerName<ManageCompanyUserController>.Name);
        }

        [Breadcrumb("Edit User", FromAction = nameof(Users))]
        public async Task<IActionResult> Edit(long? id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (id == null || _context.ApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Users));
                }

                var companyUser = await _context.ApplicationUser.AsNoTracking()
                    .Include(u => u.Country).
                    Include(u => u.ClientCompany)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Users));
                }
                var usersInCompany = _context.ApplicationUser.AsNoTracking().Where(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId && c.Id != companyUser.Id);
                bool isManagerTaken = false;

                foreach (var user in usersInCompany)
                {
                    if (await userManager.IsInRoleAsync(user, MANAGER.DISPLAY_NAME))
                    {
                        isManagerTaken = true;
                        break;
                    }
                }

                var availableRoles = RoleGroups.CompanyAppRoles
                    .Where(r => r != AppRoles.COMPANY_ADMIN && r != AppRoles.MANAGER || !isManagerTaken) // Exclude MANAGER if already taken
                    .Select(r => new SelectListItem
                    {
                        Value = r.ToString(),
                        Text = r.GetType()
                                .GetMember(r.ToString())
                                .First()
                                .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                    })
                    .ToList();

                companyUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !companyUser.IsPasswordChangeRequired : true;
                companyUser.AvailableRoles = availableRoles;
                return View(companyUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {UserId} for {UserEmail}.", id, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error editing user. Try again");
            }
            return RedirectToAction(nameof(Users), ControllerName<ManageCompanyUserController>.Name);
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
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model, userEmail);
                    return View(model);
                }
                var result = await companyUserService.UpdateAsync(id, model, userEmail, portal_base_url);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }

                    notifyService.Error("Correct the highlighted errors.");
                    await LoadModel(model, User.Identity?.Name);
                    return View(model); // 🔥 fields now highlight
                }

                notifyService.Success(result.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {UserId} for {UserEmail}.", id, userEmail);
                notifyService.Error($"Error to create Company user. Try again.", 3);
            }
            return RedirectToAction(nameof(Users), ControllerName<ManageCompanyUserController>.Name);
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

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Optional extra safety
                if (await userManager.IsInRoleAsync(user, "COMPANY_ADMIN"))
                {
                    return Json(new { success = false, message = "Company Admin cannot be deleted" });
                }

                var result = await userManager.DeleteAsync(user);

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
                logger.LogError(ex, "Error deleting user {UserId}. {UserEmail}", userId, userEmail);

                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while deleting the user"
                });
            }
        }
    }
}