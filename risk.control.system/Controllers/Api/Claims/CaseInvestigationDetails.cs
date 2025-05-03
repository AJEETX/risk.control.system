using Microsoft.AspNetCore.Authorization;
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
        private static HttpClient httpClient = new HttpClient();

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
                    ContactNumber = customer.ContactNumber,
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
            var isAgencyUser = _context.VendorApplicationUser.Any(u => u.Email == currentUserEmail);
            if (isAgencyUser)
            {
                beneficiary.ContactNumber = new string('*', beneficiary.ContactNumber.Length - 4) + beneficiary.ContactNumber.Substring(beneficiary.ContactNumber.Length - 4);
            }
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
                ContactNumber = beneficiary.ContactNumber
            }
            );
        }

        [HttpGet("GetInvestigationFaceIdData")]
        public async Task<IActionResult> GetInvestigationFaceIdData(long claimId, long faceId)
        {
            var claim = claimsService.GetCasesWithDetail()
                .Include(c => c.InvestigationReport)
                .Include(c => c.InvestigationReport.DigitalIdReport)
                .FirstOrDefault(c => c.Id == claimId);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);
            var agentReport = _context.DigitalIdReport.FirstOrDefault(l => l.Id == faceId);

            string mapUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Applicationsettings.GMAPData}";
            string imageAddress = string.Empty;
            string faceLat = string.Empty, faceLng = string.Empty;
            if (!string.IsNullOrWhiteSpace(agentReport?.IdImageLongLat))
            {
                var longLat = agentReport.IdImageLongLat.IndexOf("/");
                faceLat = agentReport.IdImageLongLat.Substring(0, longLat)?.Trim();
                faceLng = agentReport.IdImageLongLat.Substring(longLat + 1)?.Trim();
                var longLatString = faceLat + "," + faceLng;
                imageAddress = await httpClientService.GetRawAddress((faceLat), (faceLng));
                mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=14&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            }

            var data = new
            {
                Title = "Investigation Data",
                LocationData = agentReport?.IdImageData ?? "Location Data",
                LatLong = agentReport.IdImageLocationUrl,
                ImageAddress = imageAddress,
                Location = agentReport?.IdImage != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(agentReport?.IdImage)) :
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                FacePosition =
                new
                {
                    Lat = string.IsNullOrWhiteSpace(faceLat) ? decimal.Parse("-37.00") : decimal.Parse(faceLat),
                    Lng = string.IsNullOrWhiteSpace(faceLng) ? decimal.Parse("140.00") : decimal.Parse(faceLng)
                }
            };

            return Ok(data);
        }

        [HttpGet("GetInvestigationPanData")]
        public async Task<IActionResult> GetInvestigationPanData(long claimId)
        {
            var claim = claimsService.GetCasesWithDetail()
                .Include(c => c.InvestigationReport)
                .Include(c => c.InvestigationReport.PanIdReport)
                .FirstOrDefault(c => c.Id == claimId);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            string panLatitude = string.Empty, panLongitude = string.Empty;
            string panLocationUrl = $"https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            string panAddressAddress = string.Empty;
            if (!string.IsNullOrWhiteSpace(claim.InvestigationReport?.PanIdReport?.IdImageLongLat))
            {
                var ocrlongLat = claim.InvestigationReport.PanIdReport.IdImageLongLat.IndexOf("/");
                panLatitude = claim.InvestigationReport.PanIdReport.IdImageLongLat.Substring(0, ocrlongLat)?.Trim();
                panLongitude = claim.InvestigationReport.PanIdReport.IdImageLongLat.Substring(ocrlongLat + 1)?.Trim();
                var ocrLongLatString = panLatitude + "," + panLongitude;
                panAddressAddress = await httpClientService.GetRawAddress((panLatitude), (panLongitude));
                panLocationUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={ocrLongLatString}&zoom=14&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{ocrLongLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            }

            var data = new
            {
                Title = "Investigation Data",
                QrData = claim.InvestigationReport?.PanIdReport?.IdImageData,
                OcrData = claim.InvestigationReport?.PanIdReport?.IdImage != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.InvestigationReport?.PanIdReport?.IdImage)) :
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                OcrLatLong = claim.InvestigationReport?.PanIdReport.IdImageLocationUrl,
                OcrAddress = panAddressAddress,
                OcrPosition =
                new
                {
                    Lat = string.IsNullOrWhiteSpace(panLatitude) ? decimal.Parse("-37.0000") : decimal.Parse(panLatitude),
                    Lng = string.IsNullOrWhiteSpace(panLongitude) ? decimal.Parse("140.00") : decimal.Parse(panLongitude)
                }
            };

            return Ok(data);
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
                        url = agentReport.IdImageLocationUrl,
                        distance = agentReport.Distance,
                        duration = agentReport.Duration,
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
                        url = agentReport.IdImageLocationUrl,
                        distance = agentReport.Distance,
                        duration = agentReport.Duration,
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
                        url = faceReport.IdImageLocationUrl,
                        distance = faceReport.Distance,
                        duration = faceReport.Duration,
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
                        url = faceReport.IdImageLocationUrl,
                        distance = faceReport.Distance,
                        duration = faceReport.Duration,
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
                        url = docReport.IdImageLocationUrl,
                        distance = docReport.Distance,
                        duration = docReport.Duration,
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
                        url = docReport.IdImageLocationUrl,
                        distance = docReport.Distance,
                        duration = docReport.Duration,
                    });
                }
            }
            return Ok();
        }

    }
}