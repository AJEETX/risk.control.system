using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using System.Globalization;
using System.Security.Claims;

using static risk.control.system.AppConstant.Applicationsettings;
using static risk.control.system.Helpers.Permissions;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Claims
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR,AGENT")]
    public class ClaimsVendorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDashboardService dashboardService;
        private readonly UserManager<VendorApplicationUser> userManager;

        public ClaimsVendorController(ApplicationDbContext context, IDashboardService dashboardService, UserManager<VendorApplicationUser> userManager)
        {
            _context = context;
            this.dashboardService = dashboardService;
            this.userManager = userManager;
        }

        [HttpGet("GetOpen")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> GetOpen()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var vendorUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(c => c.Email == userEmail.Value);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.VendorId == vendorUser.VendorId);
            }

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var openSubstatusesForSupervisor = _context.InvestigationCaseSubStatus.Where(i =>
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
            ).Select(s => s.InvestigationCaseSubStatusId).ToList();

            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submittedToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            if (userRole.Value.Contains(AppRoles.AGENCY_ADMIN.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => openSubstatusesForSupervisor.Contains(a.InvestigationCaseSubStatusId) &&
                ( a.InvestigationCaseSubStatus == assignedToAgentStatus) ||
                ( a.InvestigationCaseSubStatus == submittedToAssesssorStatus)
                );

                var response = applicationDbContext
                   .Select(a => new ClaimsInvesgationResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       AssignedToAgency = a.AssignedToAgency,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                       Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Company = a.PolicyDetail.ClientCompany.Name,
                       Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = a.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                       Name = a.CustomerDetail.CustomerName,
                       Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.LineOfBusiness.Name + "</span>"),
                       Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                       ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.ClaimType.GetEnumDisplayName() + "</span>"),
                       Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                       Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                       Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                       timePending = a.GetTimePending(),
                       PolicyNum = a.PolicyDetail.ContractNumber,
                       BeneficiaryPhoto = a.BeneficiaryDetail.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                       TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
                   })?
                   .ToList();

                return Ok(response);
            }
            else if(userRole.Value.Contains(AppRoles.SUPERVISOR.ToString()))
            
                    {
                applicationDbContext = applicationDbContext.Where(a => openSubstatusesForSupervisor.Contains(a.InvestigationCaseSubStatusId) &&
                (a.UserEmailActioned == vendorUser.Email && a.InvestigationCaseSubStatus == assignedToAgentStatus) ||
                (a.UserEmailActioned == vendorUser.Email && a.InvestigationCaseSubStatus == submittedToAssesssorStatus)
                );

                var response = applicationDbContext
                   .Select(a => new ClaimsInvesgationResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       AssignedToAgency = a.AssignedToAgency,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                       Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Company = a.PolicyDetail.ClientCompany.Name,
                       Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = a.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                       Name = a.CustomerDetail.CustomerName,
                       Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.LineOfBusiness.Name + "</span>"),
                       Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                       ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.ClaimType.GetEnumDisplayName() + "</span>"),
                       Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                       Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                       Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                       timePending = a.GetTimePending(),
                       PolicyNum = a.PolicyDetail.ContractNumber,
                       BeneficiaryPhoto = a.BeneficiaryDetail.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                       TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
                   })?
                   .ToList();

                return Ok(response);
            }
            return Ok(null);
        }

        [HttpGet("GetOpenMap")]
        public async Task<IActionResult> GetOpenMap()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.VendorId == vendorUser.VendorId);
            }

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var openSubstatusesForSupervisor = _context.InvestigationCaseSubStatus.Where(i =>
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR)
            ).Select(s => s.InvestigationCaseSubStatusId).ToList();

            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submittedToVendorSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            if (userRole.Value.Contains(AppRoles.AGENCY_ADMIN.ToString()) || userRole.Value.Contains(AppRoles.SUPERVISOR.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => openSubstatusesForSupervisor.Contains(a.InvestigationCaseSubStatusId));

                var claimsAllocated = new List<ClaimsInvestigation>();

                foreach (var item in applicationDbContext)
                {
                    
                    if (item.VendorId.HasValue
                        && item.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId
                        || item.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId
                        || item.InvestigationCaseSubStatusId == submittedToVendorSupervisorStatus.InvestigationCaseSubStatusId)
                    {
                        claimsAllocated.Add(item);
                    }
                }
                var response = claimsAllocated
                   .Select(a => new MapResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       Address = LocationDetail.GetAddress(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Description = a.PolicyDetail.CauseOfLoss,
                       Price = a.PolicyDetail.SumAssuredValue,
                       Type = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? "home" : "building",
                       Bed = a.CustomerDetail.CustomerIncome.GetEnumDisplayName(),
                       Bath = a.CustomerDetail.ContactNumber,
                       Size = a.CustomerDetail.Description,
                       Position = new Position
                       {
                           Lat = a.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                            decimal.Parse(a.CustomerDetail.PinCode.Latitude) : decimal.Parse(a.BeneficiaryDetail.PinCode.Latitude),
                           Lng = a.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                             decimal.Parse(a.CustomerDetail.PinCode.Longitude) : decimal.Parse(a.BeneficiaryDetail.PinCode.Longitude)
                       },
                       Url = "/ClaimsVendor/Detail?Id=" + a.ClaimsInvestigationId
                   })?
                   .ToList();

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
                var vendor = _context.Vendor.Include(c => c.PinCode).FirstOrDefault(c => c.VendorId == vendorUser.VendorId);

                return Ok(new
                {
                    response = response,
                    lat = vendor.PinCode.Latitude,
                    lng = vendor.PinCode.Longitude
                });
            }
            return Ok(null);
        }

        [HttpGet("GetNew")]
        public async Task<IActionResult> GetNew()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);

            applicationDbContext = applicationDbContext
                    .Include(a => a.PolicyDetail)
                    .ThenInclude(a => a.LineOfBusiness)
                    .Where(i => i.VendorId == vendorUser.VendorId);
            var claims = new List<ClaimsInvestigation>();
            List<ClaimsInvestigation> newAllocateClaims = new List<ClaimsInvestigation>();
            var userAdminOrSuperVisor = userRole.Value.Contains(AppRoles.AGENCY_ADMIN.ToString()) || userRole.Value.Contains(AppRoles.SUPERVISOR.ToString());
            if (userAdminOrSuperVisor)
            {
                applicationDbContext = applicationDbContext.Where(a =>
                a.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId);
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
                    _context.ClaimsInvestigation.UpdateRange(newAllocateClaims);
                    _context.SaveChanges();
                }
            }
            else if (userRole.Value.Contains(AppRoles.AGENT.ToString()))
            {
                List<ClaimsInvestigation> newInvestigateClaims = new List<ClaimsInvestigation>();
                applicationDbContext = applicationDbContext.Where(a => a.UserEmailActionedTo == currentUserEmail
                && a.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId);
                foreach (var claim in applicationDbContext)
                {
                    claim.InvestigateView += 1;
                    if (claim.InvestigateView <= 1)
                    {
                        newInvestigateClaims.Add(claim);
                    }
                    claims.Add(claim);
                }
                if (newInvestigateClaims.Count > 0)
                {
                    _context.ClaimsInvestigation.UpdateRange(newInvestigateClaims);
                    _context.SaveChanges();
                }
            }


            var response = claims
                   .Select(a => new ClaimsInvesgationResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                       Company = a.PolicyDetail.ClientCompany.Name,
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       AssignedToAgency = a.AssignedToAgency,
                       Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = a.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                       Name = a.CustomerDetail.CustomerName,
                       Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                       Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                       ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                       Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                       Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                       Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                       timePending = a.GetTimePending(),
                       PolicyNum = a.PolicyDetail.ContractNumber,
                       BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                       TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                       IsNewAssigned = userAdminOrSuperVisor ? a.AllocateView <= 1 : a.InvestigateView <= 1,
                   })
                    ?.ToList();

            return Ok(response);
        }

        [HttpGet("GetNewMap")]
        public async Task<IActionResult> GetNewMap()
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
                    if(item.VendorId == vendorUser.VendorId && item.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId)
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
                         Bed = a.CustomerDetail.CustomerIncome.GetEnumDisplayName(),
                         Bath = a.CustomerDetail.ContactNumber,
                         Size = a.CustomerDetail.Description,
                         Position = new Position
                         {
                             Lat = a.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                            decimal.Parse(a.CustomerDetail.PinCode.Latitude) : decimal.Parse(a.BeneficiaryDetail.PinCode.Latitude),
                             Lng = a.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                             decimal.Parse(a.CustomerDetail.PinCode.Longitude) : decimal.Parse(a.BeneficiaryDetail.PinCode.Longitude)
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
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> GetReport()
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

            var vendorUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(c => c.Email == currentUserEmail);
            var claims = applicationDbContext.Where(i => i.VendorId == vendorUser.VendorId &&
            i.UserEmailActionedTo == string.Empty &&
            i.UserRoleActionedTo == $"{AppRoles.SUPERVISOR.GetEnumDisplayName()} ({vendorUser.Vendor.Email})" &&
            i.InvestigationCaseSubStatusId == submittedToVendorSupervisorStatus.InvestigationCaseSubStatusId)?.ToList();

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
                   .Select(a => new ClaimsInvesgationResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                       AssignedToAgency = a.AssignedToAgency,
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Company = a.PolicyDetail.ClientCompany.Name,
                       Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = a.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                       Name = a.CustomerDetail.CustomerName,
                       Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                       Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                       ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                       Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                       Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                       Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                       timePending = a.GetTimePending(),
                       PolicyNum = a.PolicyDetail.ContractNumber,
                       BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                       TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                       IsNewAssigned = a.VerifyView <= 1
                   })
                    ?.ToList();

            return Ok(response);
        }

        [HttpGet("GetReportMap")]
        public async Task<IActionResult> GetReportMap()
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

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.VendorId == vendorUser.VendorId);
            }
            // SHOWING DIFFERRENT PAGES AS PER ROLES
            var claimsSubmitted = new List<ClaimsInvestigation>();
            if (userRole.Value.Contains(AppRoles.AGENCY_ADMIN.ToString()) || userRole.Value.Contains(AppRoles.SUPERVISOR.ToString()))
            {
                foreach (var item in applicationDbContext)
                {
                    
                    if (item.VendorId == vendorUser.VendorId
                        && item.InvestigationCaseSubStatusId == submittedToVendorSupervisorStatus.InvestigationCaseSubStatusId)
                    {
                        claimsSubmitted.Add(item);
                    }
                }
            }
            var response = claimsSubmitted
                   .Select(a => new MapResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       Address = LocationDetail.GetAddress(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Description = a.PolicyDetail.CauseOfLoss,
                       Price = a.PolicyDetail.SumAssuredValue,
                       Type = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? "home" : "building",
                       Bed = a.CustomerDetail.CustomerIncome.GetEnumDisplayName(),
                       Bath = a.CustomerDetail.ContactNumber,
                       Size = a.CustomerDetail.Description,
                       Position = new Position
                       {
                           Lat = a.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                            decimal.Parse(a.CustomerDetail.PinCode.Latitude) : decimal.Parse(a.BeneficiaryDetail.PinCode.Latitude),
                           Lng = a.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                             decimal.Parse(a.CustomerDetail.PinCode.Longitude) : decimal.Parse(a.BeneficiaryDetail.PinCode.Longitude)
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

        [HttpGet("GetCompleted")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> GetCompleted()
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

            var agencyUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);

            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var reviewCases = _context.InvestigationTransaction.Where(i => i.IsReviewCase &&
                    i.InvestigationCaseStatusId == inprogressStatus.InvestigationCaseStatusId &&
                    i.InvestigationCaseSubStatusId == reassignedStatus.InvestigationCaseSubStatusId &&
                    i.UserEmailActionedTo == string.Empty);

            if (agencyUser.IsVendorAdmin)
            {
                var claimsSubmitted = new List<ClaimsInvestigation>();
                foreach (var claim in applicationDbContext)
                {
                    var reviewClaimIds = reviewCases.Select(r => r.ClaimsInvestigationId);
                    var previousReport = _context.PreviousClaimReport.Any(r => r.VendorId == claim.VendorId && claim.ClaimsInvestigationId == r.ClaimsInvestigationId);
                    if ((claim.InvestigationCaseStatusId== finishedStatus.InvestigationCaseStatusId &&
                        claim.VendorId == agencyUser.VendorId && 
                        claim.InvestigationCaseSubStatusId== approvedStatus.InvestigationCaseSubStatusId ||
                        claim.InvestigationCaseSubStatusId == rejectedStatus.InvestigationCaseSubStatusId) ||
                        (reviewClaimIds.Contains(claim.ClaimsInvestigationId)) && claim.ReviewCount == 1 && claim.IsReviewCase && previousReport)
                    {
                        claimsSubmitted.Add(claim);
                    }
                }
                var response = claimsSubmitted
                       .Select(a => new ClaimsInvesgationResponse
                       {
                           Id = a.ClaimsInvestigationId,
                           PolicyId = a.PolicyDetail.ContractNumber,
                           Amount = string.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                           AssignedToAgency = a.AssignedToAgency,
                           Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                           PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                           Company = a.PolicyDetail.ClientCompany.Name,
                           Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                           Customer = a.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                           Name = a.CustomerDetail.CustomerName,
                           Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                           Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                           ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                           Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                           Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                           Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                           timePending = a.GetTimePending(),
                           PolicyNum = a.PolicyDetail.ContractNumber,
                           BeneficiaryPhoto =  a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                           BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                           TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
                       })
                        ?.ToList();

                return Ok(response);
            }
            else
            {
                var userAttendedClaims = _context.InvestigationTransaction.Where(t => (t.UserEmailActioned == agencyUser.Email &&
                            t.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId))?.Select(c => c.ClaimsInvestigationId);

                var claimsSubmitted = new List<ClaimsInvestigation>();
                foreach (var claim in applicationDbContext)
                {
                    var previousReport = _context.PreviousClaimReport.Any(r => r.ClaimsInvestigationId == claim.ClaimsInvestigationId);

                    var isReview = reviewCases.Any(i => i.IsReviewCase &&
                    claim.ReviewCount == 1 &&
                    i.ClaimsInvestigationId == claim.ClaimsInvestigationId &&
                     i.InvestigationCaseSubStatusId == reassignedStatus.InvestigationCaseSubStatusId &&
                     i.UserEmailActionedTo == string.Empty &&
                     i.UserRoleActionedTo == $"{AppRoles.CREATOR.GetEnumDisplayName()} ( {claim.PolicyDetail.ClientCompany.Email})");

                    if ((claim.InvestigationCaseStatus.Name == CONSTANTS.CASE_STATUS.FINISHED &&
                        claim.VendorId == agencyUser.VendorId && 
                        claim.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR) ||
                        (claim.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR) || 
                        ( isReview && previousReport))
                    {
                        if (userAttendedClaims.Contains(claim.ClaimsInvestigationId) )
                        {
                            claimsSubmitted.Add(claim);
                        }
                    }
                }
                var response = claimsSubmitted
                       .Select(a => new ClaimsInvesgationResponse
                       {
                           Id = a.ClaimsInvestigationId,
                           PolicyId = a.PolicyDetail.ContractNumber,
                           Amount = string.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                           AssignedToAgency = a.AssignedToAgency,
                           Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                           PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                           Company = a.PolicyDetail.ClientCompany.Name,
                           Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                           Customer = a.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                           Name = a.CustomerDetail.CustomerName,
                           Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                           Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                           ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                           Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                           Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                           Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                           timePending = a.GetTimePending(),
                           PolicyNum = a.PolicyDetail.ContractNumber,
                           BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                           BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                           TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
                       })
                        ?.ToList();

                return Ok(response);
            }
            
        }
        [HttpGet("GetSubmitted")]
        [Authorize(Roles = "AGENT")]
        public async Task<IActionResult> GetSubmitted()
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

            var agencyUser = _context.VendorApplicationUser.Include(u=>u.Vendor).FirstOrDefault(c => c.Email == currentUserEmail);
            var submittedToSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var userAttendedClaims = _context.InvestigationTransaction.Where(t => (t.UserEmailActioned == agencyUser.Email &&
            t.UserRoleActionedTo == $"{AppRoles.SUPERVISOR.GetEnumDisplayName()} ({agencyUser.Vendor.Email})" &&
                           t.InvestigationCaseSubStatusId == submittedToSupervisorStatus.InvestigationCaseSubStatusId))?.Select(c => c.ClaimsInvestigationId);

            var claimsSubmitted = new List<ClaimsInvestigation>();
            foreach (var item in applicationDbContext)
            {
                if (userAttendedClaims.Contains(item.ClaimsInvestigationId))
                {
                    claimsSubmitted.Add(item);
                }
            }
            var response = claimsSubmitted
                   .Select(a => new ClaimsInvesgationResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                       AssignedToAgency = a.AssignedToAgency,
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Company = a.PolicyDetail.ClientCompany.Name,
                       Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = a.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                       Name = a.CustomerDetail.CustomerName,
                       Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                       Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                       ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                       Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                       Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                       Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                       timePending = a.GetTimePending(),
                       PolicyNum = a.PolicyDetail.ContractNumber,
                       BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                       TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
                   })
                    ?.ToList();

            return Ok(response);

        }

        private IQueryable<ClaimsInvestigation> GetClaims()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.ClientCompany)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(b=>b.BeneficiaryRelation)
               .Include(c => c.BeneficiaryDetail.ClaimReport)
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
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(l => l.PreviousClaimReports)
               .Include(c=>c.BeneficiaryDetail)
               .ThenInclude(l=>l.ClaimReport)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderByDescending(o => o.Created);
        }
    }
}