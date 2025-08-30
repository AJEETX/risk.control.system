using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = MANAGER.DISPLAY_NAME)]
    public class ManagerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IInvestigationService service;

        public ManagerController(ApplicationDbContext context,
            IInvestigationService service)
        {
            _context = context;
            this.service = service;
        }

        [HttpGet("GetActive")]
        public async Task<IActionResult> GetActive(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User.Identity.Name;

            var response = await service.GetManagerActive(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

            return Ok(response);
        }
        [HttpGet("GetReport")]
        public async Task<IActionResult> GetReport()
        {
            var userEmail = HttpContext.User.Identity.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return BadRequest("User identity is missing.");
            }

            var companyUser = await _context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (companyUser == null)
            {
                return NotFound("User not found.");
            }

            // Fetching the statuses once
            var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;

            var finishStatus = CONSTANTS.CASE_STATUS.FINISHED;

            if (approvedStatus == null || finishStatus == null)
            {
                return NotFound("Required statuses not found.");
            }

            var claims = await _context.Investigations
                .Include(i => i.Vendor)
                .Include(i => i.PolicyDetail)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.PinCode)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.Country)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.PinCode)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.Country)
                .Where(i => !i.Deleted && i.ClientCompanyId == companyUser.ClientCompanyId &&
                            (i.SubStatus == approvedStatus) &&
                            i.Status == finishStatus).ToListAsync();

            var response = claims.Select(a => new ClaimsInvestigationResponse
            {
                Id = a.Id,
                AutoAllocated = a.IsAutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = a.Vendor.Email,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentPath != null ?
                    a.PolicyDetail?.DocumentPath :
                    Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ImagePath != null ?
                    a.CustomerDetail?.ImagePath :
                    Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ??
                    "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /></span>",
                Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                Status = a.ORIGIN.GetEnumDisplayName(),
                ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetManagerTimeCompleted(a),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ?
                    a.BeneficiaryDetail.ImagePath :
                    Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i></span>" :
                    a.BeneficiaryDetail.Name,
                Agency = a.Vendor?.Name,
                OwnerDetail = $"data:image/*;base64,{Convert.ToBase64String(a.Vendor.DocumentImage)}",
                TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime ?? DateTime.Now).TotalSeconds,
                PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration,
                CanDownload = CanDownload(a.Id, userEmail)
            }).ToList();

            return Ok(response);
        }

        private static string GetManagerTimeCompleted(InvestigationTask a)
        {
            if (a.CreatorSla == 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} days since created!\"></i>");
            }
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");

            else if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= 3 || DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");

            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours < 24 &&
                DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours == 0 && DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Minutes == 0 && DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        [HttpGet("GetReject")]
        public async Task<IActionResult> GetReject()
        {
            var userEmail = HttpContext.User.Identity.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return BadRequest("User identity is missing.");
            }

            // Fetch the company user
            var companyUser = await _context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser == null)
            {
                return NotFound("User not found.");
            }

            // Get the rejected status
            var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
            var finishStatus = CONSTANTS.CASE_STATUS.FINISHED;

            if (rejectedStatus == null)
            {
                return NotFound("Rejected status not found.");
            }

            var claims = await _context.Investigations
                .Include(i => i.Vendor)
                .Include(i => i.PolicyDetail)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.PinCode)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.Country)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.PinCode)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.Country)
                .Where(i => !i.Deleted && i.ClientCompanyId == companyUser.ClientCompanyId &&
                            (i.SubStatus == rejectedStatus) &&
                            i.Status == finishStatus).ToListAsync();

            var response = claims.Select(a => new ClaimsInvestigationResponse
            {
                Id = a.Id,
                AutoAllocated = a.IsAutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = a.Vendor.Email,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentPath != null ?
                    a.PolicyDetail.DocumentPath :
                    Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ImagePath != null ?
                    a.CustomerDetail?.ImagePath :
                    Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ??
                    "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /></span>",
                Policy = $"<span class='badge badge-light'>{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()}</span>",
                Status = $"ORIGIN of Claim: {a.ORIGIN.GetEnumDisplayName()}",
                ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetManagerTimeCompleted(a),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ?
                    a.BeneficiaryDetail?.ImagePath :
                    Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" :
                    a.BeneficiaryDetail.Name,
                OwnerDetail = $"data:image/*;base64,{Convert.ToBase64String(a.Vendor.DocumentImage)}",
                Agency = a.Vendor?.Name,
                TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime.Value).TotalSeconds,
                PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration,
                CanDownload = CanDownload(a.Id, userEmail)
            }).ToList();

            return Ok(response);
        }


        private bool CanDownload(long id, string userEmail)
        {
            var tracker = _context.PdfDownloadTracker
                          .FirstOrDefault(t => t.ReportId == id && t.UserEmail == userEmail);
            bool canDownload = true;
            if (tracker != null && tracker.DownloadCount > 3)
            {
                canDownload = false;
            }
            return canDownload;
        }
    }
}