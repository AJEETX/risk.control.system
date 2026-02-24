using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Company;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.CompanyAdmin
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class ManageCompanyController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ICompanyService companyService;
        private readonly ICompanyUserService companyUserService;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<ManageCompanyController> logger;
        private readonly string portal_base_url;

        public ManageCompanyController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICompanyService companyService,
            ICompanyUserService companyUserService,
            INotyfService notifyService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<ManageCompanyController> logger)
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
            return RedirectToAction(nameof(CompanyProfile));
        }

        [Breadcrumb("Company Profile", FromAction = nameof(Index))]
        public async Task<IActionResult> CompanyProfile()
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
                var clientCompany = await _context.ClientCompany.AsNoTracking()
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
                logger.LogError(ex, "Error getting Company for {UserEmail}.", userEmail);
                notifyService.Error("Error getting Company. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb("Edit Company", FromAction = nameof(CompanyProfile))]
        public async Task<IActionResult> Edit()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(CompanyProfile));
                }
                var clientCompany = await _context.ClientCompany.AsNoTracking()
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
            return RedirectToAction(nameof(ManageCompanyController.CompanyProfile), "ManageCompany");
        }

        private async Task Load(ClientCompany model)
        {
            var country = await _context.Country.AsNoTracking().FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }
    }
}