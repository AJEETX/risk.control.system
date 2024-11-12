using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;
using risk.control.system.Services;
using Microsoft.AspNetCore.Authorization;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Api.Claims
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "PORTAL_ADMIN,COMPANY_ADMIN,AGENCY_ADMIN,CREATOR,ASSESSOR,MANAGER,SUPERVISOR,AGENT")]
    public class ClaimsInvestigationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IClaimsService claimsService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpClientService httpClientService;
        private static HttpClient httpClient = new HttpClient();

        public ClaimsInvestigationController(ApplicationDbContext context, 
            IClaimsService claimsService,
            IWebHostEnvironment webHostEnvironment, IHttpClientService httpClientService)
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

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath,"img", "no-policy.jpg");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            var response = new
            {
                Document =
                    policy.DocumentImage != null ?
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(policy.DocumentImage)) :
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                ContractNumber = policy.ContractNumber,
                ClaimType = policy.ClaimType.GetEnumDisplayName(),
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

        [HttpGet("GetCustomerDetail")]
        public async Task<IActionResult> GetCustomerDetail(string id)
        {
            var customer = await _context.CustomerDetail
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .FirstOrDefaultAsync(p => p.CustomerDetailId == id);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            return Ok(
                new
                {
                    Customer = customer?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(customer.ProfilePicture)) :
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                    CustomerName = customer.CustomerName,
                    ContactNumber = customer.ContactNumber,
                    Address = customer.Addressline + "  " + customer.District.Name + "  " + customer.State.Name + "  " + customer.Country.Name + "  " + customer.PinCode.Code,
                    Occupation = customer.CustomerOccupation.GetEnumDisplayName(),
                    Income = customer.CustomerIncome.GetEnumDisplayName(),
                    Education = customer.CustomerEducation.GetEnumDisplayName(),
                    DateOfBirth = customer.CustomerDateOfBirth,
                }
                );
        }

        [HttpGet("GetBeneficiaryDetail")]
        public async Task<IActionResult> GetBeneficiaryDetail(long id, string claimId)
        {
            var beneficiary = await _context.BeneficiaryDetail
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .FirstOrDefaultAsync(p => p.BeneficiaryDetailId == id && p.ClaimsInvestigationId == claimId);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            return Ok(new
            {
                Beneficiary = beneficiary?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(beneficiary.ProfilePicture)) :
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                BeneficiaryName = beneficiary.BeneficiaryName,
                Dob = (int)beneficiary.BeneficiaryDateOfBirth.Subtract(DateTime.Now).TotalDays / 365,
                Income = beneficiary.BeneficiaryIncome.GetEnumDisplayName(),
                BeneficiaryRelation = beneficiary.BeneficiaryRelation.Name,
                Address = beneficiary.Addressline + "  " + beneficiary.District.Name + "  " + beneficiary.State.Name + "  " + beneficiary.Country.Name + "  " + beneficiary.PinCode.Code,
                ContactNumber = beneficiary.BeneficiaryContactNumber
            }
            );
        }

        [HttpGet("GetInvestigationData")]
        public async Task<IActionResult> GetInvestigationData(long id, string claimId)
        {
            var claim = claimsService.GetClaims()
                .Include(c=>c.AgencyReport)
                .Include(c=>c.AgencyReport.DocumentIdReport)
                .Include(c=>c.AgencyReport.DigitalIdReport)
                .FirstOrDefault(c=> c.ClaimsInvestigationId == claimId);

            var beneficiary = await _context.BeneficiaryDetail
                .FirstOrDefaultAsync(p => p.BeneficiaryDetailId == id && p.ClaimsInvestigationId == claimId);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            string mapUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Applicationsettings.GMAPData}";
            string imageAddress = string.Empty;
            string faceLat = string.Empty, faceLng = string.Empty;
            string ocrLatitude = string.Empty, ocrLongitude = string.Empty;
            if (!string.IsNullOrWhiteSpace(claim.AgencyReport?.DigitalIdReport?.DigitalIdImageLongLat))
            {
                var longLat = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
                faceLat = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
                faceLng = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim();
                var longLatString = faceLat + "," + faceLng;
                imageAddress = await httpClientService.GetRawAddress((faceLat), (faceLng));
                mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=18&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            }

            string ocrUrl = $"https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            string ocrAddress = string.Empty;
            if (!string.IsNullOrWhiteSpace(claim.AgencyReport?.DocumentIdReport?.DocumentIdImageLongLat))
            {
                var ocrlongLat = claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLat.IndexOf("/");
                ocrLatitude = claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLat.Substring(0, ocrlongLat)?.Trim();
                ocrLongitude = claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLat.Substring(ocrlongLat + 1)?.Trim();
                var ocrLongLatString = ocrLatitude + "," + ocrLongitude;
                ocrAddress = await httpClientService.GetRawAddress((ocrLatitude), (ocrLongitude));
                ocrUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={ocrLongLatString}&zoom=18&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{ocrLongLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            }

            var data = new
            {
                Title = "Investigation Data",
                QrData = claim.AgencyReport?.DocumentIdReport?.DocumentIdImageData,
                LocationData = claim.AgencyReport?.DigitalIdReport?.DigitalIdImageData ?? "Location Data",
                LatLong = mapUrl,
                ImageAddress = imageAddress,
                Location = claim.AgencyReport?.DigitalIdReport?.DigitalIdImage != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.AgencyReport?.DigitalIdReport?.DigitalIdImage)) :
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                OcrData = claim.AgencyReport?.DocumentIdReport?.DocumentIdImage != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.AgencyReport?.DocumentIdReport?.DocumentIdImage)) :
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                OcrLatLong = ocrUrl,
                OcrAddress = ocrAddress,
                FacePosition =
                new
                {
                    Lat = string.IsNullOrWhiteSpace(faceLat) ? decimal.Parse("-37.00") : decimal.Parse(faceLat),
                    Lng = string.IsNullOrWhiteSpace(faceLng) ? decimal.Parse("140.00") : decimal.Parse(faceLng)
                },
                OcrPosition =
                new
                {
                    Lat = string.IsNullOrWhiteSpace(ocrLatitude) ? decimal.Parse("-37.0000") : decimal.Parse(ocrLatitude),
                    Lng = string.IsNullOrWhiteSpace(ocrLongitude) ? decimal.Parse("140.00") : decimal.Parse(ocrLongitude)
                }
            };

            return Ok(data);
        }

        [HttpGet("GetCustomerMap")]
        public async Task<IActionResult> GetCustomerMap(string id)
        {
            var customer = await _context.CustomerDetail
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .FirstOrDefaultAsync(p => p.CustomerDetailId == id);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-image.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            var latitude = customer.PinCode.Latitude;
            var longitude = customer.PinCode.Longitude.Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}.\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m} \r\nElevation(sea level):{weatherData.elevation} metres";

            var longLatString = latitude + "," + longitude;
            var imageAddress = await httpClientService.GetRawAddress((latitude), (longitude));
            var customerMapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=18&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            var data = new
            {
                profileMap = customer.CustomerLocationMap,
                weatherData = weatherCustomData,
                address = customer.Addressline + " " + customer.District.Name + " " + customer.State.Name + " " + customer.Country.Name + " " + customer.PinCode.Code,
                position = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) }
            };
            return Ok(data);
        }

        [HttpGet("GetBeneficiaryMap")]
        public async Task<IActionResult> GetBeneficiaryMap(long id, string claimId)
        {
            var beneficiary = await _context.BeneficiaryDetail
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .FirstOrDefaultAsync(p => p.BeneficiaryDetailId == id && p.ClaimsInvestigationId == claimId);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-image.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            var latitude = beneficiary.PinCode.Latitude;
            var longitude = beneficiary.PinCode.Longitude.Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}.\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m} \r\nElevation(sea level):{weatherData.elevation} metres";

            var longLatString = latitude + "," + longitude;
            var imageAddress = await httpClientService.GetRawAddress((latitude), (longitude));
            var customerMapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=18&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            var data = new
            {
                profileMap = beneficiary.BeneficiaryLocationMap,
                weatherData = weatherCustomData,
                address = beneficiary.Addressline + " " + beneficiary.District.Name + " " + beneficiary.State.Name + " " + beneficiary.Country.Name + " " + beneficiary.PinCode.Code,
                position = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) }
            };
            return Ok(data);
        }

        [HttpGet("GetFaceDetail")]
        public IActionResult GetFaceDetail(string claimid)
        {
            var claim = claimsService.GetClaims()
                .Include(c=>c.AgencyReport)
                .Include(c=>c.AgencyReport.DigitalIdReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimid);

            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                var center = new { Lat = decimal.Parse(claim.CustomerDetail.PinCode.Latitude), Lng = decimal.Parse(claim.CustomerDetail.PinCode.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.PinCode.Latitude), Lng = decimal.Parse(claim.CustomerDetail.PinCode.Longitude) };

                if (claim.AgencyReport is not null && claim.AgencyReport?.DigitalIdReport?.DigitalIdImageLongLat is not null)
                {
                    var longLat = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
                    var latitude = claim.AgencyReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.AgencyReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim();

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new { center, dakota, frick });
                }
            }
            else
            {
                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.PinCode.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.PinCode.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.PinCode.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.PinCode.Longitude) };

                if (claim.AgencyReport is not null && claim.AgencyReport?.DigitalIdReport?.DigitalIdImageLongLat is not null)
                {
                    var longLat = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
                    var latitude = claim.AgencyReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.AgencyReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim();

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new { center, dakota, frick });
                }
            }
            return Ok();
        }

        [HttpGet("GetOcrDetail")]
        public IActionResult GetOcrDetail(string claimid)
        {
            var claim = claimsService.GetClaims()
                .Include(c => c.AgencyReport)
                .Include(c => c.AgencyReport.DocumentIdReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimid);

            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                var center = new { Lat = decimal.Parse(claim.CustomerDetail.PinCode.Latitude), Lng = decimal.Parse(claim.CustomerDetail.PinCode.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.PinCode.Latitude), Lng = decimal.Parse(claim.CustomerDetail.PinCode.Longitude) };

                if (claim.AgencyReport is not null && claim.AgencyReport?.DocumentIdReport?.DocumentIdImageLongLat is not null)
                {
                    var longLat = claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLat.IndexOf("/");
                    var latitude = claim.AgencyReport?.DocumentIdReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.AgencyReport?.DocumentIdReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new { center, dakota, frick });
                }
            }
            else
            {
                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.PinCode.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.PinCode.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.PinCode.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.PinCode.Longitude) };

                if (claim.AgencyReport is not null && claim.AgencyReport?.DocumentIdReport?.DocumentIdImageLongLat is not null)
                {
                    var longLat = claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLat.IndexOf("/");
                    var latitude = claim.AgencyReport?.DocumentIdReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.AgencyReport?.DocumentIdReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new { center, dakota, frick });
                }
            }
            return Ok();
        }
    }
}