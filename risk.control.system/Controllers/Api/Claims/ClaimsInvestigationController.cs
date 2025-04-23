//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.CodeAnalysis;
//using Microsoft.EntityFrameworkCore;

//using risk.control.system.AppConstant;
//using risk.control.system.Data;
//using risk.control.system.Helpers;
//using risk.control.system.Models;
//using risk.control.system.Models.ViewModel;
//using risk.control.system.Services;

//using static risk.control.system.AppConstant.Applicationsettings;

//using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

//namespace risk.control.system.Controllers.Api.Claims
//{
//    [ApiExplorerSettings(IgnoreApi = true)]
//    [Route("api/[controller]")]
//    [ApiController]
//    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
//    public class ClaimsInvestigationController : ControllerBase
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IClaimsService claimsService;
//        private readonly IWebHostEnvironment webHostEnvironment;
//        private readonly IHttpClientService httpClientService;
//        private static HttpClient httpClient = new HttpClient();

//        public ClaimsInvestigationController(ApplicationDbContext context,
//            IClaimsService claimsService,
//            IWebHostEnvironment webHostEnvironment,
//            IHttpClientService httpClientService)
//        {
//            _context = context;
//            this.claimsService = claimsService;
//            this.webHostEnvironment = webHostEnvironment;
//            this.httpClientService = httpClientService;
//        }

//        [HttpGet("GetPolicyDetail")]
//        public async Task<IActionResult> GetPolicyDetail(long id)
//        {
//            var policy = await _context.PolicyDetail
//                .Include(p => p.InvestigationServiceType)
//                .Include(p => p.CostCentre)
//                .Include(p => p.CaseEnabler)
//                .FirstOrDefaultAsync(p => p.PolicyDetailId == id);

//            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-policy.jpg");

//            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

//            var response = new
//            {
//                Document =
//                    policy.DocumentImage != null ?
//                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(policy.DocumentImage)) :
//                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
//                ContractNumber = policy.ContractNumber,
//                ClaimType = policy.ClaimType.GetEnumDisplayName(),
//                ContractIssueDate = policy.ContractIssueDate.ToString("dd-MMM-yyyy"),
//                DateOfIncident = policy.DateOfIncident.ToString("dd-MMM-yyyy"),
//                SumAssuredValue = policy.SumAssuredValue,
//                InvestigationServiceType = policy.InvestigationServiceType.Name,
//                CaseEnabler = policy.CaseEnabler.Name,
//                CauseOfLoss = policy.CauseOfLoss,
//                CostCentre = policy.CostCentre.Name,
//            };
//            return Ok(response);
//        }

//        [HttpGet("GetPolicyNotes")]
//        public async Task<IActionResult> GetPolicyNotes(long claimId)
//        {
//            var claim = await _context.Investigations
//                .Include(c => c.CaseNotes)
//                .FirstOrDefaultAsync(c => c.Id == claimId);

//            var response = new
//            {
//                notes = claim.CaseNotes.ToList()
//            };
//            return Ok(response);
//        }
//        [HttpGet("GetCustomerDetail")]
//        public async Task<IActionResult> GetCustomerDetail(long id)
//        {
//            var currentUserEmail = HttpContext.User.Identity.Name;
//            var isAgencyUser = _context.VendorApplicationUser.Any(u => u.Email == currentUserEmail);

//            var customer = await _context.CustomerDetail
//                .Include(c => c.Country)
//                .Include(c => c.State)
//                .Include(c => c.District)
//                .Include(c => c.PinCode)
//                .FirstOrDefaultAsync(p => p.CustomerDetailId == id);
//            if (isAgencyUser)
//            {
//                customer.ContactNumber = new string('*', customer.ContactNumber.Length - 4) + customer.ContactNumber.Substring(customer.ContactNumber.Length - 4);
//            }
//            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

//            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

//            return Ok(
//                new
//                {
//                    Customer = customer?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(customer.ProfilePicture)) :
//                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
//                    CustomerName = customer.Name,
//                    ContactNumber = customer.ContactNumber,
//                    Address = customer.Addressline + "  " + customer.District.Name + "  " + customer.State.Name + "  " + customer.Country.Name + "  " + customer.PinCode.Code,
//                    Occupation = customer.Occupation.GetEnumDisplayName(),
//                    Income = customer.Income.GetEnumDisplayName(),
//                    Education = customer.Education.GetEnumDisplayName(),
//                    DateOfBirth = customer.DateOfBirth.GetValueOrDefault().ToString("dd-MM-yyyy"),
//                }
//                );
//        }

//        [HttpGet("GetBeneficiaryDetail")]
//        public async Task<IActionResult> GetBeneficiaryDetail(long id, long claimId)
//        {
//            var beneficiary = await _context.BeneficiaryDetail
//                .Include(c => c.BeneficiaryRelation)
//                .Include(c => c.Country)
//                .Include(c => c.State)
//                .Include(c => c.District)
//                .Include(c => c.PinCode)
//                .FirstOrDefaultAsync(p => p.BeneficiaryDetailId == id && p.InvestigationTaskId == claimId);
//            var currentUserEmail = HttpContext.User.Identity.Name;
//            var isAgencyUser = _context.VendorApplicationUser.Any(u => u.Email == currentUserEmail);
//            if (isAgencyUser)
//            {
//                beneficiary.ContactNumber = new string('*', beneficiary.ContactNumber.Length - 4) + beneficiary.ContactNumber.Substring(beneficiary.ContactNumber.Length - 4);
//            }
//            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

//            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

//            return Ok(new
//            {
//                Beneficiary = beneficiary?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(beneficiary.ProfilePicture)) :
//                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
//                BeneficiaryName = beneficiary.Name,
//                Dob = (int)beneficiary.DateOfBirth.GetValueOrDefault().Subtract(DateTime.Now).TotalDays / 365,
//                Income = beneficiary.Income.GetEnumDisplayName(),
//                BeneficiaryRelation = beneficiary.BeneficiaryRelation.Name,
//                Address = beneficiary.Addressline + "  " + beneficiary.District.Name + "  " + beneficiary.State.Name + "  " + beneficiary.Country.Name + "  " + beneficiary.PinCode.Code,
//                ContactNumber = beneficiary.ContactNumber
//            }
//            );
//        }

//        [HttpGet("GetInvestigationFaceIdData")]
//        public async Task<IActionResult> GetInvestigationFaceIdData(long claimId)
//        {
//            var claim = await _context.Investigations
//                .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.DigitalIdReport)
//                .FirstOrDefaultAsync(c => c.Id == claimId);

//            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

//            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

//            string mapUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Applicationsettings.GMAPData}";
//            string imageAddress = string.Empty;
//            string faceLat = string.Empty, faceLng = string.Empty;
//            if (!string.IsNullOrWhiteSpace(claim.InvestigationReport?.DigitalIdReport?.DigitalIdImageLongLat))
//            {
//                var longLat = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
//                faceLat = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
//                faceLng = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim();
//                var longLatString = faceLat + "," + faceLng;
//                imageAddress = await httpClientService.GetRawAddress((faceLat), (faceLng));
//                mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=14&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
//            }

//            var data = new
//            {
//                Title = "Investigation Data",
//                LocationData = claim.InvestigationReport?.DigitalIdReport?.DigitalIdImageData ?? "Location Data",
//                LatLong = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLocationUrl,
//                ImageAddress = imageAddress,
//                Location = claim.InvestigationReport?.DigitalIdReport?.DigitalIdImage != null ?
//                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.InvestigationReport?.DigitalIdReport?.DigitalIdImage)) :
//                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
//                FacePosition =
//                new
//                {
//                    Lat = string.IsNullOrWhiteSpace(faceLat) ? decimal.Parse("-37.00") : decimal.Parse(faceLat),
//                    Lng = string.IsNullOrWhiteSpace(faceLng) ? decimal.Parse("140.00") : decimal.Parse(faceLng)
//                }
//            };

//            return Ok(data);
//        }

//        [HttpGet("GetInvestigationPanData")]
//        public async Task<IActionResult> GetInvestigationPanData(long claimId)
//        {
//            var claim = await _context.Investigations
//                .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.PanIdReport)
//                .FirstOrDefaultAsync(c => c.Id == claimId);

//            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

//            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

//            string panLatitude = string.Empty, panLongitude = string.Empty;
//            string panLocationUrl = $"https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
//            string panAddressAddress = string.Empty;
//            if (!string.IsNullOrWhiteSpace(claim.InvestigationReport?.PanIdReport?.DocumentIdImageLongLat))
//            {
//                var ocrlongLat = claim.InvestigationReport.PanIdReport.DocumentIdImageLongLat.IndexOf("/");
//                panLatitude = claim.InvestigationReport.PanIdReport.DocumentIdImageLongLat.Substring(0, ocrlongLat)?.Trim();
//                panLongitude = claim.InvestigationReport.PanIdReport.DocumentIdImageLongLat.Substring(ocrlongLat + 1)?.Trim();
//                var ocrLongLatString = panLatitude + "," + panLongitude;
//                panAddressAddress = await httpClientService.GetRawAddress((panLatitude), (panLongitude));
//                panLocationUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={ocrLongLatString}&zoom=14&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{ocrLongLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
//            }

//            var data = new
//            {
//                Title = "Investigation Data",
//                QrData = claim.InvestigationReport?.PanIdReport?.DocumentIdImageData,
//                OcrData = claim.InvestigationReport?.PanIdReport?.DocumentIdImage != null ?
//                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.InvestigationReport?.PanIdReport?.DocumentIdImage)) :
//                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
//                OcrLatLong = claim.InvestigationReport?.PanIdReport.DocumentIdImageLocationUrl,
//                OcrAddress = panAddressAddress,
//                OcrPosition =
//                new
//                {
//                    Lat = string.IsNullOrWhiteSpace(panLatitude) ? decimal.Parse("-37.0000") : decimal.Parse(panLatitude),
//                    Lng = string.IsNullOrWhiteSpace(panLongitude) ? decimal.Parse("140.00") : decimal.Parse(panLongitude)
//                }
//            };

//            return Ok(data);
//        }

//        [HttpGet("GetInvestigationPassportData")]
//        public async Task<IActionResult> GetInvestigationPassportData(string claimId)
//        {
//            var claim = claimsService.GetClaims()
//                .Include(c => c.InvestigationReport)
//                .Include(c => c.InvestigationReport.PassportIdReport)
//                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

//            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");
//            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

//            string passportUrl = $"https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
//            string passportAddress = string.Empty;
//            string passportLat = string.Empty, passportLng = string.Empty;
//            if (!string.IsNullOrWhiteSpace(claim.InvestigationReport?.PassportIdReport?.DocumentIdImageLongLat))
//            {
//                var passportlongLat = claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLat.IndexOf("/");
//                passportLat = claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLat.Substring(0, passportlongLat)?.Trim();
//                passportLng = claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLat.Substring(passportlongLat + 1)?.Trim();
//                var passportLongLatString = passportLat + "," + passportLng;
//                passportAddress = await httpClientService.GetRawAddress((passportLat), (passportLng));
//                passportUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={passportLongLatString}&zoom=14&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{passportLongLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
//            }

//            var data = new
//            {
//                Title = "Investigation Data",

//                PassportData = claim.InvestigationReport?.PassportIdReport?.DocumentIdImageData ?? "Passport Data",
//                PassportImage = claim.InvestigationReport?.PassportIdReport?.DocumentIdImage != null ?
//                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.InvestigationReport?.PassportIdReport?.DocumentIdImage)) :
//                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
//                PassportLatLong = claim.InvestigationReport?.PassportIdReport.DocumentIdImageLocationUrl,
//                PassportAddress = passportAddress,
//                PassportPosition =
//                new
//                {
//                    Lat = string.IsNullOrWhiteSpace(passportLat) ? decimal.Parse("-37.0000") : decimal.Parse(passportLat),
//                    Lng = string.IsNullOrWhiteSpace(passportLng) ? decimal.Parse("140.00") : decimal.Parse(passportLng)
//                }
//            };

//            return Ok(data);
//        }

//        [HttpGet("GetInvestigationVideoData")]
//        public async Task<IActionResult> GetInvestigationVideoData(string claimId)
//        {
//            var claim = claimsService.GetClaims()
//                .Include(c => c.InvestigationReport)
//                .Include(c => c.InvestigationReport.VideoReport)
//                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

//            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

//            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);
//            string videoUrl = $"https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
//            string videoAddress = string.Empty;
//            string videoLat = string.Empty, videoLng = string.Empty;
//            if (!string.IsNullOrWhiteSpace(claim.InvestigationReport?.VideoReport?.DocumentIdImageLongLat))
//            {
//                var videolongLat = claim.InvestigationReport.VideoReport.DocumentIdImageLongLat.IndexOf("/");
//                videoLat = claim.InvestigationReport.VideoReport.DocumentIdImageLongLat.Substring(0, videolongLat)?.Trim();
//                videoLng = claim.InvestigationReport.VideoReport.DocumentIdImageLongLat.Substring(videolongLat + 1)?.Trim();
//                var videoLongLatString = videoLat + "," + videoLng;
//                videoAddress = await httpClientService.GetRawAddress((videoLat), (videoLng));
//                videoUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={videoLongLatString}&zoom=14&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{videoLongLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
//            }


//            var data = new
//            {
//                Title = "Investigation Data",
//                VideoData = claim.InvestigationReport?.VideoReport?.DocumentIdImageData ?? "Video Data",
//                VideoImage = claim.InvestigationReport?.VideoReport?.DocumentIdImage != null ?
//                string.Format("data:video/*;base64,{0}", Convert.ToBase64String(claim.InvestigationReport?.VideoReport?.DocumentIdImage)) :
//                string.Format("data:video/*;base64,{0}", Convert.ToBase64String(noDataimage)),
//                VideoLatLong = claim.InvestigationReport?.VideoReport.DocumentIdImageLocationUrl,
//                VideoAddress = videoAddress,
//                VideoPosition =
//                new
//                {
//                    Lat = string.IsNullOrWhiteSpace(videoLat) ? decimal.Parse("-37.0000") : decimal.Parse(videoLat),
//                    Lng = string.IsNullOrWhiteSpace(videoLng) ? decimal.Parse("140.00") : decimal.Parse(videoLng)
//                }
//            };

//            return Ok(data);
//        }

//        [HttpGet("GetInvestigationAudioData")]
//        public async Task<IActionResult> GetInvestigationAudioData(string claimId)
//        {
//            var claim = claimsService.GetClaims()
//                .Include(c => c.InvestigationReport)
//                .Include(c => c.InvestigationReport.AudioReport)
//                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

//            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

//            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

//            string audioUrl = $"https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=150x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
//            string audioAddress = string.Empty;
//            string audioLat = string.Empty, audioLng = string.Empty;
//            if (!string.IsNullOrWhiteSpace(claim.InvestigationReport?.AudioReport?.DocumentIdImageLongLat))
//            {
//                var audiolongLat = claim.InvestigationReport.AudioReport.DocumentIdImageLongLat.IndexOf("/");
//                audioLat = claim.InvestigationReport.AudioReport.DocumentIdImageLongLat.Substring(0, audiolongLat)?.Trim();
//                audioLng = claim.InvestigationReport.AudioReport.DocumentIdImageLongLat.Substring(audiolongLat + 1)?.Trim();
//                var passportLongLatString = audioLat + "," + audioLng;
//                audioAddress = await httpClientService.GetRawAddress((audioLat), (audioLng));
//                audioUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={passportLongLatString}&zoom=14&size=300x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{passportLongLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
//            }

//            var data = new
//            {
//                Title = "Investigation Data",
//                AudioData = claim.InvestigationReport?.AudioReport?.DocumentIdImageData ?? "Audio Data",
//                AudioImage = claim.InvestigationReport?.AudioReport?.DocumentIdImage != null ?
//                string.Format("data:audio/*;base64,{0}", Convert.ToBase64String(claim.InvestigationReport?.AudioReport?.DocumentIdImage)) :
//                string.Format("data:audio/*;base64,{0}", Convert.ToBase64String(noDataimage)),
//                AudioLatLong = claim.InvestigationReport?.AudioReport.DocumentIdImageLocationUrl,
//                AudioAddress = audioAddress,
//                AudioPosition =
//                new
//                {
//                    Lat = string.IsNullOrWhiteSpace(audioLat) ? decimal.Parse("-37.0000") : decimal.Parse(audioLat),
//                    Lng = string.IsNullOrWhiteSpace(audioLng) ? decimal.Parse("140.00") : decimal.Parse(audioLng)
//                }
//            };

//            return Ok(data);
//        }

//        [HttpGet("GetCustomerMap")]
//        public async Task<IActionResult> GetCustomerMap(long id)
//        {
//            var customer = await _context.CustomerDetail
//                .Include(c => c.Country)
//                .Include(c => c.State)
//                .Include(c => c.District)
//                .Include(c => c.PinCode)
//                .FirstOrDefaultAsync(p => p.CustomerDetailId == id);

//            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-image.png");

//            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

//            var latitude = customer.Latitude;
//            var longitude = customer.Longitude.Trim();
//            var latLongString = latitude + "," + longitude;
//            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
//            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
//            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}.\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m} \r\nElevation(sea level):{weatherData.elevation} metres";
//            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=400x400&maptype=roadmap&markers=color:red%7Clabel:A%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

//            var data = new
//            {
//                profileMap = url,
//                weatherData = weatherCustomData,
//                address = customer.Addressline + " " + customer.District.Name + " " + customer.State.Name + " " + customer.Country.Name + " " + customer.PinCode.Code,
//                position = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) }
//            };
//            return Ok(data);
//        }

//        [HttpGet("GetBeneficiaryMap")]
//        public async Task<IActionResult> GetBeneficiaryMap(long id, long claimId)
//        {
//            var beneficiary = await _context.BeneficiaryDetail
//                .Include(c => c.BeneficiaryRelation)
//                .Include(c => c.Country)
//                .Include(c => c.State)
//                .Include(c => c.District)
//                .Include(c => c.PinCode)
//                .FirstOrDefaultAsync(p => p.BeneficiaryDetailId == id && p.InvestigationTaskId == claimId);

//            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-image.png");

//            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

//            var latitude = beneficiary.Latitude;
//            var longitude = beneficiary.Longitude.Trim();
//            var latLongString = latitude + "," + longitude;
//            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
//            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
//            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}.\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m} \r\nElevation(sea level):{weatherData.elevation} metres";

//            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=400x400&maptype=roadmap&markers=color:red%7Clabel:A%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
//            var data = new
//            {
//                profileMap = url,
//                weatherData = weatherCustomData,
//                address = beneficiary.Addressline + " " + beneficiary.District.Name + " " + beneficiary.State.Name + " " + beneficiary.Country.Name + " " + beneficiary.PinCode.Code,
//                position = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) }
//            };
//            return Ok(data);
//        }

//        [HttpGet("GetAgentDetail")]
//        public async Task<IActionResult> GetAgentDetail(long claimid)
//        {
//            var claim = await _context.Investigations
//                .Include(c => c.ClientCompany)
//                .ThenInclude(c => c.Country)
//                .Include(c => c.PolicyDetail)
//                .ThenInclude(c => c.InvestigationServiceType)
//                .Include(c => c.PolicyDetail)
//                .ThenInclude(c => c.CostCentre)
//                .Include(c => c.PolicyDetail)
//                .ThenInclude(c => c.CaseEnabler)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.PinCode)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.District)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.State)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.Country)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.PinCode)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.District)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.State)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.Country)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.BeneficiaryRelation)
//                .Include(c => c.CaseNotes)
//                .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.CaseQuestionnaire)
//                 .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.DigitalIdReport)
//                .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.PanIdReport)
//                 .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.AgentIdReport)
//                .FirstOrDefaultAsync(c => c.Id == claimid);

//            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
//            {
//                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.AgentIdReport?.DigitalIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.AgentIdReport.DigitalIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.AgentIdReport?.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.AgentIdReport?.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim();
//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new
//                    {
//                        center,
//                        dakota,
//                        frick,
//                        url = claim.InvestigationReport?.AgentIdReport.DigitalIdImageLocationUrl,
//                        distance = claim.InvestigationReport.AgentIdReport.Distance,
//                        duration = claim.InvestigationReport.AgentIdReport.Duration,
//                    });
//                }
//            }
//            else
//            {
//                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.AgentIdReport?.DigitalIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.AgentIdReport.DigitalIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.AgentIdReport?.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.AgentIdReport?.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim();
//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new
//                    {
//                        center,
//                        dakota,
//                        frick,
//                        url = claim.InvestigationReport?.AgentIdReport.DigitalIdImageLocationUrl,
//                        distance = claim.InvestigationReport.AgentIdReport.Distance,
//                        duration = claim.InvestigationReport.AgentIdReport.Duration,
//                    });
//                }
//            }
//            return Ok();
//        }

//        [HttpGet("GetFaceDetail")]
//        public async Task<IActionResult> GetFaceDetail(long claimid)
//        {
//            var claim = await _context.Investigations
//                .Include(c => c.ClientCompany)
//                .ThenInclude(c => c.Country)
//                .Include(c => c.PolicyDetail)
//                .ThenInclude(c => c.InvestigationServiceType)
//                .Include(c => c.PolicyDetail)
//                .ThenInclude(c => c.CostCentre)
//                .Include(c => c.PolicyDetail)
//                .ThenInclude(c => c.CaseEnabler)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.PinCode)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.District)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.State)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.Country)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.PinCode)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.District)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.State)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.Country)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.BeneficiaryRelation)
//                .Include(c => c.CaseNotes)
//                .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.CaseQuestionnaire)
//                 .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.DigitalIdReport)
//                .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.PanIdReport)
//                 .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.AgentIdReport)
//                .FirstOrDefaultAsync(c => c.Id == claimid);

//            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
//            {
//                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.DigitalIdReport?.DigitalIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim();

//                    var latLongString = latitude + "," + longitude;

//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new
//                    {
//                        center,
//                        dakota,
//                        frick,
//                        url = claim.InvestigationReport?.DigitalIdReport.DigitalIdImageLocationUrl,
//                        distance = claim.InvestigationReport.DigitalIdReport.Distance,
//                        duration = claim.InvestigationReport.DigitalIdReport.Duration,
//                    });
//                }
//            }
//            else
//            {
//                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.DigitalIdReport?.DigitalIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.DigitalIdReport?.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim();

//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new
//                    {
//                        center,
//                        dakota,
//                        frick,
//                        url = claim.InvestigationReport?.DigitalIdReport.DigitalIdImageLocationUrl,
//                        distance = claim.InvestigationReport.DigitalIdReport.Distance,
//                        duration = claim.InvestigationReport.DigitalIdReport.Duration,
//                    });
//                }
//            }
//            return Ok();
//        }

//        [HttpGet("GetOcrDetail")]
//        public async Task<IActionResult> GetOcrDetail(long claimid)
//        {
//            var claim = await _context.Investigations
//                .Include(c => c.ClientCompany)
//                .ThenInclude(c => c.Country)
//                .Include(c => c.PolicyDetail)
//                .ThenInclude(c => c.InvestigationServiceType)
//                .Include(c => c.PolicyDetail)
//                .ThenInclude(c => c.CostCentre)
//                .Include(c => c.PolicyDetail)
//                .ThenInclude(c => c.CaseEnabler)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.PinCode)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.District)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.State)
//                .Include(c => c.CustomerDetail)
//                .ThenInclude(c => c.Country)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.PinCode)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.District)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.State)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.Country)
//                .Include(c => c.BeneficiaryDetail)
//                .ThenInclude(c => c.BeneficiaryRelation)
//                .Include(c => c.CaseNotes)
//                .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.CaseQuestionnaire)
//                 .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.DigitalIdReport)
//                .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.PanIdReport)
//                 .Include(c => c.InvestigationReport)
//                .ThenInclude(c => c.AgentIdReport)
//                .FirstOrDefaultAsync(c => c.Id == claimid);

//            if (claim.PolicyDetail.InsuranceType== InsuranceType.UNDERWRITING)
//            {
//                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.PanIdReport?.DocumentIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.PanIdReport.DocumentIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.PanIdReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.PanIdReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new { 
//                        center, 
//                        dakota, 
//                        frick, 
//                        url = claim.InvestigationReport?.PanIdReport?.DocumentIdImageLocationUrl,
//                        distance = claim.InvestigationReport.PanIdReport.Distance,
//                        duration = claim.InvestigationReport.PanIdReport.Duration,
//                    });
//                }
//            }
//            else
//            {
//                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.PanIdReport?.DocumentIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.PanIdReport.DocumentIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.PanIdReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.PanIdReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new { 
//                        center, 
//                        dakota, 
//                        frick, 
//                        url = claim.InvestigationReport?.PanIdReport?.DocumentIdImageLocationUrl,
//                        distance = claim.InvestigationReport.PanIdReport.Distance,
//                        duration = claim.InvestigationReport.PanIdReport.Duration,
//                    });
//                }
//            }
//            return Ok();
//        }

//        [HttpGet("GetPassportDetail")]
//        public IActionResult GetPassportDetail(string claimid)
//        {
//            var claim = claimsService.GetClaims()
//                .Include(c => c.InvestigationReport)
//                .Include(c => c.InvestigationReport.PassportIdReport)
//                .FirstOrDefault(c => c.ClaimsInvestigationId == claimid);

//            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CONSTANTS.UNDERWRITING).LineOfBusinessId;
//            if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness)
//            {
//                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.PassportIdReport?.DocumentIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.PassportIdReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.PassportIdReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new { 
//                        center, 
//                        dakota, 
//                        frick, 
//                        url = claim.InvestigationReport?.PassportIdReport?.DocumentIdImageLocationUrl,
//                        distance = claim.InvestigationReport.PassportIdReport.Distance,
//                        duration = claim.InvestigationReport.PassportIdReport.Duration,
//                    });
//                }
//            }
//            else
//            {
//                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.PassportIdReport?.DocumentIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.PassportIdReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.PassportIdReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new { 
//                        center, 
//                        dakota, 
//                        frick, 
//                        url = claim.InvestigationReport?.PassportIdReport?.DocumentIdImageLocationUrl,
//                        distance = claim.InvestigationReport.PassportIdReport.Distance,
//                        duration = claim.InvestigationReport.PassportIdReport.Duration,
//                    });
//                }
//            }
//            return Ok();
//        }

//        [HttpGet("GetAudioDetail")]
//        public IActionResult GetAudioDetail(string claimid)
//        {
//            var claim = claimsService.GetClaims()
//                .Include(c => c.InvestigationReport)
//                .Include(c => c.InvestigationReport.AudioReport)
//                .FirstOrDefault(c => c.ClaimsInvestigationId == claimid);
//            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CONSTANTS.UNDERWRITING).LineOfBusinessId;

//            if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness)
//            {
//                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.AudioReport?.DocumentIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.AudioReport.DocumentIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.AudioReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.AudioReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new { 
//                        center, 
//                        dakota, 
//                        frick, 
//                        url = claim.InvestigationReport?.AudioReport?.DocumentIdImageLocationUrl,
//                        distance = claim.InvestigationReport.AudioReport.Distance,
//                        duration = claim.InvestigationReport.AudioReport.Duration,
//                    });
//                }
//            }
//            else
//            {
//                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.AudioReport?.DocumentIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.AudioReport.DocumentIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.AudioReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.AudioReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new
//                    {
//                        center,
//                        dakota,
//                        frick,
//                        url = claim.InvestigationReport?.AudioReport?.DocumentIdImageLocationUrl,
//                        distance = claim.InvestigationReport.AudioReport.Distance,
//                        duration = claim.InvestigationReport.AudioReport.Duration,
//                    });
//                }
//            }
//            return Ok();
//        }


//        [HttpGet("GetVideoDetail")]
//        public IActionResult GetVideoDetail(string claimid)
//        {
//            var claim = claimsService.GetClaims()
//                .Include(c => c.InvestigationReport)
//                .Include(c => c.InvestigationReport.VideoReport)
//                .FirstOrDefault(c => c.ClaimsInvestigationId == claimid);
//            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CONSTANTS.UNDERWRITING).LineOfBusinessId;

//            if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness)
//            {
//                var center = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.CustomerDetail.Latitude), Lng = decimal.Parse(claim.CustomerDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.VideoReport?.DocumentIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.VideoReport.DocumentIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.VideoReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.VideoReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new { 
//                        center, 
//                        dakota, 
//                        frick, 
//                        url = claim.InvestigationReport?.VideoReport?.DocumentIdImageLocationUrl,
//                        distance = claim.InvestigationReport.VideoReport.Distance,
//                        duration = claim.InvestigationReport.VideoReport.Duration,
//                    });
//                }
//            }
//            else
//            {
//                var center = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };
//                var dakota = new { Lat = decimal.Parse(claim.BeneficiaryDetail.Latitude), Lng = decimal.Parse(claim.BeneficiaryDetail.Longitude) };

//                if (claim.InvestigationReport is not null && claim.InvestigationReport?.VideoReport?.DocumentIdImageLongLat is not null)
//                {
//                    var longLat = claim.InvestigationReport.VideoReport.DocumentIdImageLongLat.IndexOf("/");
//                    var latitude = claim.InvestigationReport?.VideoReport?.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
//                    var longitude = claim.InvestigationReport?.VideoReport?.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim();

//                    var frick = new { Lat = decimal.Parse(latitude), Lng = decimal.Parse(longitude) };
//                    return Ok(new
//                    {
//                        center,
//                        dakota,
//                        frick,
//                        url = claim.InvestigationReport?.VideoReport?.DocumentIdImageLocationUrl,
//                        distance = claim.InvestigationReport.VideoReport.Distance,
//                        duration = claim.InvestigationReport.VideoReport.Duration,
//                    });
//                }
//            }
//            return Ok();
//        }
//    }
//}