using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb(" Service")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class VendorServiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VendorServiceController> logger;
        private readonly INotyfService notifyService;

        public VendorServiceController(ApplicationDbContext context,
            ILogger<VendorServiceController> logger,
            INotyfService notifyService)
        {
            _context = context;
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
                    return RedirectToAction(nameof(Index), "Dashboard");
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
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(vendorInvestigationServiceType);

            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Add", FromAction = "Details")]
        public IActionResult Create(long id)
        {
            try
            {
                var vendor = _context.Vendor.Include(v => v.Country).FirstOrDefault(v => v.VendorId == id);
                ViewData["Currency"] = Extensions.GetCultureByCountry(vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorInvestigationServiceType service, long VendorId)
        {
            if (service == null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 || ((service.SelectedDistrictIds.Count <= 0)) || VendorId < 1)
            {
                notifyService.Custom("OOPs !!!..Invalid Data.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = VendorId });
            }
            try
            {
                var isCountryValid = await _context.Country.AnyAsync(c => c.CountryId == service.SelectedCountryId);
                var isStateValid = await _context.State.AnyAsync(s => s.StateId == service.SelectedStateId);

                if (!isCountryValid || !isStateValid)
                {
                    notifyService.Error("Invalid country/state,.");
                    return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = VendorId });
                }

                var stateWideServices = _context.VendorInvestigationServiceType
                      .AsEnumerable() // Switch to client-side evaluation
                      .Where(v =>
                          v.VendorId == VendorId &&
                          v.InsuranceType == service.InsuranceType &&
                          v.InvestigationServiceTypeId == service.InvestigationServiceTypeId &&
                          v.CountryId == service.SelectedCountryId &&
                          v.StateId == service.SelectedStateId)?
                      .ToList();
                bool isAllDistricts = service.SelectedDistrictIds?.Contains(-1) == true; // how to set this value in case all districts selected

                // Handle state-wide service existence
                if (isAllDistricts)
                {
                    // Handle state-wide service creation
                    if (stateWideServices is not null && stateWideServices.Any(s => s.SelectedDistrictIds?.Contains(-1) == true))
                    {
                        var currentService = stateWideServices.FirstOrDefault(s => s.SelectedDistrictIds?.Contains(-1) == true);
                        currentService.IsUpdated = true;
                        _context.VendorInvestigationServiceType.Update(currentService);
                        await _context.SaveChangesAsync();
                        notifyService.Custom($"Service [{ALL_DISTRICT}] already exists for the State!", 3, "orange", "fas fa-truck");
                        return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = VendorId });
                    }
                }
                else
                {
                    // Handle state-wide service creation
                    if (stateWideServices != null && stateWideServices.Any(s => s.SelectedDistrictIds != null && s.SelectedDistrictIds.Intersect(service.SelectedDistrictIds ?? new List<long>()).Any()))
                    {
                        var currentService = stateWideServices.FirstOrDefault(s => s.SelectedDistrictIds != null && s.SelectedDistrictIds.Intersect(service.SelectedDistrictIds ?? new List<long>()).Any());
                        currentService.IsUpdated = true;
                        _context.VendorInvestigationServiceType.Update(currentService);
                        await _context.SaveChangesAsync();
                        notifyService.Custom($"Service already exists for the District!", 3, "orange", "fas fa-truck");
                        return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = VendorId });
                    }
                }

                service.Updated = DateTime.Now;
                service.UpdatedBy = HttpContext.User?.Identity?.Name;
                service.Created = DateTime.Now;
                service.VendorId = VendorId;
                service.CountryId = service.SelectedCountryId;
                service.StateId = service.SelectedStateId;

                _context.Add(service);
                await _context.SaveChangesAsync();
                if (isAllDistricts)
                {
                    notifyService.Custom($"Service [{ALL_DISTRICT}] added successfully.", 3, "orange", "fas fa-truck");
                }
                else
                {
                    notifyService.Custom("Service created successfully.", 3, "green", "fas fa-truck");
                }

                return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = service.VendorId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = service.VendorId });
            }
        }

        [Breadcrumb(" Edit", FromAction = "Details")]
        public IActionResult Edit(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("OOPs !!!..Agency Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long VendorInvestigationServiceTypeId, VendorInvestigationServiceType service, long VendorId)
        {
            if (VendorInvestigationServiceTypeId != service.VendorInvestigationServiceTypeId || service is null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 ||
                (service.SelectedDistrictIds.Count <= 0) || VendorId < 1)
            {
                notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(Edit), "VendorService", new { id = VendorInvestigationServiceTypeId });
            }
            try
            {
                var existingVendorServices = _context.VendorInvestigationServiceType
                         .AsNoTracking() // Switch to client-side evaluation
                        .Where(v =>
                            v.VendorId == VendorId &&
                            v.InsuranceType == service.InsuranceType &&
                            v.InvestigationServiceTypeId == service.InvestigationServiceTypeId &&
                            v.CountryId == service.SelectedCountryId &&
                            v.StateId == service.SelectedStateId &&
                            v.VendorInvestigationServiceTypeId != service.VendorInvestigationServiceTypeId)?
                        .ToList();
                bool isAllDistricts = service.SelectedDistrictIds?.Contains(-1) == true; // how to set this value in case all districts selected

                if (isAllDistricts)
                {
                    // Handle state-wide service creation
                    if (existingVendorServices is not null && existingVendorServices.Any(s => s.SelectedDistrictIds?.Contains(-1) == true))
                    {
                        var currentService = existingVendorServices.FirstOrDefault(s => s.SelectedDistrictIds?.Contains(-1) == true);
                        currentService.IsUpdated = true;
                        _context.VendorInvestigationServiceType.Update(currentService);
                        await _context.SaveChangesAsync();
                        notifyService.Custom($"Service [{ALL_DISTRICT}] already exists for the State!", 3, "orange", "fas fa-truck");
                        return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = service.VendorId });
                    }
                }
                else
                {
                    if (existingVendorServices is not null && existingVendorServices.Any(s => s.SelectedDistrictIds != null && s.SelectedDistrictIds.Intersect(service.SelectedDistrictIds ?? new List<long>()).Any()))
                    {
                        var currentService = existingVendorServices.FirstOrDefault(s => s.SelectedDistrictIds != null && s.SelectedDistrictIds.Intersect(service.SelectedDistrictIds ?? new List<long>()).Any());
                        currentService.IsUpdated = true;
                        _context.VendorInvestigationServiceType.Update(currentService);
                        await _context.SaveChangesAsync();
                        notifyService.Custom($"Service already exists for the District!", 3, "orange", "fas fa-truck");
                        return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = service.VendorId });
                    }
                }

                service.CountryId = service.SelectedCountryId;
                service.StateId = service.SelectedStateId;
                service.Updated = DateTime.Now;
                service.UpdatedBy = HttpContext.User?.Identity?.Name;
                service.IsUpdated = true;
                _context.Update(service);
                await _context.SaveChangesAsync();
                _context.Update(service);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Service updated successfully.", 3, "orange", "fas fa-truck");
                return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = service.VendorId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Error to create Service");
                return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = service.VendorId });
            }
        }

        [Breadcrumb(" Delete", FromAction = "Details")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                if (id < 1)
                {
                    notifyService.Error("OOPs !!!..Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

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
                    return RedirectToAction(nameof(Index), "Dashboard");
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
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
                    return RedirectToAction(nameof(Index), "Dashboard");
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}