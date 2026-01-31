using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Creator
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> logger;
        private readonly ICustomerCreateEditService customerCreateEditService;
        private readonly ApplicationDbContext context;
        private readonly INotyfService notifyService;

        public CustomerController(ILogger<CustomerController> logger,
            ICustomerCreateEditService customerCreateEditService,
            ApplicationDbContext context,
            INotyfService notifyService)
        {
            this.logger = logger;
            this.customerCreateEditService = customerCreateEditService;
            this.context = context;
            this.notifyService = notifyService;
        }

        public async Task<IActionResult> CreateCustomer(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CaseCreateEditController.Create), "CaseCreateEdit");
                }
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
                var claimsPage = new MvcBreadcrumbNode("New", "CaseCreateEdit", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Create", "CaseCreateEdit", "Add/Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateCustomer", "Customer", $"Create Customer") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                if (currentUser.ClientCompany.HasSampleData)
                {
                    var customerDetail = await customerCreateEditService.GetCustomerDetailAsync(id, currentUser.ClientCompany.CountryId.Value);
                    await LoadDropDowns(customerDetail, currentUser);
                    return View(customerDetail);
                }
                else
                {
                    ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                    ViewData["GenderList"] = new SelectList(Enum.GetValues(typeof(Gender)).Cast<Gender>());
                    ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>());
                    ViewData["EducationList"] = new SelectList(Enum.GetValues(typeof(Education)).Cast<Education>());
                    ViewData["OccupationList"] = new SelectList(Enum.GetValues(typeof(Occupation)).Cast<Occupation>());

                    var blankCustomerDetail = new CustomerDetail { Country = currentUser.ClientCompany.Country, CountryId = currentUser.ClientCompany.CountryId, InvestigationTaskId = id };

                    return View(blankCustomerDetail);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating customer {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error creating customer. Try Again");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CreateCustomer(CustomerDetail model)
        {
            var userEmail = HttpContext.User.Identity.Name;
            try
            {
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors and try again.");
                    await LoadDropDowns(model, currentUser);
                    return View(model);
                }

                var result = await customerCreateEditService.CreateAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors.");
                    await LoadDropDowns(model, currentUser);
                    return View(model);
                }

                notifyService.Custom($"Customer <b>{model.Name}</b> added successfully", 3, "green", "fas fa-user-plus");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating customer {Id}. {UserEmail}", model.CustomerDetailId, userEmail);
                notifyService.Error("Error creating customer.Try Again");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.InvestigationTaskId });
            }
        }

        public async Task<IActionResult> EditCustomer(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CaseCreateEditController.Create), "CaseCreateEdit");
                }

                var model = await context.CustomerDetail
                    .Include(c => c.PinCode)
                    .Include(c => c.District)
                    .Include(c => c.State)
                    .Include(c => c.Country)
                    .FirstOrDefaultAsync(i => i.InvestigationTaskId == id);

                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Customer Not Found.Try Again");
                    return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = id });
                }
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                ViewData["GenderList"] = new SelectList(Enum.GetValues(typeof(Gender)).Cast<Gender>(), model.Gender);
                ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);
                ViewData["EducationList"] = new SelectList(Enum.GetValues(typeof(Education)).Cast<Education>(), model.Education);
                ViewData["OccupationList"] = new SelectList(Enum.GetValues(typeof(Occupation)).Cast<Occupation>(), model.Occupation);

                var claimsPage = new MvcBreadcrumbNode("New", "CaseCreateEdit", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Create", "CaseCreateEdit", "Add/Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditCustomer", "Customer", $"Edit Customer") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting customer {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error editing Customer.Try Again");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditCustomer(CustomerDetail model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors and try again.");
                    await LoadDropDowns(model, currentUser);
                    return View(model);
                }
                var result = await customerCreateEditService.EditAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors.");
                    await LoadDropDowns(model, currentUser);
                    return View(model);
                }

                notifyService.Custom($"Customer <b>{model.Name}</b> edited successfully", 3, "orange", "fas fa-user-plus");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing customer {Id}. {UserEmail}", model.CustomerDetailId, userEmail);
                notifyService.Error("Error edting customer. Try again.");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.InvestigationTaskId });
            }
        }

        private async Task LoadDropDowns(CustomerDetail model, ApplicationUser currentUser)
        {
            var country = await context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = country.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;

            // Enum dropdowns
            ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
            ViewData["GenderList"] = new SelectList(Enum.GetValues(typeof(Gender)).Cast<Gender>(), model.Gender);
            ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);
            ViewData["EducationList"] = new SelectList(Enum.GetValues(typeof(Education)).Cast<Education>(), model.Education);
            ViewData["OccupationList"] = new SelectList(Enum.GetValues(typeof(Occupation)).Cast<Occupation>(), model.Occupation);
        }
    }
}