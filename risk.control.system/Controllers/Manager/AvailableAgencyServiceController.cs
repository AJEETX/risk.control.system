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
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Manager
{
    [Breadcrumb("Manage Agency(s)")]
    [Authorize(Roles = $"{MANAGER.DISPLAY_NAME}")]
    public class AvailableAgencyServiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAgencyServiceTypeManager vendorServiceTypeManager;
        private readonly ILogger<AvailableAgencyServiceController> logger;
        private readonly INotyfService notifyService;

        public AvailableAgencyServiceController(ApplicationDbContext context,
            IAgencyServiceTypeManager vendorServiceTypeManager,
            ILogger<AvailableAgencyServiceController> logger,
            INotyfService notifyService)
        {
            _context = context;
            this.vendorServiceTypeManager = vendorServiceTypeManager;
            this.logger = logger;
            this.notifyService = notifyService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Breadcrumb("Manage Service")]
        public IActionResult Service(long id)
        {
            if (id <= 0)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            var model = new ServiceModel { Id = id };
            var serviceMainLink = new MvcBreadcrumbNode("Agencies", "AvailableAgency", "Manager Agency(s)");
            var serviceSecondLink = new MvcBreadcrumbNode("Agencies", "AvailableAgency", "Available Agencies") { Parent = serviceMainLink };
            var serviceThirdLink = new MvcBreadcrumbNode("Details", "AvailableAgency", "Agency Profile") { Parent = serviceSecondLink, RouteValues = new { id = id } };
            var servicesPage = new MvcBreadcrumbNode("Service", "AvailableAgencyService", $"Manager Service") { Parent = serviceThirdLink, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = servicesPage;

            return View(model);
        }

        [Breadcrumb(" Add", FromAction = "Service")]
        public async Task<IActionResult> Create(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var model = new VendorInvestigationServiceType { Country = vendor.Country, CountryId = vendor.CountryId, Vendor = vendor };

                var serviceMainLink = new MvcBreadcrumbNode("Agencies", "AvailableAgency", "Manager Agency(s)");
                var serviceSecondLink = new MvcBreadcrumbNode("Agencies", "AvailableAgency", "Available Agencies") { Parent = serviceMainLink };
                var serviceThirdLink = new MvcBreadcrumbNode("Details", "AvailableAgency", "Agency Profile") { Parent = serviceSecondLink, RouteValues = new { id = id } };
                var servicesFourthLink = new MvcBreadcrumbNode("Service", "AvailableAgencyService", $"Manager Service") { Parent = serviceThirdLink, RouteValues = new { id = id } };
                var serviceAddPage = new MvcBreadcrumbNode("Create", "AvailableAgencyService", $"Add Service") { Parent = servicesFourthLink };
                ViewData["BreadcrumbNode"] = serviceAddPage;
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
            return RedirectToAction(nameof(Service), "AvailableAgencyService", new { id = service.VendorId });
        }

        [Breadcrumb(" Edit", FromAction = "Service")]
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
                var currentUser = await _context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                var serviceType = _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.Country)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.Vendor)
                    .First(v => v.VendorInvestigationServiceTypeId == id);

                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.InsuranceType == serviceType.InsuranceType), "InvestigationServiceTypeId", "Name", serviceType.InvestigationServiceTypeId);

                var serviceMainLink = new MvcBreadcrumbNode("Agencies", "AvailableAgency", "Manager Agency(s)");
                var serviceSecondLink = new MvcBreadcrumbNode("Agencies", "AvailableAgency", "Available Agencies") { Parent = serviceMainLink };
                var serviceThirdLink = new MvcBreadcrumbNode("Details", "AvailableAgency", "Agency Profile") { Parent = serviceSecondLink, RouteValues = new { id = serviceType.VendorId } };
                var servicesFourthLink = new MvcBreadcrumbNode("Service", "AvailableAgencyService", $"Manager Service") { Parent = serviceThirdLink, RouteValues = new { id = serviceType.VendorId } };
                var createPage = new MvcBreadcrumbNode("Edit", "AvailableAgencyService", $"Edit Service") { Parent = servicesFourthLink };
                ViewData["BreadcrumbNode"] = createPage;

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
        public async Task<IActionResult> Edit(long VendorInvestigationServiceTypeId, VendorInvestigationServiceType service)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var result = await vendorServiceTypeManager.EditAsync(VendorInvestigationServiceTypeId, service, userEmail);
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
                logger.LogError(ex, "Error editing service for {ServiceId} . {UserEmail}", VendorInvestigationServiceTypeId, userEmail);
                notifyService.Custom("Error editing service. Try again.", 3, "red", "fas fa-cog");
            }
            return RedirectToAction(nameof(Service), "AvailableAgencyService", new { id = service.VendorId });
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