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
        private readonly ILogger<InvestigationController> logger;
        private readonly ApplicationDbContext context;
        private readonly IFeatureManager featureManager;
        private readonly INotyfService notifyService;
        private readonly IInvestigationService service;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IPhoneService phoneService;

        public InvestigationController(ILogger<InvestigationController> logger,
            ApplicationDbContext context,
            IFeatureManager featureManager,
            INotyfService notifyService, IInvestigationService service,
            IEmpanelledAgencyService empanelledAgencyService,
            IPhoneService phoneService)
        {
            this.logger = logger;
            this.context = context;
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

                return View(claimsInvestigation);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(CreatePolicy));
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
                        Addressline = random.Next(100, 999) + " GOOD STREET",
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
                    return View(customerDetail);
                }
                var blankCustomerDetail = new CustomerDetail { Country = currentUser.ClientCompany.Country, CountryId = currentUser.ClientCompany.CountryId, InvestigationTaskId = id };
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
                        Addressline = random.Next(100, 999) + " GREAT ROAD",
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
                    return View(model);
                }
                else
                {
                    var model = new BeneficiaryDetail { InvestigationTaskId = id, Country = currentUser.ClientCompany.Country, CountryId = currentUser.ClientCompany.CountryId };
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
