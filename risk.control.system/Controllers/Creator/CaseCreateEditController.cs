using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Api;
using risk.control.system.Services.Common;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Creator
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class CaseCreateEditController : Controller
    {
        private readonly ILogger<CaseCreateEditController> logger;
        private readonly ICaseCreateEditService caseCreateEditService;
        private readonly INavigationService navigationService;
        private readonly ApplicationDbContext context;
        private readonly INotyfService notifyService;
        private readonly IInvestigationService service;

        public CaseCreateEditController(ILogger<CaseCreateEditController> logger,
            ICaseCreateEditService createCreateEditService,
            INavigationService navigationService,
            ApplicationDbContext context,
            INotyfService notifyService,
            IInvestigationService service)
        {
            this.logger = logger;
            this.caseCreateEditService = createCreateEditService;
            this.navigationService = navigationService;
            this.context = context;
            this.notifyService = notifyService;
            this.service = service;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(New));
        }

        [Breadcrumb(" Add/Assign")]
        public async Task<IActionResult> New()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                bool userCanCreate = true;
                bool hasClaim = true;
                int availableCount = 0;

                var companyUser = await context.ApplicationUser.AsNoTracking().Include(u => u.ClientCompany).Include(u => u.Country).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
                var fileIdentifier = companyUser.Country.Code.ToLower();

                if (companyUser.ClientCompany.LicenseType == LicenseType.Trial)
                {
                    var totalReadyToAssign = await service.GetAutoCount(currentUserEmail);
                    hasClaim = totalReadyToAssign > 0;
                    userCanCreate = userCanCreate && companyUser.ClientCompany.TotalToAssignMaxAllowed > totalReadyToAssign;
                    var totalClaimsCreated = await context.Investigations.AsNoTracking().CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
                    availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated;
                    if (!userCanCreate)
                    {
                        notifyService.Warning($"MAX Case limit = <b>{companyUser.ClientCompany.TotalCreatedClaimAllowed}</b> reached");
                    }
                    else
                    {
                        notifyService.Information($"Limit available = <b>{availableCount}</b>");
                    }
                }

                return View(new CreateClaims
                {
                    BulkUpload = companyUser.ClientCompany.BulkUpload,
                    UserCanCreate = userCanCreate,
                    HasClaims = hasClaim,
                    FileSampleIdentifier = fileIdentifier,
                    AutoAllocation = companyUser.ClientCompany.AutoAllocation
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred fetching Add/Assign apge. {UserEmail}", currentUserEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(New));
            }
        }

        [Breadcrumb(" Add New", FromAction = nameof(New))]
        public async Task<IActionResult> Add()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var model = await caseCreateEditService.Create(userEmail);
                if (model.Trial)
                {
                    if (!model.AllowedToCreate)
                    {
                        notifyService.Information($"MAX Case limit = <b>{model.TotalCount}</b> reached");
                    }
                    else
                    {
                        notifyService.Information($"Limit available = <b>{model.AvailableCount}</b>");
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred creating case. {UserEmail}", userEmail);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(Create));
            }
        }

        [Breadcrumb(title: " Add Case", FromAction = nameof(New))]
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var currentUser = await context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

                if (currentUser.ClientCompany.HasSampleData)
                {
                    var model = await caseCreateEditService.AddCaseDetail(userEmail);
                    await LoadDropDowns(model.PolicyDetailDto, userEmail);
                    return View(model);
                }
                var modelWithoutSampleData = PopulateViewModelMetadata(currentUser);
                return View(modelWithoutSampleData);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred to create case. {UserEmail} ", userEmail);
                notifyService.Error("Error creating case detail. Try Again.");
                return RedirectToAction(nameof(Add));
            }
        }

        private async Task PopulateViewModelMetadata(ApplicationUser user)
        {
            var model = new CreateCaseViewModel();

            model.PolicyDetailDto.CurrencySymbol = CustomExtensions.GetCultureByCountry(user.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

            model.PolicyDetailDto.CaseEnablers = await context.CaseEnabler.AsNoTracking().OrderBy(s => s.Code)
                .Select(s => new SelectListItem { Text = s.Name, Value = s.CaseEnablerId.ToString() }).ToListAsync();

            model.PolicyDetailDto.CostCentres = await context.CostCentre.AsNoTracking().OrderBy(s => s.Code)
                .Select(s => new SelectListItem { Text = s.Name, Value = s.CostCentreId.ToString() }).ToListAsync();

            model.PolicyDetailDto.InvestigationServiceTypes = await context.InvestigationServiceType.AsNoTracking().OrderBy(s => s.Code)
                .Select(s => new SelectListItem { Text = s.Name, Value = s.InvestigationServiceTypeId.ToString() }).ToListAsync();

            model.PolicyDetailDto.InsuranceTypes = Enum.GetValues(typeof(InsuranceType))
                .Cast<InsuranceType>()
                .Select(e => new SelectListItem { Text = e.ToString(), Value = e.ToString() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCaseViewModel model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadDropDowns(model.PolicyDetailDto, userEmail);
                    return View(model);
                }
                var result = await caseCreateEditService.CreateAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await LoadDropDowns(model.PolicyDetailDto, userEmail);
                    return View(model);
                }
                notifyService.Success($"Policy #{result.CaseId} created successfully");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = result.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred to create case. {UserEmail} ", userEmail);
                notifyService.Error("Error creating case detail. Try Again.");
                return RedirectToAction(nameof(Add));
            }
        }

        private async Task LoadDropDowns(PolicyDetailDto model, string userEmail)
        {
            var currentUser = await context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

            model.CurrencySymbol = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

            model.CaseEnablers = new SelectList(context.CaseEnabler.AsNoTracking().OrderBy(s => s.Code), "CaseEnablerId", "Name", model.CaseEnablerId);

            model.CostCentres = new SelectList(context.CostCentre.AsNoTracking().OrderBy(s => s.Code), "CostCentreId", "Name", model.CostCentreId);

            model.InsuranceTypes = new SelectList(Enum.GetValues(typeof(InsuranceType)).Cast<InsuranceType>(), model.InsuranceType);

            model.InvestigationServiceTypes = new SelectList(context.InvestigationServiceType.AsNoTracking().Where(i => i.InsuranceType == model.InsuranceType).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", model.InvestigationServiceTypeId);
        }

        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 0)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(New), "CaseCreateEdit");
                }

                var model = await caseCreateEditService.GetEditPolicyDetail(id);
                if (model == null)
                {
                    notifyService.Error("Case Not Found!!!");
                    return RedirectToAction(nameof(Add));
                }
                await LoadDropDowns(model.PolicyDetailDto, userEmail);

                ViewData["BreadcrumbNode"] = navigationService.GetEditCasePath(id);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred to get edit case {Id}. {UserEmail} ", id, userEmail);
                notifyService.Error("Error editing case detail. Try Again");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(EditPolicyDto model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadDropDowns(model.PolicyDetailDto, userEmail);
                    return View(model);
                }
                var result = await caseCreateEditService.EditAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await LoadDropDowns(model.PolicyDetailDto, userEmail);
                    return View(model);
                }
                notifyService.Custom($"Policy <b>#{result.CaseId}</b> edited successfully", 3, "orange", "far fa-file-powerpoint");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = model.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred to edit case {Id}. {UserEmail} ", model.Id, userEmail);
                notifyService.Error("OOPs !!!..Error editing Case detail. Try again.");
                return RedirectToAction(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, new { id = model.Id });
            }
        }
    }
}