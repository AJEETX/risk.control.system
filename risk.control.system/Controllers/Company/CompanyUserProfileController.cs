using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Company;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Company
{
    [Breadcrumb("User Profile")]
    [Authorize(Roles = $"{COMPANY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class CompanyUserProfileController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly ICompanyUserService companyUserService;
        private readonly ILogger<CompanyUserProfileController> logger;
        private readonly string portal_base_url = string.Empty;

        public CompanyUserProfileController(ApplicationDbContext context,
            ICompanyUserService companyUserService,
            INotyfService notifyService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<CompanyUserProfileController> logger)
        {
            this._context = context;
            this.companyUserService = companyUserService;
            this.notifyService = notifyService;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var companyUser = await _context.ApplicationUser.AsNoTracking()
                    .Include(u => u.PinCode)
                    .Include(u => u.Country)
                    .Include(u => u.State)
                    .Include(u => u.District)
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

                return View(companyUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting user Profile. {UserEmail}", userEmail);
                notifyService.Error("Error getting user Profile. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb("Edit Profile")]
        public async Task<IActionResult> Edit(long? userId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (userId == null)
                {
                    notifyService.Error("USER NOT FOUND");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var companyUser = await _context.ApplicationUser.AsNoTracking().Include(u => u.ClientCompany).Include(c => c.Country).FirstOrDefaultAsync(u => u.Id == userId);
                if (companyUser == null)
                {
                    notifyService.Error("USER NOT FOUND");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                return View(companyUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting user {UserId} Profile. {UserEmail}", userId, userEmail);
                notifyService.Error("Error getting user Profile. Try again.");
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
                    await LoadModel(model, userEmail);
                    return View(model); // 🔥 fields now highlight
                }
                notifyService.Success(result.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing user {UserId} Profile. {UserEmail}", id, userEmail);
                notifyService.Error("Error getting user Profile. Try again.");
            }
            return this.RedirectToAction<DashboardController>(x => x.Index());
        }

        private async Task LoadModel(ApplicationUser model, string currentUserEmail)
        {
            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var company = await _context.ClientCompany.AsNoTracking().Include(c => c.Country).FirstOrDefaultAsync(v => v.ClientCompanyId == companyUser.ClientCompanyId);
            model.ClientCompany = company;
            model.Country = company.Country;
            model.CountryId = company.CountryId;

            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }

        [Breadcrumb("Change Password")]
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            return View();
        }
    }
}