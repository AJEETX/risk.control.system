using System.Net;
using System.Text.RegularExpressions;

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
        private const string vallidDomainExpression = "^[a-zA-Z0-9.-]+$";
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> appUserManager;
        private readonly ITinyUrlService urlService;
        private readonly IFileStorageService fileStorageService;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly UserManager<VendorApplicationUser> userAgencyManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ISmsService smsService;
        private readonly IFeatureManager featureManager;
        private readonly IInvestigationService service;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<CompanyController> logger;
        private readonly string baseUrl;
        public CompanyController(ApplicationDbContext context,
            UserManager<ApplicationUser> appUserManager,
        ITinyUrlService urlService,
            IFileStorageService fileStorageService,
            UserManager<ClientCompanyApplicationUser> userManager,
            UserManager<VendorApplicationUser> userAgencyManager,
            SignInManager<ApplicationUser> signInManager,
            INotyfService notifyService,
            ICustomApiCLient customApiCLient,
            RoleManager<ApplicationRole> roleManager,
            IFeatureManager featureManager,
            IInvestigationService service,
             IHttpContextAccessor httpContextAccessor,
            ILogger<CompanyController> logger,
            ISmsService SmsService)
        {
            this._context = context;
            this.appUserManager = appUserManager;
            this.urlService = urlService;
            this.fileStorageService = fileStorageService;
            this.signInManager = signInManager;
            this.notifyService = notifyService;
            this.customApiCLient = customApiCLient;
            this.userManager = userManager;
            this.userAgencyManager = userAgencyManager;
            this.roleManager = roleManager;
            this.featureManager = featureManager;
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

                var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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

                var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(CompanyProfile));
                }
                var clientCompany = await _context.ClientCompany
                    .Include(c => c.Country)
                    .Include(c => c.State)
                    .Include(c => c.District)
                    .Include(c => c.PinCode)
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
        public async Task<IActionResult> Edit(ClientCompany model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    await Load(model);
                    return View(model);
                }
                if (model.Document is not null)
                {
                    if (model.Document.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        ModelState.AddModelError(nameof(model.Document), "File too large.");
                        await Load(model);
                        return View(model);
                    }

                    var ext = Path.GetExtension(model.Document.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        ModelState.AddModelError(nameof(model.Document), "Invalid file type.");
                        await Load(model);
                        return View(model);
                    }

                    if (!AllowedMime.Contains(model.Document.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        ModelState.AddModelError(nameof(model.Document), "Invalid Document Image  content type.");
                        await Load(model);
                        return View(model);
                    }

                    if (!ImageSignatureValidator.HasValidSignature(model.Document))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        ModelState.AddModelError(nameof(model.Document), "Invalid file content.");
                        await Load(model);
                        return View(model);
                    }
                }

                if (model.SelectedCountryId < 1 || model.SelectedStateId < 1 || model.SelectedDistrictId < 1 || model.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(CompanyProfile));
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (model.ClientCompanyId < 1)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction(nameof(CompanyProfile));
                }
                var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(CompanyProfile));
                }

                var existCompany = await _context.ClientCompany.Include(c => c.Country).FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (model.Document is not null)
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(model.Document, model.Email, "user");
                    existCompany.DocumentUrl = relativePath;
                    using var dataStream = new MemoryStream();
                    model.Document.CopyTo(dataStream);
                    existCompany.DocumentImage = dataStream.ToArray();
                    existCompany.DocumentImageExtension = Path.GetExtension(fileName);
                }

                existCompany.CountryId = model.CountryId;
                existCompany.StateId = model.StateId;
                existCompany.DistrictId = model.DistrictId;
                existCompany.PinCodeId = model.PinCodeId;
                existCompany.Name = WebUtility.HtmlEncode(model.Name);
                //existCompany.Code = clientCompany.Code;
                existCompany.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber.TrimStart('0'));
                existCompany.Branch = WebUtility.HtmlEncode(model.Branch);
                existCompany.BankName = WebUtility.HtmlEncode(model.BankName);
                existCompany.BankAccountNumber = WebUtility.HtmlEncode(model.BankAccountNumber);
                existCompany.IFSCCode = WebUtility.HtmlEncode(model.IFSCCode.ToUpper());
                existCompany.Addressline = WebUtility.HtmlEncode(model.Addressline);
                //existCompany.Description = clientCompany.Description;

                existCompany.PinCodeId = model.SelectedPincodeId;
                existCompany.DistrictId = model.SelectedDistrictId;
                existCompany.StateId = model.SelectedStateId;
                existCompany.CountryId = model.SelectedCountryId;

                existCompany.Updated = DateTime.Now;
                existCompany.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ClientCompany.Update(existCompany);
                await _context.SaveChangesAsync();
                string message = "Company edited. \nDomain : " + model.Email + "\n" + baseUrl;
                await smsService.DoSendSmsAsync(existCompany.Country.Code, existCompany.Country.ISDCode + existCompany.PhoneNumber, message);
            }

            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(CompanyProfile));
            }
            notifyService.Custom($"Company <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
            return RedirectToAction(nameof(CompanyController.CompanyProfile), "Company");
        }

        private async Task Load(ClientCompany model)
        {
            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
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
        public async Task<IActionResult> CreateUser()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = await _context.ClientCompany.Include(c => c.Country).FirstOrDefaultAsync(v => v.ClientCompanyId == companyUser.ClientCompanyId);
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
        private async Task LoadModel(ClientCompanyApplicationUser model, string currentUserEmail)
        {
            var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var company = await _context.ClientCompany.Include(c => c.Country).FirstOrDefaultAsync(v => v.ClientCompanyId == companyUser.ClientCompanyId);
            var existingUsers = _context.ClientCompanyApplicationUser.Where(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
            var isManagerTaken = await existingUsers.AnyAsync(u => u.UserRole == CompanyRole.MANAGER);
            var availableRoles = Enum.GetValues(typeof(CompanyRole))
                .Cast<CompanyRole>()
                .Where(role => role != CompanyRole.COMPANY_ADMIN && (isManagerTaken ? role != CompanyRole.MANAGER : true))
                .Select(role => new SelectListItem
                {
                    Value = role.ToString(),
                    Text = role.ToString()
                })
                .ToList();

            model.ClientCompany = company;
            model.Country = company.Country;
            model.CountryId = company.CountryId;

            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.AvailableRoles = availableRoles;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(ClientCompanyApplicationUser model, string emailSuffix)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (!ModelState.IsValid)
                {
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model, currentUserEmail);
                    return View(model);
                }
                if (!Regex.IsMatch(emailSuffix, vallidDomainExpression))
                {
                    ModelState.AddModelError("", "Invalid domain address.");
                    await LoadModel(model, currentUserEmail);
                    return View(model);
                }
                emailSuffix = WebUtility.HtmlEncode(emailSuffix.Trim().ToLower());
                model.Email = WebUtility.HtmlEncode(model.Email.Trim().ToLower());
                var userFullEmail = model.Email.Trim().ToLower() + "@" + emailSuffix;
                var userExist = await appUserManager.Users.AnyAsync(u => u.Email == userFullEmail && !u.Deleted);
                if (userExist)
                {
                    notifyService.Error($"User with email {userFullEmail} already exists.");
                    ModelState.AddModelError(nameof(model.Email), "Email already in use.");
                    await LoadModel(model, currentUserEmail);
                    return View(model);
                }
                if (model.ProfileImage == null || model.ProfileImage.Length == 0)
                {
                    notifyService.Error("No Profile Image.");
                    ModelState.AddModelError(nameof(model.ProfileImage), "No Profile Image.");
                    await LoadModel(model, currentUserEmail);
                    return View(model);
                }
                if (model.ProfileImage.Length > MAX_FILE_SIZE)
                {
                    notifyService.Error($"Document image Size exceeds the max size: 5MB");
                    ModelState.AddModelError(nameof(model.ProfileImage), "File too large.");
                    await LoadModel(model, currentUserEmail);
                    return View(model);
                }

                var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                if (!AllowedExt.Contains(ext))
                {
                    notifyService.Error($"Invalid Document image type");
                    ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file type.");
                    await LoadModel(model, currentUserEmail);
                    return View(model);
                }

                if (!AllowedMime.Contains(model.ProfileImage.ContentType))
                {
                    notifyService.Error($"Invalid Document Image content type");
                    ModelState.AddModelError(nameof(model.ProfileImage), "Invalid Document Image  content type.");
                    await LoadModel(model, currentUserEmail);
                    return View(model);
                }

                if (!ImageSignatureValidator.HasValidSignature(model.ProfileImage))
                {
                    notifyService.Error($"Invalid or corrupted Document Image ");
                    ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file content.");
                    await LoadModel(model, currentUserEmail);
                    return View(model);
                }

                if (model.SelectedCountryId < 1 || model.SelectedStateId < 1 || model.SelectedDistrictId < 1 || model.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(CreateUser), "Company");
                }

                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(model.ProfileImage, emailSuffix, "user");
                    using var dataStream = new MemoryStream();
                    model.ProfileImage.CopyTo(dataStream);
                    model.ProfilePicture = dataStream.ToArray();
                    model.ProfilePictureUrl = relativePath;
                    model.ProfilePictureExtension = Path.GetExtension(fileName);
                }
                //DEMO
                model.Active = true;
                model.Password = Applicationsettings.TestingData;
                model.Email = userFullEmail;
                model.EmailConfirmed = true;
                model.UserName = userFullEmail;
                model.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber.TrimStart('0'));
                model.FirstName = WebUtility.HtmlEncode(model.FirstName);
                model.LastName = WebUtility.HtmlEncode(model.LastName);
                model.Addressline = WebUtility.HtmlEncode(model.Addressline);
                model.CountryId = model.SelectedCountryId;
                model.StateId = model.SelectedStateId;
                model.DistrictId = model.SelectedDistrictId;
                model.PinCodeId = model.SelectedPincodeId;

                model.Updated = DateTime.Now;
                model.UpdatedBy = HttpContext.User?.Identity?.Name;
                model.Role = (AppRoles)Enum.Parse(typeof(AppRoles), model.UserRole.ToString());
                model.IsClientAdmin = model.UserRole == CompanyRole.COMPANY_ADMIN;
                IdentityResult result = await userManager.CreateAsync(model, model.Password);

                if (result.Succeeded)
                {
                    var roles = await userManager.GetRolesAsync(model);
                    var roleResult = await userManager.RemoveFromRolesAsync(model, roles);
                    roleResult = await userManager.AddToRolesAsync(model, new List<string> { model.UserRole.ToString() });
                    var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.CountryId);
                    notifyService.Custom($"User <b>{model.Email}</b> created successfully.", 3, "green", "fas fa-user-plus");
                    string message = "User created . \nEmail : " + model.Email + "\n" + baseUrl;
                    await smsService.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, message);
                    return RedirectToAction(nameof(CompanyController.Users), "Company");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);

            }
            notifyService.Error("OOPs !!!.Error creating user, Try again");
            return RedirectToAction(nameof(CreateUser), "Company");
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
        public async Task<IActionResult> EditUser(string id, ClientCompanyApplicationUser model)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (!ModelState.IsValid)
                {
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model, currentUserEmail);
                    return View(model);
                }
                if (model.ProfileImage is not null)
                {
                    if (model.ProfileImage.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        ModelState.AddModelError(nameof(model.ProfileImage), "File too large.");
                        await LoadModel(model, currentUserEmail);
                        return View(model);
                    }

                    var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file type.");
                        await LoadModel(model, currentUserEmail);
                        return View(model);
                    }

                    if (!AllowedMime.Contains(model.ProfileImage.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid Document Image  content type.");
                        await LoadModel(model, currentUserEmail);
                        return View(model);
                    }

                    if (!ImageSignatureValidator.HasValidSignature(model.ProfileImage))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file content.");
                        await LoadModel(model, currentUserEmail);
                        return View(model);
                    }
                }
                if (model.SelectedCountryId < 1 || model.SelectedStateId < 1 || model.SelectedDistrictId < 1 || model.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(EditUser), "Company", new { userid = id });
                }

                if (id != model.Id.ToString())
                {
                    notifyService.Error("USER NOT FOUND!");
                    return RedirectToAction(nameof(CompanyController.Users), "Company");
                }
                var user = await userManager.FindByIdAsync(id);
                if (model?.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var domain = model.Email.Split('@')[1];
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(model.ProfileImage, domain, "user");

                    model.ProfilePictureUrl = relativePath;
                    using var dataStream = new MemoryStream();
                    model.ProfileImage.CopyTo(dataStream);
                    model.ProfilePicture = dataStream.ToArray();
                    model.ProfilePictureExtension = Path.GetExtension(fileName);
                }

                if (user != null)
                {
                    user.ProfilePictureUrl = model?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                    user.ProfilePicture = model?.ProfilePicture ?? user.ProfilePicture;
                    user.ProfilePictureExtension = model?.ProfilePictureExtension ?? user.ProfilePictureExtension;
                    user.PhoneNumber = WebUtility.HtmlEncode(model?.PhoneNumber.TrimStart('0')) ?? user.PhoneNumber;
                    user.FirstName = WebUtility.HtmlEncode(model?.FirstName);
                    user.LastName = WebUtility.HtmlEncode(model?.LastName);
                    if (!string.IsNullOrWhiteSpace(model?.Password))
                    {
                        user.Password = model.Password;
                    }
                    user.Active = model.Active;
                    user.Addressline = WebUtility.HtmlEncode(model.Addressline);

                    user.CountryId = model.SelectedCountryId;
                    user.StateId = model.SelectedStateId;
                    user.DistrictId = model.SelectedDistrictId;
                    user.PinCodeId = model.SelectedPincodeId;

                    user.IsUpdated = true;
                    user.Updated = DateTime.Now;
                    user.Comments = model.Comments;
                    user.PhoneNumber = model.PhoneNumber.TrimStart('0');
                    user.UserRole = model.UserRole;
                    user.Role = model.Role != null ? model.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                    user.IsClientAdmin = user.UserRole == CompanyRole.COMPANY_ADMIN;
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.Now.ToString();
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        var roleResult = await userManager.RemoveFromRolesAsync(user, roles);
                        await userManager.AddToRoleAsync(user, user.UserRole.ToString());
                        var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == user.CountryId);

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
                    notifyService.Error("OOPS !!!..Agency Not Found");
                    return RedirectToAction("EmpanelledVendors", "Vendors");
                }

                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Agency Not Found");
                    return RedirectToAction("EmpanelledVendors", "Vendors");
                }
                //vendor.SelectedByCompany = true; // THIS IS TO NOT SHOW EDIT PROFILE 
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
                notifyService.Error("OOPS !!!..Agency Not Found");
                return RedirectToAction("EmpanelledVendors", "Vendors");
            }

        }
        private async Task Load(Vendor model)
        {
            var vendor = await _context.Vendor.Include(c => c.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAgency(long vendorId, Vendor model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await Load(model);
                    return View(model);
                }
                if (model.Document is not null)
                {
                    if (model.Document.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        ModelState.AddModelError(nameof(model.Document), "File too large.");
                        await Load(model);
                        return View(model);
                    }

                    var ext = Path.GetExtension(model.Document.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        ModelState.AddModelError(nameof(model.Document), "Invalid file type.");
                        await Load(model);
                        return View(model);
                    }

                    if (!AllowedMime.Contains(model.Document.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        ModelState.AddModelError(nameof(model.Document), "Invalid Document Image  content type.");
                        await Load(model);
                        return View(model);
                    }

                    if (!ImageSignatureValidator.HasValidSignature(model.Document))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        ModelState.AddModelError(nameof(model.Document), "Invalid file content.");
                        await Load(model);
                        return View(model);
                    }
                }
                if (vendorId != model.VendorId || model.SelectedCountryId < 1 || model.SelectedStateId < 1 || model.SelectedDistrictId < 1 || model.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(Edit), "Vendors", new { id = vendorId });
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (model.Document is not null)
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(model.Document, model.Email, "user");

                    using var dataStream = new MemoryStream();
                    model.Document.CopyTo(dataStream);
                    model.DocumentImage = dataStream.ToArray();

                    model.DocumentImageExtension = Path.GetExtension(fileName);
                    model.DocumentUrl = relativePath;
                }
                else
                {
                    var existingVendor = await _context.Vendor.AsNoTracking().FirstOrDefaultAsync(c => c.VendorId == vendorId);
                    if (existingVendor.DocumentImage != null || existingVendor.DocumentUrl != null)
                    {
                        model.DocumentImage = existingVendor.DocumentImage;
                        model.DocumentUrl = existingVendor.DocumentUrl;
                    }
                }
                model.IsUpdated = true;
                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber.TrimStart('0'));
                model.Name = WebUtility.HtmlEncode(model.Name);
                model.Addressline = WebUtility.HtmlEncode(model.Addressline);
                model.PinCodeId = model.SelectedPincodeId;
                model.DistrictId = model.SelectedDistrictId;
                model.StateId = model.SelectedStateId;
                model.CountryId = model.SelectedCountryId;
                var pinCode = await _context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefaultAsync(s => s.PinCodeId == model.SelectedPincodeId);

                var companyAddress = model.Addressline + ", " + pinCode.District.Name + ", " + pinCode.State.Name + ", " + pinCode.Country.Code;
                var companyCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
                var companyLatLong = companyCoordinates.Latitude + "," + companyCoordinates.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={companyLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                model.AddressLatitude = companyCoordinates.Latitude;
                model.AddressLongitude = companyCoordinates.Longitude;
                model.AddressMapLocation = url;

                _context.Vendor.Update(model);
                var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.CountryId);

                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, "Agency edited. \n Domain : " + model.Email + "\n" + baseUrl);

                var saved = await _context.SaveChangesAsync();
                notifyService.Custom($"Agency <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");

            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Error editing agency. Try again.");
            }
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
        public async Task<IActionResult> CreateAgencyUser(long id)
        {
            if (id <= 0)
            {
                notifyService.Error("OOPS !!!..Error creating user");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var allRoles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>().Where(r => r != AgencyRole.AGENCY_ADMIN).ToList();
            var vendor = await _context.Vendor.AsNoTracking().Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
            if (vendor == null)
            {
                notifyService.Error("OOPS !!!..Agency Not Found. Try again.");
                return RedirectToAction(nameof(AgencyDetail), "Company", new { id = id });
            }

            var model = new VendorApplicationUser { Country = vendor.Country, CountryId = vendor.CountryId, Vendor = vendor, AgencyRole = allRoles, };

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
        public async Task<IActionResult> CreateAgencyUser(VendorApplicationUser model, string emailSuffix)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model);
                    return View(model);
                }
                if (!Regex.IsMatch(emailSuffix, vallidDomainExpression))
                {
                    ModelState.AddModelError("", "Invalid domain address.");
                    await LoadModel(model);
                    return View(model);
                }
                emailSuffix = WebUtility.HtmlEncode(emailSuffix.Trim().ToLower());
                model.Email = WebUtility.HtmlEncode(model.Email.Trim().ToLower());

                var userFullEmail = model.Email.Trim().ToLower() + "@" + emailSuffix;
                var userExist = await appUserManager.Users.AnyAsync(u => u.Email == userFullEmail && !u.Deleted);
                if (userExist)
                {
                    notifyService.Error($"User with email {userFullEmail} already exists.");
                    await LoadModel(model);
                    return View(model);
                }

                if (model.ProfileImage == null || model.ProfileImage.Length == 0)
                {
                    notifyService.Error("Invalid ProfileImage Image ");
                    await LoadModel(model);
                    return View(model);
                }
                if (model.ProfileImage.Length > MAX_FILE_SIZE)
                {
                    notifyService.Error($"Document image Size exceeds the max size: 5MB");
                    ModelState.AddModelError(nameof(model.ProfileImage), "File too large.");
                    await LoadModel(model);
                    return View(model);
                }

                var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                if (!AllowedExt.Contains(ext))
                {
                    notifyService.Error($"Invalid Document image type");
                    ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file type.");
                    await LoadModel(model);
                    return View(model);
                }

                if (!AllowedMime.Contains(model.ProfileImage.ContentType))
                {
                    notifyService.Error($"Invalid Document Image content type");
                    ModelState.AddModelError(nameof(model.ProfileImage), "Invalid Document Image  content type.");
                    await LoadModel(model);
                    return View(model);
                }

                if (!ImageSignatureValidator.HasValidSignature(model.ProfileImage))
                {
                    notifyService.Error($"Invalid or corrupted Document Image ");
                    ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file content.");
                    await LoadModel(model);
                    return View(model);
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(model.ProfileImage, emailSuffix, "user");

                    model.ProfilePictureUrl = relativePath;
                    model.ProfilePictureExtension = Path.GetExtension(fileName);

                    using var dataStream = new MemoryStream();
                    model.ProfileImage.CopyTo(dataStream);
                    model.ProfilePicture = dataStream.ToArray();
                }

                model.Password = Applicationsettings.TestingData; //DEMO
                model.Email = userFullEmail;
                model.EmailConfirmed = true;
                model.UserName = userFullEmail;
                model.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber.TrimStart('0'));
                model.Addressline = WebUtility.HtmlEncode(model.Addressline);
                model.PinCodeId = model.SelectedPincodeId;
                model.DistrictId = model.SelectedDistrictId;
                model.StateId = model.SelectedStateId;
                model.CountryId = model.SelectedCountryId;

                model.Role = (AppRoles)Enum.Parse(typeof(AppRoles), model.UserRole.ToString());
                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.IsVendorAdmin = model.UserRole == AgencyRole.AGENCY_ADMIN;
                if (model.Role == AppRoles.AGENT)
                {
                    var pincode = await _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(c => c.PinCodeId == model.PinCodeId);
                    var userAddress = $"{model.Addressline}, {pincode.Name}, {pincode.District.Name}, {pincode.State.Name}, {pincode.Country.Name}";
                    var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(userAddress);
                    var customerLatLong = coordinates.Latitude + "," + coordinates.Longitude;
                    model.AddressLatitude = coordinates.Latitude;
                    model.AddressLongitude = coordinates.Longitude;
                    model.AddressMapLocation = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                }
                model.Id = 0;
                IdentityResult result = await userAgencyManager.CreateAsync(model, model.Password);

                if (result.Succeeded)
                {
                    var roleResult = await userAgencyManager.AddToRolesAsync(model, new List<string> { model.UserRole.ToString() });
                    var roles = await userAgencyManager.GetRolesAsync(model);
                    var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
                    if (!model.Active)
                    {
                        var createdUser = await userAgencyManager.FindByEmailAsync(model.Email);
                        var lockUser = await userAgencyManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userAgencyManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, "Agency user created. \nEmail : " + model.Email + "\n" + baseUrl);
                            notifyService.Custom($"User created.", 3, "orange", "fas fa-user-lock");
                        }
                    }
                    else
                    {
                        await smsService.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, "Agency user created. \nEmail : " + model.Email + "\n" + baseUrl);

                        var onboardAgent = roles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(model.MobileUId);

                        if (onboardAgent)
                        {
                            var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == model.VendorId);

                            string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

                            var message = $"Dear {model.FirstName}\n" +
                            $"Click on link below to install the mobile app\n\n" +
                            $"{tinyUrl}\n\n" +
                            $"Thanks\n\n" +
                            $"{baseUrl}";

                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, message, true);
                            notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                        }
                        else
                        {
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, "Agency user created. \nEmail : " + model.Email + "\n" + baseUrl);
                        }
                        notifyService.Custom($"User <b>{model.Email}</b> created successfully.", 3, "green", "fas fa-user-plus");
                    }
                    return RedirectToAction(nameof(AgencyDetail), "Company", new { id = model.VendorId });
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

        private async Task LoadModel(VendorApplicationUser model)
        {
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);
            var nonAdminRoles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>().Where(r => r != AgencyRole.AGENCY_ADMIN).ToList();
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.Vendor = vendor;
            model.AgencyRole = nonAdminRoles;
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

                var vendorApplicationUser = await _context.VendorApplicationUser.Include(u => u.Vendor).Include(v => v.Country).FirstOrDefaultAsync(v => v.Id == userId);

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAgencyUser(string id, VendorApplicationUser model, string editby)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Custom($"Correct the error(s)", 3, "red", "fas fa-building");
                    await LoadModel(model);
                    return View(model);
                }
                if (model.ProfileImage is not null)
                {
                    if (model.ProfileImage.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        ModelState.AddModelError(nameof(model.ProfileImage), "File too large.");
                        await LoadModel(model);
                        return View(model);
                    }

                    var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file type.");
                        await LoadModel(model);
                        return View(model);
                    }

                    if (!AllowedMime.Contains(model.ProfileImage.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid Document Image  content type.");
                        await LoadModel(model);
                        return View(model);
                    }

                    if (!ImageSignatureValidator.HasValidSignature(model.ProfileImage))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file content.");
                        await LoadModel(model);
                        return View(model);
                    }
                }
                if (model.SelectedCountryId < 1 || model.SelectedStateId < 1 || model.SelectedDistrictId < 1 || model.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(EditUser), "Vendors", new { userid = id });
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var user = await userAgencyManager.FindByIdAsync(id);
                if (user == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (model?.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var domain = model.Email.Split('@')[1];
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(model.ProfileImage, domain, "user");
                    using var dataStream = new MemoryStream();
                    model.ProfileImage.CopyTo(dataStream);
                    model.ProfilePicture = dataStream.ToArray();

                    model.ProfilePictureUrl = relativePath;
                    model.ProfilePictureExtension = Path.GetExtension(fileName);
                }
                user.ProfilePicture = model?.ProfilePicture ?? user.ProfilePicture;
                user.ProfilePictureUrl = model?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                user.ProfilePictureExtension = model?.ProfilePictureExtension ?? user.ProfilePictureExtension;
                user.PhoneNumber = model?.PhoneNumber ?? user.PhoneNumber;
                user.PhoneNumber = WebUtility.HtmlEncode(user.PhoneNumber.TrimStart('0'));
                user.FirstName = WebUtility.HtmlEncode(model?.FirstName);
                user.LastName = WebUtility.HtmlEncode(model?.LastName);
                if (!string.IsNullOrWhiteSpace(model?.Password))
                {
                    user.Password = model.Password;
                }
                user.Addressline = WebUtility.HtmlEncode(model.Addressline);
                user.Active = model.Active;

                user.PinCodeId = model.SelectedPincodeId;
                user.DistrictId = model.SelectedDistrictId;
                user.StateId = model.SelectedStateId;
                user.CountryId = model.SelectedCountryId;

                user.IsUpdated = true;
                user.Updated = DateTime.Now;
                user.Comments = WebUtility.HtmlEncode(model.Comments);
                user.UpdatedBy = currentUserEmail;
                user.SecurityStamp = DateTime.Now.ToString();
                user.UserRole = model.UserRole;
                user.IsVendorAdmin = user.UserRole == null;
                user.Role = model.Role != null ? model.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());

                if (user.Role == AppRoles.AGENT)
                {
                    var pincode = await _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(c => c.PinCodeId == user.PinCodeId);
                    var userAddress = $"{user.Addressline}, {pincode.Name}, {pincode.District.Name}, {pincode.State.Name}, {pincode.Country.Name}";
                    var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(userAddress);
                    var customerLatLong = coordinates.Latitude + "," + coordinates.Longitude;
                    user.AddressLatitude = coordinates.Latitude;
                    user.AddressLongitude = coordinates.Longitude;
                    user.AddressMapLocation = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                }
                var result = await userAgencyManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var roles = await userAgencyManager.GetRolesAsync(user);
                    var roleResult = await userAgencyManager.RemoveFromRolesAsync(user, roles);
                    await userAgencyManager.AddToRoleAsync(user, user.UserRole.ToString());
                    var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == user.CountryId);
                    if (!user.Active)
                    {
                        var createdUser = await userAgencyManager.FindByEmailAsync(user.Email);
                        var lockUser = await userAgencyManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userAgencyManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user edited and locked. \nEmail : " + user.Email + "\n" + baseUrl);
                            notifyService.Custom($"User <b>{user.Email}<b> edited and locked.", 3, "orange", "fas fa-user-lock");
                        }
                    }
                    else
                    {
                        var createdUser = await userAgencyManager.FindByEmailAsync(user.Email);
                        var lockUser = await userAgencyManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userAgencyManager.SetLockoutEndDateAsync(createdUser, DateTime.Now);
                        var onboardAgent = createdUser.Role == AppConstant.AppRoles.AGENT && string.IsNullOrWhiteSpace(user.MobileUId);
                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            if (onboardAgent)
                            {
                                var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == user.VendorId);
                                string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

                                var message = $"Dear {user.FirstName}\n";
                                message += $"Click on link below to install the mobile app\n\n";
                                message += $"{tinyUrl}\n\n";
                                message += $"Thanks\n\n";
                                message += $"{baseUrl}";
                                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, message, true);
                                notifyService.Custom($"Agent onboarding initiated.", 3, "orange", "fas fa-user-check");
                            }
                            else
                            {
                                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user edited.\n Email : " + user.Email + "\n" + baseUrl);
                                notifyService.Custom($"User <b>{user.Email}<b> edited successfully.", 3, "orange", "fas fa-user-check");
                            }
                        }
                    }
                }
                else
                {
                    notifyService.Error("OOPS !!!..Error Editing User, Try again");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Error Editing User, Try again");
            }
            if (editby == "company")
            {
                return RedirectToAction(nameof(Users), "Vendors", new { id = model.VendorId });
            }
            else if (editby == "empanelled")
            {
                return RedirectToAction(nameof(CompanyController.AgencyUsers), "Company", new { id = model.VendorId });
            }
            else
            {
                return RedirectToAction(nameof(AgencyController.Users), "Agency");
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
        public async Task<IActionResult> CreateService(long id)
        {
            try
            {
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
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
            try
            {
                //if (!ModelState.IsValid)
                //{
                //    notifyService.Error("Please correct the errors");
                //    return View(service);
                //}
                if (service == null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 || (service.SelectedDistrictIds.Count <= 0) || VendorId < 1)
                {
                    notifyService.Custom("OOPs !!!..Invalid Data.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = VendorId });
                }
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
        public async Task<IActionResult> EditService(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("OOPs !!!..Agency Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
            try
            {
                //if (!ModelState.IsValid)
                //{
                //    notifyService.Error("Please correct the errors");
                //    return View(service);
                //}
                if (VendorInvestigationServiceTypeId != service.VendorInvestigationServiceTypeId || service is null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 ||
                    (service.SelectedDistrictIds.Count <= 0) || VendorId < 1)
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(EditService), "VendorService", new { id = VendorInvestigationServiceTypeId });
                }
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
                var currentUser = await _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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

            try
            {
                if (!ModelState.IsValid || vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("No agency selected !!!");
                    return RedirectToAction(nameof(AvailableVendors), "Company");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = await _context.ClientCompany.Include(c => c.EmpanelledVendors).FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

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

                if (!ModelState.IsValid || vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("OOPs !!!..Not Agency Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = await _context.ClientCompany
                    .Include(c => c.EmpanelledVendors)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
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