using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers
{
    public class VendorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly IToastNotification toastNotification;
        private readonly IWebHostEnvironment webHostEnvironment;

        public VendorsController(ApplicationDbContext context,
            INotyfService notifyService,
            IToastNotification toastNotification, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.notifyService = notifyService;
            this.toastNotification = toastNotification;
            this.webHostEnvironment = webHostEnvironment;
        }

        // GET: Vendors
        [Breadcrumb("Manage Agency(s)")]
        public IActionResult Index()
        {
            return RedirectToAction("Agencies");
        }

        [Breadcrumb("Agencies", FromAction = "Index")]
        public IActionResult Agencies()
        {
            return View();
        }

        public JsonResult PostRating(int rating, long mid)
        {
            //save data into the database

            var rt = new AgencyRating();
            string ip = "123";
            rt.Rate = rating;
            rt.IpAddress = ip;
            rt.VendorId = mid;

            //save into the database
            _context.Ratings.Add(rt);
            _context.SaveChanges();
            return Json("You rated this " + rating.ToString() + " star(s)");
        }

        // GET: Vendors/Details/5
        [Breadcrumb(" Manage Agency", FromAction = "Agencies")]
        public async Task<IActionResult> Details(long id)
        {
            if (id == null || _context.Vendor == null)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
            }

            var vendor = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .FirstOrDefaultAsync(m => m.VendorId == id);
            if (vendor == null)
            {
                return NotFound();
            }
            //var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies");
            //var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency") { Parent = agencysPage, RouteValues = new { id = id } };
            //var editPage = new MvcBreadcrumbNode("Edit", "Vendors", $"Edit") { Parent = agencyPage, RouteValues = new { id = id } };
            //ViewData["BreadcrumbNode"] = editPage;

            return View(vendor);
        }

        [Breadcrumb("Manage Service", FromAction = "Agencies")]
        public async Task<IActionResult> Service(string id)
        {
            if (id == null || _context.Vendor == null)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
            }
            ViewData["vendorId"] = id;
            //var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies");
            //var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency") { Parent = agencysPage, RouteValues = new { id = id } };
            //var editPage = new MvcBreadcrumbNode("Service", "Vendors", $"Services") { Parent = agencyPage, RouteValues = new { id = id } };
            //ViewData["BreadcrumbNode"] = editPage;

            return View();
        }

        // GET: Vendors/Create
        [Breadcrumb(" Add Agency")]
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View();
        }

        // POST: Vendors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vendor vendor, string domainAddress, string mailAddress)
        {
            try
            {
                if (vendor is not null)
                {
                    Domain domainData = (Domain)Enum.Parse(typeof(Domain), domainAddress, true);

                    vendor.Email = mailAddress.ToLower() + domainData.GetEnumDisplayName();

                    IFormFile? vendorDocument = Request.Form?.Files?.FirstOrDefault();
                    if (vendorDocument is not null)
                    {
                        string newFileName = vendor.Email + Guid.NewGuid().ToString();
                        string fileExtension = Path.GetExtension(vendorDocument.FileName);
                        newFileName += fileExtension;
                        string path = Path.Combine(webHostEnvironment.WebRootPath, "agency");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        var upload = Path.Combine(webHostEnvironment.WebRootPath, "agency", newFileName);
                        vendorDocument.CopyTo(new FileStream(upload, FileMode.Create));
                        vendor.DocumentUrl = "/agency/" + newFileName;

                        using var dataStream = new MemoryStream();
                        vendorDocument.CopyTo(dataStream);
                        vendor.DocumentImage = dataStream.ToArray();
                    }
                    vendor.Status = VendorStatus.ACTIVE;
                    vendor.ActivatedDate = DateTime.UtcNow;
                    vendor.DomainName = domainData;
                    vendor.Updated = DateTime.UtcNow;
                    vendor.UpdatedBy = HttpContext.User?.Identity?.Name;

                    _context.Add(vendor);
                    await _context.SaveChangesAsync();

                    var response = SmsService.SendSingleMessage(vendor.PhoneNumber, "Agency created. Domain : " + vendor.Email);

                    notifyService.Custom($"Agency created successfully.", 3, "green", "fas fa-building");
                    return RedirectToAction(nameof(Index));
                }
                notifyService.Error($"Error to create agency!.", 3);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Breadcrumb(" Edit Agency", FromAction = "Agencies")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id == null || _context.Vendor == null)
            {
                return NotFound();
            }

            var vendor = await _context.Vendor.FindAsync(id);
            if (vendor == null)
            {
                return NotFound();
            }

            var country = _context.Country.OrderBy(o => o.Name);
            var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == vendor.CountryId).OrderBy(d => d.Name);
            var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == vendor.StateId).OrderBy(d => d.Name);
            var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == vendor.DistrictId).OrderBy(d => d.Name);

            ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", vendor.CountryId);
            ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", vendor.StateId);
            ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", vendor.DistrictId);
            ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", vendor.PinCodeId);

            //var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "All Agencies");
            //var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencysPage, RouteValues = new { id = id } };
            //var editPage = new MvcBreadcrumbNode("Edit", "Vendors", $"Edit Agency") { Parent = agencyPage, RouteValues = new { id = id } };
            //ViewData["BreadcrumbNode"] = editPage;

            return View(vendor);
        }

        // POST: Vendors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long vendorId, Vendor vendor)
        {
            if (vendorId != vendor.VendorId)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
            }

            if (vendor is not null)
            {
                try
                {
                    IFormFile? vendorDocument = Request.Form?.Files?.FirstOrDefault();
                    if (vendorDocument is not null)
                    {
                        string newFileName = Guid.NewGuid().ToString();
                        string fileExtension = Path.GetExtension(vendorDocument.FileName);
                        newFileName += fileExtension;
                        string path = Path.Combine(webHostEnvironment.WebRootPath, "agency");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        var upload = Path.Combine(webHostEnvironment.WebRootPath, "agency", newFileName);

                        using var dataStream = new MemoryStream();
                        vendorDocument.CopyTo(dataStream);
                        vendor.DocumentImage = dataStream.ToArray();
                        vendorDocument.CopyTo(new FileStream(upload, FileMode.Create));
                        vendor.DocumentUrl = "/agency/" + newFileName;
                    }
                    else
                    {
                        var existingVendor = await _context.Vendor.AsNoTracking().FirstOrDefaultAsync(c => c.VendorId == vendorId);
                        if (existingVendor.DocumentImage != null || existingVendor.DocumentUrl != null)
                        {
                            vendor.DocumentImage = existingVendor.DocumentImage;
                            vendor.DocumentUrl = existingVendor.DocumentUrl;
                        }
                    }
                    vendor.Updated = DateTime.UtcNow;
                    vendor.UpdatedBy = HttpContext.User?.Identity?.Name;

                    _context.Vendor.Update(vendor);

                    var response = SmsService.SendSingleMessage(vendor.PhoneNumber, "Agency edited. Domain : " + vendor.Email);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorExists(vendor.VendorId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                notifyService.Custom($"Agency edited successfully.", 3, "orange", "fas fa-building");
                return RedirectToAction(nameof(VendorsController.Details), "Vendors", new { id = vendorId });
            }
            notifyService.Error($"Err Company delete.", 3);
            return RedirectToAction(nameof(VendorsController.Details), "Vendors", new { id = vendorId });
        }

        // GET: Vendors/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            if (id == null || _context.Vendor == null)
            {
                return NotFound();
            }

            var vendor = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .FirstOrDefaultAsync(m => m.VendorId == id);
            if (vendor == null)
            {
                return NotFound();
            }
            var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies");
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency") { Parent = agencysPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Delete", "Vendors", $"Delete Agency") { Parent = agencyPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = editPage;
            return View(vendor);
        }

        // POST: Vendors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long VendorId)
        {
            if (_context.Vendor == null)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return Problem("Entity set 'ApplicationDbContext.Vendor'  is null.");
            }
            var vendor = await _context.Vendor.FindAsync(VendorId);
            if (vendor != null)
            {
                vendor.Updated = DateTime.UtcNow;
                vendor.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Vendor.Remove(vendor);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Agency deleted successfully.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Index));
            }
            notifyService.Error($"Err Agency delete.", 3);
            return RedirectToAction(nameof(Index));
        }

        private bool VendorExists(long id)
        {
            return (_context.Vendor?.Any(e => e.VendorId == id)).GetValueOrDefault();
        }
    }
}