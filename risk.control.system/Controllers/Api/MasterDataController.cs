using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Api
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    public class MasterDataController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public MasterDataController(ApplicationDbContext context)
        {
            this.context = context;
        }


        [HttpGet("GetInvestigationServicesByInsuranceType")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetInvestigationServicesByInsuranceType(string insuranceType)
        {
            InsuranceType type;
            var services = new List<InvestigationServiceType>();
            if (!string.IsNullOrWhiteSpace(insuranceType) && Enum.TryParse(insuranceType, out type))
            {
                services = await context.InvestigationServiceType.Where(s => s.InsuranceType == type).ToListAsync();
            }
            return Ok(services);
        }

        //[HttpGet("GetStatesByCountryId")]
        //public async Task<IActionResult> GetStatesByCountryId(long countryId)
        //{
        //    long cId;
        //    var states = new List<State>();
        //    if (countryId > 0) { }
        //    {
        //        cId = countryId;
        //        states = await context.State.Where(s => s.CountryId.Equals(cId)).OrderBy(s => s.Code).ToListAsync();
        //    }
        //    return Ok(states);
        //}

        //[HttpGet("GetDistrictByStateId")]
        //public async Task<IActionResult> GetDistrictByStateId(long stateId)
        //{
        //    long sId;
        //    var districts = new List<District>();
        //    if (stateId > 0)
        //    {
        //        sId = stateId;
        //        districts = await context.District.Where(s => s.State.StateId.Equals(sId)).OrderBy(s => s.Code).ToListAsync();
        //    }
        //    return Ok(districts);
        //}

        //[HttpGet("GetPinCodesByDistrictId")]
        //public async Task<IActionResult> GetPinCodesByDistrictId(long districtId)
        //{
        //    long sId;
        //    var pincodes = new List<PinCode>();
        //    if (districtId > 0)
        //    {
        //        sId = districtId;
        //        pincodes = await context.PinCode.Where(s => s.District.DistrictId.Equals(sId)).OrderBy(s => s.Code).ToListAsync();
        //    }
        //    return Ok(pincodes);
        //}

        //[HttpGet("GetPincodesByDistrictIdWithoutPreviousSelected")]
        //public async Task<IActionResult> GetPincodesByDistrictIdWithoutPreviousSelected(long districtId, string caseId)
        //{
        //    long sId;
        //    var pincodes = new List<PinCode>();
        //    var remaingPincodes = new List<PinCode>();

        //    if (districtId > 0)
        //    {
        //        sId = districtId;
        //        pincodes = await context.PinCode.Where(s => s.District.DistrictId.Equals(sId)).OrderBy(s => s.Code).ToListAsync();
        //    }
        //    return Ok(pincodes);
        //}

        [HttpGet("GetUserBySearch")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetUserBySearch(string search = "")
        {
            // First, get IDs of VendorApplicationUsers who are AGENTs
            var vendorAgentIds = await context.Set<VendorApplicationUser>()
                .Where(v => v.UserRole == AgencyRole.AGENT)
                .Select(v => v.Id)
                .ToListAsync();

            IQueryable<ApplicationUser> query = context.ApplicationUser
                .Where(a => !a.Deleted && a.Email.ToLower() != PORTAL_ADMIN.EMAIL.ToLower() &&
                            !vendorAgentIds.Contains(a.Id));

            if (!string.IsNullOrWhiteSpace(search))
            {
                string loweredSearch = search.Trim().ToLower();
                query = query.Where(a => a.Email.ToLower().StartsWith(loweredSearch));
            }

            var userEmails = await query
                .OrderBy(a => a.Email)
                .Take(10)
                .Select(a => a.Email)
                .ToListAsync();

            return Ok(userEmails);
        }
    }
}