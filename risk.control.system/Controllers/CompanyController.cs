using System.ComponentModel.DataAnnotations;
using System.Reflection;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class CompanyController : Controller
    {
        private AppRoles[] companyRoles = new[]
                {
                    AppRoles.COMPANY_ADMIN,
                    AppRoles.CREATOR,
                    AppRoles.MANAGER,
                    AppRoles.ASSESSOR
                };
        private AppRoles[] agencyRoles = new[]
                {
                    AppRoles.AGENCY_ADMIN,
                    AppRoles.SUPERVISOR,
                    AppRoles.AGENT
                };
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ICompanyService companyService;
        private readonly IVendorServiceTypeManager vendorServiceTypeManager;
        private readonly IAgencyUserCreateEditService agencyUserCreateEditService;
        private readonly IAgencyCreateEditService agencyCreateEditService;
        private readonly ICompanyUserService companyUserService;
        private readonly IFeatureManager featureManager;
        private readonly IInvestigationService service;
        private readonly ILogger<CompanyController> logger;
        private readonly string portal_base_url;
        public CompanyController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICompanyService companyService,
            IVendorServiceTypeManager vendorServiceTypeManager,
            IAgencyUserCreateEditService agencyUserCreateEditService,
            IAgencyCreateEditService agencyCreateEditService,
            ICompanyUserService companyUserService,
            INotyfService notifyService,
            IFeatureManager featureManager,
            IInvestigationService service,
             IHttpContextAccessor httpContextAccessor,
            ILogger<CompanyController> logger,
            ISmsService SmsService)
        {
            this._context = context;
            this.userManager = userManager;
            this.companyService = companyService;
            this.vendorServiceTypeManager = vendorServiceTypeManager;
            this.agencyUserCreateEditService = agencyUserCreateEditService;
            this.agencyCreateEditService = agencyCreateEditService;
            this.companyUserService = companyUserService;
            this.notifyService = notifyService;
            this.featureManager = featureManager;
            this.service = service;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
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

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
                logger.LogError(ex, "Error getting Company for {UserName}.", HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting Company. Try again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Edit Company", FromAction = "CompanyProfile")]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
                logger.LogError(ex, "Error getting Company for {UserName}.", HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting Company. Try again");
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
                var userEmail = HttpContext.User?.Identity?.Name;

                var result = await companyService.EditAsync(userEmail, model, portal_base_url);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await Load(model);
                    return View(model);
                }
                notifyService.Custom($"Company <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {CompanyId} for {UserName}.",model.ClientCompanyId, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error editing Company. Try again.");
                return RedirectToAction(nameof(CompanyProfile));
            }
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

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
                var usersInCompany = _context.ApplicationUser.Where(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
                bool isManagerTaken = false;

                foreach (var user in usersInCompany)
                {
                    if (await userManager.IsInRoleAsync(user, MANAGER.DISPLAY_NAME))
                    {
                        isManagerTaken = true;
                        break;
                    }
                }

                var availableRoles = companyRoles
                    .Where(r => r != AppRoles.COMPANY_ADMIN && r != AppRoles.MANAGER || !isManagerTaken) // Exclude MANAGER if already taken
                    .Select(r => new SelectListItem
                    {
                        Value = r.ToString(),
                        Text = r.GetType()
                                .GetMember(r.ToString())
                                .First()
                                .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                    })
                    .ToList();


                var model = new ApplicationUser { Country = company.Country, ClientCompany = company, CountryId = company.CountryId, AvailableRoles = availableRoles };
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating user for {UserName}.", HttpContext.User?.Identity?.Name);
                notifyService.Error("Error creating user. Try again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        private async Task LoadModel(ApplicationUser model, string currentUserEmail)
        {
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var company = await _context.ClientCompany.Include(c => c.Country).FirstOrDefaultAsync(v => v.ClientCompanyId == companyUser.ClientCompanyId);
            var usersInCompany = _context.ApplicationUser.Where(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
            bool isManagerTaken = false;

            foreach (var user in usersInCompany)
            {
                if (await userManager.IsInRoleAsync(user, MANAGER.DISPLAY_NAME))
                {
                    isManagerTaken = true;
                    break;
                }
            }

            var availableRoles = companyRoles
                .Where(r => r != AppRoles.COMPANY_ADMIN && r != AppRoles.MANAGER || !isManagerTaken) // Exclude MANAGER if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
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
        public async Task<IActionResult> CreateUser(ApplicationUser model, string emailSuffix)
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadModel(model, userEmail);
                    return View(model);
                }
                var result = await companyUserService.CreateAsync(model, emailSuffix, userEmail, portal_base_url);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }

                    notifyService.Error("Correct the highlighted errors.");
                    await LoadModel(model, userEmail);
                    return View(model); // 🔥 fields now highlight
                }

                notifyService.Success(result.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating user for {UserName}.", HttpContext.User?.Identity?.Name);
                notifyService.Error("Error creating user. Try again");
            }
            return RedirectToAction(nameof(CompanyController.Users), "Company");
        }

        [Breadcrumb("Edit User", FromAction = "Users")]
        public async Task<IActionResult> EditUser(long? userId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (userId == null || _context.ApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Users));
                }

                var clientCompanyApplicationUser = await _context.ApplicationUser
                    .Include(u => u.Country).
                    Include(u => u.ClientCompany)
                    .FirstOrDefaultAsync(c => c.Id == userId);

                if (clientCompanyApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Users));
                }
                var usersInCompany = _context.ApplicationUser.Where(c => !c.Deleted && c.ClientCompanyId == clientCompanyApplicationUser.ClientCompanyId && c.Id != clientCompanyApplicationUser.Id);
                bool isManagerTaken = false;

                foreach (var user in usersInCompany)
                {
                    if (await userManager.IsInRoleAsync(user, MANAGER.DISPLAY_NAME))
                    {
                        isManagerTaken = true;
                        break;
                    }
                }

                var availableRoles = companyRoles
                    .Where(r => r != AppRoles.COMPANY_ADMIN && r != AppRoles.MANAGER || !isManagerTaken) // Exclude MANAGER if already taken
                    .Select(r => new SelectListItem
                    {
                        Value = r.ToString(),
                        Text = r.GetType()
                                .GetMember(r.ToString())
                                .First()
                                .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                    })
                    .ToList();

                clientCompanyApplicationUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !clientCompanyApplicationUser.IsPasswordChangeRequired : true;
                clientCompanyApplicationUser.AvailableRoles = availableRoles;
                return View(clientCompanyApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {UserId} for {UserName}.",userId, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error editing user. Try again");
            }
            return RedirectToAction(nameof(CompanyController.Users), "Company");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(long id, ApplicationUser model)
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;

                if (!ModelState.IsValid)
                {
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model, userEmail);
                    return View(model);
                }
                var result = await companyUserService.UpdateAsync(id, model, User.Identity?.Name, portal_base_url);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }

                    notifyService.Error("Correct the highlighted errors.");
                    await LoadModel(model, User.Identity?.Name);
                    return View(model); // 🔥 fields now highlight
                }

                notifyService.Success(result.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {UserId} for {UserName}.", id, HttpContext.User?.Identity?.Name);
                notifyService.Error($"Error to create Company user. Try again.", 3);
            }
            return RedirectToAction(nameof(CompanyController.Users), "Company");
        }

        [Breadcrumb(title: " Delete", FromAction = "Users")]
        public async Task<IActionResult> DeleteUser(long userId)
        {
            try
            {
                if (userId < 1 || userId == 0)
                {
                    notifyService.Error("Invalid Data. Try Again");
                    return RedirectToAction(nameof(Users));
                }

                var model = await _context.ApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode)
                    .FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("User Not Found.Try Again");
                    return RedirectToAction(nameof(Users));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {UserId} for {UserName}.", userId, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting user. Try again");
                return RedirectToAction(nameof(Users));
            }

        }
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string email)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var model = await _context.ApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode)
                    .FirstOrDefaultAsync(c => c.Email == email);
                if (model == null)
                {
                    notifyService.Error("Error getting user. Try again");
                    return RedirectToAction(nameof(Users));
                }

                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.Deleted = true;
                _context.ApplicationUser.Update(model);
                await _context.SaveChangesAsync();
                notifyService.Custom($"User <b>{model.Email}</b> deleted successfully", 3, "orange", "fas fa-user-minus");
                return RedirectToAction(nameof(CompanyController.Users), "Company");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting {UserId} for {UserName}.", email, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting user. Try again");
                return RedirectToAction(nameof(Users));
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
                var userEmail = HttpContext.User?.Identity?.Name;

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
                var vendorUserCount = await _context.ApplicationUser.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted);
                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

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
                logger.LogError(ex, "Error getting {AgencyId} for {UserName}.", id, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting Agency. Try again");
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
                logger.LogError(ex, "Error getting {AgencyId} for {UserName}.", id, HttpContext.User?.Identity?.Name);
                notifyService.Error("Agency Not Found. Try again.");
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
                var userEmail = HttpContext.User?.Identity?.Name;

                var result = await agencyCreateEditService.EditAsync(userEmail, model, portal_base_url);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await Load(model);
                    return View(model);
                }
                notifyService.Custom($"Agency <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {AgencyId} for {UserName}.", vendorId, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error editing agency. Try again.");
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
            var availableRoles = agencyRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN) // Exclude MANAGER if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            var vendor = await _context.Vendor.AsNoTracking().Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
            if (vendor == null)
            {
                notifyService.Error("OOPS !!!..Agency Not Found. Try again.");
                return RedirectToAction(nameof(AgencyDetail), "Company", new { id = id });
            }

            var model = new ApplicationUser { Country = vendor.Country, CountryId = vendor.CountryId, Vendor = vendor, AvailableRoles = availableRoles, };

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
        public async Task<IActionResult> CreateAgencyUser(ApplicationUser model, string emailSuffix)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model);
                    return View(model);
                }
                model.Id = 0; // Ensure Id is 0 for new user
                var vendorUserModel = new CreateVendorUserRequest
                {
                    User = model,
                    EmailSuffix = emailSuffix,
                    CreatedBy = User?.Identity?.Name
                };
                var result = await agencyUserCreateEditService.CreateVendorUserAsync(vendorUserModel, ModelState, portal_base_url);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);
                    notifyService.Error(result.Message);
                    await LoadModel(model);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error Creating AgencyUser by {UserName}.",HttpContext.User?.Identity?.Name);
                notifyService.Error("OOPS !!!..Error Creating User. Try again.");
            }
            return RedirectToAction(nameof(AgencyUsers), "Company", new { id = model.VendorId });
        }

        private async Task LoadModel(ApplicationUser model)
        {
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);
            var availableRoles = agencyRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN) // Exclude MANAGER if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.Vendor = vendor;
            model.AvailableRoles = availableRoles;
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

                var vendorApplicationUser = await _context.ApplicationUser.Include(u => u.Vendor).Include(v => v.Country).FirstOrDefaultAsync(v => v.Id == userId);

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
                logger.LogError(ex, "Error getting {AgencyId} by {UserName}.",userId, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting user. Try again");
                return RedirectToAction(nameof(AgencyUsers));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAgencyUser(string id, ApplicationUser model, string editby)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model);
                    return View(model);
                }
                var result = await agencyUserCreateEditService.EditVendorUserAsync(new EditVendorUserRequest
                {
                    UserId = id,
                    Model = model,
                    UpdatedBy = User?.Identity?.Name
                },
                ModelState, portal_base_url);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);
                    notifyService.Error(result.Message);
                    await LoadModel(model);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {AgencyId} by {UserName}.", id, HttpContext.User?.Identity?.Name);
                notifyService.Error("OOPS !!!..Error editing User. Try again.");
            }
            if (editby == "company")
            {
                return RedirectToAction(nameof(Users), "Vendors", new { id = model.VendorId });
            }
            else
            {
                return RedirectToAction(nameof(CompanyController.AgencyUsers), "Company", new { id = model.VendorId });
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

                var model = await _context.ApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == userId);
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
                logger.LogError(ex, "Error getting {AgencyId} by {UserName}.", userId, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting user. Try again");
                return RedirectToAction(nameof(AgencyUsers));
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
                var model = await _context.ApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Email == email);
                if (model == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.Deleted = true;
                _context.ApplicationUser.Update(model);
                await _context.SaveChangesAsync();
                notifyService.Custom($"User <b>{model.Email}</b> deleted successfully", 3, "orange", "fas fa-user-minus");
                return RedirectToAction(nameof(AgencyUsers), "Company", new { id = vendorId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting {AgencyUser} by {UserName}.", email, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error deleting user. Try again");
                return RedirectToAction(nameof(AgencyUsers));
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
                logger.LogError(ex, "Error occurred creating Service for {AgencyId} by {UserName}",id, HttpContext.User.Identity.Name);
                notifyService.Error("Error occurred. Try again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(VendorInvestigationServiceType service, long VendorId)
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
                logger.LogError(ex, "Error occurred creating Service for {AgencyId} by {UserName}", VendorId, HttpContext.User.Identity.Name);
                notifyService.Error("Error creating agency service. Try again.");
            }
            return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = service.VendorId });
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
                var currentUser = await _context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
                logger.LogError(ex, "Error occurred getting Service for {ServiceId} by {UserName}", id, HttpContext.User.Identity.Name);
                notifyService.Error("Error occurred. Try again.");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(long VendorInvestigationServiceTypeId, VendorInvestigationServiceType service, long VendorId)
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
                logger.LogError(ex, "Error occurred editing Service for {ServiceId} by {UserName}", VendorInvestigationServiceTypeId, HttpContext.User.Identity.Name);
                notifyService.Error("Error editing agency service. Try again.");
            }
            return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = service.VendorId });
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
                var currentUser = await _context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
                logger.LogError(ex, "Error getting agency {ServiceId} by {UserName}", id, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error getting agency service. Try again.");
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
                logger.LogError(ex, "Error deleting agency {ServiceId} by {UserName}", id, HttpContext.User?.Identity?.Name);
                notifyService.Error("Error deleting agency service. Try again.");
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

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
                logger.LogError(ex, "Error empanelling agency(s)");
                notifyService.Error("Error empanelling agency(s). Try again.");
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

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
                logger.LogError(ex, "Error de-panelling  agency(s)");
                notifyService.Error("Error de-panelling  agency(s). Try again");
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
                logger.LogError(ex, "Error getting {AgencyId} by {UserName}",id, HttpContext.User.Identity.Name);
                notifyService.Error("Error getting agency. Try again");
                return RedirectToAction(nameof(AvailableVendors));
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
                logger.LogError(ex, "Error getting {AgencyId} by {UserName}", id, HttpContext.User.Identity.Name);
                notifyService.Error("Error getting agency. Try again");
                return RedirectToAction(nameof(EmpanelledVendors));
            }
        }
    }
}