using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    public class CustomerInsurancePolicyController : Controller
    {
        private readonly IClaimPolicyService claimPolicyService;
        private readonly ApplicationDbContext _context;

        public CustomerInsurancePolicyController(IClaimPolicyService claimPolicyService, ApplicationDbContext context)
        {
            this.claimPolicyService = claimPolicyService;
            this._context = context;
        }

        [Breadcrumb(title: " Add Customer", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public async Task<IActionResult> CreateCustomer(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var pinCode = _context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE2);
            var district = _context.District.Include(d => d.State).FirstOrDefault(d => d.DistrictId== pinCode.District.DistrictId);
            var state = _context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == district.State.StateId);
            var country = _context.Country.FirstOrDefault(c=>c.CountryId == state.Country.CountryId);
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

            return View(claimsInvestigation);
        }

        [Breadcrumb(title: " Edit Customer", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public async Task<IActionResult> EditCustomer(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
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
                return NotFound();
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

            return View(claimsInvestigation);
        }
    }
}