using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Admin Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class ClientCompanyController : Controller
    {
        private const string vendorMapSize = "800x800";
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ISmsService smsService;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;

        public ClientCompanyController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            INotyfService notifyService,
            RoleManager<ApplicationRole> roleManager,
            ICustomApiCLient customApiCLient,
            ISmsService SmsService,
            UserManager<ClientCompanyApplicationUser> userManager)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            this.customApiCLient = customApiCLient;
            smsService = SmsService;
            this.userManager = userManager;
        }

        // GET: ClientCompanies/Create
        [Breadcrumb("Add Company")]
        public IActionResult Create()
        {
            var country = _context.Country.FirstOrDefault();
            var model = new ClientCompany { Country = country, SelectedCountryId = country.CountryId, CountryId = country.CountryId };
            return View(model);
        }

        // POST: ClientCompanies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientCompany clientCompany, string domainAddress, string mailAddress)
        {
            if (clientCompany is null || clientCompany.SelectedCountryId < 1 || clientCompany.SelectedStateId < 1 || clientCompany.SelectedDistrictId < 1 || clientCompany.SelectedPincodeId < 1)
            {
                notifyService.Custom($"Please check input fields.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Create));
            }
            Domain domainData = (Domain)Enum.Parse(typeof(Domain), domainAddress, true);

            clientCompany.Email = mailAddress.ToLower() + domainData.GetEnumDisplayName();
            IFormFile? companyDocument = Request.Form?.Files?.FirstOrDefault();
            if (companyDocument is not null)
            {
                string newFileName = clientCompany.Email;
                string fileExtension = Path.GetExtension(Path.GetFileName(companyDocument.FileName));
                newFileName += fileExtension;
                string path = Path.Combine(webHostEnvironment.WebRootPath, "company");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                var upload = Path.Combine(webHostEnvironment.WebRootPath, "company", newFileName);
                companyDocument.CopyTo(new FileStream(upload, FileMode.Create));
                clientCompany.DocumentUrl = "/company/" + newFileName;

                using var dataStream = new MemoryStream();
                companyDocument.CopyTo(dataStream);
                clientCompany.DocumentImage = dataStream.ToArray();
                clientCompany.DocumentImageExtension = fileExtension;
            }

            var pinCode = _context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.PinCodeId == clientCompany.SelectedPincodeId);

            var companyAddress = clientCompany.Addressline + ", " + pinCode.District.Name + ", " + pinCode.State.Name + ", " + pinCode.Country.Code;
            var companyCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
            var companyLatLong = companyCoordinates.Latitude + "," + companyCoordinates.Longitude;
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={companyLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            clientCompany.AddressLatitude = companyCoordinates.Latitude;
            clientCompany.AddressLongitude = companyCoordinates.Longitude;
            clientCompany.AddressMapLocation = url;
            var isdCode = _context.Country.FirstOrDefault(c => c.CountryId == clientCompany.SelectedCountryId)?.ISDCode;
            await smsService.DoSendSmsAsync(isdCode + clientCompany.PhoneNumber, "Company account created. Domain : " + clientCompany.Email);

            //clientCompany.Description = "New company added.";
            clientCompany.AgreementDate = DateTime.Now;
            clientCompany.Status = CompanyStatus.ACTIVE;
            clientCompany.PinCodeId = clientCompany.SelectedPincodeId;
            clientCompany.DistrictId = clientCompany.SelectedDistrictId;
            clientCompany.StateId = clientCompany.SelectedStateId;
            clientCompany.CountryId = clientCompany.SelectedCountryId;

            clientCompany.Updated = DateTime.Now;
            clientCompany.UpdatedBy = HttpContext.User?.Identity?.Name;
            var addedCompany = _context.Add(clientCompany);
            await _context.SaveChangesAsync();
            notifyService.Custom($"Company created successfully.", 3, "green", "fas fa-building");
            return RedirectToAction(nameof(Companies));
        }

        // GET: ClientCompanies/Delete/5
        [Breadcrumb("Delete ", FromAction = "Companies")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id < 1 || _context.ClientCompany == null)
            {
                notifyService.Error("Company not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var clientCompany = await _context.ClientCompany
                .Include(c => c.Country)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.ClientCompanyId == id);
            if (clientCompany == null)
            {
                notifyService.Error("Company not found!");
                return RedirectToAction(nameof(Index), "Dashboard");

            }
            var hasClaims = _context.Investigations.Any(c => c.ClientCompanyId == id && !c.Deleted);
            clientCompany.HasClaims = hasClaims;
            return View(clientCompany);
        }

        // POST: ClientCompanies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long ClientCompanyId)
        {
            if (ClientCompanyId <= 0)
            {
                notifyService.Error("Company not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var clientCompany = await _context.ClientCompany.Include(c => c.Country).FirstOrDefaultAsync(c => c.ClientCompanyId == ClientCompanyId);
            if (clientCompany != null)
            {
                clientCompany.Updated = DateTime.Now;
                clientCompany.UpdatedBy = HttpContext.User?.Identity?.Name;
                clientCompany.Deleted = true;
                _context.ClientCompany.Update(clientCompany);

                var companyUsers = await _context.ClientCompanyApplicationUser.Where(c => c.ClientCompanyId == ClientCompanyId).ToListAsync();
                foreach (var companyUser in companyUsers)
                {
                    companyUser.Deleted = true;
                    companyUser.Updated = DateTime.Now;
                    companyUser.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.ClientCompanyApplicationUser.Update(companyUser);
                }
                await _context.SaveChangesAsync();

                await smsService.DoSendSmsAsync(clientCompany.Country.ISDCode + clientCompany.PhoneNumber, "Company account deleted. Domain : " + clientCompany.Email);

                notifyService.Custom($"Company {clientCompany.Email} deleted successfully.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Index));
            }

            notifyService.Error("Company not found!");
            return RedirectToAction(nameof(Index), "Dashboard");
        }

        // GET: ClientCompanies/Details/5
        [Breadcrumb("Company Profile", FromAction = "Companies")]
        public async Task<IActionResult> Details(long id)
        {
            if (id < 1 || _context.ClientCompany == null)
            {
                notifyService.Error("Company not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var clientCompany = await _context.ClientCompany
                .Include(c => c.Country)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .Include(c => c.District)
                .FirstOrDefaultAsync(m => m.ClientCompanyId == id);
            if (clientCompany == null)
            {
                notifyService.Error("Company not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            return View(clientCompany);
        }

        // GET: ClientCompanies/Edit/5
        [Breadcrumb(title: "Edit Company", FromAction = "Details")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id <= 0)
            {
                notifyService.Error("Company not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var clientCompany = await _context.ClientCompany.Include(c => c.Country).FirstOrDefaultAsync(c => c.ClientCompanyId == id);
            if (clientCompany == null)
            {
                notifyService.Error("Company not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var agencysPage = new MvcBreadcrumbNode("Companies", "ClientCompany", "Admin Settings");
            var agency2Page = new MvcBreadcrumbNode("Companies", "ClientCompany", "Companies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "ClientCompany", "Company Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Edit", "ClientCompany", $"Edit Company") { Parent = agencyPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View(clientCompany);
        }

        // POST: ClientCompanies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClientCompany clientCompany)
        {
            if (clientCompany is null || clientCompany.SelectedCountryId < 1 || clientCompany.SelectedStateId < 1 || clientCompany.SelectedDistrictId < 1 || clientCompany.SelectedPincodeId < 1)
            {
                notifyService.Custom($"Please check input fields.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Edit), "ClientCompany", new { id = clientCompany.ClientCompanyId });
            }
            try
            {
                IFormFile? companyDocument = Request.Form?.Files?.FirstOrDefault();
                if (companyDocument is not null)
                {
                    string newFileName = clientCompany.Email + Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(companyDocument.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "company");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "company", newFileName);

                    using var dataStream = new MemoryStream();
                    companyDocument.CopyTo(dataStream);
                    clientCompany.DocumentImage = dataStream.ToArray();
                    companyDocument.CopyTo(new FileStream(upload, FileMode.Create));
                    clientCompany.DocumentUrl = "/company/" + newFileName;
                    clientCompany.DocumentImageExtension = fileExtension;
                }
                else
                {
                    var existingClientCompany = await _context.ClientCompany.AsNoTracking().FirstOrDefaultAsync(c => c.ClientCompanyId == clientCompany.ClientCompanyId);
                    if (existingClientCompany.DocumentUrl != null || existingClientCompany.DocumentUrl != null)
                    {
                        clientCompany.DocumentImage = existingClientCompany.DocumentImage;
                        clientCompany.DocumentUrl = existingClientCompany.DocumentUrl;
                    }
                }
                var pinCode = _context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.PinCodeId == clientCompany.SelectedPincodeId);

                var companyAddress = clientCompany.Addressline + ", " + pinCode.District.Name + ", " + pinCode.State.Name + ", " + pinCode.Country.Code;
                var companyCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
                var companyLatLong = companyCoordinates.Latitude + "," + companyCoordinates.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={companyLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                clientCompany.AddressLatitude = companyCoordinates.Latitude;
                clientCompany.AddressLongitude = companyCoordinates.Longitude;
                clientCompany.AddressMapLocation = url;

                clientCompany.PinCodeId = clientCompany.SelectedPincodeId;
                clientCompany.DistrictId = clientCompany.SelectedDistrictId;
                clientCompany.StateId = clientCompany.SelectedStateId;
                clientCompany.CountryId = clientCompany.SelectedCountryId;

                clientCompany.Updated = DateTime.Now;
                clientCompany.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ClientCompany.Update(clientCompany);
                await _context.SaveChangesAsync();

                await smsService.DoSendSmsAsync(pinCode.Country.ISDCode + clientCompany.PhoneNumber, "Company account edited. Domain : " + clientCompany.Email);
                notifyService.Custom($"Company edited successfully.", 3, "orange", "fas fa-building");
                return RedirectToAction(nameof(ClientCompanyController.Details), "ClientCompany", new { id = clientCompany.ClientCompanyId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Custom($"Error editing company.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Edit), "ClientCompany", new { id = clientCompany.ClientCompanyId });
            }

        }

        // GET: ClientCompanies
        public IActionResult Index()
        {
            return RedirectToAction("Companies");
        }

        [Breadcrumb("Companies")]
        public async Task<IActionResult> Companies()
        {
            var applicationDbContext = _context.ClientCompany
                .Include(c => c.Country)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .Include(c => c.State)
                .AsQueryable();

            var applicationDbContextResult = await applicationDbContext.ToListAsync();

            return View(applicationDbContextResult);
        }

        [Breadcrumb("Empanelled Agency(s)")]
        [HttpGet]
        public async Task<IActionResult> EmpanelledVendors(long id)
        {
            var applicationDbContext = _context.Vendor
                .Where(v => v.Clients.Any(c => c.ClientCompanyId == id))
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .AsQueryable();

            var applicationDbContextResult = await applicationDbContext.ToListAsync();
            ViewBag.CompanyId = id;

            return View(applicationDbContextResult);
        }

        [Breadcrumb("Available Agency(s)")]
        [HttpGet]
        public async Task<IActionResult> AvailableVendors(long id)
        {
            var applicationDbContext = _context.Vendor
                .Where(v => !v.Clients.Any(c => c.ClientCompanyId == id)
                //&& userVendorids.Contains(v.VendorId)
                && (v.VendorInvestigationServiceTypes != null) && v.VendorInvestigationServiceTypes.Count > 0)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .AsQueryable();

            var applicationDbContextResult = await applicationDbContext.ToListAsync();
            ViewBag.CompanyId = id;
            return View(applicationDbContextResult);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AvailableVendors(long id, List<string> vendors)
        {
            if (vendors is not null && vendors.Count > 0)
            {
                var company = await _context.ClientCompany.FindAsync(id);
                if (company != null)
                {
                    var empanelledVendors = _context.Vendor.Where(v => vendors.Contains(v.VendorId.ToString()))
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.InvestigationServiceType)
                    .Include(v => v.VendorInvestigationServiceTypes);

                    company.EmpanelledVendors.AddRange(empanelledVendors);

                    foreach (var empanelledVendor in empanelledVendors)
                    {
                        empanelledVendor.Clients.Add(company);
                        _context.Vendor.Update(empanelledVendor);
                    }
                    company.Updated = DateTime.Now;
                    company.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.ClientCompany.Update(company);
                    var savedRows = await _context.SaveChangesAsync();
                    notifyService.Custom($"Agency(s) empanelled.", 3, "green", "fas fa-thumbs-up");
                    return RedirectToAction("Details", new { id = company.ClientCompanyId });
                }
            }
            return Problem();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpanelledVendors(long id, List<string> vendors)
        {
            var company = await _context.ClientCompany.FindAsync(id);

            var empanelledVendors = _context.Vendor.Where(v => vendors.Contains(v.VendorId.ToString()))
                .Where(v => v.Clients.Any(c => c.ClientCompanyId == id))
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes);
            foreach (var v in empanelledVendors)
            {
                company.EmpanelledVendors.Remove(v);
                v.Clients.Remove(company);
                _context.Vendor.Update(v);
            }
            _context.ClientCompany.Update(company);
            company.Updated = DateTime.Now;
            company.UpdatedBy = HttpContext.User?.Identity?.Name;
            var savedRows = await _context.SaveChangesAsync();
            notifyService.Custom($"Agency(s) de-panelled.", 3, "red", "far fa-thumbs-down");
            return RedirectToAction("Details", new { id = company.ClientCompanyId });
        }

        [Breadcrumb("Agency Detail")]
        public async Task<IActionResult> VendorDetail(string companyId, long id, string backurl)
        {
            if (id < 1 || _context.Vendor == null)
            {
                notifyService.Error("Agency not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var vendor = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .FirstOrDefaultAsync(m => m.VendorId == id);
            if (vendor == null)
            {
                notifyService.Error("Agency not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            ViewBag.CompanyId = companyId;
            ViewBag.Backurl = backurl;

            return View(vendor);
        }
    }
}