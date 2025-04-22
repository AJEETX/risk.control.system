using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

using Hangfire;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private const string CLAIM = "claims";
        private const string UNDERWRITING = "underwriting";
        private Regex regex = new Regex(@"^[\w/\:.-]+;base64,");
        private static string PanIdfyUrl = "https://pan-card-verification-at-lowest-price.p.rapidapi.com/verification/marketing/pan";
        private static string RapidAPIKey = "df0893831fmsh54225589d7b9ad1p15ac51jsnb4f768feed6f";
        private static string PanTask_id = "pan-card-verification-at-lowest-price.p.rapidapi.com";
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientService httpClientService;
        private readonly IConfiguration configuration;
        private readonly IAgentIdService agentIdService;
        private readonly ICompareFaces compareFaces;
        private readonly UserManager<VendorApplicationUser> userVendorManager;
        private readonly IAgentService agentService;
        private readonly IFeatureManager featureManager;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly ISmsService smsService;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IICheckifyService iCheckifyService;
        private static string FaceMatchBaseUrl = "https://2j2sgigd3l.execute-api.ap-southeast-2.amazonaws.com/Development/icheckify";
        private static Random randomNumber = new Random();
        private string portal_base_url = string.Empty;

        //test PAN FNLPM8635N
        public AgentController(ApplicationDbContext context, IHttpClientService httpClientService,
            IConfiguration configuration,
            IAgentIdService agentIdService,
            ICompareFaces compareFaces,
            UserManager<VendorApplicationUser> userVendorManager,
             IHttpContextAccessor httpContextAccessor,
            IAgentService agentService,
            IFeatureManager featureManager,
            IBackgroundJobClient backgroundJobClient,
            ISmsService SmsService,
            IClaimsInvestigationService claimsInvestigationService, IMailboxService mailboxService,
            IWebHostEnvironment webHostEnvironment, IICheckifyService iCheckifyService)
        {
            this._context = context;
            this.httpClientService = httpClientService;
            this.configuration = configuration;
            this.agentIdService = agentIdService;
            this.compareFaces = compareFaces;
            this.userVendorManager = userVendorManager;
            this.agentService = agentService;
            this.featureManager = featureManager;
            this.backgroundJobClient = backgroundJobClient;
            smsService = SmsService;
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
                var user2Onboard = await agentService.ResetUid(mobile.TrimStart('+'), sendSMS);

                if (user2Onboard == null)
                {
                    return BadRequest($"Agent does not exist");
                }

                return Ok(new { Email = user2Onboard.Email, Pin = user2Onboard.SecretPin });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
                    return BadRequest($"{nameof(request)} invalid");
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
                var user2Onboards = _context.VendorApplicationUser.Include(u => u.Country).Where(
                    u => u.Country.ISDCode + u.PhoneNumber == request.Mobile.TrimStart('+'));
                foreach (var user2Onboard in user2Onboards)
                {
                    var isAgent = await userVendorManager.IsInRoleAsync(user2Onboard, agentRole?.Name);
                    if (isAgent && string.IsNullOrWhiteSpace(user2Onboard.MobileUId) && user2Onboard.Active)
                    {
                        user2Onboard.MobileUId = request.Uid;
                        user2Onboard.SecretPin = randomNumber.Next(1000, 9999).ToString();
                        _context.VendorApplicationUser.Update(user2Onboard);
                        await _context.SaveChangesAsync();
                        if (request.SendSMS)
                        {
                            //SEND SMS
                            string message = $"Dear {user2Onboard.Email}, ";
                            message += $"                                ";
                            message += $"icheckifyApp Pin:{user2Onboard.SecretPin}";
                            message += $"                                      ";
                            message += $"Thanks                           ";
                            message += $"                                ";
                            message += $"https://icheckify.co.in";
                            await smsService.DoSendSmsAsync(request.Mobile, message);
                        }

                        return Ok(new { Email = user2Onboard.Email, Pin = user2Onboard.SecretPin });
                    }
                }
                return BadRequest($"Err");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
                if (mobileUidExist.ProfilePicture == null)
                {
                    return BadRequest($"{mobileUidExist.Email}  {nameof(mobileUidExist.ProfilePicture)}  does not exists");
                }
                if (!request.VerifyId)
                {
                    return Ok(new { Email = mobileUidExist.Email, Pin = mobileUidExist.SecretPin });
                }

                var image = Convert.FromBase64String(request.Image);

                //var savedImage = ImageCompression.ConverterSkia(image);
                //string path = Path.Combine(webHostEnvironment.WebRootPath, "onboard");
                //if (!Directory.Exists(path))
                //{
                //    Directory.CreateDirectory(path);
                //}

                //using MemoryStream stream = new MemoryStream(image);

                //var filePath = Path.Combine(path, $"face{DateTime.Now.ToString("dd-MMM-yyyy-HH-mm-ss")}.jpg");
                //CompressImage.CompressimageWindows(stream, filePath);

                //var savedImage = await System.IO.File.ReadAllBytesAsync(filePath);

                //var saveImageBase64Image2Verify = Convert.ToBase64String(savedImage);

                //var saveImageBase64String = Convert.ToBase64String(mobileUidExist.ProfilePicture);

                var matched = await compareFaces.Do(mobileUidExist.ProfilePicture, image);
                if (matched.Item1)
                {
                    return Ok(new { Email = mobileUidExist.Email, Pin = mobileUidExist.SecretPin });
                }

                return BadRequest("face mismatch");

                //var faceImageDetail = await httpClientService.GetFaceMatch(new MatchImage { Source = saveImageBase64String, Dest = saveImageBase64Image2Verify }, FaceMatchBaseUrl);

                //if (faceImageDetail == null || faceImageDetail?.Confidence == null)
                //{
                //    return BadRequest("face mismatch");
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest("face matcherror " + ex.StackTrace);
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

                var body = await httpClientService.VerifyPanNew(maskedImage.DocumentId, PanIdfyUrl, RapidAPIKey, PanTask_id);

                if (body != null && body.valid)
                {
                    return Ok(new { Email = mobileUidExist.Email, Pin = mobileUidExist.SecretPin });
                }
                return BadRequest("document verify issue");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest("document verify issue");
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AllowAnonymous]
        [HttpGet("GetImage")]
        public IActionResult GetImage(string claimId, string type)
        {
            try
            {
                var claim = _context.ClaimsInvestigation
                 .Include(c => c.AgencyReport)
                 .Include(c => c.AgencyReport.DigitalIdReport)
                 .Include(c => c.AgencyReport.PanIdReport)
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
                if (claim != null)
                {
                    if (type.ToLower() == "face")
                    {
                        var image = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.AgencyReport?.DigitalIdReport?.DigitalIdImage));
                        return Ok(new { Image = image, Valid = claim.AgencyReport?.DigitalIdReport?.DigitalIdImageMatchConfidence ?? "00.00" });
                    }

                    if (type.ToLower() == "ocr")
                    {
                        var image = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.AgencyReport?.PanIdReport?.DocumentIdImage));
                        return Ok(new { Image = image, Valid = claim.AgencyReport.PanIdReport?.DocumentIdImageValid.ToString() });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest();
            }

            return Ok();
        }

        [AllowAnonymous]
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("agent")]
        public async Task<IActionResult> GetAll(string email = "agentx@verify.com")
        {
            try
            {

                var agent = _context.VendorApplicationUser.FirstOrDefault(u=>u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (!string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
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

                var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                            i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == email && c.Role == AppRoles.AGENT);
                if (vendorUser == null)
                {
                    return Unauthorized("Invalid User !!!");
                }
                applicationDbContext = applicationDbContext.Where(i => i.VendorId == vendorUser.VendorId);
                var claimsAssigned = new List<ClaimsInvestigation>();

                foreach (var item in applicationDbContext)
                {
                    if (item.VendorId == vendorUser.VendorId
                        && item.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId)
                    {
                        claimsAssigned.Add(item);
                    }
                }
                var filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-policy.jpg");

                var noDocumentimage = await System.IO.File.ReadAllBytesAsync(filePath);

                filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

                var noCustomerimage = await System.IO.File.ReadAllBytesAsync(filePath);
                var claimLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIM).LineOfBusinessId;

                var claim2Agent = claimsAssigned
                    .Select(c =>
                new
                {
                    claimId = c.ClaimsInvestigationId,
                    Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId),
                    claimType = c.PolicyDetail.InsuranceType == InsuranceType.CLAIM ? ClaimType.DEATH : ClaimType.HEALTH,
                    DocumentPhoto = c.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(c.PolicyDetail.DocumentImage)) :
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDocumentimage)),
                    CustomerName = c.CustomerDetail.Name,
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
                    Locations = new
                    {
                        c.BeneficiaryDetail.BeneficiaryDetailId,
                        Photo = c.BeneficiaryDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(c.BeneficiaryDetail.ProfilePicture)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noCustomerimage)),
                        c.BeneficiaryDetail.Country.Name,
                        BeneficiaryName = c.BeneficiaryDetail.Name,
                        c.BeneficiaryDetail.Addressline,
                        c.BeneficiaryDetail.PinCode.Code,
                        District = c.BeneficiaryDetail.District.Name,
                        State = c.BeneficiaryDetail.State.Name
                    }
                });
                return Ok(claim2Agent);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, ex.StackTrace);
            }
        }

        [AllowAnonymous]
        [HttpGet("agent-map")]
        public async Task<IActionResult> IndexMap(string email = "agentx@verify.com")
        {
            try
            {
                var agent = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (!string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.ClientCompany)
                    .Include(c => c.PolicyDetail)
                    .ThenInclude(c => c.CaseEnabler)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.BeneficiaryRelation)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.District)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.State)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.Country)
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

                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == email && c.Role == AppRoles.AGENT);
                if (vendorUser == null)
                {
                    return Unauthorized("Invalid User !!!");
                }
                applicationDbContext = applicationDbContext.Where(i => i.VendorId == vendorUser.VendorId);
                var claimsAssigned = new List<ClaimsInvestigation>();

                foreach (var item in applicationDbContext)
                {
                    if (item.VendorId == vendorUser.VendorId
                        && item.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId)
                    {
                        claimsAssigned.Add(item);
                    }
                }
                var filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-policy.jpg");

                var noDocumentimage = await System.IO.File.ReadAllBytesAsync(filePath);

                filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

                var noCustomerimage = await System.IO.File.ReadAllBytesAsync(filePath);
                var claimLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIM).LineOfBusinessId;
                var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

                var claim2Agent = claimsAssigned
                    .Select(c =>
                new
                {
                    ClaimId = c.ClaimsInvestigationId,
                    Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId),
                    Coordinate = new
                    {
                        Lat = c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ?
                            decimal.Parse(c.CustomerDetail.Latitude) : decimal.Parse(c.BeneficiaryDetail.Latitude),
                        Lng = c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ?
                             decimal.Parse(c.CustomerDetail.Longitude) : decimal.Parse(c.BeneficiaryDetail.Longitude)
                    },
                    Address = LocationDetail.GetAddress(c.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, c.CustomerDetail, c.BeneficiaryDetail),
                    PolicyNumber = c.PolicyDetail.ContractNumber,
                });
                return Ok(claim2Agent);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, ex.StackTrace);
            }
        }

        [AllowAnonymous]
        [HttpGet("get")]
        public async Task<IActionResult> Get(string claimId, string email = "agentx@verify.com")
        {
            try
            {
                var agent = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (!string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var claim = _context.ClaimsInvestigation
                    .Include(c => c.AgencyReport)
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
                var beneficiary = _context.BeneficiaryDetail
                    .Include(c => c.BeneficiaryRelation)
                    .Include(c => c.PinCode)
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
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == email && c.Role == AppRoles.AGENT);

                return Ok(
                    new
                    {
                        Policy = new
                        {
                            ClaimId = claim.ClaimsInvestigationId,
                            PolicyNumber = claim.PolicyDetail.ContractNumber,
                            ClaimType = claim.PolicyDetail.InsuranceType.GetEnumDisplayName(),
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
                            BeneficiaryId = beneficiary.BeneficiaryDetailId,
                            Name = beneficiary.Name,
                            Photo = beneficiary.ProfilePicture != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(beneficiary.ProfilePicture)) :
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noCustomerimage)),
                            Relation = beneficiary.BeneficiaryRelation.Name,
                            Income = beneficiary.Income.GetEnumDisplayName(),
                            Phone = beneficiary.ContactNumber,
                            DateOfBirth = beneficiary.DateOfBirth.GetValueOrDefault().ToString("dd-MMM-yyyy"),
                            Address = beneficiary.Addressline + " " + beneficiary.District.Name + " " + beneficiary.State.Name + " " + beneficiary.Country.Name + " " + beneficiary.PinCode.Code
                        },
                        Customer = new
                        {
                            Name = claim.CustomerDetail.Name,
                            Occupation = claim.CustomerDetail.Occupation.GetEnumDisplayName(),
                            Photo = claim.CustomerDetail.ProfilePicture != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.CustomerDetail.ProfilePicture)) :
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noCustomerimage)),
                            Income = claim.CustomerDetail.Income.GetEnumDisplayName(),
                            Phone = claim.CustomerDetail.ContactNumber,
                            DateOfBirth = claim.CustomerDetail.DateOfBirth.GetValueOrDefault().ToString("dd-MMM-yyyy"),
                            Address = claim.CustomerDetail.Addressline + " " + claim.CustomerDetail.District.Name + " " + claim.CustomerDetail.State.Name + " " + claim.CustomerDetail.Country.Name + " " + claim.CustomerDetail.PinCode.Code
                        },
                        InvestigationData = new
                        {
                            LocationImage = claim?.AgencyReport?.DigitalIdReport?.DigitalIdImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim?.AgencyReport?.DigitalIdReport?.DigitalIdImage)) :
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                            OcrImage = claim?.AgencyReport?.PanIdReport?.DocumentIdImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim?.AgencyReport?.PanIdReport?.DocumentIdImage)) :
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                            OcrData = claim?.AgencyReport?.PanIdReport?.DocumentIdImageData,
                            LocationLongLat = claim?.AgencyReport?.DigitalIdReport?.DigitalIdImageLongLat,
                            OcrLongLat = claim?.AgencyReport?.PanIdReport?.DocumentIdImageLongLat,
                        },
                        Remarks = claim?.AgencyReport?.AgentRemarks,
                        Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId)
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500);
            }
        }

        
        [AllowAnonymous]
        [HttpPost("faceid")]
        public async Task<IActionResult> FaceId(FaceData data)
        {

            if (data == null ||
                     string.IsNullOrWhiteSpace(data.LocationImage) ||
                     !data.LocationImage.IsBase64String() ||
                     string.IsNullOrEmpty(data.LocationLongLat))
            {
                return BadRequest();
            }
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == data.Email && c.Role == AppRoles.AGENT);

            if (vendorUser == null || vendorUser.Role != AppRoles.AGENT || !vendorUser.Active)
            {
                return Unauthorized("Invalid User !!!");
            }
            if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
            {
                if (!string.IsNullOrWhiteSpace(vendorUser.MobileUId))
                {
                    return StatusCode(401, new { message = "Offboarded Agent." });
                }
            }


            if(data.Type == "0")
            {
                var response = await agentIdService.GetAgentId(data);
                response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                return Ok(response);
            }
            else if (data.Type == "1")
            {
                var response = await agentIdService.GetFaceId(data);
                response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                return Ok(response);
            }
            return BadRequest();
        }

        [AllowAnonymous]
        [HttpPost("documentid")]
        public async Task<IActionResult> DocumentId(DocumentData data)
        {
            if (data == null
                    || string.IsNullOrWhiteSpace(data.OcrImage)
                    || !data.OcrImage.IsBase64String()
                    || string.IsNullOrEmpty(data.OcrLongLat)
                    )
            {
                return BadRequest();
            }
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == data.Email && c.Role == AppRoles.AGENT);

            if (vendorUser == null || vendorUser.Role != AppRoles.AGENT || !vendorUser.Active)
            {
                return Unauthorized("Invalid User !!!");
            }
            if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
            {
                if (!string.IsNullOrWhiteSpace(vendorUser.MobileUId))
                {
                    return StatusCode(401, new { message = "Offboarded Agent." });
                }
            }
            var response = await agentIdService.GetDocumentId(data);
            response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("audio")]
        public async Task<IActionResult> Audio(AudioData data)
        {
            if (data == null)
            {
                return BadRequest();
            }
            if (!string.IsNullOrWhiteSpace(Path.GetFileName(data.MediaFile.Name)))
            {
                data.Name = Path.GetFileName(data.MediaFile.Name);
                using (var ds = new MemoryStream())
                {
                    data.MediaFile.CopyTo(ds);
                    data.Mediabytes = ds.ToArray();
                };
            }

            var response = await iCheckifyService.GetAudio(data);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == data.Email && c.Role == AppRoles.AGENT);
            response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("video")]
        public async Task<IActionResult> Video(VideoData data)
        {
            if (data == null)
            {
                return BadRequest();
            }

            var response = await iCheckifyService.GetVideo(data);

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == data.Email && c.Role == AppRoles.AGENT);
            response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
            return Ok(response);
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
                var agent = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == data.Email && c.Role == AppRoles.AGENT);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (!string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var (vendor, contract) = await claimsInvestigationService.SubmitToVendorSupervisor(
                    data.Email,
                    data.ClaimId,
                    data.Remarks, data.Question1, data.Question2, data.Question3, data.Question4);

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimReportSubmitToVendorSupervisor(data.Email, data.ClaimId, portal_base_url));

                return Ok(new { data, Registered = agent.Active && !string.IsNullOrWhiteSpace(agent.MobileUId) });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, ex.StackTrace);
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, ex.StackTrace);
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}