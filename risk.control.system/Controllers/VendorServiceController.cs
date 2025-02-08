using AspNetCoreHero.ToastNotification.Abstractions;

using Google.Api;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb(" Service")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class VendorServiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly IToastNotification toastNotification;

        public VendorServiceController(ApplicationDbContext context,
            INotyfService notifyService,
            IToastNotification toastNotification)
        {
            _context = context;
            this.notifyService = notifyService;
            this.toastNotification = toastNotification;
        }

        // GET: VendorService
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.VendorInvestigationServiceType
                .Include(v => v.InvestigationServiceType)
                .Include(v => v.LineOfBusiness)
                .Include(v => v.PincodeServices)
                .Include(v => v.State)
                .Include(v => v.Vendor);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: VendorService/Details/5
        [Breadcrumb(" Manage Service", FromAction = "Details", FromController = typeof(VendorsController))]
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                if (id < 1)
                {
                    return NotFound();
                }

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.LineOfBusiness)
                    .Include(v => v.PincodeServices)
                    .Include(v => v.District)
                    .Include(v => v.Country)
                    .Include(v => v.State)
                    .Include(v => v.Vendor)
                    .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
                if (vendorInvestigationServiceType == null)
                {
                    return NotFound();
                }

                return View(vendorInvestigationServiceType);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // GET: VendorService/Create
        [Breadcrumb(" Add", FromAction = "Details")]
        public IActionResult Create(long id)
        {
            try
            {
                var vendor = _context.Vendor.Include(v=>v.Country).FirstOrDefault(v => v.VendorId == id);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
                //ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
                var model = new VendorInvestigationServiceType { Country = vendor.Country, SelectedMultiPincodeId = new List<long>(), CountryId = vendor.CountryId, Vendor = vendor, PincodeServices = new List<ServicedPinCode>() };

                var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Agencies") { Parent = agencysPage };
                var agencyDetailPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manage Service") { Parent = agencyDetailPage, RouteValues = new { id = id } };
                var createPage = new MvcBreadcrumbNode("Create", "VendorService", $"Add Service") { Parent = editPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = createPage;
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // POST: VendorService/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorInvestigationServiceType service, long VendorId)
        {
            if (service == null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 || (service.SelectedDistrictId < 1 && service.SelectedDistrictId != -1) || VendorId < 1)
            {
                notifyService.Custom("OOPs !!!..Invalid Data.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = VendorId });
            }
            try
            {
                bool removedExistingService = false;
                int removedExistingServiceCount = 0;
                var isCountryValid = await _context.Country.AnyAsync(c => c.CountryId == service.SelectedCountryId);
                var isStateValid = await _context.State.AnyAsync(s => s.StateId == service.SelectedStateId);
                var isDistrictValid = service.SelectedDistrictId == -1 ||
                                      await _context.District.AnyAsync(d => d.DistrictId == service.SelectedDistrictId);

                if (!isCountryValid || !isStateValid || !isDistrictValid)
                {
                    notifyService.Error("Invalid country, state, or district selected.");
                    return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = VendorId });
                }

                var stateWideService = _context.VendorInvestigationServiceType
                        .AsEnumerable() // Switch to client-side evaluation
                        .Where(v =>
                            v.VendorId == VendorId &&
                            v.LineOfBusinessId == service.LineOfBusinessId &&
                            v.InvestigationServiceTypeId == service.InvestigationServiceTypeId &&
                            v.CountryId == (long?)service.SelectedCountryId &&
                            v.StateId == (long?)service.SelectedStateId &&
                            v.DistrictId == null)
                        .ToList();

                List<PinCode> pinCodes = new List<PinCode>();

                // Handle state-wide service existence
                if (service.SelectedDistrictId == -1)
                {
                    // Handle state-wide service creation
                    if (stateWideService is null || !stateWideService.Any())
                    {
                        pinCodes = new List<PinCode> { new PinCode { Name = ALL_PINCODE, Code = ALL_PINCODE } };
                    }
                    else
                    {
                        stateWideService.FirstOrDefault().IsUpdated = true;
                        _context.VendorInvestigationServiceType.Update(stateWideService.FirstOrDefault());
                        await _context.SaveChangesAsync();
                        notifyService.Custom($"Service [{ALL_DISTRICT}] already exists for the State!", 3, "orange", "fas fa-truck");
                        return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = VendorId });
                    }
                }

                // Handle district-specific services
                else
                {
                    pinCodes = await _context.PinCode.Where(p => service.SelectedMultiPincodeId.Contains(p.PinCodeId)).ToListAsync();
                }

                var servicePinCodes = pinCodes.Select(p =>
                    new ServicedPinCode
                    {
                        Name = p.Name,
                        Pincode = p.Code,
                        VendorInvestigationServiceTypeId = service.VendorInvestigationServiceTypeId
                    }).ToList();

                service.PincodeServices = servicePinCodes;
                service.VendorId = VendorId;
                service.CountryId = service.SelectedCountryId;
                service.StateId = service.SelectedStateId;

                if (service.SelectedDistrictId == -1)
                {
                    service.DistrictId = null;
                }
                else
                {
                    service.DistrictId = service.SelectedDistrictId;
                }

                service.Updated = DateTime.Now;
                service.UpdatedBy = HttpContext.User?.Identity?.Name;
                service.Created = DateTime.Now;

                _context.Add(service);
                await _context.SaveChangesAsync();
                if (removedExistingService)
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
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorsController.Service), "Vendors",new { id = service.VendorId });
            }
        }

        // GET: VendorService/Edit/5
        [Breadcrumb(" Edit", FromAction = "Details")]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("OOPs !!!..Agency Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType.FindAsync(id);
                if (vendorInvestigationServiceType == null)
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var services = _context.VendorInvestigationServiceType
                    .Include(v => v.Country)
                    .Include(v => v.Vendor)
                    .Include(v => v.PincodeServices)
                    .First(v => v.VendorInvestigationServiceTypeId == id);

                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
                //ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType
                //    .Include(i => i.LineOfBusiness)
                //    .Where(i => i.LineOfBusiness.LineOfBusinessId == vendorInvestigationServiceType.LineOfBusinessId),
                //    "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);

                if (vendorInvestigationServiceType.DistrictId == null)
                {
                    var pinCodes = new List<PinCode> { new PinCode { Name = ALL_PINCODE, Code = ALL_PINCODE_CODE } };
                    services.PincodeServices = pinCodes.Select(p =>
                        new ServicedPinCode
                        {
                            Name = p.Name,
                            Pincode = p.Code,
                            VendorInvestigationServiceTypeId = vendorInvestigationServiceType.VendorInvestigationServiceTypeId
                        }).ToList();
                    ViewBag.PinCodeId = pinCodes
                    .Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.Code
                    }).ToList();
                }
                else
                {
                    ViewBag.PinCodeId = _context.PinCode.Include(c => c.District).Where(p => p.District.DistrictId == vendorInvestigationServiceType.DistrictId)
                    .Select(x => new SelectListItem
                    {
                        Text = x.Name + " - " + x.Code,
                        Value = x.PinCodeId.ToString()
                    }).ToList();
                }


                var selectedPincodeWithArea = services.PincodeServices;
                var vendorServiceTypes = new List<long>();

                foreach (var service in selectedPincodeWithArea)
                {
                    var pincodeServices = _context.PinCode.Where(p => p.Code == service.Pincode && p.Name == service.Name).Select(p => p.PinCodeId)?.ToList();
                    vendorServiceTypes.AddRange(pincodeServices);
                }

                services.SelectedMultiPincodeId = vendorServiceTypes;
                if (services.DistrictId == null)
                {
                    services.SelectedDistrictId = -1;
                }

                var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Agencies") { Parent = agencysPage };
                var agencyDetailPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencyPage, RouteValues = new { id = services.VendorId } };
                var editPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manage Service") { Parent = agencyDetailPage, RouteValues = new { id = services.VendorId } };
                var createPage = new MvcBreadcrumbNode("Edit", "VendorService", $"Edit Service") { Parent = editPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = createPage;

                return View(services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // POST: VendorService/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long VendorInvestigationServiceTypeId, VendorInvestigationServiceType service, long VendorId)
        {
            if (VendorInvestigationServiceTypeId != service.VendorInvestigationServiceTypeId || service.SelectedMultiPincodeId.Count <= 0 || service is null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 ||
                (service.SelectedDistrictId != -1 && service.SelectedDistrictId < 1) || VendorId < 1)
            {
                notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(Edit), "VendorService", new { id = VendorInvestigationServiceTypeId });
            }
            try
            {
                var stateWideService = await _context.VendorInvestigationServiceType.AsNoTracking().
                        FirstOrDefaultAsync(v =>
                            v.VendorId == VendorId &&
                            v.LineOfBusinessId == service.LineOfBusinessId &&
                            v.InvestigationServiceTypeId == service.InvestigationServiceTypeId &&
                            v.CountryId == (long?)service.SelectedCountryId &&
                            v.StateId == (long?)service.SelectedStateId &&
                            v.DistrictId == null);
                List<PinCode> pinCodes = new List<PinCode>();
                // Remove all state-level services
                if (service.SelectedDistrictId == -1)
                {
                    if (stateWideService is null)
                    {
                        pinCodes = new List<PinCode>
                        {
                            new PinCode { Name = ALL_PINCODE, Code = ALL_PINCODE }
                        };
                    }
                }
                else
                {
                    var agencyServicedPincodes = _context.ServicedPinCode.Where(s =>
                        s.VendorInvestigationServiceTypeId == service.VendorInvestigationServiceTypeId);
                    if (agencyServicedPincodes is not null)
                    {
                        _context.ServicedPinCode.RemoveRange(agencyServicedPincodes);
                    }

                    // Retrieve the selected pin codes
                    pinCodes = await _context.PinCode
                        .Where(pinCode => service.SelectedMultiPincodeId.Distinct().Contains(pinCode.PinCodeId))
                        .ToListAsync();
                }

                service.PincodeServices = pinCodes.Select(p =>
                new ServicedPinCode
                {
                    Name = p.Name,
                    Pincode = p.Code,
                    VendorInvestigationServiceTypeId = VendorInvestigationServiceTypeId
                })?.ToList();

                service.CountryId = service.SelectedCountryId;
                service.StateId = service.SelectedStateId;
                if (service.SelectedDistrictId == -1)
                {
                    service.DistrictId = null;
                }
                else
                {
                    service.DistrictId = service.SelectedDistrictId;
                }

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
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Error to create Service");
                return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = service.VendorId });
            }
        }

        // GET: VendorService/Delete/5
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
                
                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.LineOfBusiness)
                    .Include(v => v.PincodeServices)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.Country)
                    .Include(v => v.Vendor)
                    .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
                if (vendorInvestigationServiceType == null)
                {
                    return NotFound();
                }
                var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Agencies") { Parent = agencysPage };
                var agencyDetailPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencyPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var editPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manage Service") { Parent = agencyDetailPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var createPage = new MvcBreadcrumbNode("Delete", "VendorService", $"Delete Service") { Parent = editPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                ViewData["BreadcrumbNode"] = createPage;

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // POST: VendorService/Delete/5
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
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}