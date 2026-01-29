using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Admin Settings ")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME}")]
    public class AgencyServiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVendorServiceTypeManager vendorServiceTypeManager;
        private readonly INotyfService notifyService;
        private readonly ILogger<AgencyServiceController> logger;

        public AgencyServiceController(ApplicationDbContext context,
            IVendorServiceTypeManager vendorServiceTypeManager,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AgencyServiceController> logger)
        {
            _context = context;
            this.vendorServiceTypeManager = vendorServiceTypeManager;
            this.notifyService = notifyService;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Service));
        }

        [Breadcrumb("Manage Service")]
        public IActionResult Service()
        {
            return View();
        }

        [Breadcrumb("Add Service")]
        public async Task<IActionResult> CreateService()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (vendorUser is null)
                {
                    notifyService.Error("User Not Found!!!..Try again");
                    return RedirectToAction(nameof(Service), "AgencyService");
                }
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);
                if (vendor is null)
                {
                    notifyService.Error("Agency Not Found!!!..Try again");
                    return RedirectToAction(nameof(Service), "AgencyService");
                }
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var model = new VendorInvestigationServiceType
                {
                    Country = vendor.Country,
                    CountryId = vendor.CountryId,
                    Vendor = vendor
                };
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating service for {UserName}", userEmail ?? "Anonymous");
                notifyService.Error("Error creating service.Try again.");
                return RedirectToAction(nameof(Service), "AgencyService");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(VendorInvestigationServiceType service)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var result = await vendorServiceTypeManager.CreateAsync(service, userEmail);

                if (!result.Success)
                {
                    notifyService.Custom(result.Message, 3, "orange", "fas fa-truck");
                }
                else
                {
                    notifyService.Custom(result.Message, 3, result.IsAllDistricts ? "orange" : "green", "fas fa-truck");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating service for {UserName}", userEmail ?? "Anonymous");
                notifyService.Error("Error creating service. Try again.");
            }
            return RedirectToAction(nameof(Service), "AgencyService");
        }

        [Breadcrumb("Edit Service", FromAction = "Service")]
        public async Task<IActionResult> EditService(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await _context.ApplicationUser.Include(c => c.Vendor).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.Vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                var vendorInvestigationServiceType = _context.VendorInvestigationServiceType
                    .Include(v => v.Country)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.Vendor)
                    .First(v => v.VendorInvestigationServiceTypeId == id);

                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.InsuranceType == vendorInvestigationServiceType.InsuranceType), "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {SserviceId} for {UserName}", id, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Custom($"Error editing service. Try again", 3, "red", "fas fa-truck");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(long vendorInvestigationServiceTypeId, VendorInvestigationServiceType service)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var result = await vendorServiceTypeManager.EditAsync(vendorInvestigationServiceTypeId, service, userEmail);
                if (!result.Success)
                {
                    notifyService.Custom(result.Message, 3, "orange", "fas fa-truck");
                }
                else
                {
                    notifyService.Custom(result.Message, 3, result.Success ? "green" : "orange", "fas fa-truck");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {ServiceId} for {UserName}", vendorInvestigationServiceTypeId, userEmail ?? "Anonymous");
                notifyService.Custom("Error editing service. Try again.", 3, "red", "fas fa-truck");
            }
            return RedirectToAction(nameof(Service), "AgencyService");
        }

        [Breadcrumb("Delete Service", FromAction = "Service")]
        public async Task<IActionResult> DeleteService(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Custom($"Invalid Data.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Service), "AgencyService");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await _context.ApplicationUser.Include(c => c.Vendor).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.Vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.Country)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.Vendor)
                    .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
                if (vendorInvestigationServiceType == null)
                {
                    notifyService.Error($"Service Not Found. Try again");
                    return RedirectToAction(nameof(Service), "AgencyService");
                }

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {ServiceId} for {UserName}", id, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error($"Error deleting service. Try again");
                return RedirectToAction(nameof(Service), "AgencyService");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (id <= 0)
                {
                    notifyService.Custom($"Service Not Found.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Service), "AgencyService");
                }
                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType.FindAsync(id);
                if (vendorInvestigationServiceType != null)
                {
                    vendorInvestigationServiceType.Updated = DateTime.Now;
                    vendorInvestigationServiceType.UpdatedBy = currentUserEmail;
                    _context.VendorInvestigationServiceType.Remove(vendorInvestigationServiceType);
                    await _context.SaveChangesAsync();
                    notifyService.Custom($"Service deleted successfully.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Service), "AgencyService");
                }
                notifyService.Error($"Err Service delete.", 3);
                return RedirectToAction(nameof(Service), "AgencyService");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting {ServiceId} for {UserName}", id, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error($"Error deleting service. Try again");
                return RedirectToAction(nameof(Service), "AgencyService");
            }
        }

        [Breadcrumb("Services", FromAction = "Service")]
        public async Task<IActionResult> ServiceDetail(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("Service Not Found.");
                    return RedirectToAction(nameof(Service));
                }

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.Country)
                    .Include(v => v.Vendor)
                    .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
                if (vendorInvestigationServiceType == null)
                {
                    notifyService.Error("Service Not Found.");
                    return RedirectToAction(nameof(Service));
                }

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {ServiceId} for {UserName}", id, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("Service Not Found.");
                return RedirectToAction(nameof(Service));
            }
        }
    }
}