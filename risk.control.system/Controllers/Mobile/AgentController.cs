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
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Common;
using risk.control.system.Services.Report;

namespace risk.control.system.Controllers.Mobile
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IProcessSubmittedReportService _processSubmittedReportService;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IAgentAnswerService _answerService;
        private readonly IMediaIdfyService _mediaIdfyService;
        private readonly IDocumentIdfyService _documentIdfyService;
        private readonly IAgentFaceIdfyService _agentFaceIdfyService;
        private readonly ILogger<AgentController> _logger;
        private readonly ICloneReportService _cloneReportService;
        private readonly IFaceIdfyService _agentIdService;
        private readonly IAmazonApiService _compareFaces;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAgentService _agentService;
        private readonly IFeatureManager _featureManager;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IWebHostEnvironment _env;
        private readonly ISmsService _smsService;
        private readonly ICaseNotificationService _mailService;
        private string _portalBaseUrl = string.Empty;

        //test PAN FNLPM8635N
        public AgentController(ApplicationDbContext context,
            IProcessSubmittedReportService processSubmittedReportService,
            RoleManager<ApplicationRole> roleManager,
            IAgentAnswerService answerService,
            IMediaIdfyService mediaIdfyService,
            IDocumentIdfyService documentIdfyService,
            IAgentFaceIdfyService agentFaceIdfyService,
            ILogger<AgentController> logger,
            ICloneReportService cloneReportService,
            IConfiguration configuration,
            IFaceIdfyService agentIdService,
            IAmazonApiService compareFaces,
            UserManager<ApplicationUser> userManager,
             IHttpContextAccessor httpContextAccessor,
            IAgentService agentService,
            IFeatureManager featureManager,
            IBackgroundJobClient backgroundJobClient,
            IWebHostEnvironment webHostEnvironment,
            ISmsService SmsService,
            ICaseNotificationService mailboxService)
        {
            this._context = context;
            this._processSubmittedReportService = processSubmittedReportService;
            this._roleManager = roleManager;
            this._answerService = answerService;
            this._mediaIdfyService = mediaIdfyService;
            this._documentIdfyService = documentIdfyService;
            this._agentFaceIdfyService = agentFaceIdfyService;
            this._logger = logger;
            this._cloneReportService = cloneReportService;
            this._agentIdService = agentIdService;
            this._compareFaces = compareFaces;
            this._userManager = userManager;
            this._agentService = agentService;
            this._featureManager = featureManager;
            this._backgroundJobClient = backgroundJobClient;
            this._env = webHostEnvironment;
            _smsService = SmsService;
            this._mailService = mailboxService;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _portalBaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
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
                var user2Onboard = await _agentService.GetPin(agentEmail, _portalBaseUrl);

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
                _logger.LogError(ex, "Error occurred for {UserEmail}.", agentEmail);
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
                var user2Onboard = await _agentService.ResetUid(mobile.TrimStart('+'), _portalBaseUrl, sendSMS);

                if (user2Onboard == null)
                {
                    return BadRequest($"Agent does not exist");
                }

                return Ok(new { Email = user2Onboard.Email, Pin = user2Onboard.SecretPin });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {Mobile}.", mobile);
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
                var userWithUid = await _context.ApplicationUser.Include(u => u.Country).FirstOrDefaultAsync(v => v.MobileUId == request.Uid);
                if (!request.SendSMSForRetry)
                {
                    if (userWithUid != null)
                    {
                        return BadRequest($"UID {request.Uid} already exists.");
                    }

                    var agentRole = await _roleManager.FindByNameAsync(AppRoles.AGENT.ToString());
                    var matchingUsers = await _context.ApplicationUser.Include(u => u.Country).Where(u => (u.Country!.ISDCode + u.PhoneNumber) == normalizedMobile).ToListAsync();
                    foreach (var user in matchingUsers)
                    {
                        var isAgent = await _userManager.IsInRoleAsync(user, agentRole!.Name!);
                        if (isAgent && string.IsNullOrWhiteSpace(user.MobileUId) && user.Active)
                        {
                            user.MobileUId = request.Uid;
                            int pin = RandomNumberGenerator.GetInt32(0, 10000);
                            user.SecretPin = pin.ToString("D4");
                            _context.ApplicationUser.Update(user);
                            await _context.SaveChangesAsync();
                            await SendVerificationSmsAsync(user.Country!.Code, user.Email!, request.Mobile, user.SecretPin);
                            return Ok(new { user.Email, Pin = user.SecretPin });
                        }
                    }
                }
                else if (request.SendSMSForRetry && userWithUid != null)
                {
                    await SendVerificationSmsAsync(userWithUid.Country!.Code, userWithUid.Email!, request.Mobile, userWithUid.SecretPin!);
                    return Ok(new { userWithUid.Email, Pin = userWithUid.SecretPin });
                }

                return BadRequest("Mobile number and/or eligible agent not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {Mobile}.", request.Mobile);
                return BadRequest("An error occurred while verifying the mobile number.");
            }
        }

        private async Task SendVerificationSmsAsync(string countryCode, string email, string mobile, string pin)
        {
            string message = $"Dear {email},\n\n" +
                             $"App PIN: {pin}\n\n" +
                             $"{_portalBaseUrl}";
            await _smsService.DoSendSmsAsync(countryCode, mobile, message);
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
                var mobileUidExist = await _context.ApplicationUser.FirstOrDefaultAsync(v => v.MobileUId == request.Uid);
                if (mobileUidExist == null)
                {
                    return BadRequest($"{nameof(request.Uid)} {request.Uid} not exists");
                }
                if (mobileUidExist.ProfilePictureUrl == null || string.IsNullOrWhiteSpace(mobileUidExist.ProfilePictureUrl))
                {
                    return BadRequest($"{mobileUidExist.Email}  {nameof(mobileUidExist.ProfilePictureUrl)}  does not exists");
                }
                if (!request.VerifyId)
                {
                    return Ok(new { Email = mobileUidExist.Email, Pin = mobileUidExist.SecretPin });
                }

                var image = Convert.FromBase64String(request.Image);
                var registeredImage = await System.IO.File.ReadAllBytesAsync(Path.Combine(_env.ContentRootPath, mobileUidExist.ProfilePictureUrl));
                var matched = await _compareFaces.FaceMatch(registeredImage, image);
                if (matched.Item1)
                {
                    return Ok(new { Email = mobileUidExist.Email, Pin = mobileUidExist.SecretPin });
                }

                return BadRequest("face mismatch");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {Uid}.", request.Uid);
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
        //        var mobileUidExist = _context.ApplicationUser.FirstOrDefault(v => v.MobileUId == request.Uid);
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
                var agent = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await _featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var assignedToAgentStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;

                var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == email && c.Role == AppRoles.AGENT);
                if (vendorUser == null)
                {
                    return Unauthorized("Invalid User !!!");
                }
                var claims = await _context.Investigations
                .Include(c => c.PolicyDetail)
               .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c!.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c!.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c!.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c!.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c!.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c!.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c!.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c!.District)
                .Where(i => i.VendorId == vendorUser.VendorId &&
                i.SubStatus == assignedToAgentStatus &&
                i.TaskedAgentEmail == vendorUser.Email).ToListAsync();

                var claim2Agent = claims
                    .Select(c =>
                    new
                    {
                        claimId = c.Id,
                        Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId),
                        claimType = c.PolicyDetail!.InsuranceType == InsuranceType.CLAIM ? ClaimType.DEATH.GetEnumDisplayName() : ClaimType.HEALTH.GetEnumDisplayName(),
                        DocumentPhoto = c.PolicyDetail.DocumentPath != null ?
                        Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(_env.ContentRootPath, c.PolicyDetail.DocumentPath))) :
                        Applicationsettings.NO_POLICY_IMAGE,
                        CustomerName = c.CustomerDetail!.Name,
                        CustomerEmail = email,
                        PolicyNumber = c.PolicyDetail.ContractNumber,
                        Gender = c.CustomerDetail.Gender!.GetEnumDisplayName(),
                        c.CustomerDetail.Addressline,
                        c.CustomerDetail.PinCode!.Code,
                        CustomerPhoto = c?.CustomerDetail.ImagePath != null ?
                        Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(_env.ContentRootPath, c?.CustomerDetail.ImagePath!))) :
                        Applicationsettings.NO_USER,
                        Country = c!.CustomerDetail.Country!.Name,
                        State = c.CustomerDetail.State!.Name,
                        District = c.CustomerDetail.District!.Name,
                        Locations = new
                        {
                            c.BeneficiaryDetail!.BeneficiaryDetailId,
                            Photo = c.BeneficiaryDetail?.ImagePath != null ?
                            Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(_env.ContentRootPath, c.BeneficiaryDetail.ImagePath))) :
                            Applicationsettings.NO_USER,
                            c.BeneficiaryDetail!.Country!.Name,
                            BeneficiaryName = c.BeneficiaryDetail.Name,
                            c.BeneficiaryDetail.Addressline,
                            c.BeneficiaryDetail.PinCode!.Code,
                            District = c.BeneficiaryDetail.District!.Name,
                            State = c.BeneficiaryDetail.State!.Name
                        }
                    })?.ToList();
                return Ok(claim2Agent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {Agent}.", email);
                return StatusCode(500, $"Error occurred for {email}");
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
                var agent = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await _featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == email && c.Role == AppRoles.AGENT);
                if (vendorUser == null)
                {
                    return Unauthorized("Invalid User !!!");
                }
                var claims = await _context.Investigations
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c!.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c!.District)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c!.State)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c!.Country)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c!.Country)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c!.District)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c!.PinCode)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c!.State)
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
                        Lat = c.PolicyDetail!.InsuranceType == InsuranceType.UNDERWRITING ?
                            decimal.Parse(c.CustomerDetail!.Latitude!) : decimal.Parse(c.BeneficiaryDetail!.Latitude!),
                        Lng = c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ?
                             decimal.Parse(c.CustomerDetail!.Longitude!) : decimal.Parse(c.BeneficiaryDetail!.Longitude!)
                    },
                    Address = LocationDetail.GetAddress(c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, c.CustomerDetail!, c.BeneficiaryDetail!),
                    PolicyNumber = c.PolicyDetail.ContractNumber,
                });
                return Ok(claim2Agent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {Email}.", email);
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
                var agent = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await _featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var caseDetail = await _context.Investigations
                    .Include(c => c.PolicyDetail)
                    .ThenInclude(c => c!.CostCentre)
                    .Include(c => c.PolicyDetail)
                    .ThenInclude(c => c!.CaseEnabler)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c!.District)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c!.State)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c!.Country)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c!.PinCode)
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                var beneficiary = await _context.BeneficiaryDetail
                    .Include(c => c.BeneficiaryRelation)
                    .Include(c => c.PinCode)
                    .Include(c => c.District)
                    .Include(c => c.State)
                    .Include(c => c.Country)
                    .FirstOrDefaultAsync(c => c.InvestigationTaskId == caseId);

                var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == email && c.Role == AppRoles.AGENT);

                object locations = null!;
                if (await _featureManager.IsEnabledAsync(FeatureFlags.ENABLE_REAL_TIME_REPORT_TEMPlATE))
                {
                    locations = await _cloneReportService.GetReportTemplate(caseId, agent.Email!);
                }
                var docPath = Path.Combine(_env.ContentRootPath, caseDetail!.PolicyDetail!.DocumentPath!);
                var docByte = System.IO.File.ReadAllBytes(docPath);
                var docBase64 = Convert.ToBase64String(docByte);
                var documentPhoto = caseDetail.PolicyDetail.DocumentPath != null ? docBase64 : Applicationsettings.NO_POLICY_IMAGE;
                var customerPhoto = caseDetail.CustomerDetail!.ImagePath != null ?
                            Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(_env.ContentRootPath, caseDetail.CustomerDetail.ImagePath))) : Applicationsettings.NO_USER;
                var beneficiaryPhoto = beneficiary!.ImagePath != null ?
                            Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(_env.ContentRootPath, beneficiary.ImagePath))) : Applicationsettings.NO_USER;
                return Ok(
                    new
                    {
                        Policy = new
                        {
                            ClaimId = caseDetail.Id,
                            PolicyNumber = caseDetail.PolicyDetail.ContractNumber,
                            ClaimType = caseDetail.PolicyDetail.InsuranceType == InsuranceType.CLAIM ? ClaimType.DEATH.GetEnumDisplayName() : ClaimType.HEALTH.GetEnumDisplayName(),
                            Document = documentPhoto,
                            IssueDate = caseDetail.PolicyDetail.ContractIssueDate.ToString("dd-MMM-yyyy"),
                            IncidentDate = caseDetail.PolicyDetail.DateOfIncident.ToString("dd-MMM-yyyy"),
                            Amount = caseDetail.PolicyDetail.SumAssuredValue,
                            BudgetCentre = caseDetail.PolicyDetail.CostCentre!.Name,
                            Reason = caseDetail.PolicyDetail.CaseEnabler!.Name
                        },
                        beneficiary = new
                        {
                            BeneficiaryId = beneficiary.BeneficiaryDetailId,
                            Name = beneficiary.Name,
                            Photo = beneficiaryPhoto,
                            Relation = beneficiary.BeneficiaryRelation!.Name,
                            Income = beneficiary.Income!.GetEnumDisplayName(),
                            Phone = beneficiary.PhoneNumber,
                            DateOfBirth = beneficiary.DateOfBirth.GetValueOrDefault().ToString("dd-MMM-yyyy"),
                            Address = beneficiary.Addressline + " " + beneficiary.District!.Name + " " + beneficiary.State!.Name + " " + beneficiary.Country!.Name + " " + beneficiary.PinCode!.Code
                        },
                        Customer = new
                        {
                            Name = caseDetail.CustomerDetail.Name,
                            Occupation = caseDetail.CustomerDetail.Occupation!.GetEnumDisplayName(),
                            Photo = customerPhoto,
                            Income = caseDetail.CustomerDetail.Income!.GetEnumDisplayName(),
                            Phone = caseDetail.CustomerDetail.PhoneNumber,
                            DateOfBirth = caseDetail.CustomerDetail.DateOfBirth.GetValueOrDefault().ToString("dd-MMM-yyyy"),
                            Address = caseDetail.CustomerDetail.Addressline + " " + caseDetail.CustomerDetail.District!.Name + " " + caseDetail.CustomerDetail.State!.Name + " " + caseDetail.CustomerDetail.Country!.Name + " " + caseDetail.CustomerDetail.PinCode!.Code
                        },
                        InvestigationData = new
                        {
                            locations
                        },
                        Remarks = caseDetail?.InvestigationReport?.AgentRemarks,
                        Registered = vendorUser!.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId)
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {CaseId} for {Email}.", caseId, email);
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
                var agent = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                var locations = await _cloneReportService.GetReportTemplate(caseId, agent.Email!);
                return Ok(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {CaseId} for {Email}.", caseId, email);
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

                var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == data.Email && c.Role == AppRoles.AGENT);

                if (vendorUser == null || vendorUser.Role != AppRoles.AGENT || !vendorUser.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await _featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(vendorUser.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var isAgentReportName = data.ReportName == DigitalIdReportType.AGENT_FACE.GetEnumDisplayName();
                if (isAgentReportName)
                {
                    var response = await _agentFaceIdfyService.CaptureAgentId(data);
                    response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                    return Ok(response);
                }
                else
                {
                    var response = await _agentIdService.CaptureFaceId(data);
                    response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {Email}.", data.Email);
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

                var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == data.Email && c.Role == AppRoles.AGENT);

                if (vendorUser == null || vendorUser.Role != AppRoles.AGENT || !vendorUser.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await _featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(vendorUser.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var response = await _documentIdfyService.CaptureDocumentId(data);
                response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {Email}.", data.Email);
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

                var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == data.Email && c.Role == AppRoles.AGENT);

                if (vendorUser == null || vendorUser.Role != AppRoles.AGENT || !vendorUser.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await _featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(vendorUser.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var response = await _mediaIdfyService.CaptureMedia(data);
                response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {Email}.", data.Email);
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
                var answerSubmitted = await _answerService.CaptureAnswers(email, locationName, caseId, Questions);
                if (answerSubmitted)
                    return Ok(new { success = answerSubmitted });
                else
                    return BadRequest("Error in submitting answers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {CaseId} for {Email}.", caseId, email);
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
                var agent = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == data.Email && c.Role == AppRoles.AGENT);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                if (await _featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED))
                {
                    if (string.IsNullOrWhiteSpace(agent.MobileUId))
                    {
                        return StatusCode(401, new { message = "Offboarded Agent." });
                    }
                }
                var (vendor, contract) = await _processSubmittedReportService.SubmitToVendorSupervisor(data.Email, data.CaseId, data.Remarks);

                _backgroundJobClient.Enqueue(() => _mailService.NotifyCaseReportSubmitToVendorSupervisor(data.Email, data.CaseId, _portalBaseUrl));

                return Ok(new { data, Registered = agent.Active && !string.IsNullOrWhiteSpace(agent.MobileUId) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {Email}.", data.Email);
                return StatusCode(500);
            }
        }
    }
}