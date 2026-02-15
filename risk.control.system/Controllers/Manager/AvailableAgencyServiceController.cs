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
    [Breadcrumb("Manage Agency")]
    [Authorize(Roles = $"{MANAGER.DISPLAY_NAME}")]
    public class AvailableAgencyServiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAgencyServiceTypeManager vendorServiceTypeManager;
        private readonly INavigationService navigationService;
        private readonly ILogger<AvailableAgencyServiceController> logger;
        private readonly INotyfService notifyService;

        public AvailableAgencyServiceController(ApplicationDbContext context,
            IAgencyServiceTypeManager vendorServiceTypeManager,
            INavigationService navigationService,
            ILogger<AvailableAgencyServiceController> logger,
            INotyfService notifyService)
        {
            _context = context;
            this.vendorServiceTypeManager = vendorServiceTypeManager;
            this.navigationService = navigationService;
            this.logger = logger;
            this.notifyService = notifyService;
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
            ViewData["BreadcrumbNode"] = navigationService.GetAgencyServiceManagerPath(id, ControllerName<AvailableAgencyController>.Name, "Available Agencies");
            return View(model);
        }

        public async Task<IActionResult> Create(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
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
                ViewData["BreadcrumbNode"] = navigationService.GetAgencyServiceActionPath(id, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Add Service", "Create");
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}", id, userEmail);
                notifyService.Error("Error getting agency service. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorInvestigationServiceType service, long VendorId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var result = await vendorServiceTypeManager.CreateAsync(service, userEmail);

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
                logger.LogError(ex, "Error creating service for {AgencyId}. {UserEmail}", VendorId, userEmail);
                notifyService.Error("Error creating service. Try again.");
            }
            return RedirectToAction(nameof(Service), ControllerName<AvailableAgencyServiceController>.Name, new { id = service.VendorId });
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
                var serviceType = _context.VendorInvestigationServiceType.AsNoTracking()
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

                ViewData["BreadcrumbNode"] = navigationService.GetAgencyServiceActionPath(serviceType.VendorId, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Edit Service", "Edit");

                return View(serviceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting service for {ServiceId}. {UserEmail}", id, userEmail);
                notifyService.Error("Error getting agency service. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VendorInvestigationServiceType service)
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
                logger.LogError(ex, "Error editing Service. {UserEmail}", userEmail);
                notifyService.Custom("Error editing service. Try again.", 3, "red", "fas fa-cog");
            }
            return RedirectToAction(nameof(Service), ControllerName<AvailableAgencyServiceController>.Name, new { id = service.VendorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
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