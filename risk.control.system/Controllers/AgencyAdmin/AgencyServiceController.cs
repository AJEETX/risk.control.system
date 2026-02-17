using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Agency;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Breadcrumb("Admin Settings ")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME}")]
    public class AgencyServiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAgencyServiceTypeManager vendorServiceTypeManager;
        private readonly INotyfService notifyService;
        private readonly ILogger<AgencyServiceController> logger;

        public AgencyServiceController(ApplicationDbContext context,
            IAgencyServiceTypeManager vendorServiceTypeManager,
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
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var vendorUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);
                if (vendorUser is null)
                {
                    notifyService.Error("User Not Found!!!..Try again");
                    return RedirectToAction(nameof(Service));
                }
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);
                if (vendor is null)
                {
                    notifyService.Error("Agency Not Found!!!..Try again");
                    return RedirectToAction(nameof(Service));
                }

                var model = new VendorInvestigationServiceType
                {
                    Country = vendor.Country,
                    CountryId = vendor.CountryId,
                    Vendor = vendor,
                    Currency = CustomExtensions.GetCultureByCountry(vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol
                };
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating service for {UserEmail}", userEmail ?? "Anonymous");
                notifyService.Error("Error creating service.Try again.");
                return RedirectToAction(nameof(Service));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorInvestigationServiceType service)
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
                logger.LogError(ex, "Error creating service for {UserEmail}", userEmail ?? "Anonymous");
                notifyService.Error("Error creating service. Try again.");
            }
            return RedirectToAction(nameof(Service));
        }

        [Breadcrumb("Edit Service", FromAction = nameof(Service))]
        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id <= 0)
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-cog");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var serviceType = _context.VendorInvestigationServiceType
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
                return View(serviceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {SserviceId} for {UserEmail}", id, userEmail ?? "Anonymous");
                notifyService.Custom($"Error editing service. Try again", 3, "red", "fas fa-cog");
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
                logger.LogError(ex, "Error editing Service for {UserEmail}", userEmail ?? "Anonymous");
                notifyService.Custom("Error editing service. Try again.", 3, "red", "fas fa-cog");
            }
            return RedirectToAction(nameof(Service));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var service = await _context.VendorInvestigationServiceType.AsNoTracking()
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