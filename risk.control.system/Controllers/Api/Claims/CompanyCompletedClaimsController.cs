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
    public class CompanyCompletedClaimsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CompanyCompletedClaimsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetReport")]
        public async Task<IActionResult> GetReport()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims().Where(c =>
                c.CustomerDetail != null && c.CaseLocations.Count > 0 &&
                c.CaseLocations.All(c => c.ClaimReport != null));
            var user = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == user);

            var claimsSubmitted = await applicationDbContext.Where(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (c.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR
                ))?
                 .ToListAsync();

            var response =
                claimsSubmitted
            .Select(a => new ClaimsInvesgationResponse
            {
                Id = a.ClaimsInvestigationId,
                AutoAllocated = a.AutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.CaseLocations?.FirstOrDefault()),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.CaseLocations?.FirstOrDefault()),
                Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.CustomerName != null ? a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /> </span>",
                Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                Location = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                        string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "</span> ")),
                Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                timePending = a.GetTimePending(),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.CaseLocations.Count != 0 && a.CaseLocations.FirstOrDefault().ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CaseLocations.FirstOrDefault().ProfilePicture)) :
                                      @Applicationsettings.NO_USER,
                BeneficiaryName = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                        a.CaseLocations.FirstOrDefault().BeneficiaryName,
                Agency = a.Vendor?.Name,
                TimeElapsed = DateTime.UtcNow.Subtract(a.Created).TotalSeconds
            })?.ToList();

            return Ok(response);
        }

        [HttpGet("GetReportMap")]
        public async Task<IActionResult> GetReportMap()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims().Where(c =>
                c.CustomerDetail != null && c.CaseLocations.Count > 0 &&
                c.CaseLocations.All(c => c.ClaimReport != null));
            var user = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == user);

            var claimsSubmitted = await applicationDbContext.Where(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                c.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ||
                c.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                c.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR
                )
                 .ToListAsync();

            var response = claimsSubmitted
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
            return applicationDbContext.OrderBy(o => o.Created);
        }
    }
}