﻿using Microsoft.AspNetCore.Http;
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

        [HttpGet("GetSubstatusBystatusId")]
        public async Task<IActionResult> GetSubstatusBystatusId(string InvestigationCaseStatusId)
        {
            string lId;
            var subStatuses = new List<InvestigationCaseSubStatus>();
            if (!string.IsNullOrEmpty(InvestigationCaseStatusId))
            {
                lId = InvestigationCaseStatusId;
                subStatuses = await context.InvestigationCaseSubStatus
                    .Include(i => i.InvestigationCaseStatus).Where(s =>
                    s.InvestigationCaseStatus.InvestigationCaseStatusId.Equals(lId)).ToListAsync();
            }
            return Ok(subStatuses?.Select(s => new { s.Code, s.InvestigationCaseSubStatusId }));
        }

        [HttpGet("GetInvestigationServicesByLineOfBusinessId")]
        public async Task<IActionResult> GetInvestigationServicesByLineOfBusinessId(long LineOfBusinessId)
        {
            long lId;
            var services = new List<InvestigationServiceType>();
            if (LineOfBusinessId > 0)
            {
                lId = LineOfBusinessId;
                services = await context.InvestigationServiceType.Where(s => s.LineOfBusiness.LineOfBusinessId.Equals(lId)).ToListAsync();
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

        [HttpGet("GetPincodesByDistrictIdWithoutPreviousSelectedService")]
        public async Task<IActionResult> GetPincodesByDistrictIdWithoutPreviousSelectedService(long districtId, string district, long stateId, long vendorId, long lobId, long serviceId)
        {
            long sId;
            var pincodes = new List<PinCode>();
            var remaingPincodes = new List<PinCode>();
            if (districtId == -1)
            {
                var stateWideService = await context.VendorInvestigationServiceType.AsNoTracking().
                        AnyAsync(v =>
                            v.VendorId == vendorId &&
                            v.LineOfBusinessId == lobId &&
                            v.InvestigationServiceTypeId == serviceId &&
                            v.StateId == stateId &&
                            v.DistrictId == null);
                if (stateWideService)
                {
                    return Ok();
                }
                var pincode = new PinCode { Code = "State Wide", PinCodeId = -1 };
                pincodes.Add(pincode);
                return Ok(pincodes);
            }
            if (districtId > 0 && !string.IsNullOrWhiteSpace(district))
            {
                sId = districtId;
                var validDistrict = context.PinCode.Include(p => p.District).Any(s => s.StateId == stateId &&
                    (s.District.Name.ToLower().Contains(district.ToLower()) || 
                    (s.DistrictId == null && districtId ==-1)));
                if (!validDistrict)
                {
                    return Ok();
                }
                pincodes = await context.PinCode.Include(p => p.District).Where(s => s.District.DistrictId.Equals(sId) && s.District.Name.ToLower().Contains(district.ToLower())).ToListAsync();

                var vendor = context.Vendor
                    .Include(c => c.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.Country)
                    .Include(c => c.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.State)
                    .Include(c => c.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .Include(c => c.VendorInvestigationServiceTypes)
                    .FirstOrDefault(c => c.VendorId == vendorId);

                var existingVendorServices = vendor.VendorInvestigationServiceTypes.Where(i=>i.StateId == stateId);

                var existingServicedPincodes = new List<ServicedPinCode>();

                if (existingVendorServices is not null && existingVendorServices.Any())
                {
                    var pinCodeString = pincodes.Select(p => p.Code).ToList();

                    foreach (var existingVendorService in existingVendorServices)
                    {
                        if(existingVendorService.DistrictId == null)
                        {
                            var pinCodesForStateWide = await context.ServicedPinCode.Where(p => p.VendorInvestigationServiceTypeId == existingVendorService.VendorInvestigationServiceTypeId)?.ToListAsync();

                            existingServicedPincodes.AddRange(pinCodesForStateWide);
                        }
                        else if (existingVendorService.LineOfBusinessId == lobId && existingVendorService.InvestigationServiceTypeId == serviceId && 
                            existingVendorService.StateId == stateId && existingVendorService.DistrictId == districtId)
                        {
                            //foreach (var pincodeService in existingVendorService.PincodeServices)
                            //{
                            //    existingServicedPincodes.Add(pincodeService);
                            //}
                        }
                    }

                    var existingPicodes = existingServicedPincodes.Select(e => e.Pincode).ToList();
                    var remaingPincodesString = pinCodeString.Except(existingPicodes).ToList();
                    remaingPincodes = pincodes.Where(p => remaingPincodesString.Contains(p.Code)).OrderBy(s => s.Code).ToList();
                    return Ok(remaingPincodes);
                }
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