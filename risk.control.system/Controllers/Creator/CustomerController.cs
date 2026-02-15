using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Creator
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> logger;
        private readonly INavigationService navigationService;
        private readonly ICustomerCreateEditService customerCreateEditService;
        private readonly ApplicationDbContext context;
        private readonly INotyfService notifyService;

        public CustomerController(ILogger<CustomerController> logger,
            INavigationService navigationService,
            ICustomerCreateEditService customerCreateEditService,
            ApplicationDbContext context,
            INotyfService notifyService)
        {
            this.logger = logger;
            this.navigationService = navigationService;
            this.customerCreateEditService = customerCreateEditService;
            this.context = context;
            this.notifyService = notifyService;
        }

        public async Task<IActionResult> Create(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CaseCreateEditController.Create), ControllerName<CaseCreateEditController>.Name);
                }
                var currentUser = await context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
                ViewData["BreadcrumbNode"] = navigationService.GetInvestigationPath(id, "Add Customer", nameof(Create), ControllerName<CustomerController>.Name);

                if (currentUser.ClientCompany.HasSampleData)
                {
                    var customerDetail = await customerCreateEditService.GetCustomerDetailAsync(id, currentUser.ClientCompany.CountryId.Value);
                    await LoadDropDowns(customerDetail, currentUser);
                    return View(customerDetail);
                }
                var blankCustomerDetail = PrepareBlankCustomer(id, currentUser);
                return View(blankCustomerDetail);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating customer {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error creating customer. Try Again");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(CustomerDetail model)
        {
            var userEmail = HttpContext.User.Identity.Name;
            try
            {
                var currentUser = await context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
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
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = model.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating customer {Id}. {UserEmail}", model.CustomerDetailId, userEmail);
                notifyService.Error("Error creating customer.Try Again");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = model.InvestigationTaskId });
            }
        }

        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CaseCreateEditController.Create), ControllerName<CaseCreateEditController>.Name);
                }

                var model = await context.CustomerDetail.AsNoTracking()
                    .Include(c => c.PinCode)
                    .Include(c => c.District)
                    .Include(c => c.State)
                    .Include(c => c.Country)
                    .FirstOrDefaultAsync(i => i.InvestigationTaskId == id);

                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Customer Not Found.Try Again");
                    return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = id });
                }
                var currentUser = await context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
                model.CurrencySymbol = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                PopulateCustomerLists(model);

                ViewData["BreadcrumbNode"] = navigationService.GetInvestigationPath(id, "Edit Customer", nameof(Edit), ControllerName<CustomerController>.Name); ;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting customer {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error editing Customer.Try Again");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(CustomerDetail model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var currentUser = await context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

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
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = model.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing customer {Id}. {UserEmail}", model.CustomerDetailId, userEmail);
                notifyService.Error("Error edting customer. Try again.");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = model.InvestigationTaskId });
            }
        }

        private CustomerDetail PrepareBlankCustomer(long id, ApplicationUser user)
        {
            var countryCode = user.ClientCompany.Country.Code.ToUpper();

            return new CustomerDetail
            {
                Country = user.ClientCompany.Country,
                CountryId = user.ClientCompany.CountryId,
                InvestigationTaskId = id,
                CurrencySymbol = CustomExtensions.GetCultureByCountry(countryCode).NumberFormat.CurrencySymbol,

                // Reusable Enum helper
                GenderList = GetEnumSelectList<Gender>(),
                IncomeList = GetEnumSelectList<Income>(),
                EducationList = GetEnumSelectList<Education>(),
                OccupationList = GetEnumSelectList<Occupation>()
            };
        }

        // Generic helper to convert Enums to SelectListItems
        private IEnumerable<SelectListItem> GetEnumSelectList<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>()
                .Select(e => new SelectListItem { Text = e.ToString(), Value = e.ToString() });
        }

        private void PopulateCustomerLists(CustomerDetail model)
        {
            model.GenderList = new SelectList(Enum.GetValues(typeof(Gender)).Cast<Gender>(), model.Gender);
            model.IncomeList = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);
            model.EducationList = new SelectList(Enum.GetValues(typeof(Education)).Cast<Education>(), model.Education);
            model.OccupationList = new SelectList(Enum.GetValues(typeof(Occupation)).Cast<Occupation>(), model.Occupation);
        }

        private async Task LoadDropDowns(CustomerDetail model, ApplicationUser currentUser)
        {
            var country = await context.Country.AsNoTracking().FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = country.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;

            model.CurrencySymbol = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
            model.GenderList = new SelectList(Enum.GetValues(typeof(Gender)).Cast<Gender>(), model.Gender);
            model.IncomeList = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);
            model.EducationList = new SelectList(Enum.GetValues(typeof(Education)).Cast<Education>(), model.Education);
            model.OccupationList = new SelectList(Enum.GetValues(typeof(Occupation)).Cast<Occupation>(), model.Occupation);
        }
    }
}