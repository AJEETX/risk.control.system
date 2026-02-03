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
    [Authorize(Roles = $"{MANAGER.DISPLAY_NAME}")]
    public class EmpanelledAgencyServiceController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly IAgencyServiceTypeManager vendorServiceTypeManager;
        private readonly ILogger<EmpanelledAgencyServiceController> logger;

        public EmpanelledAgencyServiceController(ApplicationDbContext context,
            IAgencyServiceTypeManager vendorServiceTypeManager,
            INotyfService notifyService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<EmpanelledAgencyServiceController> logger)
        {
            this._context = context;
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

        [Breadcrumb("Manage Service", FromAction = nameof(EmpanelledAgencyController.Detail), FromController = typeof(EmpanelledAgencyController))]
        public IActionResult Service(long id)
        {
            if (id <= 0)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }

            var model = new ServiceModel { Id = id };

            var claimsPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Manage Agency(s)");
            var agencyPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Empanelled Agencies") { Parent = claimsPage, };
            var detailsPage = new MvcBreadcrumbNode("Detail", "EmpanelledAgency", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Service", "EmpanelledAgencyService", $"Manage Service") { Parent = detailsPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = editPage;

            return View(model);
        }

        [Breadcrumb(" Add Service", FromAction = "Service")]
        public async Task<IActionResult> Create(long id)
        {
            try
            {
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                var model = new VendorInvestigationServiceType { Country = vendor.Country, CountryId = vendor.CountryId, Vendor = vendor };
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Empanelled Agencies") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("AgencyDetail", "EmpanelledAgency", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("Service", "EmpanelledAgencyService", $"Manage Service") { Parent = detailsPage, RouteValues = new { id = id } };
                var addPage = new MvcBreadcrumbNode("Create", "EmpanelledAgencyService", $"Add Service") { Parent = editPage };
                ViewData["BreadcrumbNode"] = addPage;

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
                    notifyService.Custom(result.Message, 3, "orange", "fas fa-truck");
                }
                else
                {
                    notifyService.Custom(result.Message, 3, result.IsAllDistricts ? "orange" : "green", "fas fa-truck");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred creating Service for {AgencyId} . {UserEmail}", VendorId, HttpContext.User.Identity.Name);
                notifyService.Error("Error creating agency service. Try again.");
            }
            return RedirectToAction(nameof(Service), "EmpanelledAgencyService", new { id = service.VendorId });
        }

        [Breadcrumb(" Edit Service", FromAction = "Service")]
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
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await _context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                var vendorInvestigationServiceType = _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.Country)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.Vendor)
                    .First(v => v.VendorInvestigationServiceTypeId == id);

                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.InsuranceType == vendorInvestigationServiceType.InsuranceType), "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);

                var claimsPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Empanelled Agencies") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("AgencyDetail", "EmpanelledAgency", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var editPage = new MvcBreadcrumbNode("Service", "EmpanelledAgencyService", $"Manage Service") { Parent = detailsPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var addPage = new MvcBreadcrumbNode("Edit", "EmpanelledAgencyService", $"Edit Service") { Parent = editPage };
                ViewData["BreadcrumbNode"] = addPage;
                return View(vendorInvestigationServiceType);
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
        public async Task<IActionResult> Edit(long VendorInvestigationServiceTypeId, VendorInvestigationServiceType service, long VendorId)
        {
            try
            {
                var email = HttpContext.User?.Identity?.Name;

                var result = await vendorServiceTypeManager.EditAsync(VendorInvestigationServiceTypeId, service, email);
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
                logger.LogError(ex, "Error occurred editing Service for {ServiceId} . {UserEmail}", VendorInvestigationServiceTypeId, HttpContext.User.Identity.Name);
                notifyService.Error("Error editing agency service. Try again.");
            }
            return RedirectToAction(nameof(Service), "EmpanelledAgencyService", new { id = service.VendorId });
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