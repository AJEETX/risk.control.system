using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
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
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class CompanyController : Controller
    {
        private const string vendorMapSize = "800x800";
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ApplicationDbContext _context;
        private readonly ITinyUrlService urlService;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly UserManager<VendorApplicationUser> userAgencyManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ISmsService smsService;
        private readonly IFeatureManager featureManager;
        private readonly IInvestigationService service;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<CompanyController> logger;
        private readonly string baseUrl;
        public CompanyController(ApplicationDbContext context,
            ITinyUrlService urlService,
            UserManager<ClientCompanyApplicationUser> userManager,
            UserManager<VendorApplicationUser> userAgencyManager,
            SignInManager<ApplicationUser> signInManager,
            INotyfService notifyService,
            ICustomApiCLient customApiCLient,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            IFeatureManager featureManager,
            IInvestigationService service,
             IHttpContextAccessor httpContextAccessor,
            ILogger<CompanyController> logger,
            ISmsService SmsService)
        {
            this._context = context;
            this.urlService = urlService;
            this.signInManager = signInManager;
            this.notifyService = notifyService;
            this.customApiCLient = customApiCLient;
            this.userManager = userManager;
            this.userAgencyManager = userAgencyManager;
            this.roleManager = roleManager;
            this.featureManager = featureManager;
            this.webHostEnvironment = webHostEnvironment;
            smsService = SmsService;
            this.service = service;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
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
                    return RedirectToAction(nameof(CompanyProfile));
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
                    return RedirectToAction(nameof(CompanyProfile));
                }

                return View(clientCompany);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(CompanyProfile));
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClientCompany clientCompany)
        {
            try
            {
                if (clientCompany is null || clientCompany.SelectedCountryId < 1 || clientCompany.SelectedStateId < 1 || clientCompany.SelectedDistrictId < 1 || clientCompany.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(CompanyProfile));
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (clientCompany.ClientCompanyId < 1)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction(nameof(CompanyProfile));
                }
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(CompanyProfile));
                }

                var existCompany = _context.ClientCompany.Include(c => c.Country).FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (clientCompany.Document is not null)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(clientCompany.Document.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "company");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "company", newFileName);
                    clientCompany.Document.CopyTo(new FileStream(upload, FileMode.Create));
                    existCompany.DocumentUrl = "/company/" + newFileName;
                    using var dataStream = new MemoryStream();
                    clientCompany.Document.CopyTo(dataStream);
                    existCompany.DocumentImage = dataStream.ToArray();
                    existCompany.DocumentImageExtension = fileExtension;
                }

                existCompany.CountryId = clientCompany.CountryId;
                existCompany.StateId = clientCompany.StateId;
                existCompany.DistrictId = clientCompany.DistrictId;
                existCompany.PinCodeId = clientCompany.PinCodeId;
                existCompany.Name = clientCompany.Name;
                //existCompany.Code = clientCompany.Code;
                existCompany.PhoneNumber = clientCompany.PhoneNumber.TrimStart('0');
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
                string message = "Company edited. \nDomain : " + clientCompany.Email + "\n" + baseUrl;
                await smsService.DoSendSmsAsync(existCompany.Country.Code, existCompany.Country.ISDCode + existCompany.PhoneNumber, message);
            }

            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(CompanyProfile));
            }
            notifyService.Custom($"Company <b>{clientCompany.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
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
                var company = _context.ClientCompany.Include(c => c.Country).FirstOrDefault(v => v.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var existingUsers = _context.ClientCompanyApplicationUser.Where(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
                var isManagerTaken = existingUsers.Any(u => u.UserRole == CompanyRole.MANAGER);
                var availableRoles = Enum.GetValues(typeof(CompanyRole))
                    .Cast<CompanyRole>()
                    .Where(role => role != CompanyRole.COMPANY_ADMIN && (isManagerTaken ? role != CompanyRole.MANAGER : true))
                    .Select(role => new SelectListItem
                    {
                        Value = role.ToString(),
                        Text = role.ToString()
                    })
                    .ToList();

                var model = new ClientCompanyApplicationUser { Country = company.Country, ClientCompany = company, CountryId = company.CountryId, AvailableRoles = availableRoles };
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
        public async Task<IActionResult> CreateUser(ClientCompanyApplicationUser user, string emailSuffix)
        {
            if (user is null || user.SelectedCountryId < 1 || user.SelectedStateId < 1 || user.SelectedDistrictId < 1 || user.SelectedPincodeId < 1)
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
                    user.ProfilePictureExtension = fileExtension;
                }
                //DEMO
                user.Active = true;
                user.Password = Applicationsettings.TestingData;
                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.PhoneNumber = user.PhoneNumber.TrimStart('0');
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
                    var country = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);
                    notifyService.Custom($"User <b>{user.Email}</b> created successfully.", 3, "green", "fas fa-user-plus");
                    string message = "User created . \nEmail : " + user.Email + "\n" + baseUrl;
                    await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, message);
                    return RedirectToAction(nameof(CompanyController.Users), "Company");
                }
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
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
                    return RedirectToAction(nameof(Users));
                }

                var clientCompanyApplicationUser = await _context.ClientCompanyApplicationUser
                    .Include(u => u.Country).
                    Include(u => u.ClientCompany)
                    .FirstOrDefaultAsync(c => c.Id == userId);

                if (clientCompanyApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Users));
                }
                var existingUsers = _context.ClientCompanyApplicationUser.Where(c => !c.Deleted && c.ClientCompanyId == clientCompanyApplicationUser.ClientCompanyId && c.Id != clientCompanyApplicationUser.Id);
                var isManagerTaken = existingUsers.Any(u => u.UserRole == CompanyRole.MANAGER);
                var availableRoles = Enum.GetValues(typeof(CompanyRole))
                    .Cast<CompanyRole>()
                    .Where(role => role != CompanyRole.COMPANY_ADMIN && (isManagerTaken ? role != CompanyRole.MANAGER : true))
                    .Select(role => new SelectListItem
                    {
                        Value = role.ToString(),
                        Text = role.ToString()
                    })
                    .ToList();

                clientCompanyApplicationUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !clientCompanyApplicationUser.IsPasswordChangeRequired : true;
                clientCompanyApplicationUser.AvailableRoles = availableRoles;
                return View(clientCompanyApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Users));
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, ClientCompanyApplicationUser applicationUser)
        {
            if (applicationUser is null || applicationUser.SelectedCountryId < 1 || applicationUser.SelectedStateId < 1 || applicationUser.SelectedDistrictId < 1 || applicationUser.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(EditUser), "Company", new { userid = id });
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
                    applicationUser.ProfilePictureExtension = fileExtension;
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
                    user.PhoneNumber = applicationUser.PhoneNumber.TrimStart('0');
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
                        var country = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);

                        if (!user.Active)
                        {
                            var createdUser = await userManager.FindByEmailAsync(user.Email);
                            var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                            var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                            if (lockUser.Succeeded && lockDate.Succeeded)
                            {
                                notifyService.Custom($"User <b>{createdUser.Email}</b> edited successfully.", 3, "orange", "fas fa-user-lock");
                                string message = "User edited. \nEmail : " + createdUser.Email + "\n" + baseUrl;
                                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + createdUser.PhoneNumber, message);
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
                                notifyService.Custom($"User <b>{createdUser.Email}</b> edited successfully.", 3, "orange", "fas fa-user-check");
                                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "User edited . \nEmail : " + user.Email + "\n" + baseUrl);
                                return RedirectToAction(nameof(CompanyController.Users), "Company");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
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

                var model = await _context.ClientCompanyApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode)
                    .FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
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
                var model = await _context.ClientCompanyApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode)
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
                notifyService.Custom($"User <b>{model.Email}</b> deleted successfully", 3, "orange", "fas fa-user-minus");
                return RedirectToAction(nameof(CompanyController.Users), "Company");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
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

        [Breadcrumb("Agency Profile", FromAction = "EmpanelledVendors", FromController = typeof(VendorsController))]
        public async Task<IActionResult> AgencyDetail(long id)
        {
            try
            {
                if (id <= 0)
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
                    .FirstOrDefaultAsync(m => m.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendorUserCount = await _context.VendorApplicationUser.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted);
                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);

                var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

                var vendorAllCasesCount = await _context.Investigations.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted &&
                          (c.SubStatus == approvedStatus ||
                          c.SubStatus == rejectedStatus));

                // HACKY
                var currentCases = service.GetAgencyIdsLoad(new List<long> { vendor.VendorId });
                vendor.SelectedCountryId = vendorUserCount;
                vendor.SelectedStateId = currentCases.FirstOrDefault().CaseCount;
                vendor.SelectedDistrictId = vendorAllCasesCount;
                if (superAdminUser.IsSuperAdmin)
                {
                    vendor.SelectedByCompany = true;
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
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

                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                vendor.SelectedByCompany = true;
                var claimsPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Empanelled Agencies") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("AgencyDetail", "Company", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditAgency", "Company", $"Edit Agency") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
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

                if (vendor.Document is not null)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(vendor.Document.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "agency");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "agency", newFileName);

                    using var dataStream = new MemoryStream();
                    vendor.Document.CopyTo(dataStream);
                    vendor.DocumentImage = dataStream.ToArray();
                    vendor.Document.CopyTo(new FileStream(upload, FileMode.Create));
                    vendor.DocumentUrl = "/agency/" + newFileName;
                    vendor.DocumentImageExtension = fileExtension;
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
                vendor.PhoneNumber = vendor.PhoneNumber.TrimStart('0');
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
                var country = _context.Country.FirstOrDefault(c => c.CountryId == vendor.CountryId);

                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + vendor.PhoneNumber, "Agency edited. \n Domain : " + vendor.Email + "\n" + baseUrl);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Custom($"Agency <b>{vendor.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
            return RedirectToAction(nameof(AgencyDetail), "Company", new { id = vendorId });
        }

        [Breadcrumb(" Manage Users", FromAction = "AgencyDetail")]
        public IActionResult AgencyUsers(string id)
        {
            ViewData["vendorId"] = id;

            var claimsPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
            var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Empanelled Agencies") { Parent = claimsPage, };
            var detailsPage = new MvcBreadcrumbNode("AgencyDetail", "Company", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("AgencyUsers", "Company", $"Manage Users") { Parent = detailsPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = editPage;

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
            AgencyRole? role = null;
            string? adminEmail = null;
            var vendor = _context.Vendor.Include(v => v.Country).FirstOrDefault(v => v.VendorId == id);
            if (vendor == null)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var currentVendorUserCount = _context.VendorApplicationUser.Count(v => v.VendorId == id);
            bool status = false;
            if (currentVendorUserCount == 0)
            {
                adminEmail = "admin";
                role = AgencyRole.AGENCY_ADMIN;
                status = true;
                allRoles = allRoles.Where(r => r == AgencyRole.AGENCY_ADMIN).ToList();
            }
            else
            {
                allRoles = allRoles.Where(r => r != AgencyRole.AGENCY_ADMIN).ToList();
            }
            var model = new VendorApplicationUser { Email = adminEmail, Active = status, Country = vendor.Country, CountryId = vendor.CountryId, Vendor = vendor, AgencyRole = allRoles, UserRole = role };

            var claimsPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
            var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Empanelled Agencies") { Parent = claimsPage, };
            var detailsPage = new MvcBreadcrumbNode("AgencyDetail", "Company", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("AgencyUsers", "Company", $"Manage Users") { Parent = detailsPage, RouteValues = new { id = id } };
            var addPage = new MvcBreadcrumbNode("CreateAgencyUser", "Company", $"Add user") { Parent = editPage };
            ViewData["BreadcrumbNode"] = addPage;

            return View(model);
        }
        [HttpPost]
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
                    user.ProfilePictureExtension = fileExtension;
                }
                var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
                //DEMO
                user.Password = Applicationsettings.TestingData;
                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.PhoneNumber = user.PhoneNumber.TrimStart('0');
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
                    var country = _context.Country.FirstOrDefault(c => c.CountryId == user.SelectedCountryId);
                    if (!user.Active)
                    {
                        var createdUser = await userAgencyManager.FindByEmailAsync(user.Email);
                        var lockUser = await userAgencyManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userAgencyManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user created. \nEmail : " + user.Email + "\n" + baseUrl);
                            notifyService.Custom($"User created.", 3, "orange", "fas fa-user-lock");
                        }
                    }
                    else
                    {

                        await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user created. \nEmail : " + user.Email + "\n" + baseUrl);

                        var onboardAgent = roles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(user.MobileUId);

                        if (onboardAgent)
                        {
                            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == user.VendorId);

                            string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

                            var message = $"Dear {user.FirstName}\n" +
                            $"Click on link below to install the mobile app\n\n" +
                            $"{tinyUrl}\n\n" +
                            $"Thanks\n\n" +
                            $"{baseUrl}";

                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, message, true);
                            notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                        }
                        else
                        {
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user created. \nEmail : " + user.Email + "\n" + baseUrl);
                        }
                        notifyService.Custom($"User <b>{user.Email}</b> created successfully.", 3, "green", "fas fa-user-plus");
                    }
                    return RedirectToAction(nameof(AgencyDetail), "Company", new { id = user.VendorId });
                }

                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
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
                    .Include(u => u.Vendor)
                    .Include(v => v.Country).Where(v => v.Id == userId)
                    ?.FirstOrDefault();

                if (vendorApplicationUser == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                vendorApplicationUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !vendorApplicationUser.IsPasswordChangeRequired : true;

                var claimsPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Empanelled Agencies") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("AgencyDetail", "Company", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = vendorApplicationUser.VendorId } };
                var editPage = new MvcBreadcrumbNode("AgencyUsers", "Company", $"Manage Users") { Parent = detailsPage, RouteValues = new { id = vendorApplicationUser.VendorId } };
                var addPage = new MvcBreadcrumbNode("EditAgencyUser", "Company", $"Edit user") { Parent = editPage };
                ViewData["BreadcrumbNode"] = addPage;
                return View(vendorApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
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
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var agencySubStatuses = new[]{
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR};

                var hasClaims = _context.Investigations.Any(c => agencySubStatuses.Contains(c.SubStatus) && c.VendorId == model.VendorId);
                model.HasClaims = hasClaims;

                var claimsPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Empanelled Agencies") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("AgencyDetail", "Company", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = model.VendorId } };
                var editPage = new MvcBreadcrumbNode("AgencyUsers", "Company", $"Manage Users") { Parent = detailsPage, RouteValues = new { id = model.VendorId } };
                var addPage = new MvcBreadcrumbNode("DeleteAgencyUser", "Company", $"Delete user") { Parent = editPage };
                ViewData["BreadcrumbNode"] = addPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
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
                notifyService.Custom($"User <b>{model.Email}</b> deleted successfully", 3, "orange", "fas fa-user-minus");
                return RedirectToAction(nameof(AgencyUsers), "Company", new { id = vendorId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
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

            var claimsPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
            var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Empanelled Agencies") { Parent = claimsPage, };
            var detailsPage = new MvcBreadcrumbNode("AgencyDetail", "Company", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Service", "Company", $"Manage Service") { Parent = detailsPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = editPage;

            return View();
        }
        [Breadcrumb(" Add Service", FromAction = "Service")]
        public IActionResult CreateService(long id)
        {
            try
            {
                var vendor = _context.Vendor.Include(v => v.Country).FirstOrDefault(v => v.VendorId == id);
                var model = new VendorInvestigationServiceType { Country = vendor.Country, CountryId = vendor.CountryId, Vendor = vendor };
                ViewData["Currency"] = Extensions.GetCultureByCountry(vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Empanelled Agencies") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("AgencyDetail", "Company", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("Service", "Company", $"Manage Service") { Parent = detailsPage, RouteValues = new { id = id } };
                var addPage = new MvcBreadcrumbNode("CreateService", "Company", $"Add Service") { Parent = editPage };
                ViewData["BreadcrumbNode"] = addPage;

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(VendorInvestigationServiceType service, long VendorId)
        {
            if (service == null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 || (service.SelectedDistrictIds.Count <= 0) || VendorId < 1)
            {
                notifyService.Custom("OOPs !!!..Invalid Data.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = VendorId });
            }
            try
            {
                var isCountryValid = await _context.Country.AnyAsync(c => c.CountryId == service.SelectedCountryId);
                var isStateValid = await _context.State.AnyAsync(s => s.StateId == service.SelectedStateId);

                if (!isCountryValid || !isStateValid)
                {
                    notifyService.Error("Invalid country, state, or district selected.");
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
                        return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = service.VendorId });
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
                        return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = service.VendorId });
                    }
                }

                service.VendorId = VendorId;
                service.CountryId = service.SelectedCountryId;
                service.StateId = service.SelectedStateId;
                service.Updated = DateTime.Now;
                service.UpdatedBy = HttpContext.User?.Identity?.Name;
                service.Created = DateTime.Now;

                _context.Add(service);
                await _context.SaveChangesAsync();
                if (isAllDistricts)
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Edit Service", FromAction = "Service")]
        public IActionResult EditService(long id)
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

                var claimsPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Empanelled Agencies") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("AgencyDetail", "Company", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var editPage = new MvcBreadcrumbNode("Service", "Company", $"Manage Service") { Parent = detailsPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var addPage = new MvcBreadcrumbNode("EditService", "Company", $"Edit Service") { Parent = editPage };
                ViewData["BreadcrumbNode"] = addPage;
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
        public async Task<IActionResult> EditService(long VendorInvestigationServiceTypeId, VendorInvestigationServiceType service, long VendorId)
        {
            if (VendorInvestigationServiceTypeId != service.VendorInvestigationServiceTypeId || service is null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 ||
                (service.SelectedDistrictIds.Count <= 0) || VendorId < 1)
            {
                notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(EditService), "VendorService", new { id = VendorInvestigationServiceTypeId });
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
                        return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = service.VendorId });
                    }
                }
                else
                {
                    // Handle state-wide service creation
                    if (existingVendorServices is not null && existingVendorServices.Any(s => s.SelectedDistrictIds != null && s.SelectedDistrictIds.Intersect(service.SelectedDistrictIds ?? new List<long>()).Any()))
                    {
                        var currentService = existingVendorServices.FirstOrDefault(s => s.SelectedDistrictIds != null && s.SelectedDistrictIds.Intersect(service.SelectedDistrictIds ?? new List<long>()).Any());
                        currentService.IsUpdated = true;
                        _context.VendorInvestigationServiceType.Update(currentService);
                        await _context.SaveChangesAsync();
                        notifyService.Custom($"Service already exists for the District!", 3, "orange", "fas fa-truck");
                        return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = service.VendorId });
                    }
                }
                service.CountryId = service.SelectedCountryId;
                service.StateId = service.SelectedStateId;
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Delete Service", FromAction = "Service")]
        public async Task<IActionResult> DeleteService(long id)
        {
            try
            {
                if (id <= 0)
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
                var claimsPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("EmpanelledVendors", "Vendors", "Empanelled Agencies") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("AgencyDetail", "Company", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var editPage = new MvcBreadcrumbNode("Service", "Company", $"Manage Service") { Parent = detailsPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                var addPage = new MvcBreadcrumbNode("DeleteService", "Company", $"Delete Service") { Parent = editPage };
                ViewData["BreadcrumbNode"] = addPage;

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
                logger.LogError(ex.StackTrace);
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
                logger.LogError(ex.StackTrace);
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
                notifyService.Custom($"Agency(s) de-panelled. successfully", 3, "orange", "far fa-thumbs-down");
                return RedirectToAction("EmpanelledVendors", "Vendors");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
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
                if (1 > id)
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
                logger.LogError(ex.StackTrace);
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
                if (1 > id)
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}