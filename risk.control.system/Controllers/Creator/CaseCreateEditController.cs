using AspNetCoreHero.ToastNotification.Abstractions;
using risk.control.system.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Creator
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class CaseCreateEditController : Controller
    {
        private readonly ILogger<CaseCreateEditController> logger;
        private readonly ICaseCreateEditService caseCreateEditService;
        private readonly ApplicationDbContext context;
        private readonly INotyfService notifyService;
        private readonly IInvestigationService service;

        public CaseCreateEditController(ILogger<CaseCreateEditController> logger,
            ICaseCreateEditService createCreateEditService,
            ApplicationDbContext context,
            INotyfService notifyService,
            IInvestigationService service)
        {
            this.logger = logger;
            this.caseCreateEditService = createCreateEditService;
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

                var companyUser = await context.ApplicationUser.Include(u => u.ClientCompany).Include(u => u.Country).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
                var fileIdentifier = companyUser.Country.Code.ToLower();

                if (companyUser.ClientCompany.LicenseType == LicenseType.Trial)
                {
                    var totalReadyToAssign = await service.GetAutoCount(currentUserEmail);
                    hasClaim = totalReadyToAssign > 0;
                    userCanCreate = userCanCreate && companyUser.ClientCompany.TotalToAssignMaxAllowed > totalReadyToAssign;
                    var totalClaimsCreated = await context.Investigations.CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
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

        [Breadcrumb(" Add New", FromAction = "New")]
        public async Task<IActionResult> Create()
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

        [Breadcrumb(title: " Add Case", FromAction = "Create")]
        public async Task<IActionResult> CreateCase()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

                if (currentUser.ClientCompany.HasSampleData)
                {
                    var model = await caseCreateEditService.AddCaseDetail(userEmail);
                    await LoadDropDowns(model.PolicyDetailDto, userEmail);
                    return View(model);
                }
                else
                {
                    ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                    ViewData["CaseEnablerId"] = new SelectList(context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name");
                    ViewData["CostCentreId"] = new SelectList(context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name");
                    ViewData["InsuranceType"] = new SelectList(Enum.GetValues(typeof(InsuranceType)).Cast<InsuranceType>());
                    ViewData["InvestigationServiceTypeId"] = new SelectList(context.InvestigationServiceType.OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name");
                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred to create case. {UserEmail} ", userEmail);
                notifyService.Error("Error creating case detail. Try Again.");
                return RedirectToAction(nameof(CreateCase));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCase(CreateCaseViewModel model)
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
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = result.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred to create case. {UserEmail} ", userEmail);
                notifyService.Error("Error creating case detail. Try Again.");
                return RedirectToAction(nameof(CreateCase));
            }
        }

        private async Task LoadDropDowns(PolicyDetailDto model, string userEmail)
        {
            var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

            ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

            ViewData["CaseEnablerId"] = new SelectList(
                context.CaseEnabler.OrderBy(s => s.Code),
                "CaseEnablerId",
                "Name",
                model.CaseEnablerId
            );

            ViewData["CostCentreId"] = new SelectList(
                context.CostCentre.OrderBy(s => s.Code),
                "CostCentreId",
                "Name",
                model.CostCentreId
            );

            ViewData["InsuranceType"] = new SelectList(Enum.GetValues(typeof(InsuranceType)).Cast<InsuranceType>(), model.InsuranceType);

            ViewData["InvestigationServiceTypeId"] = new SelectList(
                    context.InvestigationServiceType
                        .Where(i => i.InsuranceType == model.InsuranceType)
                        .OrderBy(s => s.Code),
                    "InvestigationServiceTypeId",
                    "Name",
                    model.InvestigationServiceTypeId
                );
        }

        public async Task<IActionResult> EditCase(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 0)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }

                var model = await caseCreateEditService.GetEditPolicyDetail(id);
                if (model == null)
                {
                    notifyService.Error("Case Not Found!!!");
                    return RedirectToAction(nameof(CreateCase));
                }
                await LoadDropDowns(model.PolicyDetailDto, userEmail);

                var claimsPage = new MvcBreadcrumbNode("New", "CaseCreateEdit", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Create", "CaseCreateEdit", "Add/Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditCase", "CaseCreateEdit", $"Edit Case") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred to get edit case {Id}. {UserEmail} ", id, userEmail);
                notifyService.Error("Error editing case detail. Try Again");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditCase(EditPolicyDto model)
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
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred to edit case {Id}. {UserEmail} ", model.Id, userEmail);
                notifyService.Error("OOPs !!!..Error editing Case detail. Try again.");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.Id });
            }
        }
    }
}