using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private static string PanIdfyUrl = "https://idfy-verification-suite.p.rapidapi.com";
        private static string RapidAPIKey = "df0893831fmsh54225589d7b9ad1p15ac51jsnb4f768feed6f";
        private static string PanTask_id = "74f4c926-250c-43ca-9c53-453e87ceacd1";
        private static string PanGroup_id = "8e16424a-58fc-4ba4-ab20-5bc8e7c3c41e";
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientService httpClientService;
        private readonly UserManager<VendorApplicationUser> userVendorManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAgentService agentService;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IICheckifyService iCheckifyService;
        private static string FaceMatchBaseUrl = "https://2j2sgigd3l.execute-api.ap-southeast-2.amazonaws.com/Development/icheckify";
        private static Random randomNumber = new Random();
        private string portal_base_url = string.Empty;
        //test PAN FNLPM8635N
        public AgentController(ApplicationDbContext context, IHttpClientService httpClientService,
            UserManager<VendorApplicationUser> userVendorManager,
             IHttpContextAccessor httpContextAccessor,
            IAgentService agentService,
            IClaimsInvestigationService claimsInvestigationService, IMailboxService mailboxService,
            IWebHostEnvironment webHostEnvironment, IICheckifyService iCheckifyService)
        {
            this._context = context;
            this.httpClientService = httpClientService;
            this.userVendorManager = userVendorManager;
            this.agentService = agentService;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.webHostEnvironment = webHostEnvironment;
            this.iCheckifyService = iCheckifyService;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [AllowAnonymous]
        [HttpPost("ResetUid")]
        public async Task<IActionResult> ResetUid([Required] string mobile, bool sendSMS = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mobile))
                {
                    return BadRequest($"Empty mobile number");
                }
                var user2Onboard = await agentService.ResetUid(mobile, sendSMS);

                if (user2Onboard == null)
                {
                    return BadRequest($"Agent does not exist");
                }

                return Ok(new { Email = user2Onboard.Email, Pin = user2Onboard.SecretPin });
            }
            catch (Exception)
            {
                return BadRequest($"mobile number and/or Agent does not exist");
            }
        }

        [AllowAnonymous]
        [HttpPost("VerifyMobile")]
        public async Task<IActionResult> VerifyMobile(VerifyMobileRequest request)
        {
            try
            {
                if (request is null || string.IsNullOrWhiteSpace(request.Mobile) || request.Mobile.Length < 11 || string.IsNullOrWhiteSpace(request.Uid) || request.Uid.Length < 5)
                {
                    return BadRequest($"{nameof(request.Mobile)} {request.Uid} and/or {nameof(request.Mobile)} {request.Uid} invalid");
                }
                if (request.CheckUid)
                {
                    var mobileUidExist = _context.VendorApplicationUser.Any(
                                    v => v.MobileUId == request.Uid);
                    if (mobileUidExist)
                    {
                        return BadRequest($"{nameof(request.Uid)} {request.Uid} exists");
                    }
                }

                var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENT.ToString()));
                var user2Onboards = _context.VendorApplicationUser.Where(
                    u => u.PhoneNumber == request.Mobile);
                foreach (var user2Onboard in user2Onboards)
                {
                    var isAgent = await userVendorManager.IsInRoleAsync(user2Onboard, agentRole?.Name);

                    if (isAgent && string.IsNullOrWhiteSpace(user2Onboard.MobileUId) && user2Onboard.Active)
                    {
                        user2Onboard.MobileUId = request.Uid;
                        user2Onboard.SecretPin = randomNumber.Next(1000, 9999).ToString();
                        _context.VendorApplicationUser.Update(user2Onboard);
                        _context.SaveChanges();
                        if (request.SendSMS)
                        {
                            //SEND SMS
                            string message = $"Dear {user2Onboard.Email}";
                            message += $"                                       ";
                            message += $"icheckifyApp Pin:{user2Onboard.SecretPin}";
                            message += $"                                      ";
                            message += $"Thanks                           ";
                            message += $"                                ";
                            message += $"https://icheckify.co.in";
                            var response = SmsService.SendSingleMessage(request.Mobile, message, request.SendSMS);
                        }

                        return Ok(new { Email = user2Onboard.Email, Pin = user2Onboard.SecretPin });
                    }
                }
                return BadRequest($"Err");
            }
            catch (Exception)
            {
                return BadRequest($"mobile number and/or Agent does not exist");
            }
        }
        [AllowAnonymous]
        [HttpPost("VerifyId")]
        public async Task<IActionResult> VerifyId(VerifyIdRequest request)
        {
            try
            {
                if (request is null || string.IsNullOrWhiteSpace(request.Uid) || string.IsNullOrWhiteSpace(request.Image))
                {
                    return BadRequest();
                }
                var mobileUidExist = _context.VendorApplicationUser.FirstOrDefault(v => v.MobileUId == request.Uid);
                if (mobileUidExist == null)
                {
                    return BadRequest($"{nameof(request.Uid)} {request.Uid} not exists");
                }
                if (!request.VerifyId)
                {
                    return Ok(new { Email = mobileUidExist.Email, Pin = mobileUidExist.SecretPin });
                }

                var image = Convert.FromBase64String(request.Image);

                var savedImage = ImageCompression.ConverterSkia(image);
                //string path = Path.Combine(webHostEnvironment.WebRootPath, "onboard");
                //if (!Directory.Exists(path))
                //{
                //    Directory.CreateDirectory(path);
                //}

                //using MemoryStream stream = new MemoryStream(image);

                //var filePath = Path.Combine(path, $"face{DateTime.Now.ToString("dd-MMM-yyyy-HH-mm-ss")}.jpg");
                //CompressImage.CompressimageWindows(stream, filePath);

                //var savedImage = await System.IO.File.ReadAllBytesAsync(filePath);

                var saveImageBase64Image2Verify = Convert.ToBase64String(savedImage);

                var saveImageBase64String = Convert.ToBase64String(mobileUidExist.ProfilePicture);

                var faceImageDetail = await httpClientService.GetFaceMatch(new MatchImage { Source = saveImageBase64String, Dest = saveImageBase64Image2Verify }, FaceMatchBaseUrl);

                if (faceImageDetail == null || faceImageDetail?.Confidence == null)
                {
                    return BadRequest("face mismatch");
                }
                return Ok(new { Email = mobileUidExist.Email, Pin = mobileUidExist.SecretPin });
            }
            catch (Exception)
            {
                return BadRequest("face mismatch");
            }
        }

        [AllowAnonymous]
        [HttpPost("VerifyDocument")]
        public async Task<IActionResult> VerifyDocument(VerifyDocumentRequest request)
        {
            try
            {
                if (request is null || string.IsNullOrWhiteSpace(request.Uid) || string.IsNullOrWhiteSpace(request.Image))
                {
                    return BadRequest();
                }
                var mobileUidExist = _context.VendorApplicationUser.FirstOrDefault(v => v.MobileUId == request.Uid);
                if (mobileUidExist == null)
                {
                    return BadRequest($"{nameof(request.Uid)} {request.Uid} not exists");
                }
                if (!request.VerifyPan)
                {
                    return Ok(new { Email = mobileUidExist.Email, Pin = mobileUidExist.SecretPin });
                }
                if (request.Type.ToUpper() != "PAN")
                {
                    return BadRequest("incorrect document");
                }
                //VERIFY PAN
                var saveImageBase64String = Convert.ToBase64String(mobileUidExist.ProfilePicture);
                var maskedImage = await httpClientService.GetMaskedImage(new MaskImage { Image = request.Image }, FaceMatchBaseUrl);
                if (maskedImage == null || maskedImage.DocType.ToUpper() != "PAN")
                {
                    return BadRequest("document issue");
                }

                var body = await httpClientService.VerifyPan(maskedImage.DocumentId, PanIdfyUrl, RapidAPIKey, PanTask_id, PanGroup_id);

                if (body != null && body?.status == "completed" &&
                    body?.result != null &&
                    body.result?.source_output != null
                    && body.result?.source_output?.status == "id_found")
                {
                    return Ok(new { Email = mobileUidExist.Email, Pin = mobileUidExist.SecretPin });
                }
                return BadRequest("document verify issue");
            }
            catch (Exception)
            {
                return BadRequest("document verify issue");
            }
        }
        [ApiExplorerSettings(IgnoreApi = true)]

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
                    .ThenInclude(l => l.DigitalIdReport)
                    .Include(l => l.ClaimReport)
                    .ThenInclude(l => l.DocumentIdReport)
                    .FirstOrDefault(c => c.ClaimsInvestigation.ClaimsInvestigationId == claimId);

                if (caseLocation != null)
                {
                    if (type.ToLower() == "face")
                    {
                        var image = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(caseLocation.ClaimReport?.DigitalIdReport?.DigitalIdImage));
                        return Ok(new { Image = image, Valid = caseLocation.ClaimReport?.DigitalIdReport?.DigitalIdImageMatchConfidence ?? "00.00" });
                    }

                    if (type.ToLower() == "ocr")
                    {
                        var image = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(caseLocation.ClaimReport?.DocumentIdReport?.DocumentIdImage));
                        return Ok(new { Image = image, Valid = caseLocation.ClaimReport.DocumentIdReport?.DocumentIdImageValid.ToString() });
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

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c =>
            c.Email == email &&
            c.Active
            );

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
                var filePath = Path.Combine(webHostEnvironment.WebRootPath, Applicationsettings.NO_POLICY_IMAGE);

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
            return Unauthorized("UnAuthenticated User !!!");
        }
        [ApiExplorerSettings(IgnoreApi = true)]

        [AllowAnonymous]
        [HttpGet("agent-map")]
        public async Task<IActionResult> IndexMap(string email = "agent@verify.com")
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

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == email && c.Active);

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
                var filePath = Path.Combine(webHostEnvironment.WebRootPath, Applicationsettings.NO_POLICY_IMAGE);

                var noDocumentimage = await System.IO.File.ReadAllBytesAsync(filePath);

                filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

                var noCustomerimage = await System.IO.File.ReadAllBytesAsync(filePath);

                var claim2Agent = claimsAssigned
                    .Select(c =>
                new
                {
                    ClaimId = c.ClaimsInvestigationId,
                    Coordinate = new
                    {
                        Lat = c.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                            decimal.Parse(c.CustomerDetail.PinCode.Latitude) : decimal.Parse(c.CaseLocations.FirstOrDefault().PinCode.Latitude),
                        Lng = c.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                             decimal.Parse(c.CustomerDetail.PinCode.Longitude) : decimal.Parse(c.CaseLocations.FirstOrDefault().PinCode.Longitude)
                    },
                    Address = LocationDetail.GetAddress(c.PolicyDetail.ClaimType, c.CustomerDetail, c.CaseLocations?.FirstOrDefault()),
                    PolicyNumber = c.PolicyDetail.ContractNumber,
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

            var filePath = Path.Combine(webHostEnvironment.WebRootPath, Applicationsettings.NO_POLICY_IMAGE);

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
                        LocationImage = claimCase?.ClaimReport?.DigitalIdReport?.DigitalIdImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimCase?.ClaimReport?.DigitalIdReport?.DigitalIdImage)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                        OcrImage = claimCase?.ClaimReport?.DocumentIdReport?.DocumentIdImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimCase?.ClaimReport?.DocumentIdReport?.DocumentIdImage)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                        OcrData = claimCase?.ClaimReport?.DocumentIdReport?.DocumentIdImageData,
                        LocationLongLat = claimCase?.ClaimReport?.DigitalIdReport?.DigitalIdImageLongLat,
                        OcrLongLat = claimCase?.ClaimReport?.DocumentIdReport?.DocumentIdImageLongLat,
                    },
                    Remarks = claimCase?.ClaimReport?.AgentRemarks
                });
        }

        [AllowAnonymous]
        [HttpPost("faceid")]
        public async Task<IActionResult> FaceId(FaceData data)
        {
            try
            {

                if (data == null ||
                    string.IsNullOrWhiteSpace(data.LocationImage) ||
                    !data.LocationImage.IsBase64String() ||
                    string.IsNullOrEmpty(data.LocationLongLat))
                {
                    return BadRequest();
                }

                var response = await iCheckifyService.GetFaceId(data);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("documentid")]
        public async Task<IActionResult> DocumentId(DocumentData data)
        {
            try
            {

                if (data == null
                    || string.IsNullOrWhiteSpace(data.OcrImage)
                    || !data.OcrImage.IsBase64String()
                    || string.IsNullOrEmpty(data.OcrLongLat)
                    )
                {
                    return BadRequest();
                }

                var response = await iCheckifyService.GetDocumentId(data);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AllowAnonymous]
        [HttpPost("audio")]
        public async Task<IActionResult> Audio(AudioData data)
        {
            if (data == null)
            {
                return BadRequest();
            }

            await iCheckifyService.GetAudio(data);

            return Ok(data.Name);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AllowAnonymous]
        [HttpPost("video")]
        public async Task<IActionResult> Video(VideoData data)
        {
            return Ok(data);
            if (data == null)
            {
                return BadRequest();
            }

            await iCheckifyService.GetVideo(data);

            return Ok(data.Name);
        }

        [AllowAnonymous]
        [HttpPost("submit")]
        public async Task<IActionResult> Submit(SubmitData data)
        {
            try
            {
                if (data == null || string.IsNullOrWhiteSpace(data.Email) || string.IsNullOrWhiteSpace(data.Remarks) || string.IsNullOrWhiteSpace(data.ClaimId) || data.BeneficiaryId < 1)
                {
                    throw new ArgumentNullException("Argument(s) can't be null");
                }

                await claimsInvestigationService.SubmitToVendorSupervisor(
                    data.Email, data.BeneficiaryId,
                    data.ClaimId,
                    data.Remarks, data.Question1, data.Question2, data.Question3, data.Question4);

                await mailboxService.NotifyClaimReportSubmitToVendorSupervisor(data.Email, data.ClaimId, data.BeneficiaryId);

                return Ok(new { data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        [AllowAnonymous]
        [HttpPost("ip")]
        public async Task<IActionResult> WhitelistIP(string domain = "insurer.com", string ipaddress = "222.222.222.222", string url = "https://icheckify.azurewebsites.net/")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(ipaddress) || string.IsNullOrWhiteSpace(url))
                {
                    return BadRequest($"EMPTY INPUT(s)");

                }
                var ipSet = await httpClientService.WhitelistIP(url, domain, ipaddress);

                return Ok(ipSet);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AllowAnonymous]
        [HttpPost("setip")]
        public async Task<IActionResult> SetWhitelistIP(IPWhitelistRequest request)
        {
            try
            {

                var ipSet = await iCheckifyService.WhitelistIP(request);

                return Ok(ipSet);
            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}