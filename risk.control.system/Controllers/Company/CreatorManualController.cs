using AspNetCoreHero.ToastNotification.Abstractions;
using AspNetCoreHero.ToastNotification.Notyf;

using Google.Api;

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
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    [Breadcrumb(" Claims")]
    public class CreatorManualController : Controller
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

        public CreatorManualController(ApplicationDbContext context,
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
        [Breadcrumb(" Assign(manual)")]
        public IActionResult New()
        {
            try
            {
                bool userCanCreate = true;
                int availableCount = 0;
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).ThenInclude(c=>c.Country).FirstOrDefault(u => u.Email == currentUserEmail);
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
                var createdClaimsStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(s => s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
                var withDrawnClaimsStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(s => s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);
                var declinedClaimsStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(s => s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);
                var reviewClaimsStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(s => s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
                var hasClaim1 = _context.ClaimsInvestigation.Where(c =>
               c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId &&
               !c.Deleted);

                var hasClaim = _context.ClaimsInvestigation.Any(c =>
                 c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId &&
                 !c.Deleted &&
                 (c.InvestigationCaseSubStatus == createdClaimsStatus ||
                 c.InvestigationCaseSubStatus == withDrawnClaimsStatus ||
                 c.InvestigationCaseSubStatus == declinedClaimsStatus ||
                 c.InvestigationCaseSubStatus == reviewClaimsStatus
                 ));
                var fileIdentifier = $"{companyUser.ClientCompany.Country.Code.ToLower()}";
                return View(new CreateClaims { BulkUpload = companyUser.ClientCompany.BulkUpload, UserCanCreate = userCanCreate, HasClaims = hasClaim, FileSampleIdentifier = fileIdentifier });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
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

        [Breadcrumb(title: " Add Policy", FromAction = "Create")]
        public async Task<IActionResult> CreatePolicy()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var lineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIMS).LineOfBusinessId;

                ViewData["lineOfBusinessId"] = lineOfBusinessId;
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name");
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name");
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.LineOfBusinessId == lineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name");

                var currentUser = await _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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

        [Breadcrumb(title: " Edit Policy", FromAction = "Details")]
        public async Task<IActionResult> EditPolicy(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (id == null || string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
                    return RedirectToAction(nameof(CreatePolicy));
                }

                var claimsInvestigation = await _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("Claim Not Found!!!");
                    return RedirectToAction(nameof(CreatePolicy));
                }
                var currentUser = await _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i =>
               i.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorManual", "Claims");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorManual", "Assign(manual)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Create", "CreatorManual", $"Add New") { Parent = agencyPage };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorManual", $"Details") { Parent = detailsPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditPolicy", "CreatorManual", $"Edit Policy") { Parent = details1Page, RouteValues = new { id = id } };
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
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (id == null || string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
                    return RedirectToAction(nameof(CreatePolicy));
                }

                var claimsInvestigation = await _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
                    return RedirectToAction(nameof(CreatePolicy));
                }
                var claimsPage = new MvcBreadcrumbNode("New", "CreatorManual", "Claims");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorManual", "Assign(manual)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Create", "CreatorManual", $"Add New") { Parent = agencyPage };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorManual", $"Details") { Parent = detailsPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateCustomer", "CreatorManual", $"Create Customer") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                var currentUser = await _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                if (currentUser.ClientCompany.HasSampleData)
                {
                    var pinCode = _context.PinCode.Include(s => s.Country).FirstOrDefault(s => s.Country.CountryId == currentUser.ClientCompany.CountryId);
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
                        CustomerType = CustomerType.HNI,
                        Description = "DODGY PERSON",
                        Country = pinCode.Country,
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
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (id == null || string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
                    return RedirectToAction(nameof(CreatePolicy));
                }

                var customer = await _context.CustomerDetail
                    .Include(c => c.PinCode)
                    .Include(c => c.District)
                    .Include(c => c.State)
                    .Include(c => c.Country)
                    .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

                if (customer == null)
                {
                    notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
                    return RedirectToAction(nameof(CreatePolicy));
                }
                var currentUser = await _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorManual", "Claims");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorManual", "Assign(manual)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Create", "CreatorManual", $"Add New") { Parent = agencyPage };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorManual", $"Details") { Parent = detailsPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditCustomer", "CreatorManual", $"Edit Customer") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                customer.SelectedCountryId = customer.CountryId.GetValueOrDefault();
                customer.SelectedStateId = customer.StateId.GetValueOrDefault();
                customer.SelectedDistrictId = customer.DistrictId.GetValueOrDefault();
                customer.SelectedPincodeId = customer.PinCodeId.GetValueOrDefault();

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
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorManual", "Claims");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorManual", "Assign(manual)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Create", "CreatorManual", $"Add New") { Parent = agencyPage };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorManual", $"Details") { Parent = detailsPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateBeneficiary", "CreatorManual", $"Add beneficiary") { Parent = details1Page, RouteValues = new { id = id } };
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
                        StateId = pinCode.StateId.GetValueOrDefault(),
                        SelectedStateId = pinCode.StateId.GetValueOrDefault(),
                        DistrictId = pinCode.DistrictId.GetValueOrDefault(),
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
                notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
                return RedirectToAction(nameof(CreatePolicy));
            }

            try
            {
               
                var beneficiary = _context.BeneficiaryDetail
                    .Include(v => v.ClaimsInvestigation)
                    .ThenInclude(c => c.PolicyDetail)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.Country)
                    .Include(v => v.BeneficiaryRelation)
                    .First(v => v.BeneficiaryDetailId == id);

                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name", beneficiary.BeneficiaryRelationId);
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorManual", "Claims");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorManual", "Assign(manual)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Create", "CreatorManual", $"Add New") { Parent = agencyPage };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorManual", $"Details") { Parent = detailsPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateBeneficiary", "CreatorManual", $"Edit beneficiary") { Parent = details1Page, RouteValues = new { id = id } };
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
                    notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await investigationReportService.GetClaimDetails(currentUserEmail, id);
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
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

        [HttpGet]
        [Breadcrumb(" Empanelled Agencies", FromAction = "New")]
        public async Task<IActionResult> EmpanelledVendors(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(New));
                }

                var model = await empanelledAgencyService.GetEmpanelledVendors(selectedcase);

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
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.State)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.LineOfBusiness)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.InvestigationServiceType)
                    .FirstOrDefaultAsync(m => m.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS!!!.Agency Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                ViewBag.Selectedcase = selectedcase;

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorManual", "Claims");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorManual", "Assign(manual)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("EmpanelledVendors", "CreatorManual", $"Empanelled Agencies") { Parent = agencyPage, RouteValues = new { selectedcase = selectedcase } };
                var editPage = new MvcBreadcrumbNode("VendorDetail", "CreatorManual", $"Agency Detail") { Parent = detailsPage, RouteValues = new { id = id } };
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
        [Breadcrumb("Details", FromAction = "Create")]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
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
    }
}
