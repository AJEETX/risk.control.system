using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;

using Highsoft.Web.Mvc.Charts;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private Regex regex = new Regex(@"^[\w/\:.-]+;base64,");
        private readonly ApplicationDbContext _context;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private static HttpClient httpClient = new();
        private static string urlAddress = "http://icheck-webSe-kOnc2X2NMOwe-196777346.ap-southeast-2.elb.amazonaws.com";
        private static string FaceUrl = "http://icheck-webse-konc2x2nmowe-196777346.ap-southeast-2.elb.amazonaws.com/faceMatch";
        private static string PanUrl = "https://pan-card-verification-at-lowest-price.p.rapidapi.com/verifyPan/";

        //test PAN FNLPM8635N
        public AgentController(ApplicationDbContext context, IClaimsInvestigationService claimsInvestigationService, IMailboxService mailboxService, IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.webHostEnvironment = webHostEnvironment;
        }

        [AllowAnonymous]
        [HttpGet("GetImage")]
        public IActionResult GetImage(string claimId, string type)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            try
            {
                var caseLocation = _context.CaseLocation
                    .Include(l => l.ClaimsInvestigation)
                    .Include(l => l.ClaimReport)
                    .FirstOrDefault(c => c.ClaimsInvestigation.ClaimsInvestigationId == claimId);

                if (caseLocation != null)
                {
                    if (type.ToLower() == "face")
                    {
                        var image = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(caseLocation.ClaimReport?.AgentLocationPicture));
                        return Ok(new { Image = image, Valid = caseLocation.ClaimReport.LocationPictureConfidence ?? "00.00" });
                    }

                    if (type.ToLower() == "ocr")
                    {
                        var image = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(caseLocation.ClaimReport?.AgentOcrPicture));
                        return Ok(new { Image = image, Valid = caseLocation.ClaimReport.PanValid.ToString() });
                    }
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("test2")]
        public async Task<IActionResult> Test2(DocImage image)
        {
            var response = await httpClient.PostAsJsonAsync(urlAddress, image);

            var maskedImage = await response.Content.ReadAsStringAsync();

            var maskedImageDetail = JsonConvert.DeserializeObject<FaceImageDetail>(maskedImage);

            return Ok(maskedImageDetail);
        }

        [AllowAnonymous]
        [HttpGet("agent")]
        public async Task<IActionResult> Index(string email = "agent@verify.com")
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.District)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.State)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Country)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
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
                .ThenInclude(c => c.State);

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == email);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));
                var claimsAssigned = new List<ClaimsInvestigation>();

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId
                        && c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId
                        && c.AssignedAgentUserEmail == email)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsAssigned.Add(item);
                    }
                }
                var filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-policy.jpg");

                var noDocumentimage = await System.IO.File.ReadAllBytesAsync(filePath);

                filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

                var noCustomerimage = await System.IO.File.ReadAllBytesAsync(filePath);

                var claim2Agent = claimsAssigned
                    .Select(c =>
                new
                {
                    claimId = c.ClaimsInvestigationId,
                    claimType = c.PolicyDetail.ClaimType.GetEnumDisplayName(),
                    DocumentPhoto = c.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(c.PolicyDetail.DocumentImage)) :
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDocumentimage)),
                    CustomerName = c.CustomerDetail.CustomerName,
                    CustomerEmail = email,
                    PolicyNumber = c.PolicyDetail.ContractNumber,
                    Gender = c.CustomerDetail.Gender.GetEnumDisplayName(),
                    c.CustomerDetail.Addressline,
                    c.CustomerDetail.PinCode.Code,
                    CustomerPhoto = c?.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(c?.CustomerDetail.ProfilePicture)) :
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noCustomerimage)),
                    Country = c.CustomerDetail.Country.Name,
                    State = c.CustomerDetail.State.Name,
                    District = c.CustomerDetail.District.Name,
                    c.CustomerDetail.Description,
                    Locations = c.CaseLocations.Select(l => new
                    {
                        l.CaseLocationId,
                        Photo = l?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(l.ProfilePicture)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noCustomerimage)),
                        l.Country.Name,
                        l.BeneficiaryName,
                        l.Addressline,
                        l?.Addressline2,
                        l.PinCode.Code,
                        District = l.District.Name,
                        State = l.State.Name
                    })
                });
                return Ok(claim2Agent);
            }
            return Unauthorized();
        }

        [AllowAnonymous]
        [HttpGet("get")]
        public async Task<IActionResult> Get(string claimId)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId
                );
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var claimCase = _context.CaseLocation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.PinCode)
                .Include(c => c.ClaimReport)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

            var filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-policy.jpg");

            var noDocumentimage = await System.IO.File.ReadAllBytesAsync(filePath);

            filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

            var noCustomerimage = await System.IO.File.ReadAllBytesAsync(filePath);

            filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(filePath);

            return Ok(
                new
                {
                    Policy = new
                    {
                        ClaimId = claim.ClaimsInvestigationId,
                        PolicyNumber = claim.PolicyDetail.ContractNumber,
                        ClaimType = claim.PolicyDetail.ClaimType.GetEnumDisplayName(),
                        Document = claim.PolicyDetail.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.PolicyDetail.DocumentImage)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDocumentimage)),
                        IssueDate = claim.PolicyDetail.ContractIssueDate.ToString("dd-MMM-yyyy"),
                        IncidentDate = claim.PolicyDetail.DateOfIncident.ToString("dd-MMM-yyyy"),
                        Amount = claim.PolicyDetail.SumAssuredValue,
                        BudgetCentre = claim.PolicyDetail.CostCentre.Name,
                        Reason = claim.PolicyDetail.CaseEnabler.Name
                    },
                    beneficiary = new
                    {
                        BeneficiaryId = claimCase.CaseLocationId,
                        Name = claimCase.BeneficiaryName,
                        Photo = claimCase.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimCase.ProfilePicture)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noCustomerimage)),
                        Relation = claimCase.BeneficiaryRelation.Name,
                        Income = claimCase.BeneficiaryIncome.GetEnumDisplayName(),
                        Phone = claimCase.BeneficiaryContactNumber,
                        DateOfBirth = claimCase.BeneficiaryDateOfBirth.ToString("dd-MMM-yyyy"),
                        Address = claimCase.Addressline + " " + claimCase.District.Name + " " + claimCase.State.Name + " " + claimCase.Country.Name + " " + claimCase.PinCode.Code
                    },
                    Customer = new
                    {
                        Name = claim.CustomerDetail.CustomerName,
                        Occupation = claim.CustomerDetail.CustomerOccupation.GetEnumDisplayName(),
                        Photo = claim.CustomerDetail.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.CustomerDetail.ProfilePicture)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noCustomerimage)),
                        Income = claim.CustomerDetail.CustomerIncome.GetEnumDisplayName(),
                        Phone = claim.CustomerDetail.ContactNumber,
                        DateOfBirth = claim.CustomerDetail.CustomerDateOfBirth.ToString("dd-MMM-yyyy"),
                        Address = claim.CustomerDetail.Addressline + " " + claim.CustomerDetail.District.Name + " " + claim.CustomerDetail.State.Name + " " + claim.CustomerDetail.Country.Name + " " + claim.CustomerDetail.PinCode.Code
                    },
                    InvestigationData = new
                    {
                        LocationImage = claimCase?.ClaimReport?.AgentLocationPicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimCase?.ClaimReport?.AgentLocationPicture)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                        OcrImage = claimCase?.ClaimReport?.AgentOcrPicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimCase?.ClaimReport?.AgentOcrPicture)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                        OcrData = claimCase?.ClaimReport?.AgentOcrData,
                        LocationLongLat = claimCase?.ClaimReport?.LocationLongLat,
                        OcrLongLat = claimCase?.ClaimReport?.OcrLongLat,
                    },
                    Remarks = claimCase?.ClaimReport?.AgentRemarks
                });
        }

        [AllowAnonymous]
        [RequestSizeLimit(100_000_000)]
        [HttpPost("post")]
        public async Task<IActionResult> Post(Data data)
        {
            var claimCase = _context.CaseLocation
               .Include(c => c.BeneficiaryRelation)
               .Include(c => c.ClaimReport)
               .Include(c => c.PinCode)
               .Include(c => c.District)
               .Include(c => c.State)
               .Include(c => c.Country)
               .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

            if (claimCase == null)
            {
                return BadRequest();
            }
            claimCase.ClaimReport.AgentEmail = data.Email;

            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

            if (!string.IsNullOrWhiteSpace(data.LocationImage))
            {
                byte[] registeredImage = null!;

                if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredImage = claim.CustomerDetail.ProfilePicture;
                }
                if (claim.PolicyDetail.ClaimType == ClaimType.DEATH)
                {
                    registeredImage = claimCase.ProfilePicture;
                }

                try
                {
                    if (registeredImage != null)
                    {
                        var base64Image = Convert.ToBase64String(registeredImage);

                        var response = await httpClient.PostAsJsonAsync(FaceUrl, new { source = base64Image, dest = data.LocationImage });

                        var ImageData = await response.Content.ReadAsStringAsync();

                        var faceImageDetail = JsonConvert.DeserializeObject<FaceMatchDetail>(ImageData);

                        claimCase.ClaimReport.LocationPictureConfidence = faceImageDetail.Confidence;
                    }
                }
                catch (Exception ex)
                {
                    claimCase.ClaimReport.LocationPictureConfidence = "failed ";
                }

                var image = Convert.FromBase64String(data.LocationImage);
                var locationRealImage = ByteArrayToImage(image);
                MemoryStream stream = new MemoryStream(image);
                claimCase.ClaimReport.AgentLocationPicture = image;
                var filePath = Path.Combine(webHostEnvironment.WebRootPath, "document", $"loc{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.{locationRealImage.ImageType()}");
                claimCase.ClaimReport.AgentLocationPictureUrl = filePath;
                CompressImage.Compressimage(stream, filePath);
                claimCase.ClaimReport.LocationLongLatTime = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(data.OcrImage))
            {
                var inputImage = new DocImage { Image = data.OcrImage };

                var response = await httpClient.PostAsJsonAsync(urlAddress, inputImage);

                var maskedImage = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(maskedImage))
                {
                    try
                    {
                        var maskedImageDetail = JsonConvert.DeserializeObject<FaceImageDetail>(maskedImage);
                        var request = new HttpRequestMessage
                        {
                            Method = HttpMethod.Get,
                            RequestUri = new Uri(PanUrl + maskedImageDetail.DocumentId),
                            Headers =
                            {
                                { "x-rapid-api", "rapid-api-database" },
                                { "X-RapidAPI-Key", "47cd2be148msh455c39da6e1d554p1733e0jsn8bd7464ed610" },
                                { "X-RapidAPI-Host", "pan-card-verification-at-lowest-price.p.rapidapi.com" },
                            },
                        };
                        using (var panResponse = await httpClient.SendAsync(request))
                        {
                            panResponse.EnsureSuccessStatusCode();
                            var body = await panResponse.Content.ReadAsStringAsync();
                            try
                            {
                                var panData = JsonConvert.DeserializeObject<PanValidationResponse>(body);

                                if (panData != null && claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
                                {
                                    if (claim.CustomerDetail.CustomerName.ToLower().Contains(panData.first_name))
                                        claimCase.ClaimReport.PanValid = true;
                                }

                                claimCase.ClaimReport.PanValid = true;
                            }
                            catch (Exception ex)
                            {
                                var panInvalidData = JsonConvert.DeserializeObject<PanInValidationResponse>(body);
                                if (panInvalidData != null && panInvalidData.status == 500)
                                {
                                    Console.WriteLine(panInvalidData.status);
                                }
                            }
                        }

                        var image = Convert.FromBase64String(maskedImageDetail.MaskedImage);
                        var OcrRealImage = ByteArrayToImage(image);
                        MemoryStream stream = new MemoryStream(image);
                        claimCase.ClaimReport.AgentOcrPicture = image;
                        var filePath = Path.Combine(webHostEnvironment.WebRootPath, "document", $"ocr{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.{OcrRealImage.ImageType()}");
                        claimCase.ClaimReport.AgentOcrUrl = filePath;
                        CompressImage.Compressimage(stream, filePath);
                        claimCase.ClaimReport.OcrLongLatTime = DateTime.UtcNow;
                        claimCase.ClaimReport.ImageType = maskedImageDetail.DocType;
                        claimCase.ClaimReport.AgentOcrData = " Doc type: " + maskedImageDetail.DocType;

                        if (!string.IsNullOrWhiteSpace(data.OcrData))
                        {
                            claimCase.ClaimReport.AgentOcrData = claimCase.ClaimReport.AgentOcrData + ".\n " +
                                "" + data.OcrData.Replace(maskedImageDetail.DocumentId, "xxxxxxxxxx");
                        }
                    }
                    catch (Exception)
                    {
                        var image = Convert.FromBase64String(data.OcrImage);
                        var OcrRealImage = ByteArrayToImage(image);
                        MemoryStream stream = new MemoryStream(image);
                        claimCase.ClaimReport.AgentOcrPicture = image;
                        var filePath = Path.Combine(webHostEnvironment.WebRootPath, "document", $"ocr{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.{OcrRealImage.ImageType()}");
                        claimCase.ClaimReport.AgentOcrUrl = filePath;
                        CompressImage.Compressimage(stream, filePath);
                        claimCase.ClaimReport.OcrLongLatTime = DateTime.UtcNow;
                    }
                }
                else
                {
                    var image = Convert.FromBase64String(data.OcrImage);
                    var OcrRealImage = ByteArrayToImage(image);
                    MemoryStream stream = new MemoryStream(image);
                    claimCase.ClaimReport.AgentOcrPicture = image;
                    var filePath = Path.Combine(webHostEnvironment.WebRootPath, "document", $"ocr{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.{OcrRealImage.ImageType()}");
                    claimCase.ClaimReport.AgentOcrUrl = filePath;
                    CompressImage.Compressimage(stream, filePath);
                    claimCase.ClaimReport.OcrLongLatTime = DateTime.UtcNow;
                }
            }

            if (!string.IsNullOrWhiteSpace(data.LocationLongLat))
            {
                claimCase.ClaimReport.LocationLongLatTime = DateTime.UtcNow;
                claimCase.ClaimReport.LocationLongLat = data.LocationLongLat;
            }
            if (string.IsNullOrWhiteSpace(claimCase.ClaimReport.AgentOcrData) && !string.IsNullOrWhiteSpace(data.OcrData))
            {
                claimCase.ClaimReport.AgentOcrData = claimCase.ClaimReport.AgentOcrData + ".\n " +
                    "" + data.OcrData;
            }

            if (!string.IsNullOrWhiteSpace(data.OcrLongLat))
            {
                claimCase.ClaimReport.OcrLongLat = data.OcrLongLat;
                claimCase.ClaimReport.OcrLongLatTime = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(data.Question1))
            {
                claimCase.ClaimReport.Question1 = data.Question1;
            }

            if (!string.IsNullOrWhiteSpace(data.Question2))
            {
                claimCase.ClaimReport.Question2 = data.Question2;
            }

            var longLat = claimCase.ClaimReport.LocationLongLat.IndexOf("/");
            var latitude = claimCase.ClaimReport.LocationLongLat.Substring(0, longLat)?.Trim();
            var longitude = claimCase.ClaimReport.LocationLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";
            claimCase.ClaimReport.LocationData = weatherCustomData;

            _context.CaseLocation.Update(claimCase);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                throw exception;
            }

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            return Ok(new
            {
                BeneficiaryId = claimCase.CaseLocationId,
                LocationImage = !string.IsNullOrWhiteSpace(claimCase.ClaimReport.AgentLocationPictureUrl) ?
                Convert.ToBase64String(System.IO.File.ReadAllBytes(claimCase.ClaimReport.AgentLocationPictureUrl)) :
                Convert.ToBase64String(noDataimage),
                LocationLongLat = claimCase.ClaimReport.LocationLongLat,
                LocationTime = claimCase.ClaimReport.LocationLongLatTime,
                OcrImage = !string.IsNullOrWhiteSpace(claimCase.ClaimReport.AgentOcrUrl) ?
                Convert.ToBase64String(System.IO.File.ReadAllBytes(claimCase.ClaimReport.AgentOcrUrl)) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = claimCase.ClaimReport.OcrLongLat,
                OcrTime = claimCase.ClaimReport.OcrLongLatTime,
                FacePercent = claimCase.ClaimReport.LocationPictureConfidence,
                PanValid = claimCase.ClaimReport.PanValid
            });
        }

        [AllowAnonymous]
        [HttpPost("submit")]
        public async Task<IActionResult> Submit(SubmitData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Email) || string.IsNullOrWhiteSpace(data.Remarks) || string.IsNullOrWhiteSpace(data.ClaimId) || data.BeneficiaryId < 1)
            {
                throw new ArgumentNullException("Argument(s) can't be null");
            }

            await claimsInvestigationService.SubmitToVendorSupervisor(data.Email, data.BeneficiaryId, data.ClaimId, data.Remarks, data.Question1, data.Question2, data.Question3, data.Question4);

            await mailboxService.NotifyClaimReportSubmitToVendorSupervisor(data.Email, data.ClaimId, data.BeneficiaryId);

            return Ok(new { data });
        }

        [AllowAnonymous]
        [HttpGet("Vendors")]
        public async Task<IActionResult> Vendors()
        {
            var applicationDbContext = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes).ToListAsync();

            var data = applicationDbContext.Select(a => new VendorData
            {
                Image = a.DocumentImage,
                Name = a.Name,
                Code = a.Code,
                PhoneNumber = a.PhoneNumber,
                Email = a.Email,
                Addressline = a.Addressline,
                State = a.State.Name,
                Created = a.Created.ToString("dd/MM/yyyy")
            });

            var response = new VendorDataDataTable
            {
                data = data.ToList()
            };
            return Ok(response);
        }

        public Image? ByteArrayToImage(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            Image returnImage = Image.FromStream(ms);
            return returnImage;
        }
    }

    public class PanInValidationResponse
    {
        public string error { get; set; }
        public string message { get; set; }
        public int status { get; set; }
    }

    public class PanValidationResponse
    {
        public string @entity { get; set; }
        public string pan { get; set; }
        public string first_name { get; set; }
        public string middle_name { get; set; }
        public string last_name { get; set; }
    }

    public class FaceMatchDetail
    {
        public decimal FaceLeftCoordinate { get; set; }
        public decimal FaceTopCcordinate { get; set; }
        public string Confidence { get; set; }
    }

    public class FaceImageDetail
    {
        public string DocType { get; set; }
        public string DocumentId { get; set; }
        public string MaskedImage { get; set; }
    }

    public class DocImage
    {
        public string Image { get; set; }
    }

    public class SubmitData
    {
        public string Email { get; set; }
        public string ClaimId { get; set; }
        public long BeneficiaryId { get; set; }
        public string? Question1 { get; set; }
        public string? Question2 { get; set; }
        public string? Question3 { get; set; }
        public string? Question4 { get; set; }
        public string Remarks { get; set; }
    }

    public class Data
    {
        public string Email { get; set; }
        public string ClaimId { get; set; }
        public string? LocationImage { get; set; }
        public string? LocationData { get; set; }
        public string? LocationLongLat { get; set; }
        public string? OcrImage { get; set; }
        public string? OcrLongLat { get; set; }
        public string? OcrData { get; set; }
        public string? Question1 { get; set; }
        public string? Question2 { get; set; }
        public string? Remarks { get; set; }
    }

    public class VendorData
    {
        public byte[]? Image { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Addressline { get; set; }
        public string State { get; set; }
        public string Created { get; set; }
    }

    public class VendorDataDataTable
    {
        public List<VendorData> data { get; set; }
    }
}