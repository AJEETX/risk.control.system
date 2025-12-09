using System.Net;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
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

namespace risk.control.system.Controllers.Company
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class InvestigationController : Controller
    {
        private const long MAX_FILE_SIZE = 2 * 1024 * 1024; // 2MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };

        private readonly ILogger<InvestigationController> logger;
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IFeatureManager featureManager;
        private readonly INotyfService notifyService;
        private readonly IInvestigationService service;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IPhoneService phoneService;

        public InvestigationController(ILogger<InvestigationController> logger,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IFeatureManager featureManager,
            INotyfService notifyService, IInvestigationService service,
            IEmpanelledAgencyService empanelledAgencyService,
            IPhoneService phoneService)
        {
            this.logger = logger;
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.featureManager = featureManager;
            this.notifyService = notifyService;
            this.service = service;
            this.empanelledAgencyService = empanelledAgencyService;
            this.phoneService = phoneService;
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
                Console.WriteLine(ex.StackTrace);
                logger.LogError(ex.StackTrace);
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

                var companyUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(u => u.Email == currentUserEmail);
                if (companyUser.ClientCompany.LicenseType == LicenseType.Trial)
                {
                    var totalClaimsCreated = context.Investigations.Count(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
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
                Console.WriteLine(ex.StackTrace);
                logger.LogError(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(New));
            }
        }
        [Breadcrumb(" Add New", FromAction = "New")]
        public IActionResult Create()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var model = service.Create(currentUserEmail);
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(Create));
            }
        }
        [Breadcrumb(title: " Add Case", FromAction = "Create")]
        public async Task<IActionResult> CreatePolicy()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                ViewData["CaseEnablerId"] = new SelectList(context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name");
                ViewData["CostCentreId"] = new SelectList(context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name");

                var currentUser = await context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (currentUser.ClientCompany.HasSampleData)
                {
                    var model = service.AddCasePolicy(currentUserEmail);
                    ViewData["InvestigationServiceTypeId"] = new SelectList(context.InvestigationServiceType.Where(i =>
                        i.InsuranceType == model.PolicyDetail.InsuranceType).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name");
                    return View(model);
                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(CreatePolicy));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> CreatePolicy(CreateCaseViewModel model)
        {
            try
            {

                if (model == null || model.PolicyDetail == null)
                {
                    notifyService.Error("OOPs !!!..Incomplete/Invalid input");

                    return RedirectToAction(nameof(InvestigationController.CreatePolicy), "Investigation");
                }
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                var file = model.Document;

                if (file == null || file.Length == 0)
                {
                    notifyService.Error("Invalid Document Image ");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }
                if (file.Length > MAX_FILE_SIZE)
                {
                    notifyService.Error($"Document image Size exceeds the max size: 5MB");
                    ModelState.AddModelError(nameof(model.Document), "File too large.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExt.Contains(ext))
                {
                    notifyService.Error($"Invalid Document image type");
                    ModelState.AddModelError(nameof(model.Document), "Invalid file type.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                if (!AllowedMime.Contains(file.ContentType))
                {
                    notifyService.Error($"Invalid Document Image content type");
                    ModelState.AddModelError(nameof(model.Document), "Invalid Document Image  content type.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                if (!ImageSignatureValidator.HasValidSignature(file))
                {
                    notifyService.Error($"Invalid or corrupted Document Image ");
                    ModelState.AddModelError(nameof(model.Document), "Invalid file content.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                // Business validation: dates
                if (model.PolicyDetail.DateOfIncident > DateTime.UtcNow)
                {
                    notifyService.Error($"Incident date cannot be in the future");
                    ModelState.AddModelError("PolicyDetail.DateOfIncident", "Incident date cannot be in the future.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }
                if (model.PolicyDetail.ContractIssueDate > DateTime.UtcNow)
                {
                    notifyService.Error($"Issue date cannot be in the future");
                    ModelState.AddModelError("PolicyDetail.ContractIssueDate", "Contract issue date cannot be in the future.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }
                if (model.PolicyDetail.DateOfIncident < model.PolicyDetail.ContractIssueDate)
                {
                    notifyService.Error($"Incident cannot be before Issue date");
                    ModelState.AddModelError("PolicyDetail.DateOfIncident", "Incident cannot be before issue date.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                model.PolicyDetail.ContractNumber = WebUtility.HtmlEncode(model.PolicyDetail.ContractNumber);
                model.PolicyDetail.CauseOfLoss = WebUtility.HtmlEncode(model.PolicyDetail.CauseOfLoss);
                model.PolicyDetail.Comments = WebUtility.HtmlEncode(model.PolicyDetail.Comments);

                // Validate file content-type
                var allowedTypes = new[] { "image/jpeg", "image/png" };
                if (!allowedTypes.Contains(file.ContentType))
                {
                    notifyService.Error("Invalid file format");
                    ModelState.AddModelError(nameof(model.Document), "Invalid file content.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var claim = await service.CreatePolicy(currentUserEmail, model);
                if (claim == null)
                {
                    notifyService.Error("Error Creating Case detail");
                    return RedirectToAction(nameof(InvestigationController.CreatePolicy), "Investigation");
                }
                else
                {
                    notifyService.Custom($"Policy <b>#{claim.PolicyDetail.ContractNumber}</b> created successfully", 3, "green", "far fa-file-powerpoint");
                }
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = claim.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        private void LoadDropDowns(PolicyDetailDto model)
        {
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

            if (model != null)
            {
                ViewData["InvestigationServiceTypeId"] = new SelectList(
                    context.InvestigationServiceType
                        .Where(i => i.InsuranceType == model.InsuranceType)
                        .OrderBy(s => s.Code),
                    "InvestigationServiceTypeId",
                    "Name",
                    model.InvestigationServiceTypeId
                );
            }
        }

        [Breadcrumb(title: " Edit Case", FromAction = "Details")]
        public async Task<IActionResult> EditPolicy(long id)
        {
            if (id < 0)
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var claimsInvestigation = await context.Investigations
                    .Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("Case Not Found!!!");
                    return RedirectToAction(nameof(CreatePolicy));
                }
                var model = new EditPolicyDto
                {
                    Id = claimsInvestigation.Id,
                    PolicyDetail = new PolicyDetailDto
                    {
                        CaseEnablerId = claimsInvestigation.PolicyDetail.CaseEnablerId,
                        CauseOfLoss = claimsInvestigation.PolicyDetail.CauseOfLoss,
                        Comments = claimsInvestigation.PolicyDetail.Comments,
                        CostCentreId = claimsInvestigation.PolicyDetail.CostCentreId,
                        ContractIssueDate = claimsInvestigation.PolicyDetail.ContractIssueDate,
                        ContractNumber = claimsInvestigation.PolicyDetail.ContractNumber,
                        DateOfIncident = claimsInvestigation.PolicyDetail.DateOfIncident,
                        InvestigationServiceTypeId = claimsInvestigation.PolicyDetail.InvestigationServiceTypeId,
                        SumAssuredValue = claimsInvestigation.PolicyDetail.SumAssuredValue,
                    },
                    ExistingDocumentPath = claimsInvestigation.PolicyDetail.DocumentPath
                };
                var currentUser = await context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                ViewData["InvestigationServiceTypeId"] = new SelectList(context.InvestigationServiceType.Where(i =>
                        i.InsuranceType == claimsInvestigation.PolicyDetail.InsuranceType).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);

                var claimsPage = new MvcBreadcrumbNode("New", "Investigation", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "Investigation", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditPolicy", "Investigation", $"Edit Case") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(CreatePolicy));
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> EditPolicy(EditPolicyDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                var file = model.Document;

                if (file == null || file.Length == 0)
                {
                    notifyService.Error("Invalid Document");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }
                if (file.Length > MAX_FILE_SIZE)
                {
                    notifyService.Error("Document File too large");
                    ModelState.AddModelError(nameof(model.Document), "File too large.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExt.Contains(ext))
                {
                    notifyService.Error("Invalid Document type");
                    ModelState.AddModelError(nameof(model.Document), "Invalid file type.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                if (!AllowedMime.Contains(file.ContentType))
                {
                    notifyService.Error("Invalid Document content type");
                    ModelState.AddModelError(nameof(model.Document), "Invalid file content type.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                if (!ImageSignatureValidator.HasValidSignature(file))
                {
                    notifyService.Error("Invalid Document content");
                    ModelState.AddModelError(nameof(model.Document), "Invalid file content.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                // Business validation: dates
                if (model.PolicyDetail.DateOfIncident > DateTime.UtcNow)
                {
                    notifyService.Error("Incident date cannot be in the future");
                    ModelState.AddModelError("PolicyDetail.DateOfIncident", "Incident date cannot be in the future.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }
                if (model.PolicyDetail.ContractIssueDate > DateTime.UtcNow)
                {
                    notifyService.Error("Issue date cannot be in the future");
                    ModelState.AddModelError("PolicyDetail.ContractIssueDate", "Contract issue date cannot be in the future.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }
                if (model.PolicyDetail.DateOfIncident < model.PolicyDetail.ContractIssueDate)
                {
                    notifyService.Error("Incident cannot be before issue date");
                    ModelState.AddModelError("PolicyDetail.DateOfIncident", "Incident cannot be before issue date.");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                model.PolicyDetail.ContractNumber = WebUtility.HtmlEncode(model.PolicyDetail.ContractNumber);
                model.PolicyDetail.CauseOfLoss = WebUtility.HtmlEncode(model.PolicyDetail.CauseOfLoss);
                model.PolicyDetail.Comments = WebUtility.HtmlEncode(model.PolicyDetail.Comments);

                // Validate file content-type
                var allowedTypes = new[] { "image/jpeg", "image/png" };
                if (!allowedTypes.Contains(file.ContentType))
                {
                    notifyService.Error("Invalid file format");
                    LoadDropDowns(model.PolicyDetail);
                    return View(model);
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var savedTask = await service.EditPolicy(currentUserEmail, model);
                notifyService.Custom($"Policy <b>#{savedTask.PolicyDetail.ContractNumber}</b> edited successfully", 3, "orange", "far fa-file-powerpoint");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = model.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Add Customer", FromAction = "Details")]
        public async Task<IActionResult> CreateCustomer(long id)
        {
            if (id < 1)
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var claimsPage = new MvcBreadcrumbNode("New", "Investigation", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "Investigation", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateCustomer", "Investigation", $"Create Customer") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                var currentUser = await context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (currentUser.ClientCompany.HasSampleData)
                {
                    var pinCode = context.PinCode.Include(s => s.Country).OrderBy(s => s.Name).FirstOrDefault(s => s.Country.CountryId == currentUser.ClientCompany.CountryId);
                    var random = new Random();
                    var customerDetail = new CustomerDetail
                    {
                        InvestigationTaskId = id,
                        Addressline = random.Next(10, 99) + " Main Road",
                        PhoneNumber = pinCode.Country.Code.ToLower() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                        DateOfBirth = DateTime.Now.AddYears(-random.Next(25, 77)).AddDays(20),
                        Education = Education.PROFESSIONAL,
                        Income = Income.UPPER_INCOME,
                        Name = NameGenerator.GenerateName(),
                        Occupation = Occupation.SELF_EMPLOYED,
                        //CustomerType = CustomerType.HNI,
                        //Description = "DODGY PERSON",
                        Country = pinCode.Country,
                        CountryId = pinCode.CountryId,
                        SelectedCountryId = pinCode.CountryId.GetValueOrDefault(),
                        StateId = pinCode.StateId,
                        SelectedStateId = pinCode.StateId.GetValueOrDefault(),
                        DistrictId = pinCode.DistrictId,
                        SelectedDistrictId = pinCode.DistrictId.GetValueOrDefault(),
                        PinCodeId = pinCode.PinCodeId,
                        SelectedPincodeId = pinCode.PinCodeId,
                        Gender = Gender.MALE,
                    };
                    LoadDropDowns(customerDetail);

                    return View(customerDetail);
                }
                var blankCustomerDetail = new CustomerDetail { Country = currentUser.ClientCompany.Country, CountryId = currentUser.ClientCompany.CountryId, InvestigationTaskId = id };
                LoadDropDowns(blankCustomerDetail);
                return View(blankCustomerDetail);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(CreatePolicy));
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> CreateCustomer(CustomerDetail customerDetail)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors and try again.");
                    LoadDropDowns(customerDetail);
                    return View(customerDetail);
                }

                // Validate image signature
                if (!ImageSignatureValidator.HasValidSignature(customerDetail.ProfileImage))
                {
                    notifyService.Error("Invalid or corrupted image");
                    ModelState.AddModelError(nameof(customerDetail.ProfileImage), "Invalid or corrupted image.");
                    LoadDropDowns(customerDetail);
                    return View(customerDetail);
                }

                // Validate phone number
                var country = await context.Country.FindAsync(customerDetail.SelectedCountryId);
                if (await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                {
                    if (!phoneService.IsValidMobileNumber(customerDetail.PhoneNumber, country.ISDCode.ToString()))
                    {
                        notifyService.Error("Invalid mobile number");
                        ModelState.AddModelError(nameof(customerDetail.PhoneNumber), "Invalid mobile number.");
                        LoadDropDowns(customerDetail);
                        return View(customerDetail);
                    }
                }

                // Save customer
                var currentUserEmail = HttpContext.User.Identity.Name;
                var result = await service.CreateCustomer(currentUserEmail, customerDetail);

                if (result == null)
                {
                    notifyService.Error("Error creating customer.");
                    LoadDropDowns(customerDetail);
                    return View(customerDetail);
                }
                notifyService.Custom($"Customer <b>{customerDetail.Name}</b> added successfully", 3, "green", "fas fa-user-plus");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = customerDetail.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating customer");
                notifyService.Error("Unexpected error. Please try again.");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = customerDetail.InvestigationTaskId });
            }
        }
        private void LoadDropDowns(CustomerDetail model)
        {
            var pinCode = context.PinCode.Include(s => s.Country).OrderBy(s => s.Name).FirstOrDefault(s => (s.Country.CountryId == model.SelectedCountryId || s.Country.CountryId == model.CountryId));
            model.Country = pinCode.Country;
            model.CountryId = pinCode.CountryId;
            model.SelectedCountryId = pinCode.CountryId.GetValueOrDefault();
            model.StateId = pinCode.StateId;
            model.SelectedStateId = pinCode.StateId.GetValueOrDefault();
            model.DistrictId = pinCode.DistrictId;
            model.SelectedDistrictId = pinCode.DistrictId.GetValueOrDefault();
            model.PinCodeId = pinCode.PinCodeId;
            model.SelectedPincodeId = pinCode.PinCodeId;
            // Enum dropdowns
            ViewData["GenderList"] = new SelectList(Enum.GetValues(typeof(Gender)).Cast<Gender>(), model.Gender);
            ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);
            ViewData["EducationList"] = new SelectList(Enum.GetValues(typeof(Education)).Cast<Education>(), model.Education);
            ViewData["OccupationList"] = new SelectList(Enum.GetValues(typeof(Occupation)).Cast<Occupation>(), model.Occupation);
        }

        [Breadcrumb(title: " Edit Customer", FromAction = "Details")]
        public async Task<IActionResult> EditCustomer(long id)
        {
            if (id < 1)
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var customer = await context.CustomerDetail
                    .Include(c => c.PinCode)
                    .Include(c => c.District)
                    .Include(c => c.State)
                    .Include(c => c.Country)
                    .FirstOrDefaultAsync(i => i.InvestigationTaskId == id);

                if (customer == null)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CreatePolicy));
                }
                var currentUser = await context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("New", "Investigation", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "Investigation", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditCustomer", "Investigation", $"Edit Customer") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;
                LoadDropDowns(customer);

                return View(customer);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditCustomer(long investigationTaskId, CustomerDetail customerDetail)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors and try again.");
                    LoadDropDowns(customerDetail);
                    return View(customerDetail);
                }

                // Validate image signature
                if (!ImageSignatureValidator.HasValidSignature(customerDetail.ProfileImage))
                {
                    notifyService.Error("Invalid or corrupted image");
                    ModelState.AddModelError(nameof(customerDetail.ProfileImage), "Invalid or corrupted image.");
                    LoadDropDowns(customerDetail);
                    return View(customerDetail);
                }

                // Validate phone number
                var country = await context.Country.FindAsync(customerDetail.SelectedCountryId);
                if (await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                {
                    if (!phoneService.IsValidMobileNumber(customerDetail.PhoneNumber, country.ISDCode.ToString()))
                    {
                        notifyService.Error("Invalid mobile number");
                        ModelState.AddModelError(nameof(customerDetail.PhoneNumber), "Invalid mobile number.");
                        LoadDropDowns(customerDetail);
                        return View(customerDetail);
                    }
                }

                var currentUserEmail = HttpContext.User.Identity.Name;

                var company = await service.EditCustomer(currentUserEmail, customerDetail);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Error edting customer");
                    LoadDropDowns(customerDetail);
                    return View(customerDetail);
                }
                notifyService.Custom($"Customer <b>{customerDetail.Name}</b> edited successfully", 3, "orange", "fas fa-user-plus");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = customerDetail.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Error edting customer");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = customerDetail.InvestigationTaskId });
            }
        }

        [Breadcrumb("Add Beneficiary", FromAction = "Details")]
        public async Task<IActionResult> CreateBeneficiary(long id)
        {
            if (id < 1)
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                ViewData["BeneficiaryRelationId"] = new SelectList(context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");

                var claimsPage = new MvcBreadcrumbNode("New", "Investigation", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "Investigation", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateBeneficiary", "Investigation", $"Add beneficiary") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;
                var currentUser = await context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                if (currentUser.ClientCompany.HasSampleData)
                {
                    var beneRelationId = context.BeneficiaryRelation.FirstOrDefault().BeneficiaryRelationId;
                    var pinCode = context.PinCode.Include(s => s.Country).OrderBy(p => p.StateId).LastOrDefault(s => s.Country.CountryId == currentUser.ClientCompany.CountryId);
                    var random = new Random();

                    var model = new BeneficiaryDetail
                    {
                        InvestigationTaskId = id,
                        Addressline = random.Next(10, 99) + " Main Road",
                        DateOfBirth = DateTime.Now.AddYears(-random.Next(25, 77)).AddMonths(3),
                        Income = Income.MEDIUM_INCOME,
                        Name = NameGenerator.GenerateName(),
                        BeneficiaryRelationId = beneRelationId,
                        Country = pinCode.Country,
                        CountryId = pinCode.CountryId,
                        SelectedCountryId = pinCode.CountryId.GetValueOrDefault(),
                        StateId = pinCode.StateId,
                        SelectedStateId = pinCode.StateId.GetValueOrDefault(),
                        DistrictId = pinCode.DistrictId,
                        SelectedDistrictId = pinCode.DistrictId.GetValueOrDefault(),
                        PinCodeId = pinCode.PinCodeId,
                        SelectedPincodeId = pinCode.PinCodeId,
                        PhoneNumber = pinCode.Country.Code.ToLower() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                    };
                    LoadDropDowns(model);

                    return View(model);
                }
                else
                {
                    var model = new BeneficiaryDetail { InvestigationTaskId = id, Country = currentUser.ClientCompany.Country, CountryId = currentUser.ClientCompany.CountryId };
                    LoadDropDowns(model);
                    return View(model);
                }
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
        public async Task<IActionResult> CreateBeneficiary(long investigationTaskId, BeneficiaryDetail beneficiary)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors and try again.");
                    LoadDropDowns(beneficiary);
                    return View(beneficiary);
                }

                // Validate image signature
                if (!ImageSignatureValidator.HasValidSignature(beneficiary.ProfileImage))
                {
                    notifyService.Error("Invalid or corrupted image");
                    ModelState.AddModelError(nameof(beneficiary.ProfileImage), "Invalid or corrupted image.");
                    LoadDropDowns(beneficiary);
                    return View(beneficiary);
                }

                // Validate phone number
                var country = await context.Country.FindAsync(beneficiary.SelectedCountryId);
                if (await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                {
                    if (!phoneService.IsValidMobileNumber(beneficiary.PhoneNumber, country.ISDCode.ToString()))
                    {
                        notifyService.Error("Invalid mobile number");
                        ModelState.AddModelError(nameof(beneficiary.PhoneNumber), "Invalid mobile number.");
                        LoadDropDowns(beneficiary);
                        return View(beneficiary);
                    }
                }
                var currentUserEmail = HttpContext.User.Identity.Name;

                var company = await service.CreateBeneficiary(currentUserEmail, investigationTaskId, beneficiary);
                if (company == null)
                {
                    notifyService.Warning("Error creating Beneficiary !!! ");
                    LoadDropDowns(beneficiary);
                    return View(beneficiary);
                }
                notifyService.Custom($"Beneficiary <b>{beneficiary.Name}</b> added successfully", 3, "green", "fas fa-user-tie");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = beneficiary.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Warning("Error creating Beneficiary !!! ");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = beneficiary.InvestigationTaskId });
            }
        }

        private void LoadDropDowns(BeneficiaryDetail model)
        {
            var pinCode = context.PinCode.Include(s => s.Country).OrderBy(s => s.Name).FirstOrDefault(s => (s.Country.CountryId == model.SelectedCountryId || s.Country.CountryId == model.CountryId));
            model.Country = pinCode.Country;
            model.CountryId = pinCode.CountryId;
            model.SelectedCountryId = pinCode.CountryId.GetValueOrDefault();
            model.StateId = pinCode.StateId;
            model.SelectedStateId = pinCode.StateId.GetValueOrDefault();
            model.DistrictId = pinCode.DistrictId;
            model.SelectedDistrictId = pinCode.DistrictId.GetValueOrDefault();
            model.PinCodeId = pinCode.PinCodeId;
            model.SelectedPincodeId = pinCode.PinCodeId;
            // Enum dropdowns
            ViewData["BeneficiaryRelationId"] = new SelectList(context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", model.BeneficiaryRelationId);
            ViewData["IncomeList"] = new SelectList(Enum.GetValues(typeof(Income)).Cast<Income>(), model.Income);
        }
        [Breadcrumb("Edit Beneficiary", FromAction = "Details")]
        public IActionResult EditBeneficiary(long id)
        {
            if (id < 1)
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(CreatePolicy));
            }
            try
            {
                var beneficiary = context.BeneficiaryDetail
                    .Include(v => v.PinCode)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.Country)
                    .Include(v => v.BeneficiaryRelation)
                    .First(v => v.BeneficiaryDetailId == id);
                ViewData["BeneficiaryRelationId"] = new SelectList(context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name", beneficiary.BeneficiaryRelationId);
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("New", "Investigation", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "Investigation", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "Investigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateBeneficiary", "Investigation", $"Edit beneficiary") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;
                LoadDropDowns(beneficiary);

                return View(beneficiary);
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
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> EditBeneficiary(long beneficiaryDetailId, BeneficiaryDetail beneficiary)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors and try again.");
                    LoadDropDowns(beneficiary);
                    return View(beneficiary);
                }

                // Validate image signature
                if (!ImageSignatureValidator.HasValidSignature(beneficiary.ProfileImage))
                {
                    notifyService.Error("Invalid or corrupted image");
                    ModelState.AddModelError(nameof(beneficiary.ProfileImage), "Invalid or corrupted image.");
                    LoadDropDowns(beneficiary);
                    return View(beneficiary);
                }

                // Validate phone number
                var country = await context.Country.FindAsync(beneficiary.SelectedCountryId);
                if (await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                {
                    if (!phoneService.IsValidMobileNumber(beneficiary.PhoneNumber, country.ISDCode.ToString()))
                    {
                        notifyService.Error("Invalid mobile number");
                        ModelState.AddModelError(nameof(beneficiary.PhoneNumber), "Invalid mobile number.");
                        LoadDropDowns(beneficiary);
                        return View(beneficiary);
                    }
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var company = await service.EditBeneficiary(currentUserEmail, beneficiaryDetailId, beneficiary);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Error editing beneficiary");
                    LoadDropDowns(beneficiary);
                    return View(beneficiary);

                }
                notifyService.Custom($"Beneficiary <b>{beneficiary.Name}</b> edited successfully", 3, "orange", "fas fa-user-tie");

                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = beneficiary.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Error editing beneficiary");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = beneficiary.InvestigationTaskId });
            }
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
                var currentUser = context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
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
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
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
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                var model = await service.GetClaimDetails(currentUserEmail, id);
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

                var vendorUserCount = await context.VendorApplicationUser.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted && c.Role == AppRoles.AGENT);

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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}
