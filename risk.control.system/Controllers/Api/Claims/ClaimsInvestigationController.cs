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
    public class ClaimsInvestigationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpClientService httpClientService;
        private static HttpClient httpClient = new HttpClient();

        public ClaimsInvestigationController(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IHttpClientService httpClientService)
        {
            _context = context;
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

        [HttpGet("GetPolicyNotes")]
        public async Task<IActionResult> GetPolicyNotes(long claimId)
        {
            var claim = await _context.Investigations
                .Include(c => c.CaseNotes)
                .FirstOrDefaultAsync(c => c.Id == claimId);

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
                .FirstOrDefaultAsync(p => p.BeneficiaryDetailId == id && p.InvestigationTaskId == claimId);
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
        public async Task<IActionResult> GetInvestigationFaceIdData(long claimId)
        {
            var claim = await _context.Investigations
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.DigitalIdReport)
                .FirstOrDefaultAsync(c => c.Id == claimId);

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            string mapUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Applicationsettings.GMAPData}";
            string imageAddress = string.Empty;
            string faceLat = string.Empty, faceLng = string.Empty;
            if (!string.IsNullOrWhiteSpace(claim.InvestigationReport?.DigitalIdReport?.IdImageLongLat))
            {
                var longLat = claim.InvestigationReport.DigitalIdReport.IdImageLongLat.IndexOf("/");
                faceLat = claim.InvestigationReport.DigitalIdReport.IdImageLongLat.Substring(0, longLat)?.Trim();
                faceLng = claim.InvestigationReport.DigitalIdReport.IdImageLongLat.Substring(longLat + 1)?.Trim();
                var longLatString = faceLat + "," + faceLng;
                imageAddress = await httpClientService.GetRawAddress((faceLat), (faceLng));
                mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=14&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            }

            var data = new
            {
                Title = "Investigation Data",
                LocationData = claim.InvestigationReport?.DigitalIdReport?.IdImageData ?? "Location Data",
                LatLong = claim.InvestigationReport.DigitalIdReport.IdImageLocationUrl,
                ImageAddress = imageAddress,
                Location = claim.InvestigationReport?.DigitalIdReport?.IdImage != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.InvestigationReport?.DigitalIdReport?.IdImage)) :
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
            var claim = await _context.Investigations
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.PanIdReport)
                .FirstOrDefaultAsync(c => c.Id == claimId);

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
        public async Task<IActionResult> GetAgentDetail(long claimid)
        {
            var claim = await _context.Investigations
                .Include(c => c.ClientCompany)
                .ThenInclude(c => c.Country)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.CaseQuestionnaire)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.PanIdReport)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.AgentIdReport)
                .FirstOrDefaultAsync(c => c.Id == claimid);

            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

                if (claim.InvestigationReport is not null && claim.InvestigationReport?.AgentIdReport?.IdImageLongLat is not null)
                {
                    var longLat = claim.InvestigationReport.AgentIdReport.IdImageLongLat.IndexOf("/");
                    var latitude = claim.InvestigationReport?.AgentIdReport?.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.InvestigationReport?.AgentIdReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();
                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = claim.InvestigationReport?.AgentIdReport.IdImageLocationUrl,
                        distance = claim.InvestigationReport.AgentIdReport.Distance,
                        duration = claim.InvestigationReport.AgentIdReport.Duration,
                    });
                }
            }
            else
            {
                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

                if (claim.InvestigationReport is not null && claim.InvestigationReport?.AgentIdReport?.IdImageLongLat is not null)
                {
                    var longLat = claim.InvestigationReport.AgentIdReport.IdImageLongLat.IndexOf("/");
                    var latitude = claim.InvestigationReport?.AgentIdReport?.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.InvestigationReport?.AgentIdReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();
                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = claim.InvestigationReport?.AgentIdReport.IdImageLocationUrl,
                        distance = claim.InvestigationReport.AgentIdReport.Distance,
                        duration = claim.InvestigationReport.AgentIdReport.Duration,
                    });
                }
            }
            return Ok();
        }

        [HttpGet("GetFaceDetail")]
        public async Task<IActionResult> GetFaceDetail(long claimid)
        {
            var claim = await _context.Investigations
                .Include(c => c.ClientCompany)
                .ThenInclude(c => c.Country)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.CaseQuestionnaire)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.PanIdReport)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.AgentIdReport)
                .FirstOrDefaultAsync(c => c.Id == claimid);

            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

                if (claim.InvestigationReport is not null && claim.InvestigationReport?.DigitalIdReport?.IdImageLongLat is not null)
                {
                    var longLat = claim.InvestigationReport.DigitalIdReport.IdImageLongLat.IndexOf("/");
                    var latitude = claim.InvestigationReport?.DigitalIdReport?.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.InvestigationReport?.DigitalIdReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();

                    var latLongString = latitude + "," + longitude;

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = claim.InvestigationReport?.DigitalIdReport.IdImageLocationUrl,
                        distance = claim.InvestigationReport.DigitalIdReport.Distance,
                        duration = claim.InvestigationReport.DigitalIdReport.Duration,
                    });
                }
            }
            else
            {
                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

                if (claim.InvestigationReport is not null && claim.InvestigationReport?.DigitalIdReport?.IdImageLongLat is not null)
                {
                    var longLat = claim.InvestigationReport.DigitalIdReport.IdImageLongLat.IndexOf("/");
                    var latitude = claim.InvestigationReport?.DigitalIdReport?.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.InvestigationReport?.DigitalIdReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = claim.InvestigationReport?.DigitalIdReport.IdImageLocationUrl,
                        distance = claim.InvestigationReport.DigitalIdReport.Distance,
                        duration = claim.InvestigationReport.DigitalIdReport.Duration,
                    });
                }
            }
            return Ok();
        }

        [HttpGet("GetOcrDetail")]
        public async Task<IActionResult> GetOcrDetail(long claimid)
        {
            var claim = await _context.Investigations
                .Include(c => c.ClientCompany)
                .ThenInclude(c => c.Country)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.CaseQuestionnaire)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.PanIdReport)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.AgentIdReport)
                .FirstOrDefaultAsync(c => c.Id == claimid);

            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

                if (claim.InvestigationReport is not null && claim.InvestigationReport?.PanIdReport?.IdImageLongLat is not null)
                {
                    var longLat = claim.InvestigationReport.PanIdReport.IdImageLongLat.IndexOf("/");
                    var latitude = claim.InvestigationReport?.PanIdReport?.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.InvestigationReport?.PanIdReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = claim.InvestigationReport?.PanIdReport?.IdImageLocationUrl,
                        distance = claim.InvestigationReport.PanIdReport.Distance,
                        duration = claim.InvestigationReport.PanIdReport.Duration,
                    });
                }
            }
            else
            {
                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

                if (claim.InvestigationReport is not null && claim.InvestigationReport?.PanIdReport?.IdImageLongLat is not null)
                {
                    var longLat = claim.InvestigationReport.PanIdReport.IdImageLongLat.IndexOf("/");
                    var latitude = claim.InvestigationReport?.PanIdReport?.IdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.InvestigationReport?.PanIdReport?.IdImageLongLat.Substring(longLat + 1)?.Trim();

                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
                    return Ok(new
                    {
                        center,
                        dakota,
                        frick,
                        url = claim.InvestigationReport?.PanIdReport?.IdImageLocationUrl,
                        distance = claim.InvestigationReport.PanIdReport.Distance,
                        duration = claim.InvestigationReport.PanIdReport.Duration,
                    });
                }
            }
            return Ok();
        }
    }
}