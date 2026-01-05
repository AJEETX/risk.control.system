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
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class VendorsController : Controller
    {
        private AppRoles[] agencyRoles = new[]
                {
                    AppRoles.AGENCY_ADMIN,
                    AppRoles.SUPERVISOR,
                    AppRoles.AGENT
                };
        private readonly ApplicationDbContext _context;
        private readonly IAgencyCreateEditService agencyCreateEditService;
        private readonly IAgencyUserCreateEditService agencyUserCreateEditService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly INotyfService notifyService;
        private readonly IInvestigationService service;
        private readonly IFeatureManager featureManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<VendorsController> logger;
        private string portal_base_url = string.Empty;

        public VendorsController(
            ApplicationDbContext context,
            IAgencyCreateEditService agencyCreateEditService,
            IAgencyUserCreateEditService agencyUserCreateEditService,
            UserManager<ApplicationUser> userManager,
            INotyfService notifyService,
            IInvestigationService service,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<VendorsController> logger)
        {
            _context = context;
            this.agencyCreateEditService = agencyCreateEditService;
            this.agencyUserCreateEditService = agencyUserCreateEditService;
            this.userManager = userManager;
            this.notifyService = notifyService;
            this.service = service;
            this.featureManager = featureManager;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [Breadcrumb("Manage Agency(s)")]
        public IActionResult Index()
        {
            return RedirectToAction("EmpanelledVendors");
        }

        [Breadcrumb("Agencies", FromAction = "Index")]
        public IActionResult Agencies()
        {
            return View();
        }
        [Breadcrumb("Empanelled Agencies", FromAction = "Index")]
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
                    return RedirectToAction("EmpanelledVendors", "Vendors");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..User Not Found. Try again.");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = await _context.ClientCompany
                    .Include(c => c.EmpanelledVendors)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Company Not Found. Try again.");
                    return RedirectToAction("EmpanelledVendors", "Vendors");
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
                notifyService.Custom($"Agency(s) De-panelled successfully.", 3, "orange", "far fa-hand-pointer");
                return RedirectToAction("EmpanelledVendors", "Vendors");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred empanelling Agencies");
                notifyService.Error("Error occurred empanelling Agencies. Try again.");
                return RedirectToAction("EmpanelledVendors", "Vendors");
            }
        }

        [Breadcrumb("Available Agencies", FromAction = "Index")]
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
                if (!ModelState.IsValid || vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("No agency selected !!!");
                    return RedirectToAction(nameof(AvailableVendors), "Company");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction("AvailableVendors", "Vendors");
                }
                var company = await _context.ClientCompany.Include(c => c.EmpanelledVendors)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction("AvailableVendors", "Vendors");
                }
                var vendors2Empanel = _context.Vendor.AsNoTracking().Where(v => vendors.Contains(v.VendorId.ToString()));
                company.EmpanelledVendors.AddRange(vendors2Empanel.ToList());

                company.Updated = DateTime.Now;
                company.UpdatedBy = currentUserEmail;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();

                notifyService.Custom($"Agency(s) empanelled successfully", 3, "green", "fas fa-thumbs-up");
                return RedirectToAction("AvailableVendors", "Vendors");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred depanelling Agencies");
                notifyService.Error("Error occurred depanelling Agencies. Try again.");
                return RedirectToAction("AvailableVendors", "Vendors");
            }

        }
        public async Task<JsonResult> PostRating(int rating, long mid)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var existingRating = await _context.Ratings.FirstOrDefaultAsync(r => r.VendorId == mid && r.UserEmail == currentUserEmail);
            if (existingRating != null)
            {
                existingRating.Rate = rating;
                _context.Ratings.Update(existingRating);
                _context.SaveChanges();
                return Json("You rated again " + rating.ToString() + " star(s)");
            }

            var rt = new AgencyRating();
            string ip = "123";
            rt.Rate = rating;
            rt.IpAddress = ip;
            rt.VendorId = mid;
            rt.UserEmail = currentUserEmail;
            _context.Ratings.Add(rt);
            _context.SaveChanges();
            return Json("You rated this " + rating.ToString() + " star(s)");
        }

        public async Task<JsonResult> PostDetailRating(int rating, long vendorId)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Json(new { success = false, message = "You must be logged in to rate." });
            }

            var existingRating = await _context.Ratings.FirstOrDefaultAsync(r => r.VendorId == vendorId && r.UserEmail == currentUserEmail);

            if (existingRating != null)
            {
                existingRating.Rate = rating;
                _context.Ratings.Update(existingRating);
            }
            else
            {
                var newRating = new AgencyRating
                {
                    VendorId = vendorId,
                    Rate = rating,
                    UserEmail = currentUserEmail,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                };
                _context.Ratings.Add(newRating);
            }

            _context.SaveChanges();

            // Calculate new average rating
            var ratings = _context.Ratings.Where(r => r.VendorId == vendorId);
            double avgRating = ratings.Any() ? ratings.Average(r => r.Rate) : 0;

            return Json(new { success = true, message = $"You rated {rating} star(s)", avgRating = avgRating });
        }

        [Breadcrumb("Agency Profile", FromAction = "AvailableVendors")]
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("Error getting Agency");
                    return RedirectToAction("AvailableVendors", "Vendors");
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
                    notifyService.Error("Error getting Agency");
                    return RedirectToAction("AvailableVendors", "Vendors");
                }
                var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

                var vendorAllCasesCount = await _context.Investigations.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted &&
                          (c.SubStatus == approvedStatus ||
                          c.SubStatus == rejectedStatus));

                var vendorUserCount = await _context.ApplicationUser.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted);

                // HACKY
                var currentCases = service.GetAgencyIdsLoad(new List<long> { vendor.VendorId });
                vendor.SelectedCountryId = vendorUserCount;
                vendor.SelectedStateId = currentCases.FirstOrDefault().CaseCount;
                vendor.SelectedDistrictId = vendorAllCasesCount;

                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (superAdminUser.IsSuperAdmin)
                {
                    vendor.SelectedByCompany = true;
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Agency.");
                notifyService.Error("Error getting Agency. Try again.");
                return RedirectToAction("AvailableVendors", "Vendors");
            }

        }

        [Breadcrumb(" Manage Users", FromAction = "Details")]
        public IActionResult Users(string id)
        {
            ViewData["vendorId"] = id;

            var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
            var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View();
        }

        [Breadcrumb(" Add User", FromAction = "Users")]
        public async Task<IActionResult> CreateUser(long id)
        {
            if (id <= 0)
            {
                notifyService.Custom($"OOPs !!!..Error creating user.", 3, "red", "fa fa-user");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            List<SelectListItem> allRoles = null;
            AppRoles? role = null;
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
            if (vendor == null)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var currentVendorUserCount = await _context.ApplicationUser.CountAsync(v => v.VendorId == id);
            bool status = false;
            if (currentVendorUserCount == 0)
            {
                role = AppRoles.AGENCY_ADMIN;
                status = true;
                allRoles = agencyRoles
                .Where(r => r == AppRoles.AGENCY_ADMIN) // Include ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            else
            {
                allRoles = agencyRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN) // Exclude ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            var model = new ApplicationUser
            {
                Active = status,
                Country = vendor.Country,
                CountryId = vendor.CountryId,
                Vendor = vendor,
                AvailableRoles = allRoles,
                Role = role
            };

            var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Company", "Manager Agency(s)");
            var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Company", "Available Agencies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var usersPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("CreateUser", "Vendors", $"Add User") { Parent = usersPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View(model);
        }
        private async Task LoadModel(ApplicationUser model)
        {
            List<SelectListItem> allRoles = null;
            AppRoles? role = null;
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);

            var currentVendorUserCount = await _context.ApplicationUser.CountAsync(v => v.VendorId == model.VendorId);
            bool status = false;
            if (currentVendorUserCount == 0)
            {
                role = AppRoles.AGENCY_ADMIN;
                status = true;
                allRoles = agencyRoles
                .Where(r => r == AppRoles.AGENCY_ADMIN) // Include ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            else
            {
                allRoles = agencyRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN) // Include ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            model.Active = status;
            model.Vendor = vendor;
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.AvailableRoles = allRoles;
            model.Role = role;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(ApplicationUser model, string emailSuffix)
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
                return RedirectToAction(nameof(Users), "Vendors", new { id = model.VendorId });

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error Creating User. Try again.");
                notifyService.Error("OOPS !!!..Error Creating User. Try again.");
                return RedirectToAction(nameof(CreateUser), "Vendors", new { id = model.VendorId });
            }
        }

        [Breadcrumb(" Edit User", FromAction = "Users")]
        public async Task<IActionResult> EditUser(long? userId)
        {
            try
            {
                if (userId == null || userId <= 0)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorApplicationUser = _context.ApplicationUser
                    .Include(v => v.Country)?
                    .Include(v => v.Vendor)?
                    .FirstOrDefault(v => v.Id == userId);
                if (vendorApplicationUser == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
                var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
                var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = vendorApplicationUser.Vendor.VendorId } };
                var usersPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = vendorApplicationUser.Vendor.VendorId } };
                var editPage = new MvcBreadcrumbNode("EditUser", "Vendors", $"Edit User") { Parent = usersPage };
                ViewData["BreadcrumbNode"] = editPage;

                vendorApplicationUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !vendorApplicationUser.IsPasswordChangeRequired : true;

                return View(vendorApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting User.");
                notifyService.Error("Error getting User. Try again.");
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, ApplicationUser model, string editby)
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
                logger.LogError(ex, "Error editing User..");
                notifyService.Error("Error editing User. Try again.");
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

        [Breadcrumb(title: " Delete", FromAction = "Users")]
        public async Task<IActionResult> DeleteUser(long userId)
        {
            try
            {
                if (userId < 1 || userId == 0)
                {
                    notifyService.Error("Error getting User. Try again.");
                    return RedirectToAction(nameof(Users));
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var model = await _context.ApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("Error getting User. Try again.");
                    return RedirectToAction(nameof(Users));
                }

                var agencySubStatuses = new[]{
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR};

                var hasClaims = _context.Investigations.Any(c => agencySubStatuses.Contains(c.SubStatus) && c.VendorId == model.VendorId);
                model.HasClaims = hasClaims;

                var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
                var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
                var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = model.VendorId } };
                var usersPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = model.VendorId } };
                var editPage = new MvcBreadcrumbNode("DeleteUser", "Vendors", $"Delete User") { Parent = usersPage };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting User.");
                notifyService.Error("Error deleting User. Try again.");
                return RedirectToAction(nameof(Users));
            }

        }
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string email, long vendorId)
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
                notifyService.Custom($"User <b>{model.Email}</b> Deleted successfully", 3, "red", "fas fa-user-minus");
                return RedirectToAction(nameof(VendorsController.Users), "Vendors", new { id = vendorId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting User.");
                notifyService.Error("Error deleting User. Try again.");
                return RedirectToAction(nameof(Users));
            }

        }

        [Breadcrumb("Manage Service", FromAction = "Details")]
        public IActionResult Service(string id)
        {
            if (id == null || string.IsNullOrWhiteSpace(id))
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            ViewData["vendorId"] = id;

            var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
            var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var servicesPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manager Service") { Parent = agencyPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = servicesPage;

            return View();
        }

        [Breadcrumb(" Add Agency")]
        public async Task<IActionResult> Create()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            var companyUser = await _context.ApplicationUser.Include(c => c.Country).Include(c => c.ClientCompany).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var vendor = new Vendor { CountryId = companyUser.ClientCompany.CountryId, Country = companyUser.ClientCompany.Country, SelectedCountryId = companyUser.ClientCompany.CountryId.Value };
            return View(vendor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vendor model, string domainAddress)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadModel(model);
                    return View(model);
                }
                if (!RegexHelper.IsMatch(domainAddress))
                {
                    ModelState.AddModelError("Email", "Invalid email address.");
                    await LoadModel(model);
                    return View(model);
                }
                var userEmail = HttpContext.User?.Identity?.Name;

                var result = await agencyCreateEditService.CreateAsync(domainAddress, userEmail, model, portal_base_url);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await LoadModel(model);
                    return View(model);
                }
                notifyService.Custom($"Agency <b>{model.Email}</b>  created successfully.", 3, "green", "fas fa-building");
                return RedirectToAction(nameof(CompanyController.AvailableVendors), "Vendors");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Agency");
                notifyService.Error("OOPS !!!..Error creating Agency. Try again.");
                return RedirectToAction(nameof(Create));
            }
        }

        private async Task LoadModel(Vendor model)
        {
            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }

        [Breadcrumb(" Edit Agency", FromAction = "Details")]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                if (1 > id)
                {
                    notifyService.Error("Error getting User. Try again.");
                    return RedirectToAction(nameof(Users));
                }

                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("Error getting User. Try again.");
                    return RedirectToAction(nameof(Users));
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var isSuperAdmin = await _context.ApplicationUser.AnyAsync(u => u.Email.ToLower() == currentUserEmail.ToLower() && u.IsSuperAdmin);
                vendor.SelectedByCompany = isSuperAdmin;
                var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
                var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
                var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("Edit", "Vendors", $"Edit Agency") { Parent = agencyPage };
                ViewData["BreadcrumbNode"] = editPage;

                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting User.");
                notifyService.Error("Error getting User. Try again.");
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long vendorId, Vendor model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadModel(model);
                    return View(model);
                }

                var userEmail = HttpContext.User?.Identity?.Name;

                var result = await agencyCreateEditService.EditAsync(userEmail, model, portal_base_url);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await LoadModel(model);
                    return View(model);
                }
                notifyService.Custom($"Agency <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing Agency.");
                notifyService.Error("Error editing Agency. Try again.");
            }
            return RedirectToAction(nameof(VendorsController.Details), "Vendors", new { id = vendorId });
        }

        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                if (id < 1 || _context.Vendor == null)
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
                    .ThenInclude(v => v.InvestigationServiceType)
                    .FirstOrDefaultAsync(m => m.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var agencySubStatuses = new[]{
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR };

                var hasClaims = _context.Investigations.Any(c => agencySubStatuses.Contains(c.SubStatus) && c.VendorId == id);
                var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
                var editPage = new MvcBreadcrumbNode("Delete", "Vendors", $"Delete Agency") { Parent = agencyPage };
                ViewData["BreadcrumbNode"] = editPage;
                vendor.HasClaims = hasClaims;
                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);

                if (superAdminUser.IsSuperAdmin)
                {
                    vendor.SelectedByCompany = true;
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting agency");
                notifyService.Error("Error getting agency. Try again.");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long VendorId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendor = await _context.Vendor.FindAsync(VendorId);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Agency Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorUser = await _context.ApplicationUser.Where(v => v.VendorId == VendorId).ToListAsync();
                foreach (var user in vendorUser)
                {
                    user.Updated = DateTime.Now;
                    user.UpdatedBy = currentUserEmail;
                    user.Deleted = true;
                    _context.ApplicationUser.Update(user);
                }
                vendor.Updated = DateTime.Now;
                vendor.UpdatedBy = currentUserEmail;
                vendor.Deleted = true;
                _context.Vendor.Update(vendor);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Agency <b>{vendor.Email}</b> deleted successfully.", 3, "red", "fas fa-building");
                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                return RedirectToAction(nameof(AvailableVendors), "Vendors");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting agency");
                notifyService.Error("Error deleting agency. Try again.");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}