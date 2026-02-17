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
    [Breadcrumb("Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class BeneficiaryController : Controller
    {
        private readonly ILogger<BeneficiaryController> logger;
        private readonly IBeneficiaryCreateEditService beneficiaryCreateEditService;
        private readonly INavigationService navigationService;
        private readonly ApplicationDbContext context;
        private readonly INotyfService notifyService;

        public BeneficiaryController(ILogger<BeneficiaryController> logger,
            IBeneficiaryCreateEditService beneficiaryCreateEditService,
            INavigationService navigationService,
            ApplicationDbContext context,
            INotyfService notifyService)
        {
            this.logger = logger;
            this.beneficiaryCreateEditService = beneficiaryCreateEditService;
            this.navigationService = navigationService;
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
                    return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
                }
                var currentUser = await context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

                ViewData["BreadcrumbNode"] = navigationService.GetInvestigationPath(id, "Add Beneficiary", nameof(Create), ControllerName<BeneficiaryController>.Name);

                if (currentUser.ClientCompany.HasSampleData)
                {
                    var model = await beneficiaryCreateEditService.GetBeneficiaryDetailAsync(id, currentUser.ClientCompany.CountryId.Value);
                    await LoadDropDowns(model, currentUser);
                    return View(model);
                }
                var modelWithoutSampleData = new BeneficiaryDetail { InvestigationTaskId = id, Country = currentUser.ClientCompany.Country, CountryId = currentUser.ClientCompany.CountryId };

                await PopulateBeneficiaryMetadata(modelWithoutSampleData, currentUser);

                return View(modelWithoutSampleData);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting case {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error creating beneficiary. Try again.");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = id });
            }
        }

        private async Task PopulateBeneficiaryMetadata(BeneficiaryDetail model, ApplicationUser user)
        {
            model.CurrencySymbol = CustomExtensions.GetCultureByCountry(user.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

            model.BeneficiaryRelations = await context.BeneficiaryRelation
                .Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.BeneficiaryRelationId.ToString(),
                    Selected = r.BeneficiaryRelationId == model.BeneficiaryRelationId
                }).ToListAsync();

            model.Incomes = Enum.GetValues(typeof(Income)).Cast<Income>()
                .Select(i => new SelectListItem
                {
                    Text = i.ToString(),
                    Value = i.ToString(),
                    Selected = i == model.Income
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BeneficiaryDetail model)
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
                var result = await beneficiaryCreateEditService.CreateAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors.");
                    await LoadDropDowns(model, currentUser);
                    return View(model);
                }
                notifyService.Custom($"Beneficiary <b>{model.Name}</b> added successfully", 3, "green", "fas fa-user-tie");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = model.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Beneficiary for Case {Id}. {UserEmail}", model.InvestigationTaskId, userEmail);
                notifyService.Warning("Error creating Beneficiary. Try again. ");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = model.InvestigationTaskId });
            }
        }

        private async Task LoadDropDowns(BeneficiaryDetail model, ApplicationUser currentUser)
        {
            var country = await context.Country.AsNoTracking().FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId || c.CountryId == model.CountryId);
            model.Country = country;
            model.CountryId = country.CountryId;

            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;

            // Enum dropdowns
            model.CurrencySymbol = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

            model.BeneficiaryRelations = await context.BeneficiaryRelation
                .Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.BeneficiaryRelationId.ToString(),
                    Selected = r.BeneficiaryRelationId == model.BeneficiaryRelationId
                }).ToListAsync();

            model.Incomes = Enum.GetValues(typeof(Income)).Cast<Income>()
                .Select(i => new SelectListItem
                {
                    Text = i.ToString(),
                    Value = i.ToString(),
                    Selected = i == model.Income
                });
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
                var model = await context.BeneficiaryDetail
                    .Include(v => v.PinCode)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.Country)
                    .Include(v => v.BeneficiaryRelation)
                    .FirstOrDefaultAsync(v => v.InvestigationTaskId == id);
                var currentUser = await context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

                await PopulateBeneficiaryMetadata(model, currentUser);

                ViewData["BreadcrumbNode"] = navigationService.GetInvestigationPath(id, "Edit Beneficiary", nameof(Edit), ControllerName<BeneficiaryController>.Name);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Beneficiary {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error editing Beneficiary. Try again.");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BeneficiaryDetail model)
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

                var result = await beneficiaryCreateEditService.EditAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors.");
                    await LoadDropDowns(model, currentUser);
                    return View(model);
                }
                notifyService.Custom($"Beneficiary <b>{model.Name}</b> edited successfully", 3, "orange", "fas fa-user-tie");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing Beneficiary Case {Id}. {UserEmail}", model.InvestigationTaskId, userEmail);
                notifyService.Error("Error editing Beneficiary. Try again.");
            }
            return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = model.InvestigationTaskId });
        }
    }
}