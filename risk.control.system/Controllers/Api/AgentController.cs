using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

using Hangfire;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

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
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AgentController> logger;
        private readonly ICloneReportService cloneReportService;
        private readonly IHttpClientService httpClientService;
        private readonly IAgentIdfyService agentIdService;
        private readonly IVendorInvestigationService service;
        private readonly ICompareFaces compareFaces;
        private readonly UserManager<VendorApplicationUser> userVendorManager;
        private readonly IAgentService agentService;
        private readonly IFeatureManager featureManager;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ISmsService smsService;
        private readonly IMailService mailboxService;
        private static Random randomNumber = new Random();
        private string portal_base_url = string.Empty;

        //test PAN FNLPM8635N
        public AgentController(ApplicationDbContext context,
            ILogger<AgentController> logger,
            ICloneReportService cloneReportService,
            IHttpClientService httpClientService,
            IConfiguration configuration,
            IAgentIdfyService agentIdService,
            IVendorInvestigationService service,
            ICompareFaces compareFaces,
            UserManager<VendorApplicationUser> userVendorManager,
             IHttpContextAccessor httpContextAccessor,
            IAgentService agentService,
            IFeatureManager featureManager,
            IBackgroundJobClient backgroundJobClient,
            IWebHostEnvironment webHostEnvironment,
            ISmsService SmsService,
            IMailService mailboxService)
        {
            this._context = context;
            this.logger = logger;
            this.cloneReportService = cloneReportService;
            this.httpClientService = httpClientService;
            this.agentIdService = agentIdService;
            this.service = service;
            this.compareFaces = compareFaces;
            this.userVendorManager = userVendorManager;
            this.agentService = agentService;
            this.featureManager = featureManager;
            this.backgroundJobClient = backgroundJobClient;
            this.webHostEnvironment = webHostEnvironment;
            smsService = SmsService;
            this.mailboxService = mailboxService;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [HttpPost("pin")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAgentPin(string agentEmail)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(agentEmail))
                {
                    return BadRequest($"Empty email");
                }
                var user2Onboard = await agentService.GetPin(agentEmail, portal_base_url);

                if (user2Onboard == null)
                {
                    return BadRequest($"Agent does not exist");
                }

                return Ok(new
                {
                    Email = user2Onboard.Email,
                    Phone = user2Onboard.PhoneNumber,
                    Pin = user2Onboard.SecretPin
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return BadRequest($"Agent does not exist or Error");
            }
        }

        [HttpPost("ResetUid")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetUid([Required] string mobile, bool sendSMS = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mobile))
                {
                    return BadRequest($"Empty mobile number");
                }
                var user2Onboard = await agentService.ResetUid(mobile.TrimStart('+'), portal_base_url, sendSMS);

                if (user2Onboard == null)
                {
                    return BadRequest($"Agent does not exist");
                }

                return Ok(new { Email = user2Onboard.Email, Pin = user2Onboard.SecretPin });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return BadRequest($"mobile number and/or Agent does not exist");
            }
        }

        [HttpPost("VerifyMobile")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyMobile(VerifyMobileRequest request)
        {
            if (request is null)
            {
                return BadRequest("Request body cannot be null or empty.");
            }
            if (string.IsNullOrWhiteSpace(request.Mobile) || request.Mobile.Length < 11 || string.IsNullOrWhiteSpace(request.Uid) || request.Uid.Length < 5)
            {
                return BadRequest("Invalid request parameters.");
            }
            try
            {
                var normalizedMobile = request.Mobile.TrimStart('+');
                var userWithUid = await _context.VendorApplicationUser.Include(u => u.Country).FirstOrDefaultAsync(v => v.MobileUId == request.Uid);
                if (!request.SendSMSForRetry)
                {
                    if (userWithUid != null)
                    {
                        return BadRequest($"UID {request.Uid} already exists.");
                    }

                    var agentRole = await _context.ApplicationRole.FirstOrDefaultAsync(r => r.Name.Contains(AppRoles.AGENT.ToString()));
                    var matchingUsers = await _context.VendorApplicationUser.Include(u => u.Country).Where(u => (u.Country.ISDCode + u.PhoneNumber) == normalizedMobile).ToListAsync();
                    foreach (var user in matchingUsers)
                    {
                        var isAgent = await userVendorManager.IsInRoleAsync(user, agentRole.Name);
                        if (isAgent && string.IsNullOrWhiteSpace(user.MobileUId) && user.Active)
                        {
                            user.MobileUId = request.Uid;
                            int pin = RandomNumberGenerator.GetInt32(0, 10000);
                            user.SecretPin = pin.ToString("D4");
                            _context.VendorApplicationUser.Update(user);
                            await _context.SaveChangesAsync();
                            await SendVerificationSmsAsync(user.Country.Code, user.Email, request.Mobile, user.SecretPin);
                            return Ok(new { user.Email, Pin = user.SecretPin });
                        }
                    }
                }
                else if (request.SendSMSForRetry && userWithUid != null)
                {
                    await SendVerificationSmsAsync(userWithUid.Country.Code, userWithUid.Email, request.Mobile, userWithUid.SecretPin);
                    return Ok(new { userWithUid.Email, Pin = userWithUid.SecretPin });
                }

                return BadRequest("Mobile number and/or eligible agent not found.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return BadRequest("An error occurred while verifying the mobile number.");
            }
        }

        private async Task SendVerificationSmsAsync(string countryCode, string email, string mobile, string pin)
        {
            string message = $"Dear {email},\n\n" +
                             $"iCheckify-App PIN: {pin}\n\n" +
                             $"{portal_base_url}";
            await smsService.DoSendSmsAsync(countryCode, mobile, message);
        }

        [HttpPost("VerifyId")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyId(VerifyIdRequest request)
        {
            try
            {
                if (request is null)
                {
                    return BadRequest("Request body cannot be null or empty.");
                }
                if (string.IsNullOrWhiteSpace(request.Uid) || string.IsNullOrWhiteSpace(request.Image))
                {
                    return BadRequest("Uid And/Or Image is empty/null");
                }
                var mobileUidExist = await _context.VendorApplicationUser.FirstOrDefaultAsync(v => v.MobileUId == request.Uid);
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

                var matched = await compareFaces.DoFaceMatch(mobileUidExist.ProfilePicture, image);
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
                logger.LogError(ex, $"Error occurred.");
                return BadRequest("face matcherror " + ex.StackTrace);
            }
        }

        //[AllowAnonymous]
        //[HttpPost("VerifyDocument")]
        //public async Task<IActionResult> VerifyDocument(VerifyDocumentRequest request)
        //{
        //    try
        //    {
        //        if (request is null || string.IsNullOrWhiteSpace(request.Uid) || string.IsNullOrWhiteSpace(request.Image))
        //        {
        //            return BadRequest();
        //        }
        //        var mobileUidExist = _context.VendorApplicationUser.FirstOrDefault(v => v.MobileUId == request.Uid);
        //        if (mobileUidExist == null)
        //        {
        //            return BadRequest($"{nameof(request.Uid)} {request.Uid} not exists");
        //        }
        //        if (!request.VerifyPan)
        //        {
        //            return Ok(new { Email = mobileUidExist.Email, Pin = mobileUidExist.SecretPin });
        //        }
        //        if (request.Type.ToUpper() != "PAN")
        //        {
        //            return BadRequest("incorrect document");
        //        }
        //        //VERIFY PAN
        //        var saveImageBase64String = Convert.ToBase64String(mobileUidExist.ProfilePicture);
        //        var maskedImage = await httpClientService.GetMaskedImage(new MaskImage { Image = request.Image }, FaceMatchBaseUrl);
        //        if (maskedImage == null || maskedImage.DocType.ToUpper() != "PAN")
        //        {
        //            return BadRequest("document issue");
        //        }

        //        var body = await httpClientService.VerifyPanNew(maskedImage.DocumentId, PanIdfyUrl, RapidAPIKey, PanTask_id);

        //        if (body != null && body.valid)
        //        {
        //            return Ok(new { Email = mobileUidExist.Email, Pin = mobileUidExist.SecretPin });
        //        }
        //        return BadRequest("document verify issue");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        return BadRequest("document verify issue");
        //    }
        //}

        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]

        [HttpGet("agent")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        public async Task<IActionResult> GetAll(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest("Email is empty/null");
                }
                var agent = await _context.VendorApplicationUser.FirstOrDefaultAsync(u => u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var assignedToAgentStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == email && c.Role == AppRoles.AGENT);
                if (vendorUser == null)
                {
                    return Unauthorized("Invalid User !!!");
                }
                var claims = await _context.Investigations
                .Include(c => c.PolicyDetail)
               .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
               .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Where(i => i.VendorId == vendorUser.VendorId &&
                i.SubStatus == assignedToAgentStatus &&
                i.TaskedAgentEmail == vendorUser.Email).ToListAsync();

                var claim2Agent = claims
                    .Select(c =>
                    new
                    {
                        claimId = c.Id,
                        Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId),
                        claimType = c.PolicyDetail.InsuranceType == InsuranceType.CLAIM ? ClaimType.DEATH.GetEnumDisplayName() : ClaimType.HEALTH.GetEnumDisplayName(),
                        DocumentPhoto = c.PolicyDetail.DocumentPath != null ?
                        Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(webHostEnvironment.ContentRootPath, c.PolicyDetail.DocumentPath))) :
                        Applicationsettings.NO_POLICY_IMAGE,
                        CustomerName = c.CustomerDetail.Name,
                        CustomerEmail = email,
                        PolicyNumber = c.PolicyDetail.ContractNumber,
                        Gender = c.CustomerDetail.Gender.GetEnumDisplayName(),
                        c.CustomerDetail.Addressline,
                        c.CustomerDetail.PinCode.Code,
                        CustomerPhoto = c?.CustomerDetail.ImagePath != null ?
                        Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(webHostEnvironment.ContentRootPath, c?.CustomerDetail.ImagePath))) :
                        Applicationsettings.USER_PHOTO,
                        Country = c.CustomerDetail.Country.Name,
                        State = c.CustomerDetail.State.Name,
                        District = c.CustomerDetail.District.Name,
                        c.CustomerDetail.Description,
                        Locations = new
                        {
                            c.BeneficiaryDetail.BeneficiaryDetailId,
                            Photo = c.BeneficiaryDetail?.ImagePath != null ?
                            Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(webHostEnvironment.ContentRootPath, c.BeneficiaryDetail.ImagePath))) :
                            Applicationsettings.USER_PHOTO,
                            c.BeneficiaryDetail.Country.Name,
                            BeneficiaryName = c.BeneficiaryDetail.Name,
                            c.BeneficiaryDetail.Addressline,
                            c.BeneficiaryDetail.PinCode.Code,
                            District = c.BeneficiaryDetail.District.Name,
                            State = c.BeneficiaryDetail.State.Name
                        }
                    })?.ToList();
                return Ok(claim2Agent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return StatusCode(500, ex.StackTrace);
            }
        }

        [HttpGet("agent-map")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        public async Task<IActionResult> IndexMap(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest("Email is empty/null");
                }
                var agent = await _context.VendorApplicationUser.FirstOrDefaultAsync(u => u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == email && c.Role == AppRoles.AGENT);
                if (vendorUser == null)
                {
                    return Unauthorized("Invalid User !!!");
                }
                var claims = await _context.Investigations
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.District)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.State)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.Country)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.Country)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.District)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.State)
                    .Where(i => i.VendorId == vendorUser.VendorId &&
                    i.TaskedAgentEmail == vendorUser.Email &&
                    i.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).ToListAsync();

                var claim2Agent = claims
                    .Select(c =>
                new
                {
                    ClaimId = c.Id,
                    Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId),
                    Coordinate = new
                    {
                        Lat = c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ?
                            decimal.Parse(c.CustomerDetail.Latitude) : decimal.Parse(c.BeneficiaryDetail.Latitude),
                        Lng = c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ?
                             decimal.Parse(c.CustomerDetail.Longitude) : decimal.Parse(c.BeneficiaryDetail.Longitude)
                    },
                    Address = LocationDetail.GetAddress(c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, c.CustomerDetail, c.BeneficiaryDetail),
                    PolicyNumber = c.PolicyDetail.ContractNumber,
                });
                return Ok(claim2Agent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return StatusCode(500);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]

        [HttpGet("get")]
        public async Task<IActionResult> Get(long caseId, string email)
        {
            try
            {
                if (caseId < 1 || string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest("Invalid caseId And/Or Email is empty/null");
                }
                var agent = await _context.VendorApplicationUser.FirstOrDefaultAsync(u => u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var claim = await _context.Investigations
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
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                var beneficiary = await _context.BeneficiaryDetail
                    .Include(c => c.BeneficiaryRelation)
                    .Include(c => c.PinCode)
                    .Include(c => c.District)
                    .Include(c => c.State)
                    .Include(c => c.Country)
                    .FirstOrDefaultAsync(c => c.InvestigationTaskId == caseId);

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == email && c.Role == AppRoles.AGENT);

                object locations = null;
                if (await featureManager.IsEnabledAsync(FeatureFlags.ENABLE_REAL_TIME_REPORT_TEMPlATE))
                {
                    locations = await cloneReportService.GetReportTemplate(caseId, agent.Email);
                }
                var docPath = Path.Combine(webHostEnvironment.ContentRootPath, claim.PolicyDetail.DocumentPath);
                var docByte = System.IO.File.ReadAllBytes(docPath);
                var docBase64 = Convert.ToBase64String(docByte);
                var documentPhoto = claim.PolicyDetail.DocumentPath != null ? docBase64 : Applicationsettings.NO_POLICY_IMAGE;
                var customerPhoto = claim.CustomerDetail.ImagePath != null ?
                            Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(webHostEnvironment.ContentRootPath, claim.CustomerDetail.ImagePath))) : Applicationsettings.USER_PHOTO;
                var beneficiaryPhoto = beneficiary.ImagePath != null ?
                            Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(webHostEnvironment.ContentRootPath, beneficiary.ImagePath))) : Applicationsettings.USER_PHOTO;
                return Ok(
                    new
                    {
                        Policy = new
                        {
                            ClaimId = claim.Id,
                            PolicyNumber = claim.PolicyDetail.ContractNumber,
                            ClaimType = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM ? ClaimType.DEATH.GetEnumDisplayName() : ClaimType.HEALTH.GetEnumDisplayName(),
                            Document = documentPhoto,
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
                            Photo = beneficiaryPhoto,
                            Relation = beneficiary.BeneficiaryRelation.Name,
                            Income = beneficiary.Income.GetEnumDisplayName(),
                            Phone = beneficiary.PhoneNumber,
                            DateOfBirth = beneficiary.DateOfBirth.GetValueOrDefault().ToString("dd-MMM-yyyy"),
                            Address = beneficiary.Addressline + " " + beneficiary.District.Name + " " + beneficiary.State.Name + " " + beneficiary.Country.Name + " " + beneficiary.PinCode.Code
                        },
                        Customer = new
                        {
                            Name = claim.CustomerDetail.Name,
                            Occupation = claim.CustomerDetail.Occupation.GetEnumDisplayName(),
                            Photo = customerPhoto,
                            Income = claim.CustomerDetail.Income.GetEnumDisplayName(),
                            Phone = claim.CustomerDetail.PhoneNumber,
                            DateOfBirth = claim.CustomerDetail.DateOfBirth.GetValueOrDefault().ToString("dd-MMM-yyyy"),
                            Address = claim.CustomerDetail.Addressline + " " + claim.CustomerDetail.District.Name + " " + claim.CustomerDetail.State.Name + " " + claim.CustomerDetail.Country.Name + " " + claim.CustomerDetail.PinCode.Code
                        },
                        InvestigationData = new
                        {
                            locations
                        },
                        Remarks = claim?.InvestigationReport?.AgentRemarks,
                        Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId)
                    });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return StatusCode(500);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]

        [HttpGet("get-template")]
        public async Task<IActionResult> GetCaseReportTemplate(long caseId, string email)
        {
            try
            {
                if (caseId < 1 || string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest("Invalid caseId And/Or Email is empty/null");
                }
                var agent = await _context.VendorApplicationUser.FirstOrDefaultAsync(u => u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                var locations = await cloneReportService.GetReportTemplate(caseId, agent.Email);
                return Ok(locations);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return StatusCode(500);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]

        [HttpPost("faceid")]
        public async Task<IActionResult> FaceId(FaceData data)
        {
            try
            {
                if (data == null)
                {
                    return BadRequest("Request body cannot be null or empty.");
                }
                if (data.Image == null || string.IsNullOrEmpty(data.LocationLatLong))
                {
                    return BadRequest("All fields (Image, LatLong) are required and must be valid.");
                }

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == data.Email && c.Role == AppRoles.AGENT);

                if (vendorUser == null || vendorUser.Role != AppRoles.AGENT || !vendorUser.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(vendorUser.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var isAgentReportName = data.ReportName == DigitalIdReportType.AGENT_FACE.GetEnumDisplayName();
                if (isAgentReportName)
                {
                    var response = await agentIdService.CaptureAgentId(data);
                    response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                    return Ok(response);
                }
                else
                {
                    var response = await agentIdService.CaptureFaceId(data);
                    response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return StatusCode(500);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]

        [HttpPost("documentid")]
        public async Task<IActionResult> DocumentId(DocumentData data)
        {
            try
            {
                if (data == null)
                {
                    return BadRequest("Request body cannot be null or empty.");
                }
                if (data.Image == null || string.IsNullOrEmpty(data.LocationLatLong))
                {
                    return BadRequest("All fields (Image, LatLong) are required and must be valid.");
                }

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == data.Email && c.Role == AppRoles.AGENT);

                if (vendorUser == null || vendorUser.Role != AppRoles.AGENT || !vendorUser.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(vendorUser.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var response = await agentIdService.CaptureDocumentId(data);
                response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return StatusCode(500);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]

        [HttpPost("media")]
        public async Task<IActionResult> Media(DocumentData data)
        {
            try
            {
                if (data == null)
                {
                    return BadRequest("Request body cannot be null or empty.");
                }
                if (data.Image == null || string.IsNullOrEmpty(data.LocationLatLong))
                {
                    return BadRequest("All fields (Image, LatLong) are required and must be valid.");
                }

                var extension = Path.GetExtension(data.Image.FileName).ToLower();

                var supportedExtensions = new[] { ".mp4", ".webm", ".mov", ".mp3", ".wav", ".aac" };
                if (!supportedExtensions.Contains(extension))
                    return BadRequest("Unsupported media format.");

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == data.Email && c.Role == AppRoles.AGENT);

                if (vendorUser == null || vendorUser.Role != AppRoles.AGENT || !vendorUser.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(vendorUser.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var response = await agentIdService.CaptureMedia(data);
                response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return StatusCode(500);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]

        [HttpPost("answers")]
        public async Task<IActionResult> Answers(string email, string LocationLatLong, string locationName, long caseId, List<QuestionTemplate> Questions)
        {
            try
            {
                foreach (var question in Questions)
                {
                    if (question.IsRequired && string.IsNullOrEmpty(question.AnswerText))
                    {
                        ModelState.AddModelError("", $"Answer required for: {question.QuestionText}");
                    }
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest("Some answers are missing.");
                }
                var answerSubmitted = await agentIdService.CaptureAnswers(locationName, caseId, Questions);
                if (answerSubmitted)
                    return Ok(new { success = answerSubmitted });
                else
                    return BadRequest("Error in submitting answers");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return StatusCode(500);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]

        [HttpPost("submit")]
        public async Task<IActionResult> Submit(SubmitData data)
        {
            try
            {
                if (data == null)
                {
                    return BadRequest("Request body cannot be null or empty.");
                }
                if (string.IsNullOrWhiteSpace(data.Email) || string.IsNullOrWhiteSpace(data.Remarks) || data.CaseId < 1)
                {
                    return BadRequest("All fields (Email, Remarks, CaseId) are required and must be valid.");
                }
                var agent = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == data.Email && c.Role == AppRoles.AGENT);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var (vendor, contract) = await service.SubmitToVendorSupervisor(data.Email, data.CaseId, data.Remarks);

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimReportSubmitToVendorSupervisor(data.Email, data.CaseId, portal_base_url));

                return Ok(new { data, Registered = agent.Active && !string.IsNullOrWhiteSpace(agent.MobileUId) });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return StatusCode(500);
            }
        }
    }
}