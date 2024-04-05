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
    public class CompanyDraftClaimsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CompanyDraftClaimsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetAssign")]
        public async Task<IActionResult> GetAssign()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var reAssignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
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
            if (userRole.Value.Contains(AppRoles.Creator.ToString()))
            {
                applicationDbContext = applicationDbContext
                    .Include(c => c.CaseLocations)
                    .ThenInclude(c => c.PinCode)
                    .Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => (c.VendorId == null 
                    && c.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId)
                    ) || (a.IsReviewCase && a.InvestigationCaseSubStatusId == reAssignedStatus.InvestigationCaseSubStatusId));

                var claimsAssigned = new List<ClaimsInvestigation>();
                foreach (var item in applicationDbContext)
                {
                    if (item.IsReady2Assign)
                    {
                        item.CaseLocations = item.CaseLocations.Where(c => !c.VendorId.HasValue
                        && c.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId ||
                        (item.IsReviewCase && item.InvestigationCaseSubStatusId == reAssignedStatus.InvestigationCaseSubStatusId))?.ToList();
                        if (item.CaseLocations.Any())
                        {
                            claimsAssigned.Add(item);
                        }
                    }
                }
                var response = claimsAssigned
                    .Select(a => new ClaimsInvesgationResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                        PolicyId = a.PolicyDetail.ContractNumber,
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
                                      @Applicationsettings.NO_USER,
                        BeneficiaryName = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.CaseLocations.FirstOrDefault().BeneficiaryName,
                        TimeElapsed = DateTime.UtcNow.Subtract(a.Created).TotalSeconds
                    })?
                    .ToList();

                return Ok(response);
            }
            return Ok(null);
        }

        [HttpGet("GetAssignMap")]
        public IActionResult GetAssignMap()
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
            if (userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) || userRole.Value.Contains(AppRoles.CompanyAdmin.ToString()) || userRole.Value.Contains(AppRoles.Creator.ToString()))
            {
                applicationDbContext = applicationDbContext
                    .Include(c => c.CaseLocations)
                    .ThenInclude(c => c.PinCode)
                    .Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => (c.VendorId == null && c.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId)
                    ) || (a.IsReviewCase && a.InvestigationCaseSubStatusId == reAssignedStatus.InvestigationCaseSubStatusId));

                var claimsAssigned = new List<ClaimsInvestigation>();
                foreach (var item in applicationDbContext)
                {
                    if (item.IsReady2Assign)
                    {
                        item.CaseLocations = item.CaseLocations.Where(c => !c.VendorId.HasValue
                        && c.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId ||
                        (item.IsReviewCase && item.InvestigationCaseSubStatusId == reAssignedStatus.InvestigationCaseSubStatusId))?.ToList();
                        if (item.CaseLocations.Any())
                        {
                            claimsAssigned.Add(item);
                        }
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
            return Problem();
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
    }
}