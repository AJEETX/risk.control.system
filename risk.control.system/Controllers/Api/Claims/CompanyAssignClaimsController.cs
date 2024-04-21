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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.AspNetCore.Authorization;

namespace risk.control.system.Controllers.Api.Claims
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [Authorize(Roles = "Creator")]
    [ApiController]
    public class CompanyAssignClaimsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CompanyAssignClaimsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetAssigner")]
        public async Task<IActionResult> GetAssigner()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var reAssignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                      i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);
            var withdrawnByCompany = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == userEmail.Value);

            applicationDbContext = applicationDbContext.Where(a => a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (
                    a.IsReady2Assign && !a.AssignedToAgency && (a.UserEmailActioned == companyUser.Email &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId
                        || a.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId)
                ) ||
                 (a.InvestigationCaseSubStatusId == withdrawnByAgency.InvestigationCaseSubStatusId &&
                        a.UserEmailActionedTo == string.Empty &&
                        a.UserRoleActionedTo == $"{AppRoles.Creator.GetEnumDisplayName()} ({companyUser.ClientCompany.Email})")
                 ||
                 (a.InvestigationCaseSubStatusId == withdrawnByCompany.InvestigationCaseSubStatusId &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.UserEmailActioned == companyUser.Email &&
                        a.UserRoleActionedTo == $"{AppRoles.Creator.GetEnumDisplayName()} ({companyUser.ClientCompany.Email})")
                 ||
                (a.IsReviewCase && a.InvestigationCaseSubStatusId == reAssignedStatus.InvestigationCaseSubStatusId &&
                a.UserEmailActionedTo == string.Empty &&
                a.UserRoleActionedTo == $"{AppRoles.Creator.GetEnumDisplayName()} ( {companyUser.ClientCompany.Email})")
                );

            var claimsAssigned = new List<ClaimsInvestigation>();
            var newClaimsAssigned = new List<ClaimsInvestigation>();

            foreach (var item in applicationDbContext)
            {
                item.AssignAutoUploadView += 1;
                if (item.AssignAutoUploadView <= 1)
                {
                    newClaimsAssigned.Add(item);
                }
                claimsAssigned.Add(item);
            }
            if (newClaimsAssigned.Count > 0)
            {
                _context.ClaimsInvestigation.UpdateRange(newClaimsAssigned);
                _context.SaveChanges();
            }
            var response = applicationDbContext?.ToList()
                    .Select(a => new ClaimsInvesgationResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        AutoAllocated = a.AutoAllocated,
                        AssignedToAgency = a.AssignedToAgency,
                        PolicyId = a.PolicyDetail.ContractNumber,
                        Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                        Agent = !string.IsNullOrWhiteSpace(a.CurrentClaimOwner) ?
                        string.Join("", "<span class='badge badge-light'>" + a.CurrentClaimOwner + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UpdatedBy + "</span>"),
                        Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.CaseLocations?.FirstOrDefault()),
                        PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.CaseLocations?.FirstOrDefault()),
                        Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                        Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                        Name = a.CustomerDetail?.CustomerName != null ? a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /> </span>",
                        Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                        Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                        ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                        Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                        Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                        timePending = a.GetTimePending(),
                        PolicyNum = a.GetPolicyNum(),
                        BeneficiaryPhoto = a.CaseLocations.Count != 0 && a.CaseLocations.FirstOrDefault().ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CaseLocations.FirstOrDefault().ProfilePicture)) :
                                      @Applicationsettings.NO_USER,
                        BeneficiaryName = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                        a.CaseLocations.FirstOrDefault().BeneficiaryName,
                        TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                        IsNewAssigned = a.AssignAutoUploadView <= 1
                    })
                    ?.ToList();

            return Ok(response);
        }

        [HttpGet("GetAssignerMap")]
        public IActionResult GetAssignerMap()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var reAssignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (companyUser == null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);
            }
            else if (companyUser != null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);
            }

            // SHOWING DIFFERRENT PAGES AS PER ROLES
            applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId == null) ||
            (a.IsReviewCase && a.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId));

            var claimsAssigned = new List<ClaimsInvestigation>();

            foreach (var item in applicationDbContext)
            {
                item.CaseLocations = item.CaseLocations.Where(c => !c.VendorId.HasValue
                    && c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId ||
                        (item.IsReviewCase && item.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId)
                    )?.ToList();
                if (item.CaseLocations.Any())
                {
                    claimsAssigned.Add(item);
                }
            }
            var response = claimsAssigned
                    .Select(a => new MapResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        Address = LocationDetail.GetAddress(a.PolicyDetail.ClaimType, a.CustomerDetail, a.CaseLocations?.FirstOrDefault()),
                        Description = a.PolicyDetail.CauseOfLoss,
                        Price = a.PolicyDetail.SumAssuredValue,
                        Type = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? "home" : "building",
                        Bed = a.CustomerDetail.CustomerIncome.GetEnumDisplayName(),
                        Bath = a.CustomerDetail.ContactNumber,
                        Size = a.CustomerDetail.Description,
                        Position = new Position
                        {
                            Lat = a.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                           decimal.Parse(a.CustomerDetail.PinCode.Latitude) : decimal.Parse(a.CaseLocations.FirstOrDefault().PinCode.Latitude),
                            Lng = a.PolicyDetail.ClaimType == ClaimType.HEALTH ?
                            decimal.Parse(a.CustomerDetail.PinCode.Longitude) : decimal.Parse(a.CaseLocations.FirstOrDefault().PinCode.Longitude)
                        },
                        Url = (a.CaseLocations?.FirstOrDefault() != null && a.CaseLocations?.FirstOrDefault().InvestigationCaseSubStatus.Code != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR) ? "/ClaimsInvestigation/Detail?Id=" + a.ClaimsInvestigationId : "/ClaimsInvestigation/Details?Id=" + a.ClaimsInvestigationId
                    })?
                    .ToList();

            var company = _context.ClientCompany.Include(c => c.PinCode).FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

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
            return applicationDbContext.OrderByDescending(o => o.Created);
        }
    }
}