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
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class CompanyController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ICompanyService companyService;
        private readonly ICompanyUserService companyUserService;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<CompanyController> logger;
        private readonly string portal_base_url;

        public CompanyController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICompanyService companyService,
            ICompanyUserService companyUserService,
            INotyfService notifyService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<CompanyController> logger)
        {
            this._context = context;
            this.userManager = userManager;
            this.companyService = companyService;
            this.companyUserService = companyUserService;
            this.notifyService = notifyService;
            this.featureManager = featureManager;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [Breadcrumb("Manage Company")]
        public IActionResult Index()
        {
            return RedirectToAction("CompanyProfile");
        }

        [Breadcrumb("Company Profile", FromAction = "Index")]
        public async Task<IActionResult> CompanyProfile()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var clientCompany = await _context.ClientCompany
                    .Include(c => c.Country)
                    .Include(c => c.District)
                    .Include(c => c.PinCode)
                    .Include(c => c.State)
                    .FirstOrDefaultAsync(m => m.ClientCompanyId == companyUser.ClientCompanyId);
                if (clientCompany == null)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                return View(clientCompany);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Company for {UserEmail}.", HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting Company. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb("Edit Company", FromAction = "CompanyProfile")]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(CompanyProfile));
                }
                var clientCompany = await _context.ClientCompany
                    .Include(c => c.Country)
                    .Include(c => c.State)
                    .Include(c => c.District)
                    .Include(c => c.PinCode)
                    .FirstOrDefaultAsync(m => m.ClientCompanyId == companyUser.ClientCompanyId);
                if (clientCompany == null)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction(nameof(CompanyProfile));
                }

                return View(clientCompany);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Company for {UserEmail}.", HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting Company. Try again");
                return RedirectToAction(nameof(CompanyProfile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClientCompany model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    await Load(model);
                    return View(model);
                }
                var userEmail = HttpContext.User?.Identity?.Name;

                var result = await companyService.EditAsync(userEmail, model, portal_base_url);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await Load(model);
                    return View(model);
                }
                notifyService.Custom($"Company <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {CompanyId} for {UserEmail}.", model.ClientCompanyId, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error editing Company. Try again.");
                return RedirectToAction(nameof(CompanyProfile));
            }
            return RedirectToAction(nameof(CompanyController.CompanyProfile), "Company");
        }

        private async Task Load(ClientCompany model)
        {
            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }

        [Breadcrumb("Manage Users ")]
        public IActionResult Users()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            return View();
        }

        [Breadcrumb("Add User")]
        public async Task<IActionResult> CreateUser()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var company = await _context.ClientCompany.Include(c => c.Country).FirstOrDefaultAsync(v => v.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var usersInCompany = _context.ApplicationUser.Where(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
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
                logger.LogError(ex, "Error creating user for {UserEmail}.", HttpContext.User?.Identity?.Name);
                notifyService.Error("Error creating user. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        private async Task LoadModel(ApplicationUser model, string currentUserEmail)
        {
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var company = await _context.ClientCompany.Include(c => c.Country).FirstOrDefaultAsync(v => v.ClientCompanyId == companyUser.ClientCompanyId);
            var usersInCompany = _context.ApplicationUser.Where(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
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
        public async Task<IActionResult> CreateUser(ApplicationUser model, string emailSuffix)
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
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
                logger.LogError(ex, "Error creating user for {UserEmail}.", HttpContext.User?.Identity?.Name);
                notifyService.Error("Error creating user. Try again");
            }
            return RedirectToAction(nameof(CompanyController.Users), "Company");
        }

        [Breadcrumb("Edit User", FromAction = "Users")]
        public async Task<IActionResult> EditUser(long? userId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (userId == null || _context.ApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Users));
                }

                var clientCompanyApplicationUser = await _context.ApplicationUser
                    .Include(u => u.Country).
                    Include(u => u.ClientCompany)
                    .FirstOrDefaultAsync(c => c.Id == userId);

                if (clientCompanyApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Users));
                }
                var usersInCompany = _context.ApplicationUser.Where(c => !c.Deleted && c.ClientCompanyId == clientCompanyApplicationUser.ClientCompanyId && c.Id != clientCompanyApplicationUser.Id);
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

                clientCompanyApplicationUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !clientCompanyApplicationUser.IsPasswordChangeRequired : true;
                clientCompanyApplicationUser.AvailableRoles = availableRoles;
                return View(clientCompanyApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {UserId} for {UserEmail}.", userId, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error editing user. Try again");
            }
            return RedirectToAction(nameof(CompanyController.Users), "Company");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(long id, ApplicationUser model)
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;

                if (!ModelState.IsValid)
                {
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model, userEmail);
                    return View(model);
                }
                var result = await companyUserService.UpdateAsync(id, model, User.Identity?.Name, portal_base_url);

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
                logger.LogError(ex, "Error editing {UserId} for {UserEmail}.", id, HttpContext.User?.Identity?.Name);
                notifyService.Error($"Error to create Company user. Try again.", 3);
            }
            return RedirectToAction(nameof(CompanyController.Users), "Company");
        }

        [Breadcrumb(title: " Delete", FromAction = "Users")]
        public async Task<IActionResult> DeleteUser(long userId)
        {
            try
            {
                if (userId < 1 || userId == 0)
                {
                    notifyService.Error("Invalid Data. Try Again");
                    return RedirectToAction(nameof(Users));
                }

                var model = await _context.ApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode)
                    .FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("User Not Found.Try Again");
                    return RedirectToAction(nameof(Users));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {UserId} for {UserEmail}.", userId, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting user. Try again");
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string email)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var model = await _context.ApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode)
                    .FirstOrDefaultAsync(c => c.Email == email);
                if (model == null)
                {
                    notifyService.Error("Error getting user. Try again");
                    return RedirectToAction(nameof(Users));
                }

                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.Deleted = true;
                _context.ApplicationUser.Update(model);
                await _context.SaveChangesAsync();
                notifyService.Custom($"User <b>{model.Email}</b> deleted successfully", 3, "orange", "fas fa-user-minus");
                return RedirectToAction(nameof(CompanyController.Users), "Company");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting {UserId} for {UserEmail}.", email, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting user. Try again");
                return RedirectToAction(nameof(Users));
            }
        }
    }
}