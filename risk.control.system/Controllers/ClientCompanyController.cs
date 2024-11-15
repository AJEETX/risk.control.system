using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Admin Settings ")]
    public class ClientCompanyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ISmsService smsService;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;

        public ClientCompanyController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            INotyfService notifyService,
            RoleManager<ApplicationRole> roleManager,
            ISmsService SmsService,
            UserManager<ClientCompanyApplicationUser> userManager)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            smsService = SmsService;
            this.userManager = userManager;
        }

        // GET: ClientCompanies/Create
        [Breadcrumb("Add Company")]
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View();
        }

        // POST: ClientCompanies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientCompany clientCompany, string domainAddress, string mailAddress)
        {
            if (clientCompany is not null)
            {
                Domain domainData = (Domain)Enum.Parse(typeof(Domain), domainAddress, true);

                clientCompany.Email = mailAddress.ToLower() + domainData.GetEnumDisplayName();
                IFormFile? companyDocument = Request.Form?.Files?.FirstOrDefault();
                if (companyDocument is not null)
                {
                    string newFileName = clientCompany.Email;
                    string fileExtension = Path.GetExtension(Path.GetFileName( companyDocument.FileName));
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
                }

                await smsService.DoSendSmsAsync(clientCompany.PhoneNumber, "Company account created. Domain : " + clientCompany.Email);

                clientCompany.Updated = DateTime.Now;
                clientCompany.UpdatedBy = HttpContext.User?.Identity?.Name;
                var addedCompany = _context.Add(clientCompany);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Company created successfully.", 3, "green", "fas fa-building");
                return RedirectToAction(nameof(Companies));
            }
            notifyService.Custom($"Company not found.", 3, "red", "fas fa-building");
            return RedirectToAction(nameof(Index));
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

            return View(clientCompany);
        }

        // POST: ClientCompanies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long ClientCompanyId)
        {
            if (_context.ClientCompany == null)
            {
                notifyService.Error("Company not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var clientCompany = await _context.ClientCompany.FindAsync(ClientCompanyId);
            if (clientCompany != null)
            {
                clientCompany.Updated = DateTime.Now;
                clientCompany.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ClientCompany.Remove(clientCompany);
                await _context.SaveChangesAsync();

                await smsService.DoSendSmsAsync(clientCompany.PhoneNumber, "Company account deleted. Domain : " + clientCompany.Email);

                notifyService.Custom($"Company deleted successfully.", 3, "red", "fas fa-building");
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
            if (id == 0 || _context.ClientCompany == null)
            {
                notifyService.Error("Company not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var clientCompany = await _context.ClientCompany.FindAsync(id);
            if (clientCompany == null)
            {
                notifyService.Error("Company not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var country = _context.Country;
            var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == clientCompany.CountryId).OrderBy(d => d.Name);
            var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == clientCompany.StateId).OrderBy(d => d.Name);
            var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == clientCompany.DistrictId).OrderBy(d => d.Name);

            ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", clientCompany.CountryId);
            ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", clientCompany.StateId);
            ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", clientCompany.DistrictId);
            ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", clientCompany.PinCodeId);

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
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClientCompany clientCompany)
        {
            try
            {
                if (clientCompany.ClientCompanyId < 1)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userEmail = HttpContext.User?.Identity?.Name;
                if (userEmail is null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
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


                clientCompany.Updated = DateTime.Now;
                clientCompany.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ClientCompany.Update(clientCompany);
                await _context.SaveChangesAsync();

                await smsService.DoSendSmsAsync(clientCompany.PhoneNumber, "Company account edited. Domain : " + clientCompany.Email);
            }
            catch
            {
                
            }
            notifyService.Custom($"Company edited successfully.", 3, "orange", "fas fa-building");
            return RedirectToAction(nameof(ClientCompanyController.Details), "ClientCompany", new { id = clientCompany.ClientCompanyId });
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
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
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
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
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
                    .ThenInclude(v => v.LineOfBusiness)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.InvestigationServiceType)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.PincodeServices);

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
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices);
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
                notifyService.Error("Agency not found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            ViewBag.CompanyId = companyId;
            ViewBag.Backurl = backurl;

            return View(vendor);
        }
    }
}