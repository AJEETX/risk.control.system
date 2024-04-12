using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    public class CompanyController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly IHttpClientService httpClientService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IToastNotification toastNotification;

        public CompanyController(ApplicationDbContext context,
            UserManager<ClientCompanyApplicationUser> userManager,
            IHttpClientService httpClientService,
            SignInManager<ApplicationUser> signInManager,
            INotyfService notifyService,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            IToastNotification toastNotification)
        {
            this._context = context;
            this.signInManager = signInManager;
            this.notifyService = notifyService;
            this.userManager = userManager;
            this.httpClientService = httpClientService;
            this.roleManager = roleManager;
            this.webHostEnvironment = webHostEnvironment;
            this.toastNotification = toastNotification;
            UserList = new List<UsersViewModel>();
        }

        [Breadcrumb("Manage Company")]
        public async Task<IActionResult> Index()
        {
            return RedirectToAction("CompanyProfile");
        }

        [Breadcrumb("Company Profile", FromAction = "Index")]
        public async Task<IActionResult> CompanyProfile()
        {
            try
            {
                if (_context.ClientCompany == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var userEmail = HttpContext.User?.Identity?.Name;
                if(userEmail is null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if(companyUser is null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var clientCompany = await _context.ClientCompany
                    .Include(c => c.Country)
                    .Include(c => c.District)
                    .Include(c => c.PinCode)
                    .Include(c => c.State)
                    .FirstOrDefaultAsync(m => m.ClientCompanyId == companyUser.ClientCompanyId);
                if (clientCompany == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(clientCompany);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [Breadcrumb("Edit Company")]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                if (userEmail is null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var clientCompany = await _context.ClientCompany
                    .Include(c => c.Country)
                    .Include(c => c.District)
                    .Include(c => c.PinCode)
                    .Include(c => c.State)
                    .FirstOrDefaultAsync(m => m.ClientCompanyId == companyUser.ClientCompanyId);
                if (clientCompany == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == clientCompany.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == clientCompany.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == clientCompany.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", clientCompany.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", clientCompany.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", clientCompany.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", clientCompany.PinCodeId);
                return View(clientCompany);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
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

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                IFormFile? companyDocument = Request.Form?.Files?.FirstOrDefault();
                if (companyDocument is not null)
                {
                    string newFileName = clientCompany.Email + Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(companyDocument.FileName);
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
                else
                {
                    var existingClientCompany = _context.ClientCompany.AsNoTracking().FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                    if (existingClientCompany.DocumentUrl != null || existingClientCompany.DocumentUrl != null)
                    {
                        clientCompany.DocumentImage = existingClientCompany.DocumentImage;
                        clientCompany.DocumentUrl = existingClientCompany.DocumentUrl;
                    }
                }

                clientCompany.Updated = DateTime.UtcNow;
                clientCompany.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ChangeTracker.Clear();
                _context.ClientCompany.Update(clientCompany);
                await _context.SaveChangesAsync();

                var response = SmsService.SendSingleMessage(clientCompany.PhoneNumber, "Company edited. Domain : " + clientCompany.Email);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Custom($"Company edited successfully.", 3, "orange", "fas fa-building");
            return RedirectToAction(nameof(CompanyController.Index), "Company");
        }

        [Breadcrumb("Manage Users ")]
        public IActionResult User()
        {
            return View();
        }

        [Breadcrumb("Add User")]
        public IActionResult CreateUser()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name; if (userEmail is null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = _context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == companyUser.ClientCompanyId);
                if(company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = new ClientCompanyApplicationUser { ClientCompany = company };
                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(ClientCompanyApplicationUser user, string emailSuffix)
        {
            try
            {
                var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
                if (user.ProfileImage != null && user.ProfileImage.Length > 0)
                {
                    string newFileName = userFullEmail;
                    string fileExtension = Path.GetExtension(user.ProfileImage.FileName);
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "company");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "company", newFileName);
                    user.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));

                    using var dataStream = new MemoryStream();
                    user.ProfileImage.CopyTo(dataStream);
                    user.ProfilePicture = dataStream.ToArray();

                    user.ProfilePictureUrl = "/company/" + newFileName;
                }
                //DEMO
                user.Password = Applicationsettings.Password;
                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.Mailbox = new Mailbox { Name = userFullEmail };
                user.Updated = DateTime.UtcNow;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                IdentityResult result = await userManager.CreateAsync(user, user.Password);

                if (result.Succeeded)
                {
                    user.SecurityStamp = Guid.NewGuid().ToString();
                    var roles = await userManager.GetRolesAsync(user);
                    var roleResult = await userManager.RemoveFromRolesAsync(user, roles);
                    roleResult = await userManager.AddToRolesAsync(user, new List<string> { user.UserRole.ToString() });
                    var currentUser = await userManager.GetUserAsync(HttpContext.User);
                    await signInManager.RefreshSignInAsync(currentUser);
                    if (!user.Active)
                    {
                        var createdUser = await userManager.FindByEmailAsync(user.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            notifyService.Custom($"User created and locked.", 3, "orange", "fas fa-user-lock");
                            var response = SmsService.SendSingleMessage(createdUser.PhoneNumber, "User created and locked. Email : " + createdUser.Email);
                            return RedirectToAction(nameof(CompanyController.User), "Company");
                        }
                    }
                    else
                    {
                        notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
                        var response = SmsService.SendSingleMessage(user.PhoneNumber, "User created . Email : " + user.Email);
                        return RedirectToAction(nameof(CompanyController.User), "Company");
                    }
                    notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
                    return RedirectToAction(nameof(CompanyController.User), "Company");
                }
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [Breadcrumb("Edit User", FromAction = "User")]
        public async Task<IActionResult> EditUser(long? userId)
        {
            try
            {
                if (userId == null || _context.ClientCompanyApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var clientCompanyApplicationUser = await _context.ClientCompanyApplicationUser.FindAsync(userId);
                if (clientCompanyApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userEmail = HttpContext.User?.Identity?.Name;
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

                ViewBag.Show = clientCompanyApplicationUser.Email == companyUser.Email ? false : true;
                var clientCompany = _context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == clientCompanyApplicationUser.ClientCompanyId);

                if (clientCompany == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                clientCompanyApplicationUser.ClientCompany = clientCompany;

                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == clientCompanyApplicationUser.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == clientCompanyApplicationUser.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == clientCompanyApplicationUser.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", clientCompanyApplicationUser.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", clientCompanyApplicationUser.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", clientCompanyApplicationUser.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", clientCompanyApplicationUser.PinCodeId);

                return View(clientCompanyApplicationUser);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        // POST: ClientCompanyApplicationUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, ClientCompanyApplicationUser applicationUser)
        {
            try
            {
                if (id != applicationUser.Id.ToString())
                {
                    notifyService.Error("USER NOT FOUND!");
                    return RedirectToAction(nameof(CompanyController.User), "Company");
                }
                var user = await userManager.FindByIdAsync(id);
                if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                {
                    string newFileName = applicationUser.Email + Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(applicationUser.ProfileImage.FileName);
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "company");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "company", newFileName);
                    applicationUser.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                    applicationUser.ProfilePictureUrl = "/company/" + newFileName;
                    using var dataStream = new MemoryStream();
                    applicationUser.ProfileImage.CopyTo(dataStream);
                    applicationUser.ProfilePicture = dataStream.ToArray();
                }

                if (user != null)
                {
                    user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                    user.PhoneNumber = applicationUser?.PhoneNumber ?? user.PhoneNumber;
                    user.FirstName = applicationUser?.FirstName;
                    user.LastName = applicationUser?.LastName;
                    if (!string.IsNullOrWhiteSpace(applicationUser?.Password))
                    {
                        user.Password = applicationUser.Password;
                    }
                    user.Active = applicationUser.Active;
                    user.Addressline = applicationUser.Addressline;
                    user.Country = applicationUser.Country;
                    user.CountryId = applicationUser.CountryId;
                    user.State = applicationUser.State;
                    user.StateId = applicationUser.StateId;
                    user.PinCode = applicationUser.PinCode;
                    user.PinCodeId = applicationUser.PinCodeId;
                    user.Updated = DateTime.UtcNow;
                    user.Comments = applicationUser.Comments;
                    user.PhoneNumber = applicationUser.PhoneNumber;
                    user.UserRole = applicationUser.UserRole;
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.UtcNow.ToString();
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        var roleResult = await userManager.RemoveFromRolesAsync(user, roles);
                        await userManager.AddToRoleAsync(user, user.UserRole.ToString());

                        var currentUser = await userManager.GetUserAsync(HttpContext.User);
                        await signInManager.RefreshSignInAsync(currentUser);
                        if (!user.Active)
                        {
                            var createdUser = await userManager.FindByEmailAsync(user.Email);
                            var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                            var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                            if (lockUser.Succeeded && lockDate.Succeeded)
                            {
                                notifyService.Custom($"User edited and locked.", 3, "orange", "fas fa-user-lock");
                                var response = SmsService.SendSingleMessage(createdUser.PhoneNumber, "User created and locked. Email : " + createdUser.Email);
                                return RedirectToAction(nameof(CompanyController.User), "Company");
                            }
                        }
                        else
                        {
                            var createdUser = await userManager.FindByEmailAsync(user.Email);
                            var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                            var lockDate = await userManager.SetLockoutEndDateAsync(user, DateTime.Now);

                            if (lockUser.Succeeded && lockDate.Succeeded)
                            {
                                notifyService.Custom($"User edited and unlocked.", 3, "green", "fas fa-user-check");
                                var response = SmsService.SendSingleMessage(user.PhoneNumber, "User created . Email : " + user.Email);
                                return RedirectToAction(nameof(CompanyController.User), "Company");
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                notifyService.Error($"Error to create Company user.", 3);
                return RedirectToAction(nameof(CompanyController.User), "Company");
            }

            notifyService.Error($"Error to create Company user.", 3);
            return RedirectToAction(nameof(CompanyController.User), "Company");
        }

        [Breadcrumb("Available Agencies", FromAction = "Index", FromController = typeof(VendorsController))]
        public IActionResult AvailableVendors()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AvailableVendors(List<string> vendors)
        {
            try
            {
                if(vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userEmail = HttpContext.User?.Identity?.Name;
                if(string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if(companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = _context.ClientCompany.Include(c => c.CompanyApplicationUser).Include(c => c.EmpanelledVendors)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendors2Empanel = _context.Vendor.AsNoTracking().Where(v => vendors.Contains(v.VendorId.ToString()));
                company.EmpanelledVendors.AddRange(vendors2Empanel.ToList());

                company.Updated = DateTime.UtcNow;
                company.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();

                foreach (var vendor2Empanel in vendors2Empanel)
                {
                    vendor2Empanel.Clients.Add(company);
                }

                notifyService.Custom($"Agency(s) empanelled.", 3, "green", "fas fa-thumbs-up");
                return RedirectToAction("AvailableVendors");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [Breadcrumb("Empanelled Agencies", FromAction = "Index", FromController = typeof(VendorsController))]
        public IActionResult EmpanelledVendors()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpanelledVendors(List<string> vendors)
        {
            try
            {
                if (vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = _context.ClientCompany
                    .Include(c => c.CompanyApplicationUser)
                    .Include(c => c.EmpanelledVendors)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var empanelledVendors2Depanel = _context.Vendor.Include(v => v.Clients).AsNoTracking().Where(v => vendors.Contains(v.VendorId.ToString()));
                foreach (var empanelledVendor2Depanel in empanelledVendors2Depanel)
                {
                    var empanelled = company.EmpanelledVendors.FirstOrDefault(v => v.VendorId == empanelledVendor2Depanel.VendorId);
                    company.EmpanelledVendors.Remove(empanelled);
                    var clientCompany = empanelledVendor2Depanel.Clients.FirstOrDefault(c => c.ClientCompanyId == company.ClientCompanyId);

                    empanelledVendor2Depanel.Clients.Remove(clientCompany);
                }
                company.Updated = DateTime.UtcNow;
                company.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();
                notifyService.Custom($"Agency(s) de-panelled.", 3, "red", "fas fa-thumbs-down");
                return RedirectToAction("EmpanelledVendors");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Agency Detail", FromAction = "AvailableVendors")]
        public async Task<IActionResult> VendorDetail(long id, string backurl)
        {
            try
            {

                if (1> id || _context.Vendor == null)
                {
                    notifyService.Error("AGENCY NOT FOUND!");
                    return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
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
                    notifyService.Error("agency not found!");
                    return RedirectToAction(nameof(CompanyController.User), "Company");
                }
                ViewBag.Backurl = backurl;

                return View(vendor);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Agency Detail", FromAction = "EmpanelledVendors")]
        public async Task<IActionResult> VendorDetails(long id, string backurl)
        {
            try
            {

                if (1 > id || _context.Vendor == null)
                {
                    notifyService.Error("AGENCY NOT FOUND!");
                    return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
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
                    notifyService.Error("AGENCY NOT FOUND!");
                    return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
                }
                ViewBag.Backurl = backurl;

                return View(vendor);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Edit Role", FromAction = "User")]
        public async Task<IActionResult> UserRoles(string userId)
        {
            var userRoles = new List<CompanyUserRoleViewModel>();
            //ViewBag.userId = userId;
            ClientCompanyApplicationUser user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                toastNotification.AddErrorToastMessage("user not found!");
                return NotFound();
            }
            string selectedRole = string.Empty;
            //ViewBag.UserName = user.UserName;
            foreach (var role in roleManager.Roles.Where(r =>
                r.Name.Contains(AppRoles.CompanyAdmin.ToString()) ||
                r.Name.Contains(AppRoles.Creator.ToString()) ||
                r.Name.Contains(AppRoles.Assessor.ToString())))
            {
                var userRoleViewModel = new CompanyUserRoleViewModel
                {
                    RoleId = role.Id.ToString(),
                    RoleName = role?.Name
                };
                if (await userManager.IsInRoleAsync(user, role?.Name))
                {
                    userRoleViewModel.Selected = true;
                    selectedRole = role.Name;
                }
                else
                {
                    userRoleViewModel.Selected = false;
                }
                userRoles.Add(userRoleViewModel);
            }
            var model = new CompanyUserRolesViewModel
            {
                UserId = userId,
                CompanyId = user.ClientCompanyId.Value,
                UserName = user.UserName,
                CompanyUserRoleViewModel = userRoles,
                 UserRole = !string.IsNullOrWhiteSpace(selectedRole) ? (CompanyRole)Enum.Parse(typeof(CompanyRole), selectedRole, true) : null,
            };

            //var companyPage = new MvcBreadcrumbNode("Index", "Company", "Company");
            //var usersPage = new MvcBreadcrumbNode("User", "Company", "Users") { Parent = companyPage };
            //var userPage = new MvcBreadcrumbNode("EditUser", "Company", $"User") { Parent = usersPage, RouteValues = new { userid = userId } };
            //var userRolePage = new MvcBreadcrumbNode("UserRoles", "Company", $"Edit Role") { Parent = usersPage, RouteValues = new { userid = userId } };
            //ViewData["BreadcrumbNode"] = userRolePage;
            return View(model);
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string userId, CompanyUserRolesViewModel model)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                toastNotification.AddErrorToastMessage("user not found!");
                return RedirectToAction(nameof(CompanyController.User), "Company");
            }
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.Updated = DateTime.UtcNow;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            var roles = await userManager.GetRolesAsync(user);
            var result = await userManager.RemoveFromRolesAsync(user, roles);
            result = await userManager.AddToRolesAsync(user, new List<string> { model.UserRole.ToString()});
            var currentUser = await userManager.GetUserAsync(HttpContext.User);
            await signInManager.RefreshSignInAsync(currentUser);
            var response = SmsService.SendSingleMessage(user.PhoneNumber, "User role edited . Email : " + user.Email);

            notifyService.Custom($"User role(s) updated successfully.", 3, "orange", "fas fa-user-cog");
            return RedirectToAction(nameof(CompanyController.User));
        }
    }
}