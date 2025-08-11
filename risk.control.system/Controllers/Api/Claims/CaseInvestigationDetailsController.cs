﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Claims
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class CaseInvestigationDetailsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IClaimsService claimsService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpClientService httpClientService;
        private static readonly HttpClient httpClient = new HttpClient();

        public CaseInvestigationDetailsController(ApplicationDbContext context,
            IClaimsService claimsService,
            IWebHostEnvironment webHostEnvironment,
            IHttpClientService httpClientService)
        {
            _context = context;
            this.claimsService = claimsService;
            this.webHostEnvironment = webHostEnvironment;
            this.httpClientService = httpClientService;
        }

        [HttpGet("GetPolicyDetail")]
        public async Task<IActionResult> GetPolicyDetail(long id)
        {
            var policy = await _context.PolicyDetail
                .Include(p => p.InvestigationServiceType)
                .Include(p => p.CostCentre)
                .Include(p => p.CaseEnabler)
                .FirstOrDefaultAsync(p => p.PolicyDetailId == id);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-policy.jpg");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            var response = new
            {
                Document =
                    policy.DocumentImage != null ?
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(policy.DocumentImage)) :
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                ContractNumber = policy.ContractNumber,
                ClaimType = policy.InsuranceType.GetEnumDisplayName(),
                ContractIssueDate = policy.ContractIssueDate.ToString("dd-MMM-yyyy"),
                DateOfIncident = policy.DateOfIncident.ToString("dd-MMM-yyyy"),
                SumAssuredValue = policy.SumAssuredValue,
                InvestigationServiceType = policy.InvestigationServiceType.Name,
                CaseEnabler = policy.CaseEnabler.Name,
                CauseOfLoss = policy.CauseOfLoss,
                CostCentre = policy.CostCentre.Name,
            };
            return Ok(response);
        }

        [HttpGet("GetPolicyNotes")]
        public IActionResult GetPolicyNotes(long claimId)
        {
            var claim = claimsService.GetCasesWithDetail()
                .Include(c => c.CaseNotes)
                .FirstOrDefault(c => c.Id == claimId);

            var response = new
            {
                notes = claim.CaseNotes.ToList()
            };
            return Ok(response);
        }
        [HttpGet("GetCustomerDetail")]
        public async Task<IActionResult> GetCustomerDetail(long id)
        {
            var currentUserEmail = HttpContext.User.Identity.Name;
            var isAgencyUser = _context.VendorApplicationUser.Any(u => u.Email == currentUserEmail);

            var customer = await _context.CustomerDetail
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .FirstOrDefaultAsync(p => p.CustomerDetailId == id);
            if (isAgencyUser)
            {
                customer.ContactNumber = new string('*', customer.ContactNumber.Length - 4) + customer.ContactNumber.Substring(customer.ContactNumber.Length - 4);
            }
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            return Ok(
                new
                {
                    Customer = customer?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(customer.ProfilePicture)) :
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                    CustomerName = customer.Name,
                    ContactNumber = new string('*', customer.ContactNumber.ToString().Length - 4) + customer.ContactNumber.ToString().Substring(customer.ContactNumber.ToString().Length - 4),
                    Address = customer.Addressline + "  " + customer.District.Name + "  " + customer.State.Name + "  " + customer.Country.Name + "  " + customer.PinCode.Code,
                    Occupation = customer.Occupation.GetEnumDisplayName(),
                    Income = customer.Income.GetEnumDisplayName(),
                    Education = customer.Education.GetEnumDisplayName(),
                    DateOfBirth = customer.DateOfBirth.GetValueOrDefault().ToString("dd-MM-yyyy"),
                }
                );
        }

        [HttpGet("GetBeneficiaryDetail")]
        public async Task<IActionResult> GetBeneficiaryDetail(long id, long claimId)
        {
            var beneficiary = await _context.BeneficiaryDetail
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .FirstOrDefaultAsync(p => p.BeneficiaryDetailId == id && p.InvestigationTask.Id == claimId);
            var currentUserEmail = HttpContext.User.Identity.Name;

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            return Ok(new
            {
                Beneficiary = beneficiary?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(beneficiary.ProfilePicture)) :
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                BeneficiaryName = beneficiary.Name,
                Dob = (int)beneficiary.DateOfBirth.GetValueOrDefault().Subtract(DateTime.Now).TotalDays / 365,
                Income = beneficiary.Income.GetEnumDisplayName(),
                BeneficiaryRelation = beneficiary.BeneficiaryRelation.Name,
                Address = beneficiary.Addressline + "  " + beneficiary.District.Name + "  " + beneficiary.State.Name + "  " + beneficiary.Country.Name + "  " + beneficiary.PinCode.Code,
                ContactNumber = new string('*', beneficiary.ContactNumber.Length - 4) + beneficiary.ContactNumber.Substring(beneficiary.ContactNumber.Length - 4)
            }
            );
        }
        [HttpGet("GetCustomerMap")]
        public async Task<IActionResult> GetCustomerMap(long id)
        {
            var customer = await _context.CustomerDetail
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .FirstOrDefaultAsync(p => p.CustomerDetailId == id);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-image.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            var latitude = customer.Latitude;
            var longitude = customer.Longitude.Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}.\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m} \r\nElevation(sea level):{weatherData.elevation} metres";
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=400x400&maptype=roadmap&markers=color:red%7Clabel:A%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            var data = new
            {
                profileMap = url,
                weatherData = weatherCustomData,
                address = customer.Addressline + " " + customer.District.Name + " " + customer.State.Name + " " + customer.Country.Name + " " + customer.PinCode.Code,
                position = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) }
            };
            return Ok(data);
        }

        [HttpGet("GetBeneficiaryMap")]
        public async Task<IActionResult> GetBeneficiaryMap(long id, long claimId)
        {
            var beneficiary = await _context.BeneficiaryDetail
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .FirstOrDefaultAsync(p => p.BeneficiaryDetailId == id && p.InvestigationTaskId == claimId);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-image.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            var latitude = beneficiary.Latitude;
            var longitude = beneficiary.Longitude.Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}.\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m} \r\nElevation(sea level):{weatherData.elevation} metres";

            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=400x400&maptype=roadmap&markers=color:red%7Clabel:A%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            var data = new
            {
                profileMap = url,
                weatherData = weatherCustomData,
                address = beneficiary.Addressline + " " + beneficiary.District.Name + " " + beneficiary.State.Name + " " + beneficiary.Country.Name + " " + beneficiary.PinCode.Code,
                position = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) }
            };
            return Ok(data);
        }

        [HttpGet("GetAgentDetail")]
        public IActionResult GetAgentDetail(long caseId, long faceId)
        {
            var claim = claimsService.GetCasesWithDetail()
                .FirstOrDefault(c => c.Id == caseId);
            var agentReport = _context.AgentIdReport.FirstOrDefault(l => l.Id == faceId);

            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

                if (agentReport is not null)
                {
                    var longLat = agentReport.IdImageLongLat.IndexOf("/");
                    var latitude = agentReport.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = agentReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();
                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = string.Format(agentReport.IdImageLocationUrl, "500", "500"),
                        distance = agentReport.Distance,
                        duration = agentReport.Duration,
                        Address = "Life-Assured"
                    });
                }
            }
            else
            {
                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

                if (agentReport is not null)
                {
                    var longLat = agentReport.IdImageLongLat.IndexOf("/");
                    var latitude = agentReport?.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = agentReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();
                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = string.Format(agentReport.IdImageLocationUrl, "500", "500"),
                        distance = agentReport.Distance,
                        duration = agentReport.Duration,
                        Address = "Beneficiary"
                    });
                }
            }
            return Ok();
        }
        [HttpGet("GetFaceDetail")]
        public IActionResult GetFaceDetail(long caseId, long faceId)
        {
            var claim = claimsService.GetCasesWithDetail()
                .FirstOrDefault(c => c.Id == caseId);
            var faceReport = _context.DigitalIdReport.FirstOrDefault(l => l.Id == faceId);

            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

                if (faceReport is not null)
                {
                    var longLat = faceReport.IdImageLongLat.IndexOf("/");
                    var latitude = faceReport.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = faceReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();
                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = string.Format(faceReport.IdImageLocationUrl, "500", "500"),
                        distance = faceReport.Distance,
                        duration = faceReport.Duration,
                        Address = "Life-Assured"
                    });
                }
            }
            else
            {
                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

                if (faceReport is not null)
                {
                    var longLat = faceReport.IdImageLongLat.IndexOf("/");
                    var latitude = faceReport?.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = faceReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();
                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = string.Format(faceReport.IdImageLocationUrl, "500", "500"),
                        distance = faceReport.Distance,
                        duration = faceReport.Duration,
                        Address = "Beneficiary"
                    });
                }
            }
            return Ok();
        }
        [HttpGet("GetDocumentDetail")]
        public IActionResult GetDocumentDetail(long caseId, long docId)
        {
            var claim = claimsService.GetCasesWithDetail()
                .FirstOrDefault(c => c.Id == caseId);
            var docReport = _context.DocumentIdReport.FirstOrDefault(l => l.Id == docId);

            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

                if (docReport is not null)
                {
                    var longLat = docReport.IdImageLongLat.IndexOf("/");
                    var latitude = docReport.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = docReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();
                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = string.Format(docReport.IdImageLocationUrl, "500", "500"),
                        distance = docReport.Distance,
                        duration = docReport.Duration,
                        Address = "Life-Assured"
                    });
                }
            }
            else
            {
                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

                if (docReport is not null)
                {
                    var longLat = docReport.IdImageLongLat.IndexOf("/");
                    var latitude = docReport?.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = docReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();
                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = string.Format(docReport.IdImageLocationUrl, "500", "500"),
                        distance = docReport.Distance,
                        duration = docReport.Duration,
                        Address = "Beneficiary"
                    });
                }
            }
            return Ok();
        }
        [HttpGet("GetMediaDetail")]
        public IActionResult GetMediaDetail(long caseId, long docId)
        {
            var currentUserEmail = HttpContext.User.Identity.Name;
            var agent = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == currentUserEmail);
            var claim = claimsService.GetCasesWithDetail()
                .FirstOrDefault(c => c.Id == caseId);
            var docReport = _context.MediaReport.FirstOrDefault(l => l.Id == docId);

            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

                if (docReport is not null)
                {
                    var longLat = docReport.IdImageLongLat.IndexOf("/");
                    var latitude = docReport.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = docReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();
                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = string.Format(docReport.IdImageLocationUrl, "500", "500"),
                        distance = docReport.Distance,
                        duration = docReport.Duration,
                        Address = "Life-Assured"
                    });
                }
            }
            else
            {
                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

                if (docReport is not null)
                {
                    var longLat = docReport.IdImageLongLat.IndexOf("/");
                    var latitude = docReport?.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = docReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();
                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = string.Format(docReport.IdImageLocationUrl, "500", "500"),
                        distance = docReport.Distance,
                        duration = docReport.Duration,
                        Address = "Beneficiary"
                    });
                }
            }
            return Ok();
        }
    }
}