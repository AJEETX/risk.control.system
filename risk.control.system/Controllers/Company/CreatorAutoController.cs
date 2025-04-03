using AspNetCoreHero.ToastNotification.Notyf;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;

using SmartBreadcrumbs.Attributes;
using AspNetCoreHero.ToastNotification.Abstractions;
using risk.control.system.Data;
using risk.control.system.Services;
using static risk.control.system.AppConstant.Applicationsettings;
using Microsoft.EntityFrameworkCore;
using Google.Api;
using risk.control.system.Models.ViewModel;
using risk.control.system.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Nodes;
using risk.control.system.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace risk.control.system.Controllers.Company
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public partial class CreatorAutoController : Controller
    {
        private const string CLAIMS = "claims";
        private readonly ApplicationDbContext _context;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly ICreatorService creatorService;
        private readonly IFtpService ftpService;
        private readonly INotyfService notifyService;
        private readonly IInvestigationReportService investigationReportService;
        private readonly IClaimPolicyService claimPolicyService;

        public CreatorAutoController(ApplicationDbContext context,
            IEmpanelledAgencyService empanelledAgencyService,
            IClaimsInvestigationService claimsInvestigationService,
            ICreatorService creatorService,
            IFtpService ftpService,
            INotyfService notifyService,
            IInvestigationReportService investigationReportService,
            IClaimPolicyService claimPolicyService)
        {
            _context = context;
            this.claimPolicyService = claimPolicyService;
            this.empanelledAgencyService = empanelledAgencyService;
            this.claimsInvestigationService = claimsInvestigationService;
            this.creatorService = creatorService;
            this.ftpService = ftpService;
            this.notifyService = notifyService;
            this.investigationReportService = investigationReportService;
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
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(" Add/Assign")]
        public IActionResult New(int uploadId)
        {
            try
            {
                bool userCanCreate = true;
                int availableCount = 0;
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(u => u.Email == currentUserEmail);
                if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
                {
                    var totalClaimsCreated = _context.ClaimsInvestigation.Count(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
                    availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated;
                    if (totalClaimsCreated >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                    {
                        userCanCreate = false;
                        notifyService.Information($"MAX Case limit = <b>{companyUser.ClientCompany.TotalCreatedClaimAllowed}</b> reached");
                    }
                    else
                    {
                        notifyService.Information($"Limit available = <b>{availableCount}</b>");
                    }
                }
                var createdClaimsStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(s => s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
                var hasClaim = _context.ClaimsInvestigation.Any(c => c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId &&
                !c.Deleted &&
                c.InvestigationCaseSubStatus == createdClaimsStatus);
                var fileIdentifier = companyUser.ClientCompany.Country.Code.ToLower();
                
                return View(new CreateClaims { 
                    BulkUpload = companyUser.ClientCompany.BulkUpload, 
                    UserCanCreate = userCanCreate, 
                    HasClaims = hasClaim, 
                    FileSampleIdentifier = fileIdentifier,
                    UploadId = uploadId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
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
                var model = creatorService.Create(currentUserEmail);
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
                var lineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIMS).LineOfBusinessId;

                ViewData["lineOfBusinessId"] = new SelectList(_context.LineOfBusiness.OrderBy(s => s.Code), "LineOfBusinessId", "Name",lineOfBusinessId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i =>
                i.LineOfBusinessId == lineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name");

                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name");
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name");

                var currentUser = await _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c=>c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (currentUser.ClientCompany.HasSampleData)
                {
                    var model = claimPolicyService.AddClaimPolicy(currentUserEmail, lineOfBusinessId);
                    return View(model);
                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(CreatePolicy));
            }
        }

        [Breadcrumb(title: " Edit Case", FromAction = "Details")]
        public async Task<IActionResult> EditPolicy(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var claimsInvestigation = await _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("Case Not Found!!!");
                    return RedirectToAction(nameof(CreatePolicy));
                }
                var currentUser = await _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                ViewData["lineOfBusinessId"] = new SelectList(_context.LineOfBusiness.OrderBy(s => s.Code), "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i =>
                i.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorAuto", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditPolicy", "CreatorAuto", $"Edit Case") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(claimsInvestigation);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(CreatePolicy));
            }
        }

        [Breadcrumb(title: " Add Customer", FromAction = "Details")]
        public async Task<IActionResult> CreateCustomer(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorAuto", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateCustomer", "CreatorAuto", $"Create Customer") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                var currentUser = await _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (currentUser.ClientCompany.HasSampleData)
                {
                    var pinCode = _context.PinCode.Include(s => s.Country).OrderBy(s=>s.Name).FirstOrDefault(s => s.Country.CountryId == currentUser.ClientCompany.CountryId);
                    var random = new Random();
                    var customerDetail = new CustomerDetail
                    {
                        ClaimsInvestigationId = id,
                        Addressline = random.Next(100, 999) + " GOOD STREET",
                        ContactNumber = Applicationsettings.PORTAL_ADMIN_MOBILE,
                        DateOfBirth = DateTime.Now.AddYears(-random.Next(25, 77)).AddDays(20),
                        Education = Education.PROFESSIONAL,
                        Income = Income.UPPER_INCOME,
                        Name = NameGenerator.GenerateName(),
                        Occupation = Occupation.SELF_EMPLOYED,
                        //CustomerType = CustomerType.HNI,
                        //Description = "DODGY PERSON",
                        Country= pinCode.Country,
                        CountryId = pinCode.CountryId,
                        SelectedCountryId = pinCode.CountryId,
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
                    var blankCustomerDetail = new CustomerDetail { Country = currentUser.ClientCompany.Country, CountryId = currentUser.ClientCompany.CountryId, ClaimsInvestigationId = id };
                    return View(blankCustomerDetail);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(CreatePolicy));
            }
        }

        [Breadcrumb(title: " Edit Customer", FromAction = "Details")]
        public async Task<IActionResult> EditCustomer(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var customer = await _context.CustomerDetail
                    .Include(c => c.PinCode)
                    .Include(c => c.District)
                    .Include(c => c.State)
                    .Include(c => c.Country)
                    .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

                if (customer == null)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CreatePolicy));
                }
                var currentUser = await _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorAuto", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditCustomer", "CreatorAuto", $"Edit Customer") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;
                return View(customer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Add Beneficiary", FromAction = "Details")]
        public async Task<IActionResult> CreateBeneficiary(string id)
        {
            if(string.IsNullOrWhiteSpace(id))
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorAuto", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateBeneficiary", "CreatorAuto", $"Add beneficiary") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;
                ViewBag.ClaimId = id;
                var currentUser = await _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                if (currentUser.ClientCompany.HasSampleData)
                {
                    var beneRelationId = _context.BeneficiaryRelation.FirstOrDefault().BeneficiaryRelationId;
                    var pinCode = _context.PinCode.Include(s => s.Country).OrderBy(p=>p.StateId).LastOrDefault(s => s.Country.CountryId == currentUser.ClientCompany.CountryId);
                    var random = new Random();

                    var model = new BeneficiaryDetail
                    {
                        ClaimsInvestigationId = id,
                        Addressline = random.Next(100, 999) + " GREAT ROAD",
                        DateOfBirth = DateTime.Now.AddYears(-random.Next(25, 77)).AddMonths(3),
                        Income = Income.MEDIUM_INCOME,
                        Name = NameGenerator.GenerateName(),
                        BeneficiaryRelationId = beneRelationId,
                        Country = pinCode.Country,
                        CountryId = pinCode.CountryId,
                        SelectedCountryId = pinCode.CountryId,
                        StateId = pinCode.StateId,
                        SelectedStateId = pinCode.StateId.GetValueOrDefault(),
                        DistrictId = pinCode.DistrictId,
                        SelectedDistrictId = pinCode.DistrictId.GetValueOrDefault(),
                        PinCodeId = pinCode.PinCodeId,
                        SelectedPincodeId = pinCode.PinCodeId,
                        ContactNumber = Applicationsettings.PORTAL_ADMIN_MOBILE,
                    };
                    return View(model);
                }
                else
                {
                    var model = new BeneficiaryDetail { ClaimsInvestigationId = id, Country = currentUser.ClientCompany.Country, CountryId = currentUser.ClientCompany.CountryId };
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Edit Beneficiary", FromAction = "Details")]
        public IActionResult EditBeneficiary(long? id)
        {
            if (id == null || id < 1)
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CreatePolicy));
            }
            try
            {
                var beneficiary = _context.BeneficiaryDetail
                    .Include(v => v.PinCode)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.Country)
                    .Include(v => v.BeneficiaryRelation)
                    .First(v => v.BeneficiaryDetailId == id);
                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name", beneficiary.BeneficiaryRelationId);
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorAuto", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateBeneficiary", "CreatorAuto", $"Edit beneficiary") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(beneficiary);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Delete", FromAction = "New")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (id == null || string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await investigationReportService.GetClaimDetails(currentUserEmail, id);
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [Breadcrumb("Details", FromAction = "New")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                var model = await investigationReportService.GetClaimDetails(currentUserEmail, id);
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [HttpGet]
        [Breadcrumb(" Empanelled Agencies", FromAction = "New")]
        public async Task<IActionResult> EmpanelledVendors(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("No Case selected!!!. Please select Case to allocate.");
                    return RedirectToAction(nameof(New));
                }

                var model = await empanelledAgencyService.GetEmpanelledVendors(id);
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Agency Detail", FromAction = "EmpanelledVendors")]
        public async Task<IActionResult> VendorDetail(long id, string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (id == 0 || selectedcase is null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

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
                    notifyService.Error("OOPS!!!.Agency Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
                var rejectedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

                var vendorAllCasesCount = await _context.ClaimsInvestigation.CountAsync(c => c.VendorId == vendor.VendorId &&
                c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId ||
                c.InvestigationCaseSubStatusId == rejectedStatus.InvestigationCaseSubStatusId);

                var vendorUserCount = await _context.VendorApplicationUser.CountAsync(c => c.VendorId == vendor.VendorId);

                // HACKY
                var currentCases = claimsInvestigationService.GetAgencyIdsLoad(new List<long> {vendor.VendorId });
                vendor.SelectedCountryId = vendorUserCount;
                vendor.SelectedStateId = currentCases.FirstOrDefault().CaseCount;
                vendor.SelectedDistrictId = vendorAllCasesCount;
                vendor.MobileAppUrl = selectedcase;

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Case");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("EmpanelledVendors", "CreatorAuto", $"Empanelled Agencies") { Parent = agencyPage, RouteValues = new { id = selectedcase } };
                var editPage = new MvcBreadcrumbNode("VendorDetail", "CreatorAuto", $"Agency Detail") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;


                return View(vendor);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}
