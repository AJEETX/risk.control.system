using AspNetCoreHero.ToastNotification.Abstractions;
using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers
{
    public class CustomerInsurancePolicyController : Controller
    {
        private readonly IClaimPolicyService claimPolicyService;
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;

        public CustomerInsurancePolicyController(IClaimPolicyService claimPolicyService,
            INotyfService notifyService,
            ApplicationDbContext context)
        {
            this.claimPolicyService = claimPolicyService;
            this.notifyService = notifyService;
            this._context = context;
        }

        [Breadcrumb(title: " Add Customer", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
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

                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
                ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);


                var claimsPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "New & Draft") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("CreateCustomer", "ClaimsInvestigation", $"Add Customer") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;


                return View(claimsInvestigation);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Edit Customer", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
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
                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
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

                var claimsPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "New & Draft") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditCustomer", "ClaimsInvestigation", $"Edit Customer") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(claimsInvestigation);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [Breadcrumb(title: " Edit Customer", FromAction = "DetailsAuto", FromController = typeof(ClaimsInvestigationController))]
        public async Task<IActionResult> EditCustomerAuto(string id)
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
                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
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

                var claimsPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Assign(auto)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("DetailsAuto", "ClaimsInvestigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditCustomerAuto", "ClaimsInvestigation", $"Edit Customer") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;
                return View(claimsInvestigation);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [Breadcrumb(title: " Edit Customer", FromAction = "DetailsManual", FromController = typeof(ClaimsInvestigationController))]
        public async Task<IActionResult> EditCustomerManual(string id)
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
                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
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


                var claimsPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Assigner", "ClaimsInvestigation", "Assign & Re") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("DetailsManual", "ClaimsInvestigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditCustomerManual", "ClaimsInvestigation", $"Edit Customer") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;
                return View(claimsInvestigation);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}