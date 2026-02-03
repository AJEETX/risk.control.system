using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Creator
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class BeneficiaryController : Controller
    {
        private readonly ILogger<BeneficiaryController> logger;
        private readonly IBeneficiaryCreateEditService beneficiaryCreateEditService;
        private readonly ApplicationDbContext context;
        private readonly INotyfService notifyService;

        public BeneficiaryController(ILogger<BeneficiaryController> logger,
            IBeneficiaryCreateEditService beneficiaryCreateEditService,
            ApplicationDbContext context,
            INotyfService notifyService)
        {
            this.logger = logger;
            this.beneficiaryCreateEditService = beneficiaryCreateEditService;
            this.context = context;
            this.notifyService = notifyService;
        }

        public async Task<IActionResult> CreateBeneficiary(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

                var claimsPage = new MvcBreadcrumbNode("New", "CaseCreateEdit", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Create", "CaseCreateEdit", "Add/Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateBeneficiary", "Beneficiary", $"Add beneficiary") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                if (currentUser.ClientCompany.HasSampleData)
                {
                    var beneRelation = await context.BeneficiaryRelation.FirstOrDefaultAsync();
                    var pinCode = await context.PinCode.Include(s => s.Country).OrderBy(p => p.StateId).LastOrDefaultAsync(s => s.Country.CountryId == currentUser.ClientCompany.CountryId);
                    var model = await beneficiaryCreateEditService.GetBeneficiaryDetailAsync(id, currentUser.ClientCompany.CountryId.Value);
                    await LoadDropDowns(model, currentUser);

                    return View(model);
                }
                else
                {
                    var model = new BeneficiaryDetail { InvestigationTaskId = id, Country = currentUser.ClientCompany.Country, CountryId = currentUser.ClientCompany.CountryId };

                    ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                    ViewData["BeneficiaryRelationId"] = new SelectList(context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");
                    ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>());

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting case {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error creating beneficiary. Try again.");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBeneficiary(long investigationTaskId, BeneficiaryDetail model)
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
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Beneficiary case {Id}. {UserEmail}", investigationTaskId, userEmail);
                notifyService.Warning("Error creating Beneficiary. Try again. ");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.InvestigationTaskId });
            }
        }

        private async Task LoadDropDowns(BeneficiaryDetail model, ApplicationUser currentUser)
        {
            var country = await context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId || c.CountryId == model.CountryId);
            model.Country = country;
            model.CountryId = country.CountryId;

            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;

            // Enum dropdowns
            ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
            ViewData["BeneficiaryRelationId"] = new SelectList(context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", model.BeneficiaryRelationId);
            ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);
        }

        public async Task<IActionResult> EditBeneficiary(long id, long taskId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CaseCreateEditController.CreateCase), "CaseCreateEdit");
                }
                var model = await context.BeneficiaryDetail
                    .Include(v => v.PinCode)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.Country)
                    .Include(v => v.BeneficiaryRelation)
                    .FirstOrDefaultAsync(v => v.BeneficiaryDetailId == id);
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                ViewData["BeneficiaryRelationId"] = new SelectList(context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", model.BeneficiaryRelationId);
                ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);

                var claimsPage = new MvcBreadcrumbNode("New", "CaseCreateEdit", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Create", "CaseCreateEdit", "Add/Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = model.InvestigationTaskId } };
                var editPage = new MvcBreadcrumbNode("EditBeneficiary", "Beneficiary", $"Edit beneficiary") { Parent = details1Page, RouteValues = new { id = model.InvestigationTaskId } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Beneficiary {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error editing Beneficiary. Try again.");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = taskId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBeneficiary(long beneficiaryDetailId, BeneficiaryDetail model)
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
                logger.LogError(ex, "Error editing Beneficiary {Id}. {UserEmail}", beneficiaryDetailId, userEmail);
                notifyService.Error("Error editing Beneficiary. Try again.");
            }
            return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.InvestigationTaskId });
        }
    }
}