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
    [Breadcrumb(" Claims")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public partial class CreatorAutoController : Controller
    {
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
                    notifyService.Error("OOPs !!!..Contact Admin");
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
        [Breadcrumb(" Assign(auto)")]
        public IActionResult New()
        {
            try
            {
                bool userCanUpload = true;
                int availableCount = 0;
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);
                if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
                {
                    var totalClaimsCreated = _context.ClaimsInvestigation.Count(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
                    availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated;
                    if (totalClaimsCreated >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                    {
                        userCanUpload = false;
                        notifyService.Information($"MAX Claim limit = <b>{companyUser.ClientCompany.TotalCreatedClaimAllowed}</b> reached");
                    }
                    else
                    {
                        notifyService.Information($"Limit available = <b>{availableCount}</b>");
                    }
                }

                return View(companyUser.ClientCompany.BulkUpload && userCanUpload);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Add New", FromAction = "New")]
        public IActionResult Create()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }


            var model = creatorService.Create(currentUserEmail);

            if (!model.AllowedToCreate)
            {
                notifyService.Information($"MAX Claim limit = <b>{model.TotalCount}</b> reached");
            }
            else
            {
                notifyService.Information($"Limit available = <b>{model.AvailableCount}</b>");
            }

            return View(model);
        }
        [Breadcrumb(title: " Add Policy", FromAction = "Create")]
        public IActionResult CreatePolicy()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var (model, trial) = claimPolicyService.AddClaimPolicy(currentUserEmail);

                if (model == null)
                {
                    notifyService.Error("OOPS!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name");
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i =>
                i.LineOfBusinessId == model.PolicyDetail.LineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name");
                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name");
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name");
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name");
                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
                return false ?
                    View(new ClaimsInvestigation
                    {
                        PolicyDetail = new PolicyDetail
                        {
                            LineOfBusinessId = model.PolicyDetail.LineOfBusinessId
                        }
                    }) :
                    View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Edit Policy", FromAction = "Details")]
        public async Task<IActionResult> EditPolicy(string id)
        {
            try
            {
                if (id == null || string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var claimsInvestigation = await _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.ClientCompanyId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i =>
                i.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
                ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Claims");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign(auto)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Create", "CreatorAuto", $"Add New") { Parent = agencyPage };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorAuto", $"Details") { Parent = detailsPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditPolicy", "CreatorAuto", $"Edit Policy") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(claimsInvestigation);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb(title: " Add Customer", FromAction = "Details")]
        public async Task<IActionResult> CreateCustomer(string id)
        {
            try
            {
                if (id == null || string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var claimsInvestigation = await _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var pinCode = _context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE2);
                var district = _context.District.Include(d => d.State).FirstOrDefault(d => d.DistrictId == pinCode.District.DistrictId);
                var state = _context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == district.State.StateId);
                var country = _context.Country.FirstOrDefault(c => c.CountryId == state.Country.CountryId);
                var random = new Random();
                claimsInvestigation.CustomerDetail = new CustomerDetail
                {
                    Addressline = random.Next(100, 999) + " GOOD STREET",
                    ContactNumber = random.NextInt64(5555555555, 9999999999),
                    Country = country,
                    CustomerDateOfBirth = DateTime.Now.AddYears(-random.Next(25, 77)).AddDays(20),
                    CustomerEducation = Education.PROFESSIONAL,
                    CustomerIncome = Income.UPPER_INCOME,
                    CustomerName = NameGenerator.GenerateName(),
                    CustomerOccupation = Occupation.SELF_EMPLOYED,
                    CustomerType = CustomerType.HNI,
                    Description = "DODGY PERSON",
                    State = state,
                    District = district,
                    PinCode = pinCode,
                    Gender = Gender.MALE,
                };

                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == country.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == state.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == district.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CustomerDetail.Country.CountryId);
                ViewData["DistrictId"] = new SelectList(districts.OrderBy(d => d.Code), "DistrictId", "Name", claimsInvestigation.CustomerDetail.District.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", claimsInvestigation.CustomerDetail.PinCode.PinCodeId);
                ViewData["StateId"] = new SelectList(relatedStates.OrderBy(s => s.Code), "StateId", "Name", claimsInvestigation.CustomerDetail.State.StateId);

                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.ClientCompanyId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
                ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);


                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Claims");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign(auto)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Create", "CreatorAuto", $"Add New") { Parent = agencyPage };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorAuto", $"Details") { Parent = detailsPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateCustomer", "CreatorAuto", $"Create Customer") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(claimsInvestigation);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Edit Customer", FromAction = "Details")]
        public async Task<IActionResult> EditCustomer(string id)
        {
            try
            {
                if (id == null || string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var claimsInvestigation = await _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.District)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.State)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.Country)
                    .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.ClientCompanyId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
                ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == claimsInvestigation.CustomerDetail.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == claimsInvestigation.CustomerDetail.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == claimsInvestigation.CustomerDetail.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country, "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", claimsInvestigation.CustomerDetail.PinCodeId);

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Claims");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign(auto)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Create", "CreatorAuto", $"Add New") { Parent = agencyPage };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorAuto", $"Details") { Parent = detailsPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditCustomer", "CreatorAuto", $"Edit Customer") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;
                return View(claimsInvestigation);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Add Beneficiary", FromAction = "Details")]
        public IActionResult CreateBeneficiary(string id)
        {
            try
            {
                var claim = _context.ClaimsInvestigation
                                .Include(i => i.PolicyDetail)
                                .FirstOrDefault(v => v.ClaimsInvestigationId == id);

                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");

                var beneRelationId = _context.BeneficiaryRelation.FirstOrDefault().BeneficiaryRelationId;
                var pinCode = _context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE2);
                var district = _context.District.Include(d => d.State).FirstOrDefault(d => d.DistrictId == pinCode.District.DistrictId);
                var state = _context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == district.State.StateId);
                var country = _context.Country.FirstOrDefault(c => c.CountryId == state.Country.CountryId);
                var random = new Random();

                var model = new BeneficiaryDetail
                {
                    ClaimsInvestigation = claim,
                    ClaimsInvestigationId = id,
                    Addressline = random.Next(100, 999) + " GREAT ROAD",
                    BeneficiaryDateOfBirth = DateTime.Now.AddYears(-random.Next(25, 77)).AddMonths(3),
                    BeneficiaryIncome = Income.MEDIUUM_INCOME,
                    BeneficiaryName = NameGenerator.GenerateName(),
                    BeneficiaryRelationId = beneRelationId,
                    CountryId = country.CountryId,
                    StateId = state.StateId,
                    DistrictId = district.DistrictId,
                    PinCodeId = pinCode.PinCodeId,
                    BeneficiaryContactNumber = random.NextInt64(5555555555, 9999999999),
                };

                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == country.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == state.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == district.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", model.CountryId);
                ViewData["DistrictId"] = new SelectList(districts.OrderBy(s => s.Code), "DistrictId", "Name", model.DistrictId);
                ViewData["StateId"] = new SelectList(relatedStates.OrderBy(s => s.Code), "StateId", "Name", model.StateId);
                ViewData["PinCodeId"] = new SelectList(pincodes.OrderBy(s => s.Code), "PinCodeId", "Code", model.PinCodeId);
                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Claims");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign(auto)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Create", "CreatorAuto", $"Add New") { Parent = agencyPage };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorAuto", $"Details") { Parent = detailsPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateBeneficiary", "CreatorAuto", $"Add beneficiary") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Edit Beneficiary", FromAction = "Details")]
        public async Task<IActionResult> EditBeneficiary(long? id)
        {
            try
            {
                if (id == null)
                {
                    notifyService.Error("NOT FOUND!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var caseLocation = await _context.BeneficiaryDetail.FindAsync(id);
                if (caseLocation == null)
                {
                    notifyService.Error("NOT FOUND!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var services = _context.BeneficiaryDetail
                    .Include(v => v.ClaimsInvestigation)
                    .ThenInclude(c => c.PolicyDetail)
                    .Include(v => v.District)
                    .First(v => v.BeneficiaryDetailId == id);

                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == caseLocation.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == caseLocation.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == caseLocation.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country, "CountryId", "Name", caseLocation.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", caseLocation.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", caseLocation.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", caseLocation.PinCodeId);

                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name", caseLocation.BeneficiaryRelationId);


                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Claims");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign(auto)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Create", "CreatorAuto", $"Add New") { Parent = agencyPage };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorAuto", $"Details") { Parent = detailsPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateBeneficiary", "CreatorAuto", $"Edit beneficiary") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(services);
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
                if (id == null || string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await investigationReportService.GetClaimDetails(currentUserEmail, id);

                if (model == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
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
        [Breadcrumb("Details", FromAction = "Create")]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

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
