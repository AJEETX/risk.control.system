using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Company
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class InvestigationController : Controller
    {
        private readonly ILogger<InvestigationController> logger;
        private readonly ICaseCreateEditService createCreateEditService;
        private readonly ICustomerCreateEditService customerCreateEditService;
        private readonly IBeneficiaryCreateEditService beneficiaryCreateEditService;
        private readonly ApplicationDbContext context;
        private readonly INotyfService notifyService;
        private readonly IInvestigationService service;
        private readonly IEmpanelledAgencyService empanelledAgencyService;

        public InvestigationController(ILogger<InvestigationController> logger,
            ICaseCreateEditService createCreateEditService,
            ICustomerCreateEditService customerCreateEditService,
            IBeneficiaryCreateEditService beneficiaryCreateEditService,
            ApplicationDbContext context,
            INotyfService notifyService,
            IInvestigationService service,
            IEmpanelledAgencyService empanelledAgencyService)
        {
            this.logger = logger;
            this.createCreateEditService = createCreateEditService;
            this.customerCreateEditService = customerCreateEditService;
            this.beneficiaryCreateEditService = beneficiaryCreateEditService;
            this.context = context;
            this.notifyService = notifyService;
            this.service = service;
            this.empanelledAgencyService = empanelledAgencyService;
        }
        public IActionResult Index()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return RedirectToAction("New");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(" Add/Assign")]
        public async Task<IActionResult> New()
        {
            try
            {
                bool userCanCreate = true;
                int availableCount = 0;
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await context.ApplicationUser.Include(u => u.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
                if (companyUser.ClientCompany.LicenseType == LicenseType.Trial)
                {
                    var totalClaimsCreated = await context.Investigations.CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
                    availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated;
                    if (totalClaimsCreated >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                    {
                        userCanCreate = false;
                        notifyService.Information($"MAX Case limit = <b>{companyUser.ClientCompany.TotalCreatedClaimAllowed}</b> reached");
                    }
                }
                var totalReadyToAssign = await service.GetAutoCount(currentUserEmail);
                var hasClaim = totalReadyToAssign > 0;
                var fileIdentifier = companyUser.ClientCompany.Country.Code.ToLower();
                userCanCreate = userCanCreate && companyUser.ClientCompany.TotalToAssignMaxAllowed > totalReadyToAssign;

                if (!userCanCreate)
                {
                    notifyService.Custom($"MAX Assign Case limit = <b>{companyUser.ClientCompany.TotalToAssignMaxAllowed}</b> reached", 5, "#dc3545", "fa fa-upload");
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
                logger.LogError(ex, "Error occurred");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(New));
            }
        }
        [Breadcrumb(" Add New", FromAction = "New")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var model = await createCreateEditService.Create(currentUserEmail);
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
                logger.LogError(ex, "Error occurred");
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(Create));
            }
        }
        [Breadcrumb(title: " Add Case", FromAction = "Create")]
        public async Task<IActionResult> CreateCase()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

                if (currentUser.ClientCompany.HasSampleData)
                {
                    var model = await createCreateEditService.AddCasePolicy(userEmail);
                    await LoadDropDowns(model.PolicyDetail, userEmail);
                    return View(model);
                }
                else
                {
                    ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                    ViewData["CaseEnablerId"] = new SelectList(context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name");
                    ViewData["CostCentreId"] = new SelectList(context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name");
                    ViewData["InsuranceType"] = new SelectList(Enum.GetValues(typeof(InsuranceType)).Cast<InsuranceType>());
                    ViewData["InvestigationServiceTypeId"] = new SelectList(context.InvestigationServiceType.OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name");
                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred");
                notifyService.Error("Error creating case detail. Try Again.");
                return RedirectToAction(nameof(CreateCase));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> CreateCase(CreateCaseViewModel model)
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;

                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadDropDowns(model.PolicyDetail, userEmail);
                    return View(model);
                }
                var result = await createCreateEditService.CreateAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await LoadDropDowns(model.PolicyDetail, userEmail);
                    return View(model);
                }
                notifyService.Success($"Policy #{result.CaseId} created successfully");
                return RedirectToAction(nameof(Details), "Investigation", new { id = result.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred");
                notifyService.Error("Error creating case detail. Try Again.");
                return RedirectToAction(nameof(CreateCase));
            }
        }
        private async Task LoadDropDowns(PolicyDetailDto model, string userEmail)
        {
            var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

            ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

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

        [Breadcrumb(title: " Edit Case", FromAction = "Details")]
        public async Task<IActionResult> EditCase(long id)
        {
            try
            {
                if (id < 0)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await createCreateEditService.GetEditPolicyDetail(id);
                if (model == null)
                {
                    notifyService.Error("Case Not Found!!!");
                    return RedirectToAction(nameof(CreateCase));
                }
                var userEmail = HttpContext.User?.Identity?.Name;
                await LoadDropDowns(model.PolicyDetail, userEmail);

                var claimsPage = new MvcBreadcrumbNode("New", "Investigation", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "Investigation", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditPolicy", "Investigation", $"Edit Case") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred");
                notifyService.Error("Error editing case detail. Try Again");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> EditCase(EditPolicyDto model)
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;

                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadDropDowns(model.PolicyDetail, userEmail);
                    return View(model);
                }
                var result = await createCreateEditService.EditAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await LoadDropDowns(model.PolicyDetail, userEmail);
                    return View(model);
                }
                notifyService.Custom($"Policy <b>#{result.CaseId}</b> edited successfully", 3, "orange", "far fa-file-powerpoint");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred");
                notifyService.Error("OOPs !!!..Error editing Case detail. Try again.");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.Id });
            }
        }

        [Breadcrumb(title: " Add Customer", FromAction = "Details")]
        public async Task<IActionResult> CreateCustomer(long id)
        {
            try
            {
                if (id < 1)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CreateCase));
                }
                var userEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

                if (currentUser.ClientCompany.HasSampleData)
                {
                    var customerDetail = await customerCreateEditService.GetCustomerDetailAsync(id, currentUser.ClientCompany.CountryId.Value);
                    await LoadDropDowns(customerDetail, currentUser);
                    return View(customerDetail);
                }
                else
                {
                    ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                    ViewData["GenderList"] = new SelectList(Enum.GetValues(typeof(Gender)).Cast<Gender>());
                    ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>());
                    ViewData["EducationList"] = new SelectList(Enum.GetValues(typeof(Education)).Cast<Education>());
                    ViewData["OccupationList"] = new SelectList(Enum.GetValues(typeof(Occupation)).Cast<Occupation>());

                    var blankCustomerDetail = new CustomerDetail { Country = currentUser.ClientCompany.Country, CountryId = currentUser.ClientCompany.CountryId, InvestigationTaskId = id };

                    var claimsPage = new MvcBreadcrumbNode("New", "Investigation", "Cases");
                    var agencyPage = new MvcBreadcrumbNode("New", "Investigation", "Assign") { Parent = claimsPage, };
                    var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                    var editPage = new MvcBreadcrumbNode("CreateCustomer", "Investigation", $"Create Customer") { Parent = details1Page, RouteValues = new { id = id } };
                    ViewData["BreadcrumbNode"] = editPage;

                    return View(blankCustomerDetail);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating customer");
                notifyService.Error("Error creating customer. Try Again");
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> CreateCustomer(CustomerDetail model)
        {
            try
            {
                var userEmail = HttpContext.User.Identity.Name;
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
                return RedirectToAction(nameof(Details), "Investigation", new { id = model.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating customer");
                notifyService.Error("Error creating customer.Try Again");
                return RedirectToAction(nameof(Details), new { id = model.InvestigationTaskId });
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
            ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
            ViewData["GenderList"] = new SelectList(Enum.GetValues(typeof(Gender)).Cast<Gender>(), model.Gender);
            ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);
            ViewData["EducationList"] = new SelectList(Enum.GetValues(typeof(Education)).Cast<Education>(), model.Education);
            ViewData["OccupationList"] = new SelectList(Enum.GetValues(typeof(Occupation)).Cast<Occupation>(), model.Occupation);
        }

        [Breadcrumb(title: " Edit Customer", FromAction = "Details")]
        public async Task<IActionResult> EditCustomer(long id)
        {
            try
            {
                if (id < 1)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Create));
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
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                ViewData["GenderList"] = new SelectList(Enum.GetValues(typeof(Gender)).Cast<Gender>(), model.Gender);
                ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);
                ViewData["EducationList"] = new SelectList(Enum.GetValues(typeof(Education)).Cast<Education>(), model.Education);
                ViewData["OccupationList"] = new SelectList(Enum.GetValues(typeof(Occupation)).Cast<Occupation>(), model.Occupation);

                var claimsPage = new MvcBreadcrumbNode("New", "Investigation", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "Investigation", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditCustomer", "Investigation", $"Edit Customer") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing Customer.");
                notifyService.Error("Error editing Customer.Try Again");
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditCustomer(long investigationTaskId, CustomerDetail model)
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
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
                logger.LogError(ex, "Error edting customer.");
                notifyService.Error("Error edting customer. Try again.");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.InvestigationTaskId });
            }
        }

        [Breadcrumb("Add Beneficiary", FromAction = "Details")]
        public async Task<IActionResult> CreateBeneficiary(long id)
        {
            try
            {
                if (id < 1)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);

                var claimsPage = new MvcBreadcrumbNode("New", "Investigation", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "Investigation", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateBeneficiary", "Investigation", $"Add beneficiary") { Parent = details1Page, RouteValues = new { id = id } };
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

                    ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                    ViewData["BeneficiaryRelationId"] = new SelectList(context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");
                    ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>());

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating beneficiary.");
                notifyService.Error("Error creating beneficiary. Try again.");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = id });
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBeneficiary(long investigationTaskId, BeneficiaryDetail model)
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
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
                return RedirectToAction(nameof(Details), "Investigation", new { id = model.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Beneficiary.");
                notifyService.Warning("Error creating Beneficiary. Try again. ");
                return RedirectToAction(nameof(Details), "Investigation", new { id = model.InvestigationTaskId });
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
            ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
            ViewData["BeneficiaryRelationId"] = new SelectList(context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", model.BeneficiaryRelationId);
            ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);
        }
        [Breadcrumb("Edit Beneficiary", FromAction = "Details")]
        public async Task<IActionResult> EditBeneficiary(long id, long taskId)
        {
            try
            {
                if (id < 1)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CreateCase));
                }
                var model = await context.BeneficiaryDetail
                    .Include(v => v.PinCode)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.Country)
                    .Include(v => v.BeneficiaryRelation)
                    .FirstOrDefaultAsync(v => v.BeneficiaryDetailId == id);
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                ViewData["BeneficiaryRelationId"] = new SelectList(context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", model.BeneficiaryRelationId);
                ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);

                var claimsPage = new MvcBreadcrumbNode("New", "Investigation", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "Investigation", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = model.InvestigationTaskId } };
                var editPage = new MvcBreadcrumbNode("EditBeneficiary", "Investigation", $"Edit beneficiary") { Parent = details1Page, RouteValues = new { id = taskId } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing Beneficiary.");
                notifyService.Error("Error editing Beneficiary. Try again.");
                return RedirectToAction(nameof(Details), "Investigation", new { id = taskId });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> EditBeneficiary(long beneficiaryDetailId, BeneficiaryDetail model)
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
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
                logger.LogError(ex, "Error editing Beneficiary.");
                notifyService.Error("Error editing Beneficiary. Try again.");
            }
            return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.InvestigationTaskId });
        }

        [Breadcrumb(" Empanelled Agencies", FromAction = "New")]
        public async Task<IActionResult> EmpanelledVendors(long id, long vendorId = 0, bool fromEditPage = false)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (id < 1)
                {
                    notifyService.Error("No Case selected!!!. Please select Case to allocate.");
                    return RedirectToAction(nameof(New));
                }

                var model = await empanelledAgencyService.GetEmpanelledVendors(id);
                model.FromEditPage = fromEditPage;
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (vendorId > 0)
                {
                    model.VendorId = vendorId;
                }
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                notifyService.Error("Error getting Agencies. Try again.");
                return RedirectToAction(nameof(New));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReportTemplate(long caseId)
        {
            var template = await empanelledAgencyService.GetReportTemplate(caseId);

            return PartialView("_ReportTemplate", template);
        }

        [Breadcrumb("Details", FromAction = "New")]
        public async Task<IActionResult> Details(long id)
        {
            if (id < 1)
            {
                notifyService.Error("Case Not Found.Try Again");
                return RedirectToAction(nameof(New));
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                var model = await service.GetCaseDetails(currentUserEmail, id);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting case details.");
                notifyService.Error("Error getting case details. Try again.");
                return RedirectToAction(nameof(New));
            }
        }
        [Breadcrumb(" Agency Detail", FromAction = "EmpanelledVendors")]
        public async Task<IActionResult> VendorDetail(long id, long selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (id == 0 || selectedcase < 1)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendor = await context.Vendor
                    .Include(v => v.ratings)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .FirstOrDefaultAsync(m => m.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS!!!.Agency Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

                var vendorAllCasesCount = await context.Investigations.CountAsync(c => c.VendorId == vendor.VendorId &&
                !c.Deleted &&
                (c.SubStatus == approvedStatus ||
                c.SubStatus == rejectedStatus));

                var vendorUserCount = await context.ApplicationUser.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted && c.Role == AppRoles.AGENT);

                // HACKY
                var currentCases = service.GetAgencyIdsLoad(new List<long> { vendor.VendorId });
                vendor.SelectedCountryId = vendorUserCount;
                vendor.SelectedStateId = currentCases.FirstOrDefault().CaseCount;
                vendor.SelectedDistrictId = vendorAllCasesCount;
                vendor.SelectedPincodeId = selectedcase;

                var claimsPage = new MvcBreadcrumbNode("New", "Investigation", "Case");
                var agencyPage = new MvcBreadcrumbNode("New", "Investigation", "Assign") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("EmpanelledVendors", "Investigation", $"Empanelled Agencies") { Parent = agencyPage, RouteValues = new { id = selectedcase } };
                var editPage = new MvcBreadcrumbNode("VendorDetail", "Investigation", $"Agency Detail") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting case details.");
                notifyService.Error("Error getting case details. Try again.");
                return RedirectToAction(nameof(New));
            }
        }
    }
}
