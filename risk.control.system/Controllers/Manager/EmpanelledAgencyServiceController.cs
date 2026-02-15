using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Manager
{
    [Authorize(Roles = $"{MANAGER.DISPLAY_NAME}")]
    [Breadcrumb("Manage Agency")]
    public class EmpanelledAgencyServiceController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly INavigationService navigationService;
        private readonly IAgencyServiceTypeManager vendorServiceTypeManager;
        private readonly ILogger<EmpanelledAgencyServiceController> logger;

        public EmpanelledAgencyServiceController(ApplicationDbContext context,
            INavigationService navigationService,
            IAgencyServiceTypeManager vendorServiceTypeManager,
            INotyfService notifyService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<EmpanelledAgencyServiceController> logger)
        {
            this._context = context;
            this.navigationService = navigationService;
            this.vendorServiceTypeManager = vendorServiceTypeManager;
            this.notifyService = notifyService;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Service(long id)
        {
            if (id <= 0)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }

            var model = new ServiceModel { Id = id };

            ViewData["BreadcrumbNode"] = navigationService.GetAgencyServiceManagerPath(id, ControllerName<EmpanelledAgencyController>.Name, "Active Agencies");
            return View(model);
        }

        public async Task<IActionResult> Create(long id)
        {
            try
            {
                var vendor = await _context.Vendor.AsNoTracking().Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                var model = new VendorInvestigationServiceType
                {
                    Country = vendor.Country,
                    CountryId = vendor.CountryId,
                    Vendor = vendor,
                    Currency = CustomExtensions.GetCultureByCountry(vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol
                };
                ViewData["BreadcrumbNode"] = navigationService.GetAgencyServiceActionPath(id, ControllerName<EmpanelledAgencyController>.Name, "Active Agencies", "Add Service", "Create");
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred creating Service for {AgencyId} . {UserEmail}", id, HttpContext.User.Identity.Name);
                notifyService.Error("Error occurred. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorInvestigationServiceType service, long VendorId)
        {
            try
            {
                var email = HttpContext.User?.Identity?.Name;

                var result = await vendorServiceTypeManager.CreateAsync(service, email);

                if (!result.Success)
                {
                    notifyService.Custom(result.Message, 3, "orange", "fas fa-cog");
                }
                else
                {
                    notifyService.Custom(result.Message, 3, result.IsAllDistricts ? "orange" : "green", "fas fa-cog");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred creating Service for {AgencyId} . {UserEmail}", VendorId, HttpContext.User.Identity.Name);
                notifyService.Error("Error creating agency service. Try again.");
            }
            return RedirectToAction(nameof(Service), ControllerName<EmpanelledAgencyServiceController>.Name, new { id = service.VendorId });
        }

        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("OOPs !!!..Agency Id Not Found");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var serviceType = _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.Country)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.Vendor)
                    .First(v => v.VendorInvestigationServiceTypeId == id);
                serviceType.Currency = CustomExtensions.GetCultureByCountry(serviceType.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                serviceType.InvestigationServiceTypeList = await _context.InvestigationServiceType
                    .Where(i => i.InsuranceType == serviceType.InsuranceType)
                    .Select(i => new SelectListItem
                    {
                        Value = i.InvestigationServiceTypeId.ToString(),
                        Text = i.Name,
                        Selected = i.InvestigationServiceTypeId == serviceType.InvestigationServiceTypeId
                    }).ToListAsync();

                ViewData["BreadcrumbNode"] = navigationService.GetAgencyServiceActionPath(serviceType.VendorId, ControllerName<EmpanelledAgencyController>.Name, "Active Agencies", "Edit Service", "Edit");

                return View(serviceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting Service for {ServiceId}. {UserEmail}", id, userEmail);
                notifyService.Error("Error occurred. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VendorInvestigationServiceType service, long VendorId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var result = await vendorServiceTypeManager.EditAsync(service, userEmail);
                if (!result.Success)
                {
                    notifyService.Custom(result.Message, 3, "orange", "fas fa-cog");
                }
                else
                {
                    notifyService.Custom(result.Message, 3, result.Success ? "green" : "orange", "fas fa-cog");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred editing Service. {UserEmail}", userEmail);
                notifyService.Error("Error editing agency service. Try again.");
            }
            return RedirectToAction(nameof(Service), ControllerName<EmpanelledAgencyServiceController>.Name, new { id = service.VendorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(long id)
        {
            try
            {
                var service = await _context.VendorInvestigationServiceType
                    .FirstOrDefaultAsync(x => x.VendorInvestigationServiceTypeId == id);

                if (service == null)
                    return NotFound();

                _context.VendorInvestigationServiceType.Remove(service);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Service deleted successfully." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting service {ServiceId}", id);
                return StatusCode(500, new { success = false, message = "Delete failed." });
            }
        }
    }
}