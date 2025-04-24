using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Required for AllowAnonymous
using risk.control.system.Data;
using risk.control.system.Models;
using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.AppConstant;

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
        
        [HttpGet("GetStatesByCountryId")]
        public async Task<IActionResult> GetStatesByCountryId(long countryId)
        {
            long cId;
            var states = new List<State>();
            if (countryId > 0) { }
            {
                cId = countryId;
                states = await context.State.Where(s => s.CountryId.Equals(cId)).OrderBy(s => s.Code).ToListAsync();
            }
            return Ok(states);
        }

        [HttpGet("GetDistrictByStateId")]
        public async Task<IActionResult> GetDistrictByStateId(long stateId)
        {
            long sId;
            var districts = new List<District>();
            if (stateId > 0)
            {
                sId = stateId;
                districts = await context.District.Where(s => s.State.StateId.Equals(sId)).OrderBy(s => s.Code).ToListAsync();
            }
            return Ok(districts);
        }

        [HttpGet("GetPinCodesByDistrictId")]
        public async Task<IActionResult> GetPinCodesByDistrictId(long districtId)
        {
            long sId;
            var pincodes = new List<PinCode>();
            if (districtId > 0)
            {
                sId = districtId;
                pincodes = await context.PinCode.Where(s => s.District.DistrictId.Equals(sId)).OrderBy(s => s.Code).ToListAsync();
            }
            return Ok(pincodes);
        }

        [HttpGet("GetPincodesByDistrictIdWithoutPreviousSelected")]
        public async Task<IActionResult> GetPincodesByDistrictIdWithoutPreviousSelected(long districtId, string caseId)
        {
            long sId;
            var pincodes = new List<PinCode>();
            var remaingPincodes = new List<PinCode>();

            if (districtId > 0)
            {
                sId = districtId;
                pincodes = await context.PinCode.Where(s => s.District.DistrictId.Equals(sId)).OrderBy(s => s.Code).ToListAsync();
            }
            return Ok(pincodes);
        }

        [HttpGet("GetUserBySearch")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserBySearch(string search = "")
        {
            var applicationUsers = new List<ApplicationUser>();
            if (string.IsNullOrWhiteSpace(search))
            {
                return Ok(context.ApplicationUser?.Where(a => a.Email.ToLower() != PORTAL_ADMIN.EMAIL.ToLower()).OrderBy(o => o.Email).Take(10).Select(a => a.Email).OrderBy(s => s).ToList());
            }
            if (!string.IsNullOrEmpty(search))
            {
                applicationUsers = await context.ApplicationUser.Where(s =>
                   (!string.IsNullOrEmpty(search) && s.Email.ToLower().StartsWith(search.Trim().ToLower()))
                ).ToListAsync();
            }
            return Ok(applicationUsers?.OrderBy(o => o.Email).Take(10).Select(a => a.Email).OrderBy(s => s).ToList());
        }

        [HttpGet("GetIpAddress")]
        public async Task<IActionResult> GetIpAddress()
        {
            var ipAddresses = await context.IpApiResponse.ToListAsync();
            return Ok(ipAddresses);
        }
    }
}