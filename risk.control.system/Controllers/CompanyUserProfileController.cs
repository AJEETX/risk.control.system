using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("User Profile")]
    [Authorize(Roles = $"{COMPANY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class CompanyUserProfileController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly ICompanyUserService companyUserService;
        private readonly IAccountService accountService;
        private readonly ILogger<CompanyUserProfileController> logger;
        private string portal_base_url = string.Empty;

        public CompanyUserProfileController(ApplicationDbContext context,
            ICompanyUserService companyUserService,
            IAccountService accountService,
            INotyfService notifyService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<CompanyUserProfileController> logger)
        {
            this._context = context;
            this.companyUserService = companyUserService;
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
                var companyUser = await _context.ClientCompanyApplicationUser
                    .Include(u => u.PinCode)
                    .Include(u => u.Country)
                    .Include(u => u.State)
                    .Include(u => u.District)
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

                return View(companyUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting user Profile.");
                notifyService.Error("Error getting user Profile. Try again.");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Edit Profile")]
        public async Task<IActionResult> Edit(long? userId)
        {
            try
            {
                if (userId == null || _context.ClientCompanyApplicationUser == null)
                {
                    notifyService.Error("USER NOT FOUND");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var clientCompanyApplicationUser = await _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).Include(c => c.Country).FirstOrDefaultAsync(u => u.Id == userId);
                if (clientCompanyApplicationUser == null)
                {
                    notifyService.Error("USER NOT FOUND");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(clientCompanyApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting user Profile.");
                notifyService.Error("Error getting user Profile. Try again.");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // POST: ClientCompanyApplicationUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, ClientCompanyApplicationUser model)
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
                var result = await companyUserService.UpdateAsync(id, model, User.Identity?.Name);

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
                logger.LogError(ex, "Error getting user Profile.");
                notifyService.Error("Error getting user Profile. Try again.");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Error("OOPS !!!..Contact Admin");
            return RedirectToAction(nameof(Index), "Dashboard");
        }
        private async Task LoadModel(ClientCompanyApplicationUser model, string currentUserEmail)
        {
            var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var company = await _context.ClientCompany.Include(c => c.Country).FirstOrDefaultAsync(v => v.ClientCompanyId == companyUser.ClientCompanyId);
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
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrEmpty(userEmail))
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("Error occurred. Try again.");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
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
                logger.LogError(ex, "Error occurred while changing password");
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb("Password Change Success")]
        public IActionResult ChangePasswordConfirmation()
        {
            notifyService.Custom($"Password edited successfully.", 3, "orange", "fas fa-user");
            return View();
        }
    }
}