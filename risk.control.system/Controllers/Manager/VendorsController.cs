using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Manager
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class VendorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAgencyCreateEditService agencyCreateEditService;
        private readonly INotyfService notifyService;
        private readonly ILogger<VendorsController> logger;
        private readonly string portal_base_url = string.Empty;

        public VendorsController(
            ApplicationDbContext context,
            IAgencyCreateEditService agencyCreateEditService,
            INotyfService notifyService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<VendorsController> logger)
        {
            _context = context;
            this.agencyCreateEditService = agencyCreateEditService;
            this.notifyService = notifyService;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [Breadcrumb("Manage Agency(s)")]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Create));
        }

        [Breadcrumb(" Add Agency")]
        public async Task<IActionResult> Create()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            var companyUser = await _context.ApplicationUser.Include(c => c.Country).Include(c => c.ClientCompany).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var vendor = new Vendor { CountryId = companyUser.ClientCompany.CountryId, Country = companyUser.ClientCompany.Country, SelectedCountryId = companyUser.ClientCompany.CountryId.Value };
            return View(vendor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vendor model, string domainAddress)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadModel(model);
                    return View(model);
                }
                if (!RegexHelper.IsMatch(domainAddress))
                {
                    ModelState.AddModelError("Email", "Invalid email address.");
                    await LoadModel(model);
                    return View(model);
                }

                var result = await agencyCreateEditService.CreateAsync(domainAddress, userEmail, model, portal_base_url);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await LoadModel(model);
                    return View(model);
                }
                notifyService.Custom($"Agency <b>{model.Email}</b>  created successfully.", 3, "green", "fas fa-building");
                return RedirectToAction(nameof(AvailableAgencyController.Agencies), "AvailableAgency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating user. {UserEmail}.", userEmail);
                notifyService.Error("OOPS !!!..Error creating Agency. Try again.");
                return RedirectToAction(nameof(Create));
            }
        }

        private async Task LoadModel(Vendor model)
        {
            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }
    }
}