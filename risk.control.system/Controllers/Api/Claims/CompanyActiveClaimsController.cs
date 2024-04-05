using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;
using risk.control.system.Services;
using System.Globalization;

namespace risk.control.system.Controllers.Api.Claims
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyActiveClaimsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CompanyActiveClaimsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetActive")]
        public async Task<IActionResult> GetActive()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);

            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submittededToSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var submittededToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var reAssigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);

            var claimsSubmitted = new List<ClaimsInvestigation>();

            if (userRole.Value.Contains(AppRoles.Creator.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId));
                claimsSubmitted = await applicationDbContext.ToListAsync();
            }
            
            else if (userRole.Value.Contains(AppRoles.Assessor.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a =>
                openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
                a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId != null)
                );

                foreach (var item in applicationDbContext)
                {
                    if ((item.InvestigationCaseSubStatusId == submittededToAssesssorStatus.InvestigationCaseSubStatusId) ||
                        (item.IsReviewCase))
                    {
                        claimsSubmitted.Add(item);
                    }
                }
            }
            var response = claimsSubmitted
                    .Select(a => new ClaimsInvesgationResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        AutoAllocated = a.AutoAllocated,
                        CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.CustomerName) ? "" : a.CustomerDetail.CustomerName,
                        BeneficiaryFullName = a.CaseLocations.Count == 0 ? "" : a.CaseLocations.FirstOrDefault().BeneficiaryName,
                        PolicyId = a.PolicyDetail.ContractNumber,
                        Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                        AssignedToAgency = a.AssignedToAgency,
                        Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                        Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.CaseLocations?.FirstOrDefault()),
                        PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.CaseLocations?.FirstOrDefault()),
                        Document = a.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                        Customer = a.CustomerDetail?.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                        Name = a.CustomerDetail?.CustomerName != null ?
                        a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                        Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                        Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                        SubStatus = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Ready2Assign = a.IsReady2Assign,
                        ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                        Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                        Location = a.CaseLocations.Count == 0 ?
                        string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>") :
                        string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "</span> ")),
                        Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                        timePending = a.GetTimePending(),
                        Withdrawable = a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId ? true : false,
                        PolicyNum = a.GetPolicyNum(),
                        BeneficiaryPhoto = a.CaseLocations.Count != 0 && a.CaseLocations.FirstOrDefault().ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CaseLocations.FirstOrDefault().ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                        BeneficiaryName = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.CaseLocations.FirstOrDefault().BeneficiaryName,
                        TimeElapsed = DateTime.UtcNow.Subtract(a.Created).TotalSeconds
                    })?
                    .ToList();

            return Ok(response);
        }

        [HttpGet("GetActiveMap")]
        public async Task<IActionResult> GetActiveMap()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);

            var company = _context.ClientCompany.Include(c => c.PinCode).FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submittededToSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var submittededToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var reAssigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);

            var claimsSubmitted = new List<ClaimsInvestigation>();

            if (userRole.Value.Contains(AppRoles.Creator.ToString()) || userRole.Value.Contains(AppRoles.CompanyAdmin.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId));
                claimsSubmitted = await applicationDbContext.ToListAsync();
            }
            
            else if (userRole.Value.Contains(AppRoles.Assessor.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a =>
                openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
                a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId != null)
                );

                foreach (var item in applicationDbContext)
                {
                    if ((item.InvestigationCaseSubStatusId == submittededToAssesssorStatus.InvestigationCaseSubStatusId) ||
                        (item.IsReviewCase))
                    {
                        claimsSubmitted.Add(item);
                    }
                }
            }

            var response = claimsSubmitted
                   .Select(a => new MapResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       Address = LocationDetail.GetAddress(a.PolicyDetail.ClaimType, a.CustomerDetail, a.CaseLocations?.FirstOrDefault()),
                       Description = a.PolicyDetail.CauseOfLoss,
                       Price = a.PolicyDetail.SumAssuredValue,
                       Type = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? "home" : "building",
                       Bed = a.CustomerDetail?.CustomerIncome.GetEnumDisplayName(),
                       Bath = a.CustomerDetail?.ContactNumber,
                       Size = a.CustomerDetail?.Description,
                       Position = new Position
                       {
                           Lat = GetLat(a.PolicyDetail.ClaimType, a.CustomerDetail, a.CaseLocations?.FirstOrDefault()) ?? decimal.Parse(company.PinCode.Latitude),
                           Lng = GetLng(a.PolicyDetail.ClaimType, a.CustomerDetail, a.CaseLocations?.FirstOrDefault()) ?? decimal.Parse(company.PinCode.Longitude),
                       },
                       Url = (a.CaseLocations?.FirstOrDefault() != null && a.CaseLocations?.FirstOrDefault().InvestigationCaseSubStatus.Code != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR) ? "/ClaimsInvestigation/Detail?Id=" + a.ClaimsInvestigationId : "/ClaimsInvestigation/Details?Id=" + a.ClaimsInvestigationId
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
            return Ok(new
            {
                response = response,
                lat = decimal.Parse(company.PinCode.Latitude),
                lng = decimal.Parse(company.PinCode.Longitude)
            });
        }

        private IQueryable<ClaimsInvestigation> GetClaims()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.ClientCompany)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CostCentre)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.InvestigationCaseSubStatus)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.CaseLocations)
                .ThenInclude(c => c.District)
                .Include(c => c.CaseLocations)
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
               .Include(c => c.CaseLocations)
               .ThenInclude(l => l.PreviousClaimReports)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderBy(o => o.Created);
        }

        private decimal? GetLat(ClaimType? claimType, CustomerDetail a, CaseLocation location)
        {
            if (claimType == ClaimType.HEALTH)
            {
                if (a is null)
                    return null;
                return decimal.Parse(a.PinCode.Latitude);
            }
            else
            {
                if (location is null)
                    return null;
                return decimal.Parse(location.PinCode.Latitude);
            }
        }

        private decimal? GetLng(ClaimType? claimType, CustomerDetail a, CaseLocation location)
        {
            if (claimType == ClaimType.HEALTH)
            {
                if (a is null)
                    return null;
                return decimal.Parse(a.PinCode.Longitude);
            }
            else
            {
                if (location is null)
                    return null;
                return decimal.Parse(location.PinCode.Longitude);
            }
        }
    }
}