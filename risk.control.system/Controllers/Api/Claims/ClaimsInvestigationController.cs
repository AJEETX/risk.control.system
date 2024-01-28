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

namespace risk.control.system.Controllers.Api.Claims
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimsInvestigationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpClientService httpClientService;
        private static HttpClient httpClient = new HttpClient();

        public ClaimsInvestigationController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IHttpClientService httpClientService)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.httpClientService = httpClientService;
        }

        [HttpGet("GetPolicyDetail")]
        public async Task<IActionResult> GetPolicyDetail(long id)
        {
            var policy = await _context.PolicyDetail
                .Include(p => p.LineOfBusiness)
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
                    DateOfBirth = customer.CustomerDateOfBirth
                }
                );
        }

        [HttpGet("GetBeneficiaryDetail")]
        public async Task<IActionResult> GetBeneficiaryDetail(long id, string claimId)
        {
            var beneficiary = await _context.CaseLocation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .Include(c => c.ClaimReport)
                .FirstOrDefaultAsync(p => p.CaseLocationId == id && p.ClaimsInvestigationId == claimId);

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
            var beneficiary = await _context.CaseLocation
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DocumentIdReport)
                .FirstOrDefaultAsync(p => p.CaseLocationId == id && p.ClaimsInvestigationId == claimId);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            string mapUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Applicationsettings.GMAPData}";
            string imageAddress = string.Empty;
            string faceLat = string.Empty, faceLng = string.Empty;
            string ocrLatitude = string.Empty, ocrLongitude = string.Empty;
            if (!string.IsNullOrWhiteSpace(beneficiary.ClaimReport?.DigitalIdReport?.DigitalIdImageLongLat))
            {
                var longLat = beneficiary.ClaimReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
                faceLat = beneficiary.ClaimReport.DigitalIdReport.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
                faceLng = beneficiary.ClaimReport.DigitalIdReport.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim();
                var longLatString = faceLat + "," + faceLng;
                RootObject rootObject = await httpClientService.GetAddress((faceLat), (faceLng));
                imageAddress = rootObject.display_name;
                mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=18&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Applicationsettings.GMAPData}";
            }

            string ocrUrl = $"https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Applicationsettings.GMAPData}";
            string ocrAddress = string.Empty;
            if (!string.IsNullOrWhiteSpace(beneficiary.ClaimReport?.DocumentIdReport?.DocumentIdImageLongLat))
            {
                var ocrlongLat = beneficiary.ClaimReport.DocumentIdReport.DocumentIdImageLongLat.IndexOf("/");
                ocrLatitude = beneficiary.ClaimReport.DocumentIdReport.DocumentIdImageLongLat.Substring(0, ocrlongLat)?.Trim();
                ocrLongitude = beneficiary.ClaimReport.DocumentIdReport.DocumentIdImageLongLat.Substring(ocrlongLat + 1)?.Trim();
                var ocrLongLatString = ocrLatitude + "," + ocrLongitude;
                RootObject rootObject = await httpClientService.GetAddress((ocrLatitude), (ocrLongitude));
                ocrAddress = rootObject.display_name;
                ocrUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={ocrLongLatString}&zoom=18&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{ocrLongLatString}&key={Applicationsettings.GMAPData}";
            }

            var data = new
            {
                Title = "Investigation Data",
                QrData = beneficiary.ClaimReport?.DocumentIdReport?.DocumentIdImageData,
                LocationData = beneficiary.ClaimReport?.DigitalIdReport?.DigitalIdImageData ?? "Location Data",
                LatLong = mapUrl,
                ImageAddress = imageAddress,
                Location = beneficiary.ClaimReport?.DigitalIdReport?.DigitalIdImage != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(beneficiary.ClaimReport?.DigitalIdReport?.DigitalIdImage)) :
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                OcrData = beneficiary.ClaimReport?.DocumentIdReport?.DocumentIdImage != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(beneficiary.ClaimReport?.DocumentIdReport?.DocumentIdImage)) :
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
            RootObject rootObject = await httpClientService.GetAddress((latitude), (longitude));
            var imageAddress = rootObject.display_name;
            var customerMapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=18&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Applicationsettings.GMAPData}";
            var data = new
            {
                profileMap = customerMapUrl,
                weatherData = weatherCustomData,
                address = customer.Addressline + " " + customer.District.Name + " " + customer.State.Name + " " + customer.Country.Name + " " + customer.PinCode.Code,
                position = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) }
            };
            return Ok(data);
        }

        [HttpGet("GetBeneficiaryMap")]
        public async Task<IActionResult> GetBeneficiaryMap(long id, string claimId)
        {
            var beneficiary = await _context.CaseLocation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.District)
                .Include(c => c.PinCode)
            .Include(c => c.ClaimReport)
                .FirstOrDefaultAsync(p => p.CaseLocationId == id && p.ClaimsInvestigationId == claimId);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-image.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            var latitude = beneficiary.PinCode.Latitude;
            var longitude = beneficiary.PinCode.Longitude.Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}.\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m} \r\nElevation(sea level):{weatherData.elevation} metres";

            var longLatString = latitude + "," + longitude;
            RootObject rootObject = await httpClientService.GetAddress((latitude), (longitude));
            var imageAddress = rootObject.display_name;
            var customerMapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=18&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Applicationsettings.GMAPData}";
            var data = new
            {
                profileMap = customerMapUrl,
                weatherData = weatherCustomData,
                address = beneficiary.Addressline + " " + beneficiary.District.Name + " " + beneficiary.State.Name + " " + beneficiary.Country.Name + " " + beneficiary.PinCode.Code,
                position = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) }
            };
            return Ok(data);
        }

        [HttpGet("GetFaceDetail")]
        public IActionResult GetFaceDetail(string claimid)
        {
            var claim = _context.ClaimsInvestigation
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.ClientCompany)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CostCentre)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.InvestigationCaseSubStatus)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.CaseLocations)
                .ThenInclude(c => c.District)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.State)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.Country)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.District)
               .Include(c => c.InvestigationCaseStatus)
               .Include(c => c.InvestigationCaseSubStatus)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.InvestigationServiceType)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.LineOfBusiness)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.State)
               .Include(c => c.CaseLocations)
               .ThenInclude(l => l.ClaimReport)
               .Include(c => c.CaseLocations)
               .ThenInclude(l => l.ClaimReport.DigitalIdReport)
               .Include(c => c.CaseLocations)
               .ThenInclude(l => l.ClaimReport.DocumentIdReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimid);

            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                var center = new { Lat = decimal.Parse(claim.CustomerDetail.PinCode.Latitude), Lng = decimal.Parse(claim.CustomerDetail.PinCode.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.PinCode.Latitude), Lng = decimal.Parse(claim.CustomerDetail.PinCode.Longitude) };

                if (claim.CaseLocations.FirstOrDefault().ClaimReport is not null && claim.CaseLocations.FirstOrDefault().ClaimReport?.DigitalIdReport?.DigitalIdImageLongLat is not null)
                {
                    var longLat = claim.CaseLocations.FirstOrDefault().ClaimReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
                    var latitude = claim.CaseLocations.FirstOrDefault()?.ClaimReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.CaseLocations.FirstOrDefault()?.ClaimReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim();

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new { center, dakota, frick });
                }
            }
            else
            {
                var center = new { Lat = decimal.Parse(claim.CaseLocations.FirstOrDefault().PinCode.Latitude), Lng = decimal.Parse(claim.CaseLocations.FirstOrDefault().PinCode.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CaseLocations.FirstOrDefault().PinCode.Latitude), Lng = decimal.Parse(claim.CaseLocations.FirstOrDefault().PinCode.Longitude) };

                if (claim.CaseLocations.FirstOrDefault().ClaimReport is not null && claim.CaseLocations.FirstOrDefault().ClaimReport?.DigitalIdReport?.DigitalIdImageLongLat is not null)
                {
                    var longLat = claim.CaseLocations.FirstOrDefault().ClaimReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
                    var latitude = claim.CaseLocations.FirstOrDefault()?.ClaimReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.CaseLocations.FirstOrDefault()?.ClaimReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim();

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new { center, dakota, frick });
                }
            }
            return Ok();
        }

        [HttpGet("GetOcrDetail")]
        public IActionResult GetOcrDetail(string claimid)
        {
            var claim = _context.ClaimsInvestigation
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.ClientCompany)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CostCentre)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.InvestigationCaseSubStatus)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.CaseLocations)
                .ThenInclude(c => c.District)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.State)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.Country)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.District)
               .Include(c => c.InvestigationCaseStatus)
               .Include(c => c.InvestigationCaseSubStatus)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.InvestigationServiceType)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.LineOfBusiness)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.State)
               .Include(c => c.CaseLocations)
               .ThenInclude(l => l.ClaimReport)
               .Include(c => c.CaseLocations)
               .ThenInclude(l => l.ClaimReport.DigitalIdReport)
               .Include(c => c.CaseLocations)
               .ThenInclude(l => l.ClaimReport.DocumentIdReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimid);

            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                var center = new { Lat = decimal.Parse(claim.CustomerDetail.PinCode.Latitude), Lng = decimal.Parse(claim.CustomerDetail.PinCode.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.PinCode.Latitude), Lng = decimal.Parse(claim.CustomerDetail.PinCode.Longitude) };

                if (claim.CaseLocations.FirstOrDefault().ClaimReport is not null && claim.CaseLocations.FirstOrDefault().ClaimReport?.DocumentIdReport?.DocumentIdImageLongLat is not null)
                {
                    var longLat = claim.CaseLocations.FirstOrDefault().ClaimReport.DocumentIdReport.DocumentIdImageLongLat.IndexOf("/");
                    var latitude = claim.CaseLocations.FirstOrDefault()?.ClaimReport?.DocumentIdReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.CaseLocations.FirstOrDefault()?.ClaimReport?.DocumentIdReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new { center, dakota, frick });
                }
            }
            else
            {
                var center = new { Lat = decimal.Parse(claim.CaseLocations.FirstOrDefault().PinCode.Latitude), Lng = decimal.Parse(claim.CaseLocations.FirstOrDefault().PinCode.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CaseLocations.FirstOrDefault().PinCode.Latitude), Lng = decimal.Parse(claim.CaseLocations.FirstOrDefault().PinCode.Longitude) };

                if (claim.CaseLocations.FirstOrDefault().ClaimReport is not null && claim.CaseLocations.FirstOrDefault().ClaimReport?.DocumentIdReport?.DocumentIdImageLongLat is not null)
                {
                    var longLat = claim.CaseLocations.FirstOrDefault().ClaimReport.DocumentIdReport.DocumentIdImageLongLat.IndexOf("/");
                    var latitude = claim.CaseLocations.FirstOrDefault()?.ClaimReport?.DocumentIdReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.CaseLocations.FirstOrDefault()?.ClaimReport?.DocumentIdReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new { center, dakota, frick });
                }
            }
            return Ok();
        }
    }
}