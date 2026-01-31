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
using risk.control.system.Controllers.CompanyAdmin;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Manager
{
    [Authorize(Roles = $"{COMPANY_ADMIN.DISPLAY_NAME}")]
    [Breadcrumb("Manage Company")]
    public class CreateCompanyUserController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ICompanyUserService companyUserService;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<CreateCompanyUserController> logger;
        private readonly string portal_base_url;

        public CreateCompanyUserController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICompanyUserService companyUserService,
            INotyfService notifyService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<CreateCompanyUserController> logger)
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
            return RedirectToAction(nameof(CreateUser));
        }

        [Breadcrumb("Add User")]
        public async Task<IActionResult> CreateUser()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
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
                logger.LogError(ex, "Error creating user for {UserEmail}.", userEmail);
                notifyService.Error("Error creating user. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        private async Task LoadModel(ApplicationUser model, string userEmail)
        {
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
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
            return RedirectToAction(nameof(ManageCompanyUserController.Users), "ManageCompanyUser");
        }
    }
}