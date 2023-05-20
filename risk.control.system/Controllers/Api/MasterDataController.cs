using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers.Api
{
    public class MasterDataController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public MasterDataController(ApplicationDbContext context)
        {
            this.context = context;
        }
        [HttpPost, ActionName("GetStatesByCountryId")]
        public async Task<IActionResult> GetStatesByCountryId(string countryId)
        {
            string cId;
            var states = new List<State>();
            if (!string.IsNullOrEmpty(countryId))
            {
                cId = countryId;
                states = await context.State.Where(s => s.CountryId.Equals(cId)).ToListAsync();
            }
            return Ok(states);
        }

        [HttpPost, ActionName("GetDistrictByStateId")]
        public async Task<IActionResult> GetDistrictByStateId(string stateId)
        {
            string sId;
            var districts = new List<District>();
            if (!string.IsNullOrEmpty(stateId))
            {
                sId = stateId;
                districts = await context.District.Where(s => s.State.StateId.Equals(sId)).ToListAsync();
            }
            return Ok(districts);
        }

        [HttpPost, ActionName("GetPinCodesByDistrictId")]
        public async Task<IActionResult> GetPinCodesByDistrictId(string districtId)
        {
            string sId;
            var pincodes = new List<PinCode>();
            if (!string.IsNullOrEmpty(districtId))
            {
                sId = districtId;
                pincodes = await context.PinCode.Where(s => s.District.DistrictId.Equals(sId)).ToListAsync();
            }
            return Ok(pincodes);
        }

        [HttpPost, ActionName("GetUserBySearch")]
        public async Task<IActionResult> GetUserBySearch(string search)
        {
            var applicationUsers = new List<ApplicationUser>();
            if (!string.IsNullOrEmpty(search))
            {
                applicationUsers = await context.ApplicationUser.Where(s =>
                   (!string.IsNullOrEmpty(search) && s.Email.ToLower().Contains(search.Trim().ToLower()))
                   || true
                ).ToListAsync();
            }
            return Ok(applicationUsers?.Select(a => a.Email).ToList());
        }
    }
}
