using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

using Hangfire;
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

namespace risk.control.system.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private Regex regex = new Regex(@"^[\w/\:.-]+;base64,");
        private static string PanIdfyUrl = "https://pan-card-verification-at-lowest-price.p.rapidapi.com/verification/marketing/pan";
        private static string RapidAPIKey = "df0893831fmsh54225589d7b9ad1p15ac51jsnb4f768feed6f";
        private static string PanTask_id = "pan-card-verification-at-lowest-price.p.rapidapi.com";
        private readonly ApplicationDbContext _context;
        private readonly ICloneReportService cloneReportService;
        private readonly IHttpClientService httpClientService;
        private readonly IConfiguration configuration;
        private readonly IAgentIdService agentIdService;
        private readonly IVendorInvestigationService service;
        private readonly ICompareFaces compareFaces;
        private readonly UserManager<VendorApplicationUser> userVendorManager;
        private readonly IAgentService agentService;
        private readonly IFeatureManager featureManager;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly ISmsService smsService;
        private readonly IMailService mailboxService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private static string FaceMatchBaseUrl = "https://2j2sgigd3l.execute-api.ap-southeast-2.amazonaws.com/Development/icheckify";
        private static Random randomNumber = new Random();
        private string portal_base_url = string.Empty;

        //test PAN FNLPM8635N
        public AgentController(ApplicationDbContext context,
            ICloneReportService cloneReportService,
            IHttpClientService httpClientService,
            IConfiguration configuration,
            IAgentIdService agentIdService,
            IVendorInvestigationService service,
            ICompareFaces compareFaces,
            UserManager<VendorApplicationUser> userVendorManager,
             IHttpContextAccessor httpContextAccessor,
            IAgentService agentService,
            IFeatureManager featureManager,
            IBackgroundJobClient backgroundJobClient,
            ISmsService SmsService,
            IMailService mailboxService,
            IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            this.cloneReportService = cloneReportService;
            this.httpClientService = httpClientService;
            this.configuration = configuration;
            this.agentIdService = agentIdService;
            this.service = service;
            this.compareFaces = compareFaces;
            this.userVendorManager = userVendorManager;
            this.agentService = agentService;
            this.featureManager = featureManager;
            this.backgroundJobClient = backgroundJobClient;
            smsService = SmsService;
            this.mailboxService = mailboxService;
            this.webHostEnvironment = webHostEnvironment;
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

        [AllowAnonymous]
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("agent")]
        public async Task<IActionResult> GetAll(string email = "agentx@verify.com")
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
                var assignedToAgentStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;

                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == email && c.Role == AppRoles.AGENT);
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
                        DocumentPhoto = c.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(c.PolicyDetail.DocumentImage)) :
                        Applicationsettings.NO_POLICY_IMAGE,
                        CustomerName = c.CustomerDetail.Name,
                        CustomerEmail = email,
                        PolicyNumber = c.PolicyDetail.ContractNumber,
                        Gender = c.CustomerDetail.Gender.GetEnumDisplayName(),
                        c.CustomerDetail.Addressline,
                        c.CustomerDetail.PinCode.Code,
                        CustomerPhoto = c?.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(c?.CustomerDetail.ProfilePicture)) :
                        Applicationsettings.USER_PHOTO,
                        Country = c.CustomerDetail.Country.Name,
                        State = c.CustomerDetail.State.Name,
                        District = c.CustomerDetail.District.Name,
                        c.CustomerDetail.Description,
                        Locations = new
                        {
                            c.BeneficiaryDetail.BeneficiaryDetailId,
                            Photo = c.BeneficiaryDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(c.BeneficiaryDetail.ProfilePicture)) :
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
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == email && c.Role == AppRoles.AGENT);
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
                Console.WriteLine(ex.ToString());
                return StatusCode(500, ex.StackTrace);
            }
        }

        [AllowAnonymous]
        [HttpGet("get")]
        public async Task<IActionResult> Get(long caseId, string email = "agentx@verify.com")
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
                var claim = _context.Investigations
                    .Include(c => c.InvestigationReport)
                    .ThenInclude(c => c.DigitalIdReport)
                    .Include(c => c.InvestigationReport)
                    .ThenInclude(c => c.PanIdReport)
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
                    .FirstOrDefault(c => c.Id == caseId);

                var beneficiary = _context.BeneficiaryDetail
                    .Include(c => c.BeneficiaryRelation)
                    .Include(c => c.PinCode)
                    .Include(c => c.District)
                    .Include(c => c.State)
                    .Include(c => c.Country)
                    .FirstOrDefault(c => c.InvestigationTaskId == caseId);

                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == email && c.Role == AppRoles.AGENT);

                object locations = null;
                if (await featureManager.IsEnabledAsync(FeatureFlags.ENABLE_REAL_TIME_REPORT_TEMPlATE))
                {
                    locations = await cloneReportService.GetReportTemplate(caseId, agent.Email);
                }

                return Ok(
                    new
                    {
                        Policy = new
                        {
                            ClaimId = claim.Id,
                            PolicyNumber = claim.PolicyDetail.ContractNumber,
                            ClaimType = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM ? ClaimType.DEATH.GetEnumDisplayName() : ClaimType.HEALTH.GetEnumDisplayName(),
                            Document = claim.PolicyDetail.DocumentImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.PolicyDetail.DocumentImage)) :
                            Applicationsettings.NO_POLICY_IMAGE,
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
                            Applicationsettings.USER_PHOTO,
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
                            Applicationsettings.USER_PHOTO,
                            Income = claim.CustomerDetail.Income.GetEnumDisplayName(),
                            Phone = claim.CustomerDetail.ContactNumber,
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
                Console.WriteLine(ex.ToString());
                return StatusCode(500);
            }
        }
        [AllowAnonymous]
        [HttpGet("get-template")]
        public async Task<IActionResult> GetCaseReportTemplate(long caseId, string email = "agent@verify.com")
        {
            try
            {
                var agent = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == email);

                if (agent == null || agent.Role != AppRoles.AGENT || !agent.Active)
                {
                    return Unauthorized("Invalid User !!!");
                }
                var locations = await cloneReportService.GetReportTemplate(caseId, agent.Email);
                return Ok(locations);
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

            if (data == null || data.Image == null || string.IsNullOrEmpty(data.LocationLatLong))
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
            var isAgentReportName = data.ReportName == DigitalIdReportType.AGENT_FACE.GetEnumDisplayName();
            if (isAgentReportName)
            {
                var response = await agentIdService.GetAgentId(data);
                response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                return Ok(response);
            }
            else
            {
                var response = await agentIdService.GetFaceId(data);
                response.Registered = vendorUser.Active && !string.IsNullOrWhiteSpace(vendorUser.MobileUId);
                return Ok(response);
            }
        }

        [AllowAnonymous]
        [HttpPost("documentid")]
        public async Task<IActionResult> DocumentId(DocumentData data)
        {
            if (data == null || data.Image == null || string.IsNullOrEmpty(data.LocationLatLong))
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
        [HttpPost("answers")]
        public async Task<IActionResult> Answers(string locationName, long caseId, List<QuestionTemplate> Questions)
        {
            foreach (var question in Questions)
            {
                if (question.IsRequired.GetValueOrDefault() && string.IsNullOrEmpty(question.Answer))
                {
                    ModelState.AddModelError("", $"Answer required for: {question.QuestionText}");
                }
            }

            if (!ModelState.IsValid)
            {
                // Re-load data and return view with error
                // e.g. return View(model);
                return BadRequest("Some answers are missing.");
            }
            await agentIdService.Answers(locationName, caseId, Questions);

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("submit")]
        public async Task<IActionResult> Submit(SubmitData data)
        {
            try
            {
                if (data == null || string.IsNullOrWhiteSpace(data.Email) || string.IsNullOrWhiteSpace(data.Remarks) || data.CaseId < 1)
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
                var (vendor, contract) = await service.SubmitToVendorSupervisor(data.Email, data.CaseId, data.Remarks);

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimReportSubmitToVendorSupervisor(data.Email, data.CaseId, portal_base_url));

                return Ok(new { data, Registered = agent.Active && !string.IsNullOrWhiteSpace(agent.MobileUId) });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, ex.StackTrace);
            }
        }

    }
}