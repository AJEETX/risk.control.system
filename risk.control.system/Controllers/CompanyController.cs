using Amazon.Rekognition.Model;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class CompanyController : Controller
    {
        private const string vendorMapSize = "800x800";
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly UserManager<VendorApplicationUser> userAgencyManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ISmsService smsService;
        private readonly IFeatureManager featureManager;

        public CompanyController(ApplicationDbContext context,
            UserManager<ClientCompanyApplicationUser> userManager,
            UserManager<VendorApplicationUser> userAgencyManager,
            SignInManager<ApplicationUser> signInManager,
            INotyfService notifyService,
            ICustomApiCLient customApiCLient,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            IFeatureManager featureManager,
            ISmsService SmsService)
        {
            this._context = context;
            this.signInManager = signInManager;
            this.notifyService = notifyService;
            this.customApiCLient = customApiCLient;
            this.userManager = userManager;
            this.userAgencyManager = userAgencyManager;
            this.roleManager = roleManager;
            this.featureManager = featureManager;
            this.webHostEnvironment = webHostEnvironment;
            smsService = SmsService;
        }

        [Breadcrumb("Manage Company")]
        public IActionResult Index()
        {
            return RedirectToAction("CompanyProfile");
        }

        [Breadcrumb("Company Profile", FromAction = "Index")]
        public async Task<IActionResult> CompanyProfile()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
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
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(clientCompany);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Edit Company", FromAction = "CompanyProfile")]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
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
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                return View(clientCompany);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
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
                if(clientCompany is null || clientCompany.SelectedCountryId < 1 || clientCompany.SelectedStateId < 1 || clientCompany.SelectedDistrictId < 1 || clientCompany.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (clientCompany.ClientCompanyId < 1)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                var existCompany = _context.ClientCompany.Include(c=>c.Country).FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                IFormFile? companyDocument = Request.Form?.Files?.FirstOrDefault();
                if (companyDocument is not null)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(companyDocument.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "company");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "company", newFileName);
                    companyDocument.CopyTo(new FileStream(upload, FileMode.Create));
                    existCompany.DocumentUrl = "/company/" + newFileName;
                    using var dataStream = new MemoryStream();
                    companyDocument.CopyTo(dataStream);
                    existCompany.DocumentImage = dataStream.ToArray();
                }

                existCompany.CountryId = clientCompany.CountryId;
                existCompany.StateId = clientCompany.StateId;
                existCompany.DistrictId = clientCompany.DistrictId;
                existCompany.PinCodeId = clientCompany.PinCodeId;
                existCompany.Name = clientCompany.Name;
                //existCompany.Code = clientCompany.Code;
                existCompany.PhoneNumber = clientCompany.PhoneNumber;
                existCompany.Branch = clientCompany.Branch;
                existCompany.BankName = clientCompany.BankName;
                existCompany.BankAccountNumber = clientCompany.BankAccountNumber;
                existCompany.IFSCCode = clientCompany.IFSCCode;
                existCompany.Addressline = clientCompany.Addressline;
                //existCompany.Description = clientCompany.Description;

                existCompany.PinCodeId = clientCompany.SelectedPincodeId;
                existCompany.DistrictId = clientCompany.SelectedDistrictId;
                existCompany.StateId = clientCompany.SelectedStateId;
                existCompany.CountryId = clientCompany.SelectedCountryId;

                existCompany.Updated = DateTime.Now;
                existCompany.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ClientCompany.Update(existCompany);
                await _context.SaveChangesAsync();

                await smsService.DoSendSmsAsync(existCompany.Country.ISDCode+ existCompany.PhoneNumber, "Company edited. Domain : " + clientCompany.Email);
            }
            
            catch (Exception ex)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Custom($"Company {clientCompany.Email} edited successfully.", 3, "orange", "fas fa-building");
            return RedirectToAction(nameof(CompanyController.CompanyProfile), "Company");
        }

        [Breadcrumb("Manage Users ")]
        public IActionResult Users()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [Breadcrumb("Add User")]
        public IActionResult CreateUser()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = _context.ClientCompany.Include(c=>c.Country).FirstOrDefault(v => v.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = new ClientCompanyApplicationUser { Country = company.Country, ClientCompany = company, CountryId = company.CountryId };
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(ClientCompanyApplicationUser user, string emailSuffix)
        {
            if(user is null || user.SelectedCountryId < 1 || user.SelectedStateId < 1 || user.SelectedDistrictId < 1 || user.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(CreateUser), "Company");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
                if (user.ProfileImage != null && user.ProfileImage.Length > 0)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(user.ProfileImage.FileName));
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
                user.Active = true;
                user.Password = Applicationsettings.Password;
                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.Mailbox = new Mailbox { Name = userFullEmail };

                user.CountryId = user.SelectedCountryId;
                user.StateId = user.SelectedStateId;
                user.DistrictId = user.SelectedDistrictId;
                user.PinCodeId = user.SelectedPincodeId;

                user.Updated = DateTime.Now;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                user.IsClientAdmin = user.UserRole == CompanyRole.COMPANY_ADMIN;
                IdentityResult result = await userManager.CreateAsync(user, user.Password);

                if (result.Succeeded)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    var roleResult = await userManager.RemoveFromRolesAsync(user, roles);
                    roleResult = await userManager.AddToRolesAsync(user, new List<string> { user.UserRole.ToString() });
                    var isdCode = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId).ISDCode;
                    notifyService.Custom($"User {user.Email} created successfully.", 3, "green", "fas fa-user-plus");
                    await smsService.DoSendSmsAsync(isdCode + user.PhoneNumber, "User created . Email : " + user.Email);
                    return RedirectToAction(nameof(CompanyController.Users), "Company");
                }
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Edit User", FromAction = "Users")]
        public async Task<IActionResult> EditUser(long? userId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (userId == null || _context.ClientCompanyApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var clientCompanyApplicationUser = await _context.ClientCompanyApplicationUser
                    .Include(u => u.Country).
                    Include(u => u.ClientCompany)
                    .FirstOrDefaultAsync(c => c.Id== userId);

                if (clientCompanyApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                clientCompanyApplicationUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !clientCompanyApplicationUser.IsPasswordChangeRequired : true;
                return View(clientCompanyApplicationUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
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
            if (applicationUser is null || applicationUser.SelectedCountryId < 1 || applicationUser.SelectedStateId < 1 || applicationUser.SelectedDistrictId < 1 || applicationUser.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(EditUser), "Company",new {userid = id });
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (id != applicationUser.Id.ToString())
                {
                    notifyService.Error("USER NOT FOUND!");
                    return RedirectToAction(nameof(CompanyController.Users), "Company");
                }
                var user = await userManager.FindByIdAsync(id);
                if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(applicationUser.ProfileImage.FileName));
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

                    user.CountryId = applicationUser.SelectedCountryId;
                    user.StateId = applicationUser.SelectedStateId;
                    user.DistrictId = applicationUser.SelectedDistrictId;
                    user.PinCodeId = applicationUser.SelectedPincodeId;

                    user.IsUpdated = true;
                    user.Updated = DateTime.Now;
                    user.Comments = applicationUser.Comments;
                    user.PhoneNumber = applicationUser.PhoneNumber;
                    user.UserRole = applicationUser.UserRole;
                    user.Role = applicationUser.Role != null ? applicationUser.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                    user.IsClientAdmin = user.UserRole == CompanyRole.COMPANY_ADMIN;
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.Now.ToString();
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        var roleResult = await userManager.RemoveFromRolesAsync(user, roles);
                        await userManager.AddToRoleAsync(user, user.UserRole.ToString());
                        var isdCode = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId).ISDCode;

                        if (!user.Active)
                        {
                            var createdUser = await userManager.FindByEmailAsync(user.Email);
                            var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                            var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                            if (lockUser.Succeeded && lockDate.Succeeded)
                            {
                                notifyService.Custom($"User {createdUser.Email} edited and locked.", 3, "orange", "fas fa-user-lock");
                                await smsService.DoSendSmsAsync(isdCode + createdUser.PhoneNumber, "User created and locked. Email : " + createdUser.Email);
                                return RedirectToAction(nameof(CompanyController.Users), "Company");
                            }
                        }
                        else
                        {
                            var createdUser = await userManager.FindByEmailAsync(user.Email);
                            var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                            var lockDate = await userManager.SetLockoutEndDateAsync(user, DateTime.Now);

                            if (lockUser.Succeeded && lockDate.Succeeded)
                            {
                                notifyService.Custom($"User {createdUser.Email} edited.", 3, "orange", "fas fa-user-check");
                                await smsService.DoSendSmsAsync(isdCode + user.PhoneNumber, "User created . Email : " + user.Email);
                                return RedirectToAction(nameof(CompanyController.Users), "Company");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error($"Error to create Company user.", 3);
                return RedirectToAction(nameof(CompanyController.Users), "Company");
            }

            notifyService.Error($"Error to create Company user.", 3);
            return RedirectToAction(nameof(CompanyController.Users), "Company");
        }

        [Breadcrumb(title: " Delete", FromAction = "Users")]
        public async Task<IActionResult> DeleteUser(long userId)
        {
            try
            {
                if (userId < 1 || userId == 0)
                {
                    notifyService.Error("OOPS!!!.Id Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
               
                var model =await _context.ClientCompanyApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode)
                    .FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string email)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (string.IsNullOrWhiteSpace(email))
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model =await _context.ClientCompanyApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode)
                    .FirstOrDefaultAsync(c => c.Email == email);
                if (model == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.Deleted = true;
                _context.ClientCompanyApplicationUser.Update(model);
                await _context.SaveChangesAsync();
                notifyService.Custom($"User {model.Email} deleted", 3, "orange", "fas fa-user-minus");
                return RedirectToAction(nameof(CompanyController.Users), "Company");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Manage Agency(s)")]
        public IActionResult Agencies()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }
        [Breadcrumb("Available Agencies", FromAction = "Agencies")]
        public IActionResult AvailableVendors()
        {
            return View();
        }

        // GET: Vendors/Details/5
        [Breadcrumb("Agency Profile", FromAction = "EmpanelledVendors", FromController = typeof(VendorsController))]
        public async Task<IActionResult> AgencyDetail(long id)
        {
            try
            {
                if (id <= 0 )
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                var vendor = await _context.Vendor
                    .Include(v => v.ratings)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
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
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);

                if (superAdminUser.IsSuperAdmin)
                {
                    vendor.SelectedByCompany = true;
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [Breadcrumb(" Edit Agency", FromAction = "AgencyDetail")]
        public async Task<IActionResult> EditAgency(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("OOPS !!!..Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendor = await _context.Vendor.Include(v=>v.Country).FirstOrDefaultAsync(v=>v.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                vendor.SelectedByCompany = true;
                return View(vendor);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAgency(long vendorId, Vendor vendor)
        {
            if (vendor is null || vendorId != vendor.VendorId || vendor.SelectedCountryId < 1 || vendor.SelectedStateId < 1 || vendor.SelectedDistrictId < 1 || vendor.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Edit), "Vendors", new { id = vendorId });
            }

            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                IFormFile? vendorDocument = Request.Form?.Files?.FirstOrDefault();
                if (vendorDocument is not null)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(vendorDocument.FileName));
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
                vendor.IsUpdated = true;
                vendor.Updated = DateTime.Now;
                vendor.UpdatedBy = currentUserEmail;

                vendor.PinCodeId = vendor.SelectedPincodeId;
                vendor.DistrictId = vendor.SelectedDistrictId;
                vendor.StateId = vendor.SelectedStateId;
                vendor.CountryId = vendor.SelectedCountryId;
                var pinCode = _context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.PinCodeId == vendor.SelectedPincodeId);

                var companyAddress = vendor.Addressline + ", " + pinCode.District.Name + ", " + pinCode.State.Name + ", " + pinCode.Country.Code;
                var companyCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
                var companyLatLong = companyCoordinates.Latitude + "," + companyCoordinates.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={companyLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                vendor.AddressLatitude = companyCoordinates.Latitude;
                vendor.AddressLongitude = companyCoordinates.Longitude;
                vendor.AddressMapLocation = url;

                _context.Vendor.Update(vendor);
                var isdCode = _context.Country.FirstOrDefault(c => c.CountryId == vendor.CountryId).ISDCode;

                await smsService.DoSendSmsAsync(isdCode + vendor.PhoneNumber, "Agency edited. Domain : " + vendor.Email);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Custom($"Agency edited successfully.", 3, "orange", "fas fa-building");
            return RedirectToAction(nameof(AgencyDetail), "Company", new { id = vendorId });
        }

        [Breadcrumb(" Users", FromAction = "AgencyDetail")]
        public IActionResult AgencyUsers(string id)
        {
            ViewData["vendorId"] = id;

            return View();
        }
        [Breadcrumb(" Add User", FromAction = "AgencyUsers")]
        public IActionResult CreateAgencyUser(long id)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            
            if (id <= 0)
            {
                notifyService.Error("OOPS !!!..Error creating user");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var allRoles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>()?.ToList();

            var vendor = _context.Vendor.Include(v=>v.Country).FirstOrDefault(v => v.VendorId == id);
            if (vendor == null)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var currentVendorUserCount = _context.VendorApplicationUser.Count(v => v.VendorId == id);
            if (currentVendorUserCount == 0)
            {
                allRoles = allRoles.Where(r => r == AgencyRole.AGENCY_ADMIN).ToList();
            }
            else
            {
                allRoles = allRoles.Where(r => r != AgencyRole.AGENCY_ADMIN).ToList();
            }
            var model = new VendorApplicationUser {Country = vendor.Country, CountryId = vendor.CountryId, Vendor = vendor, AgencyRole = allRoles };

            return View(model);
        }
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAgencyUser(VendorApplicationUser user, string emailSuffix)
        {
            if (user is null || user.SelectedCountryId < 1 || user.SelectedStateId < 1 || user.SelectedDistrictId < 1 || user.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(CreateUser), "Vendors", new { userid = user.Id });
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (string.IsNullOrEmpty(emailSuffix) || user is null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (user.ProfileImage != null && user.ProfileImage.Length > 0)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(user.ProfileImage.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "agency");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "agency", newFileName);
                    user.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                    user.ProfilePictureUrl = "/agency/" + newFileName;

                    using var dataStream = new MemoryStream();
                    user.ProfileImage.CopyTo(dataStream);
                    user.ProfilePicture = dataStream.ToArray();
                }
                var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
                //DEMO
                user.Password = Applicationsettings.Password;
                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.Mailbox = new Mailbox { Name = userFullEmail };

                user.PinCodeId = user.SelectedPincodeId;
                user.DistrictId = user.SelectedDistrictId;
                user.StateId = user.SelectedStateId;
                user.CountryId = user.SelectedCountryId;

                user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                user.Updated = DateTime.Now;
                user.UpdatedBy = currentUserEmail;
                user.IsVendorAdmin = user.UserRole == AgencyRole.AGENCY_ADMIN;
                if (user.Role == AppRoles.AGENT)
                {
                    var pincode = _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefault(c => c.PinCodeId == user.PinCodeId);
                    var userAddress = $"{user.Addressline}, {pincode.Name}, {pincode.District.Name}, {pincode.State.Name}, {pincode.Country.Name}";
                    var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(userAddress);
                    var customerLatLong = coordinates.Latitude + "," + coordinates.Longitude;
                    user.AddressLatitude = coordinates.Latitude;
                    user.AddressLongitude = coordinates.Longitude;
                    user.AddressMapLocation = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                }
                IdentityResult result = await userAgencyManager.CreateAsync(user, user.Password);

                if (result.Succeeded)
                {
                    var roleResult = await userAgencyManager.AddToRolesAsync(user, new List<string> { user.UserRole.ToString() });
                    var roles = await userAgencyManager.GetRolesAsync(user);
                    var isdCode = _context.Country.FirstOrDefault(c => c.CountryId == user.SelectedCountryId).ISDCode;
                    if (!user.Active)
                    {
                        var createdUser = await userAgencyManager.FindByEmailAsync(user.Email);
                        var lockUser = await userAgencyManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userAgencyManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            await smsService.DoSendSmsAsync(isdCode + user.PhoneNumber, "Agency user created and locked. Email : " + user.Email);
                            notifyService.Custom($"User edited and locked.", 3, "orange", "fas fa-user-lock");
                        }
                    }
                    else
                    {

                        await smsService.DoSendSmsAsync(isdCode + user.PhoneNumber, "Agency user created. Email : " + user.Email);

                        var onboardAgent = roles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(user.MobileUId);

                        if (onboardAgent)
                        {
                            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == user.VendorId);

                            System.Uri address = new System.Uri("http://tinyurl.com/api-create.php?url=" + vendor.MobileAppUrl);
                            System.Net.WebClient client = new System.Net.WebClient();
                            string tinyUrl = client.DownloadString(address);

                            var message = $"Dear {user.FirstName}";
                            message += "                                                                                ";
                            message += $"Click on link below to install the mobile app";
                            message += "                                                                                ";
                            message += $"{tinyUrl}";
                            message += "                                                                                ";
                            message += $"Thanks";
                            message += "                                                                                ";
                            message += $"https://icheckify.co.in";

                            await smsService.DoSendSmsAsync(isdCode + user.PhoneNumber, message, true);
                            notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                        }
                        else
                        {
                            await smsService.DoSendSmsAsync(isdCode + user.PhoneNumber, "Agency user edited and unlocked. Email : " + user.Email);
                        }
                        notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
                    }
                    return RedirectToAction(nameof(AgencyDetail), "Company", new { id = user.VendorId });
                }

                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Edit User", FromAction = "AgencyUsers")]
        public async Task<IActionResult> EditAgencyUser(long? userId)
        {
            try
            {
                if (userId == null || userId <= 0)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorApplicationUser = _context.VendorApplicationUser
                    .Include(u=>u.Vendor)
                    .Include(v => v.Country).Where(v => v.Id == userId)
                    ?.FirstOrDefault();

                if (vendorApplicationUser == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                vendorApplicationUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !vendorApplicationUser.IsPasswordChangeRequired : true;
                return View(vendorApplicationUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Delete", FromAction = "AgencyUsers")]
        public async Task<IActionResult> DeleteAgencyUser(long userId)
        {
            try
            {
                if (userId < 1 || userId == 0)
                {
                    notifyService.Error("OOPS!!!.Id Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                var model = await _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var agencySubStatuses = _context.InvestigationCaseSubStatus.Where(i =>
                    i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR) ||
                    i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR) ||
                    i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT) ||
                    i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR) ||
                    i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
                    ).Select(s => s.InvestigationCaseSubStatusId).ToList();

                var hasClaims = _context.ClaimsInvestigation.Any(c => agencySubStatuses.Contains(c.InvestigationCaseSubStatus.InvestigationCaseSubStatusId) && c.VendorId == model.VendorId);
                model.HasClaims = hasClaims;
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [HttpPost, ActionName("DeleteAgencyUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAgencyUser(string email, long vendorId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (string.IsNullOrWhiteSpace(email))
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Email == email);
                if (model == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.Deleted = true;
                _context.VendorApplicationUser.Update(model);
                await _context.SaveChangesAsync();
                notifyService.Custom($"User {model.Email} deleted", 3, "orange", "fas fa-user-minus");
                return RedirectToAction(nameof(AgencyUsers), "Company", new { id = vendorId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Manage Service", FromAction = "AgencyDetail")]
        public IActionResult Service(string id)
        {
            if (id == null || string.IsNullOrWhiteSpace(id))
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
            ViewData["vendorId"] = id;

            return View();
        }
        [Breadcrumb(" Add Service", FromAction = "Service")]
        public IActionResult CreateService(long id)
        {
            try
            {
                var vendor = _context.Vendor.Include(v=>v.Country).FirstOrDefault(v => v.VendorId == id);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
                var model = new VendorInvestigationServiceType { Country = vendor.Country, CountryId = vendor.CountryId, Vendor = vendor };

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
        public async Task<IActionResult> CreateService(VendorInvestigationServiceType service, long VendorId)
        {
            if (service == null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 || (service.SelectedDistrictId < 1 && service.SelectedDistrictId != -1) || VendorId < 1)
            {
                notifyService.Custom("OOPs !!!..Invalid Data.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = VendorId });
            }
            try
            {
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
                    if (stateWideService is not null && stateWideService.Any())
                    {
                        stateWideService.FirstOrDefault().IsUpdated = true;
                        _context.VendorInvestigationServiceType.Update(stateWideService.FirstOrDefault());
                        await _context.SaveChangesAsync();
                        notifyService.Custom($"Service [{ALL_DISTRICT}] already exists for the State!", 3, "orange", "fas fa-truck");
                        return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = service.VendorId });
                    }
                }

                
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
                if (service.DistrictId == null)
                {
                    notifyService.Custom($"Service [{ALL_DISTRICT}] added successfully.", 3, "orange", "fas fa-truck");
                }
                else
                {
                    notifyService.Custom($"Service created successfully.", 3, "green", "fas fa-truck");
                }

                return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = service.VendorId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Edit Service", FromAction = "Service")]
        public async Task<IActionResult> EditService(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (id <= 0 )
                {
                    notifyService.Error("OOPs !!!..Agency Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorInvestigationServiceType = _context.VendorInvestigationServiceType
                    .Include(v => v.LineOfBusiness)
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.Country)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.Vendor)
                    .First(v => v.VendorInvestigationServiceTypeId == id);

                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);

                if (vendorInvestigationServiceType.DistrictId == null)
                {
                    vendorInvestigationServiceType.SelectedDistrictId = -1;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(long VendorInvestigationServiceTypeId, VendorInvestigationServiceType service, long VendorId)
        {
            if (VendorInvestigationServiceTypeId != service.VendorInvestigationServiceTypeId || service is null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 ||
                (service.SelectedDistrictId != -1 && service.SelectedDistrictId < 1) || VendorId < 1)
            {
                notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(EditService), "VendorService", new { id = VendorInvestigationServiceTypeId });
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
                notifyService.Custom($"Service updated successfully.", 3, "orange", "fas fa-truck");
                return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = service.VendorId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // GET: VendorService/Delete/5
        [Breadcrumb(" Delete Service", FromAction = "Service")]
        public async Task<IActionResult> DeleteService(long id)
        {
            try
            {
                if (id <= 0 )
                {
                    notifyService.Error("OOPs !!!..Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.LineOfBusiness)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.Country)
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

        // POST: VendorService/Delete/5
        [HttpPost, ActionName("DeleteService")]
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
                    notifyService.Custom($"Service deleted successfully.", 3, "orange", "fas fa-truck");
                    return RedirectToAction("Service", "Company", new { id = vendorInvestigationServiceType.VendorId });
                }
                notifyService.Error($"Err Service delete.", 3);
                return RedirectToAction("Service", "Company", new { id = vendorInvestigationServiceType.VendorId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AvailableVendors(List<string> vendors)
        {
            if (vendors is null || vendors.Count == 0)
            {
                notifyService.Error("No agency selected !!!");
                return RedirectToAction(nameof(AvailableVendors), "Company");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = _context.ClientCompany.Include(c => c.EmpanelledVendors)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendors2Empanel = _context.Vendor.AsNoTracking().Where(v => vendors.Contains(v.VendorId.ToString()));
                company.EmpanelledVendors.AddRange(vendors2Empanel.ToList());

                company.Updated = DateTime.Now;
                company.UpdatedBy = currentUserEmail;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();

                notifyService.Custom($"Agency(s) empanelled.", 3, "green", "fas fa-thumbs-up");
                return RedirectToAction("AvailableVendors", "Vendors");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
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
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("OOPs !!!..Not Agency Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = _context.ClientCompany
                    .Include(c => c.EmpanelledVendors)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var empanelledVendors2Depanel = _context.Vendor.AsNoTracking().Where(v => vendors.Contains(v.VendorId.ToString()));
                
                foreach (var empanelledVendor2Depanel in empanelledVendors2Depanel)
                {
                    var empanelled = company.EmpanelledVendors.FirstOrDefault(v => v.VendorId == empanelledVendor2Depanel.VendorId);
                    company.EmpanelledVendors.Remove(empanelled);
                }
                company.Updated = DateTime.Now;
                company.UpdatedBy = currentUserEmail;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();
                notifyService.Custom($"Agency(s) de-panelled.", 3, "orange", "far fa-thumbs-down");
                return RedirectToAction("EmpanelledVendors","Vendors");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Agency Detail", FromAction = "AvailableVendors")]
        public async Task<IActionResult> VendorDetail(long id, string backurl)
        {
            try
            {
                if (1 > id )
                {
                    notifyService.Error("AGENCY NOT FOUND!");
                    return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
                }

                var vendor = await _context.Vendor
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
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
                    return RedirectToAction(nameof(CompanyController.Users), "Company");
                }
                ViewBag.Backurl = backurl;

                return View(vendor);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Agency Detail", FromAction = "EmpanelledVendors")]
        public async Task<IActionResult> VendorDetails(long id, string backurl)
        {
            try
            {
                if (1 > id )
                {
                    notifyService.Error("AGENCY NOT FOUND!");
                    return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
                }

                var vendor = await _context.Vendor
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Edit Role", FromAction = "Users")]
        public async Task<IActionResult> UserRoles(string userId)
        {
            var userRoles = new List<CompanyUserRoleViewModel>();
            ClientCompanyApplicationUser user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                notifyService.Error("user not found!");
                return NotFound();
            }
            string selectedRole = string.Empty;
            //ViewBag.UserName = user.UserName;
            foreach (var role in roleManager.Roles.Where(r =>
                r.Name.Contains(AppRoles.COMPANY_ADMIN.ToString()) ||
                r.Name.Contains(AppRoles.CREATOR.ToString()) ||
                r.Name.Contains(AppRoles.ASSESSOR.ToString())))
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
                notifyService.Error("user not found!");
                return RedirectToAction(nameof(CompanyController.Users), "Company");
            }
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.Updated = DateTime.Now;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            var roles = await userManager.GetRolesAsync(user);
            var result = await userManager.RemoveFromRolesAsync(user, roles);
            result = await userManager.AddToRolesAsync(user, new List<string> { model.UserRole.ToString() });
            var currentUser = await userManager.GetUserAsync(HttpContext.User);
            await signInManager.RefreshSignInAsync(currentUser);
            if (result.Succeeded)
            {
                var isdCode = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId).ISDCode;
                await smsService.DoSendSmsAsync(isdCode + user.PhoneNumber, "User role edited . Email : " + user.Email);

                notifyService.Custom($"User role(s) updated successfully.", 3, "orange", "fas fa-user-cog");
                return RedirectToAction(nameof(CompanyController.Users));
            }
            notifyService.Error("OOPS !!!..Contact Admin");
            return RedirectToAction(nameof(Index), "Dashboard");
        }
    }
}