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
using SmartBreadcrumbs.Nodes;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb(" Service")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class VendorServiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVendorServiceTypeManager vendorServiceTypeManager;
        private readonly ILogger<VendorServiceController> logger;
        private readonly INotyfService notifyService;

        public VendorServiceController(ApplicationDbContext context,
            IVendorServiceTypeManager vendorServiceTypeManager,
            ILogger<VendorServiceController> logger,
            INotyfService notifyService)
        {
            _context = context;
            this.vendorServiceTypeManager = vendorServiceTypeManager;
            this.logger = logger;
            this.notifyService = notifyService;
        }

        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.VendorInvestigationServiceType
                .Include(v => v.InvestigationServiceType)
                .Include(v => v.State)
                .Include(v => v.Vendor);
            return View(await applicationDbContext.ToListAsync());
        }

        [Breadcrumb(" Manage Service", FromAction = "Details", FromController = typeof(VendorsController))]
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                if (id < 1)
                {
                    notifyService.Error("OOPs !!!..Not Found");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.District)
                    .Include(v => v.Country)
                    .Include(v => v.State)
                    .Include(v => v.Vendor)
                    .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
                if (vendorInvestigationServiceType == null)
                {
                    notifyService.Error("OOPs !!!..Service Not Found");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                return View(vendorInvestigationServiceType);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting agency {ServiceId} by {UserName}", id, User.Identity.Name);
                notifyService.Error("Error getting agency service. Try again.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(" Add", FromAction = "Details")]
        public async Task<IActionResult> Create(long id)
        {
            try
            {
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var model = new VendorInvestigationServiceType { Country = vendor.Country, CountryId = vendor.CountryId, Vendor = vendor };

                var agencysPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Available Agencies") { Parent = agencysPage };
                var agencyDetailPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manage Service") { Parent = agencyDetailPage, RouteValues = new { id = id } };
                var createPage = new MvcBreadcrumbNode("Create", "VendorService", $"Add Service") { Parent = editPage };
                ViewData["BreadcrumbNode"] = createPage;
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {AgencyId} by {UserName}", id, User.Identity.Name);
                notifyService.Error("Error getting agency service. Try again.");
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
                logger.LogError(ex, "Error creating service for {AgencyId} by {UserName}", VendorId, User.Identity.Name);
                notifyService.Error("Error creating service. Try again.");
            }
            return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = service.VendorId });
        }

        [Breadcrumb(" Edit", FromAction = "Details")]
        public async Task<IActionResult> Edit(long id)
        {
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

                var agencysPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Available Agencies") { Parent = agencysPage };
                var agencyDetailPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agencyPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var editPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manage Service") { Parent = agencyDetailPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var createPage = new MvcBreadcrumbNode("Edit", "VendorService", $"Edit Service") { Parent = editPage };
                ViewData["BreadcrumbNode"] = createPage;

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting service for {ServiceId} by {UserName}", id, User.Identity.Name);
                notifyService.Error("Error getting agency service. Try again.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long VendorInvestigationServiceTypeId, VendorInvestigationServiceType service)
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
                logger.LogError(ex, "Error editing service for {ServiceId} by {UserName}", VendorInvestigationServiceTypeId, User.Identity.Name);
                notifyService.Custom("Error editing service. Try again.", 3, "red", "fas fa-truck");
            }
            return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = service.VendorId });
        }

        [Breadcrumb(" Delete", FromAction = "Details")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                if (id < 1)
                {
                    notifyService.Error("OOPs !!!..Id Not Found");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await _context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.Country)
                    .Include(v => v.Vendor)
                    .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
                if (vendorInvestigationServiceType == null)
                {
                    notifyService.Error("OOPs !!!..Service Not Found");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var agencysPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Available Agencies") { Parent = agencysPage };
                var agencyDetailPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agencyPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var editPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manage Service") { Parent = agencyDetailPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var createPage = new MvcBreadcrumbNode("Delete", "VendorService", $"Delete Service") { Parent = editPage };
                ViewData["BreadcrumbNode"] = createPage;

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting service for {ServiceId} by {UserName}", id, User.Identity.Name);
                notifyService.Error("Error getting agency service. Try again.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                if (id == 0 || _context.VendorInvestigationServiceType == null)
                {
                    notifyService.Error("OOPs !!!..Id Not Found");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType.FindAsync(id);
                if (vendorInvestigationServiceType != null)
                {
                    vendorInvestigationServiceType.Updated = DateTime.Now;
                    vendorInvestigationServiceType.UpdatedBy = currentUserEmail;
                    _context.VendorInvestigationServiceType.Remove(vendorInvestigationServiceType);
                    await _context.SaveChangesAsync();
                    notifyService.Custom($"Service deleted successfully.", 3, "red", "fas fa-truck");
                    return RedirectToAction("Service", "Vendors", new { id = vendorInvestigationServiceType.VendorId });
                }
                notifyService.Error($"Err Service delete.", 3);
                return RedirectToAction("Details", "Vendors", new { id = vendorInvestigationServiceType.VendorId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting service for {ServiceId} by {UserName}", id, User.Identity.Name);
                notifyService.Error("Error deleting agency service. Try again.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}