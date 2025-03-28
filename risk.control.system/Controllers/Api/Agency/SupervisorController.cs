﻿using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.Applicationsettings;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Agency
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/agency/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class SupervisorController : ControllerBase
    {
        private static CultureInfo hindi = new CultureInfo("hi-IN");
        private static NumberFormatInfo hindiNFO = (NumberFormatInfo)hindi.NumberFormat.Clone();
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<VendorApplicationUser> userManager;
        private static HttpClient httpClient = new();

        public SupervisorController(ApplicationDbContext context,
             IWebHostEnvironment webHostEnvironment,
             UserManager<VendorApplicationUser> userManager)
        {
            hindiNFO.CurrencySymbol = string.Empty;
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.userManager = userManager;
        }

        [HttpGet("GetOpen")]
        public IActionResult GetOpen()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var vendorUser = _context.VendorApplicationUser.Include(c=>c.Country).Include(u => u.Vendor).FirstOrDefault(c => c.Email == userEmail.Value);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.VendorId == vendorUser.VendorId);
            }

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var openSubstatusesForSupervisor = _context.InvestigationCaseSubStatus.Where(i =>
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
            ).Select(s => s.InvestigationCaseSubStatusId).ToList();

            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var replyStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR);
            var submittedToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            IList<ClaimsInvestigation> claimsSubmitted = null!;
            if (userRole.Value.Contains(AppRoles.AGENCY_ADMIN.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => openSubstatusesForSupervisor.Contains(a.InvestigationCaseSubStatusId) &&
                (a.InvestigationCaseSubStatus == assignedToAgentStatus) ||
                a.InvestigationCaseSubStatus == replyStatus ||
                (a.InvestigationCaseSubStatus == submittedToAssesssorStatus)
                );

                claimsSubmitted = applicationDbContext?.ToList();

            }
            else if (userRole.Value.Contains(AppRoles.SUPERVISOR.ToString()))

            {
                applicationDbContext = applicationDbContext.Where(a => openSubstatusesForSupervisor.Contains(a.InvestigationCaseSubStatusId) &&
                (a.UserEmailActioned == vendorUser.Email && a.InvestigationCaseSubStatus == replyStatus) ||
                (a.UserEmailActioned == vendorUser.Email && a.InvestigationCaseSubStatus == assignedToAgentStatus) ||
                (a.UserEmailActioned == vendorUser.Email && a.InvestigationCaseSubStatus == submittedToAssesssorStatus)
                );

                claimsSubmitted = applicationDbContext?.ToList();
            }
            var response = claimsSubmitted?
                   .Select(a => new ClaimsInvestigationResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       AssignedToAgency = a.AssignedToAgency,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                       Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ? a.UserEmailActionedTo : a.UserRoleActionedTo,
                       OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwner(a))),
                       CaseWithPerson = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ? true : false,
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Company = a.ClientCompany.Name,
                       Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = a.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                       Name = a.CustomerDetail.Name,
                       Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.LineOfBusiness.Name + "</span>"),
                       Status =a.InvestigationCaseStatus.Name,
                       ServiceType = a.PolicyDetail.LineOfBusiness.Name,
                       Service =  a.PolicyDetail.InvestigationServiceType.Name,
                       Location = a.InvestigationCaseSubStatus.Name ,
                       Created = a.Created.ToString("dd-MM-yyyy"),
                       timePending = a.GetSupervisorTimePending(false, a.InvestigationCaseSubStatus == assignedToAgentStatus, false, a.InvestigationCaseSubStatus == submittedToAssesssorStatus, a.InvestigationCaseSubStatus == replyStatus),
                       PolicyNum = a.PolicyDetail.ContractNumber,
                       BeneficiaryPhoto = a.BeneficiaryDetail.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                       TimeElapsed = DateTime.Now.Subtract(a.InvestigationCaseSubStatus == assignedToAgentStatus ?
                       a.TaskToAgentTime.Value : a.InvestigationCaseSubStatus == submittedToAssesssorStatus ? 
                       a.SubmittedToAssessorTime.Value : a.InvestigationCaseSubStatus == replyStatus ? 
                       a.EnquiryReplyByAssessorTime.Value: a.Created).TotalSeconds,
                       PersonMapAddressUrl = a.SelectedAgentDrivingMap,
                       Distance = a.SelectedAgentDrivingDistance,
                       Duration = a.SelectedAgentDrivingDuration
                   })?
                   .ToList();

            return Ok(response);
        }

        private byte[] GetOwner(ClaimsInvestigation a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var allocated2agent = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            if (!string.IsNullOrWhiteSpace(a.UserEmailActionedTo) && a.InvestigationCaseSubStatusId == allocated2agent.InvestigationCaseSubStatusId)
            {
                ownerEmail = a.UserEmailActionedTo;
                var agentProfile = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == ownerEmail)?.ProfilePicture;
                if (agentProfile == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return agentProfile;
            }
            else if (string.IsNullOrWhiteSpace(a.UserEmailActionedTo) &&
                !string.IsNullOrWhiteSpace(a.UserRoleActionedTo)
                && a.AssignedToAgency)
            {
                ownerDomain = a.UserRoleActionedTo;
                var vendorImage = _context.Vendor.FirstOrDefault(v => v.Email == ownerDomain)?.DocumentImage;
                if (vendorImage == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return vendorImage;
            }
            else
            {
                ownerDomain = a.UserRoleActionedTo;
                var companyImage = _context.ClientCompany.FirstOrDefault(v => v.Email == ownerDomain)?.DocumentImage;
                if (companyImage == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return companyImage;
            }

        }
        [HttpGet("GetNew")]
        public async Task<IActionResult> GetNew()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var requestedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = _context.VendorApplicationUser.Include(v=>v.Country).FirstOrDefault(c => c.Email == currentUserEmail);

            applicationDbContext = applicationDbContext
                    .Include(a => a.PolicyDetail)
                    .ThenInclude(a => a.LineOfBusiness)
                    .Where(i => i.VendorId == vendorUser.VendorId);
            var claims = new List<ClaimsInvestigation>();
            List<ClaimsInvestigation> newAllocateClaims = new List<ClaimsInvestigation>();
            applicationDbContext = applicationDbContext.Where(a =>
                a.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                a.InvestigationCaseSubStatusId == requestedStatus.InvestigationCaseSubStatusId);
            foreach (var claim in applicationDbContext)
            {
                claim.AllocateView += 1;
                if (claim.AllocateView <= 1)
                {
                    newAllocateClaims.Add(claim);
                }
                claims.Add(claim);
            }
            if (newAllocateClaims.Count > 0)
            {
                foreach(var claim in newAllocateClaims)
                {
                    if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH && claim.CustomerDetail != null)
                    {
                        var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={claim.CustomerDetail.Latitude}&longitude={claim.CustomerDetail.Longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
                        var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
                        string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                    $"\r\n" +
                    $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                    $"\r\n" +
                    $"\r\nElevation(sea level):{weatherData.elevation} metres";
                        claim.CustomerDetail.AddressLocationInfo = weatherCustomData;
                    }
                    else if (claim.PolicyDetail.ClaimType == ClaimType.DEATH && claim.BeneficiaryDetail != null)
                    {
                        var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={claim.BeneficiaryDetail.Latitude}&longitude={claim.BeneficiaryDetail.Longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
                        var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
                        string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                    $"\r\n" +
                    $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                    $"\r\n" +
                    $"\r\nElevation(sea level):{weatherData.elevation} metres";
                        claim.BeneficiaryDetail.AddressLocationInfo = weatherCustomData;
                    }
                }
                _context.ClaimsInvestigation.UpdateRange(newAllocateClaims);
                _context.SaveChanges();
            }
            var response = new List<ClaimsInvestigationAgencyResponse>();
            foreach(var a in claims)
            {
                var claimResponse = new ClaimsInvestigationAgencyResponse
                {
                    Id = a.ClaimsInvestigationId,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ClientCompany.DocumentImage)),
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                    AssignedToAgency = a.AssignedToAgency,
                    Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                    Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                    Name = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                    Policy = a.PolicyDetail?.LineOfBusiness.Name,
                    Status = a.InvestigationCaseStatus.Name,
                    ServiceType = a.PolicyDetail?.LineOfBusiness.Name,
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.InvestigationCaseSubStatus.Name,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = a.GetSupervisorTimePending(!a.IsQueryCase, false, false, false, a.IsQueryCase),
                    PolicyNum = a.GetPolicyNumForAgency(requestedStatus.InvestigationCaseSubStatusId),
                    BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.Value).TotalSeconds,
                    IsNewAssigned = a.AllocateView <= 1,
                    IsQueryCase = a.InvestigationCaseSubStatusId == requestedStatus.InvestigationCaseSubStatusId,
                    PersonMapAddressUrl = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap,
                    AddressLocationInfo = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? a.CustomerDetail.AddressLocationInfo : a.BeneficiaryDetail.AddressLocationInfo
                };
                response.Add(claimResponse);
            }

            return Ok(response);
        }

        [HttpGet("GetNewMap")]
        public IActionResult GetNewMap()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            //var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext
                    .Include(a => a.PolicyDetail)
                    .ThenInclude(a => a.LineOfBusiness)
                    .Where(i => i.VendorId == vendorUser.VendorId);
            }
            var claims = new List<ClaimsInvestigation>();

            if (userRole.Value.Contains(AppRoles.AGENCY_ADMIN.ToString()) || userRole.Value.Contains(AppRoles.SUPERVISOR.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a =>
                a.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId);
                foreach (var item in applicationDbContext)
                {
                    claims.Add(item);
                }
            }
            else if (userRole.Value.Contains(AppRoles.AGENT.ToString()))
            {
                foreach (var item in applicationDbContext)
                {
                    if (item.VendorId == vendorUser.VendorId && item.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId)
                    {
                        claims.Add(item);
                    }
                }
            }
            var response = claims
                     .Select(a => new MapResponse
                     {
                         Id = a.ClaimsInvestigationId,
                         Address = LocationDetail.GetAddress(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                         Description = a.PolicyDetail.CauseOfLoss,
                         Price = a.PolicyDetail.SumAssuredValue,
                         Type = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? "home" : "building",
                         Bed = a.CustomerDetail.Income.GetEnumDisplayName(),
                         Bath = long.Parse(a.CustomerDetail.ContactNumber),
                         Size = a.CustomerDetail.Description,
                         Position = new Position
                         {
                             Lat = a.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                            decimal.Parse(a.CustomerDetail.Latitude) : decimal.Parse(a.BeneficiaryDetail.Latitude),
                             Lng = a.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                             decimal.Parse(a.CustomerDetail.Longitude) : decimal.Parse(a.BeneficiaryDetail.Longitude)
                         },
                         Url = "/ClaimsVendor/Detail?Id=" + a.ClaimsInvestigationId
                     })?
                     .ToList();
            var vendor = _context.Vendor.Include(c => c.PinCode).FirstOrDefault(c => c.VendorId == vendorUser.VendorId);
            foreach (var item in response)
            {
                var isExist = response.Any(r => r.Position.Lng == item.Position.Lng && r.Position.Lat == item.Position.Lat && item.Id != r.Id);
                if (isExist)
                {
                    var (lat, lng) = LocationDetail.GetLatLng(item.Position.Lat, item.Position.Lng);
                    item.Position = new Position
                    {
                        Lat = lat,
                        Lng = lng,
                    };
                }
            }
            return Ok(new
            {
                response = response,
                lat = vendor.PinCode.Latitude,
                lng = vendor.PinCode.Longitude
            });
        }

        [HttpGet("GetReport")]
        public IActionResult GetReport()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submittedToVendorSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = _context.VendorApplicationUser.Include(v=>v.Country).Include(u => u.Vendor).FirstOrDefault(c => c.Email == currentUserEmail);
            var claims = applicationDbContext.Where(i => i.VendorId == vendorUser.VendorId &&
            i.UserEmailActionedTo == string.Empty &&
            i.UserRoleActionedTo == $"{vendorUser.Vendor.Email}" &&
            i.InvestigationCaseSubStatusId == submittedToVendorSupervisorStatus.InvestigationCaseSubStatusId);

            var claimsSubmitted = new List<ClaimsInvestigation>();
            List<ClaimsInvestigation> newVerifyClaims = new List<ClaimsInvestigation>();
            foreach (var claim in claims)
            {
                claim.VerifyView += 1;
                if (claim.VerifyView <= 1)
                {
                    newVerifyClaims.Add(claim);
                }
                claimsSubmitted.Add(claim);
            }
            if (newVerifyClaims.Count > 0)
            {
                _context.ClaimsInvestigation.UpdateRange(newVerifyClaims);
                _context.SaveChanges();
            }
            var response = claimsSubmitted
                   .Select(a => new ClaimsInvestigationAgencyResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                       AssignedToAgency = a.AssignedToAgency,
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Company = a.ClientCompany.Name,
                       OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ClientCompany.DocumentImage)),
                       Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Name = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                       Policy = a.PolicyDetail?.LineOfBusiness.Name,
                       Status = a.InvestigationCaseStatus.Name,
                       ServiceType = a.PolicyDetail?.LineOfBusiness.Name,
                       Service = a.PolicyDetail.InvestigationServiceType.Name,
                       Location = a.InvestigationCaseSubStatus.Name,
                       RawStatus = a.InvestigationCaseSubStatus.Name,
                       Created = a.Created.ToString("dd-MM-yyyy"),
                       timePending = a.GetSupervisorTimePending(false, false, true, false),
                       PolicyNum = a.PolicyDetail.ContractNumber,
                       BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                       TimeElapsed = DateTime.Now.Subtract(a.SubmittedToSupervisorTime.Value).TotalSeconds,
                       IsNewAssigned = a.VerifyView <= 1,
                       PersonMapAddressUrl = a.SelectedAgentDrivingMap,
                       Distance = a.SelectedAgentDrivingDistance,
                       Duration = a.SelectedAgentDrivingDuration
                   })
                    ?.ToList();

            return Ok(response);
        }


        [HttpGet("GetCompleted")]
        public IActionResult GetCompleted()
        {

            var finishedStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);
            var inprogressStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var rejectedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var reassignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var agencyUser = _context.VendorApplicationUser.Include(v=>v.Country).FirstOrDefault(c => c.Email == currentUserEmail);

            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var reviewCases = _context.InvestigationTransaction.Where(i => i.IsReviewCase &&
                    i.InvestigationCaseStatusId == inprogressStatus.InvestigationCaseStatusId &&
                    i.InvestigationCaseSubStatusId == reassignedStatus.InvestigationCaseSubStatusId &&
                    i.UserEmailActionedTo == string.Empty)?.Distinct();

            var claimsSubmitted = new List<ClaimsInvestigation>();
            if (agencyUser.IsVendorAdmin)
            {
                foreach (var claim in applicationDbContext)
                {
                    var reviewClaimIds = reviewCases.Select(r => r.ClaimsInvestigationId);
                    var previousReport = _context.PreviousClaimReport.Any(r => r.VendorId == claim.VendorId && claim.ClaimsInvestigationId == r.ClaimsInvestigationId);
                    if ((claim.InvestigationCaseStatusId == finishedStatus.InvestigationCaseStatusId &&
                        claim.VendorId == agencyUser.VendorId &&
                        claim.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId ||
                        claim.InvestigationCaseSubStatusId == rejectedStatus.InvestigationCaseSubStatusId) ||
                        (reviewClaimIds.Contains(claim.ClaimsInvestigationId)) && claim.ReviewCount == 1 && claim.IsReviewCase && previousReport)
                    {
                        claimsSubmitted.Add(claim);
                    }
                }
               
            }
            else
            {
                var userAttendedClaims = _context.InvestigationTransaction.Where(t => (t.UserEmailActioned == agencyUser.Email &&
                            t.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId))?.Select(c => c.ClaimsInvestigationId)?.Distinct();

                foreach (var claim in applicationDbContext)
                {
                    var previousReport = _context.PreviousClaimReport.Any(r => r.ClaimsInvestigationId == claim.ClaimsInvestigationId);

                    var isReview = reviewCases.Any(i => i.IsReviewCase &&
                    claim.ReviewCount == 1 &&
                    i.ClaimsInvestigationId == claim.ClaimsInvestigationId &&
                     i.InvestigationCaseSubStatusId == reassignedStatus.InvestigationCaseSubStatusId &&
                     i.UserEmailActionedTo == string.Empty &&
                     i.UserRoleActionedTo == $"{claim.ClientCompany.Email}");

                    if ((claim.InvestigationCaseStatus.Name == CONSTANTS.CASE_STATUS.FINISHED &&
                        claim.VendorId == agencyUser.VendorId &&
                        claim.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR) ||
                        (claim.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR) ||
                        (isReview && previousReport))
                    {
                        if (userAttendedClaims.Contains(claim.ClaimsInvestigationId))
                        {
                            claimsSubmitted.Add(claim);
                        }
                    }
                }
            }
            var response = claimsSubmitted
                      .Select(a => new ClaimsInvestigationAgencyResponse
                      {
                          Id = a.ClaimsInvestigationId,
                          PolicyId = a.PolicyDetail.ContractNumber,
                          Amount = string.Format(Extensions.GetCultureByCountry(agencyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                          AssignedToAgency = a.AssignedToAgency,
                          Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                          PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                          Company = a.ClientCompany.Name,
                          OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ClientCompany.DocumentImage)),
                          Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                          Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                          Name = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                          Policy = a.PolicyDetail?.LineOfBusiness.Name,
                          Status = a.InvestigationCaseStatus.Name,
                          ServiceType = a.PolicyDetail?.LineOfBusiness.Name,
                          Service = a.PolicyDetail.InvestigationServiceType.Name,
                          Location = a.InvestigationCaseSubStatus.Name,
                          Created = a.Created.ToString("dd-MM-yyyy"),
                          timePending = a.GetSupervisorTimePending(false, false, false, true),
                          PolicyNum = a.PolicyDetail.ContractNumber,
                          BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                      string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                     Applicationsettings.NO_USER,
                          BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                       "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                       a.BeneficiaryDetail.Name,
                          TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime.Value).TotalSeconds,
                          PersonMapAddressUrl = a.SelectedAgentDrivingMap,
                          Distance = a.SelectedAgentDrivingDistance,
                          Duration = a.SelectedAgentDrivingDuration
                      })
                       ?.ToList();

            return Ok(response);
        }

        private IQueryable<ClaimsInvestigation> GetClaims()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
               .Include(c => c.PolicyDetail)
               .Include(c => c.ClientCompany)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(b => b.BeneficiaryRelation)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CostCentre)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
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
               .Include(c => c.Vendor)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderByDescending(o => o.Created);
        }
    }
}