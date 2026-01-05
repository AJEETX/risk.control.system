using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IAssessorService
    {
        Task<List<ClaimsInvestigationResponse>> GetInvestigations(string userEmail);
        Task<List<ClaimsInvestigationResponse>> GetReview(string userEmail);
        Task<List<ClaimsInvestigationResponse>> GetApprovededCases(string userEmail);
        Task<List<ClaimsInvestigationResponse>> GetRejectedCases(string userEmail);
    }
    public class AssessorService : IAssessorService
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;

        public AssessorService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            this.env = env;
        }

        public async Task<List<ClaimsInvestigationResponse>> GetApprovededCases(string userEmail)
        {
            var companyUser = await context.ApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (companyUser == null)
            {
                return null;
            }

            var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;

            var finishStatus = CONSTANTS.CASE_STATUS.FINISHED;

            if (approvedStatus == null || finishStatus == null)
            {
                return null;
            }

            var claims = await context.Investigations
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
                            i.Status == finishStatus &&
                            i.SubmittedAssessordEmail == userEmail).ToListAsync();

            var response = claims
                .Select(a => new ClaimsInvestigationResponse
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
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                    Customer = a.CustomerDetail?.ImagePath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.CustomerDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    Name = a.CustomerDetail?.Name ??
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /></span>",
                    Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Status = a.ORIGIN.GetEnumDisplayName(),
                    ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = a.GetAssessorTimePending(false, true),
                    PolicyNum = a.GetPolicyNum(),
                    BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i></span>" :
                        a.BeneficiaryDetail.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.Vendor.DocumentUrl)))),
                    Agency = a.Vendor?.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime ?? DateTime.Now).TotalSeconds,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration,
                    CanDownload = CanDownload(a.Id, userEmail)
                })
                .ToList();
            return response;
        }

        public async Task<List<ClaimsInvestigationResponse>> GetInvestigations(string userEmail)
        {
            var companyUser = await context.ApplicationUser
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            // Fetch claims based on statuses and company
            var claims = await context.Investigations
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
                            (i.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                             i.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR))
                .ToListAsync();


            // Prepare the response
            var response = claims
                .Select(a => new ClaimsInvestigationResponse
                {
                    Id = a.Id,
                    AutoAllocated = a.IsAutoAllocated,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    Document = a.PolicyDetail?.DocumentPath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                    Customer = a.CustomerDetail?.ImagePath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.CustomerDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /> </span>",
                    Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Status = a.ORIGIN.GetEnumDisplayName(),
                    ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = a.GetAssessorTimePending(true),
                    PolicyNum = a.GetPolicyNum(),
                    BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                                      "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                                      a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.SubmittedToAssessorTime.Value).TotalSeconds,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.Vendor.DocumentUrl)))),
                    Agent = a.Vendor.Name,
                    IsNewAssigned = a.IsNewSubmittedToCompany,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                })
                .ToList();

            var idsToMarkViewed = claims.Where(x => x.IsNewSubmittedToCompany).Select(x => x.Id).ToList();

            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNewSubmittedToCompany = false;

                await context.SaveChangesAsync(null, false); // mark as viewed
            }
            return response;
        }

        public async Task<List<ClaimsInvestigationResponse>> GetReview(string userEmail)
        {
            var companyUser = await context.ApplicationUser
                 .Include(c => c.Country)
                 .FirstOrDefaultAsync(c => c.Email == userEmail);
            var claims = await context.Investigations
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
                i.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                i.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR &&
                i.IsQueryCase &&
                i.RequestedAssessordEmail == userEmail)
                .ToListAsync();

            var response = claims
                .Select(a => new ClaimsInvestigationResponse
                {
                    Id = a.Id,
                    AutoAllocated = a.IsAutoAllocated,
                    CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.Name) ? "" : a.CustomerDetail.Name,
                    BeneficiaryFullName = a.BeneficiaryDetail?.Name ?? "",
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Agent = a.Vendor.Email,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwnerImage(a))),
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    Document = a.PolicyDetail?.DocumentPath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                    Customer = a.CustomerDetail?.ImagePath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.CustomerDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                    Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Status = a.ORIGIN.GetEnumDisplayName(),
                    SubStatus = a.SubStatus,
                    Ready2Assign = a.IsReady2Assign,
                    ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = a.GetAssessorTimePending(false, false, a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR, a.IsQueryCase),
                    Withdrawable = false,
                    PolicyNum = a.GetPolicyNum(),
                    BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.EnquiredByAssessorTime ?? DateTime.Now).TotalSeconds,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                })
                .ToList();
            return response;
        }
        private byte[] GetOwnerImage(InvestigationTask a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var noDataImagefilePath = Path.Combine(env.WebRootPath, "img", "no-photo.jpg");
            var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);

            if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR || a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                var agent = context.Vendor.FirstOrDefault(u => u.VendorId == a.VendorId);
                if (agent != null && !string.IsNullOrWhiteSpace(agent.DocumentUrl))
                {
                    var agentImagePath = Path.Combine(env.ContentRootPath, agent.DocumentUrl);
                    var agentProfile = System.IO.File.ReadAllBytes(agentImagePath);
                    return agentProfile;
                }
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                var vendor = context.ApplicationUser.FirstOrDefault(v => v.Email == a.TaskedAgentEmail);
                if (vendor != null && !string.IsNullOrWhiteSpace(vendor.ProfilePictureUrl))
                {
                    var vendorImagePath = Path.Combine(env.ContentRootPath, vendor.ProfilePictureUrl);
                    var vendorImage = System.IO.File.ReadAllBytes(vendorImagePath);
                    return vendorImage;
                }
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                )
            {
                var company = context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == a.ClientCompanyId);
                if (company != null && !string.IsNullOrWhiteSpace(company.DocumentUrl))
                {
                    var companyImagePath = Path.Combine(env.ContentRootPath, company.DocumentUrl);
                    var companyImage = System.IO.File.ReadAllBytes(companyImagePath);
                    return companyImage;
                }
            }
            return noDataimage;
        }

        private bool CanDownload(long id, string userEmail)
        {
            var tracker = context.PdfDownloadTracker
                          .FirstOrDefault(t => t.ReportId == id && t.UserEmail == userEmail);
            bool canDownload = true;
            if (tracker != null && tracker.DownloadCount > 3)
            {
                canDownload = false;
            }
            return canDownload;
        }

        public async Task<List<ClaimsInvestigationResponse>> GetRejectedCases(string userEmail)
        {
            var companyUser = await context.ApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser == null)
            {
                return null;
            }

            var rejectStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

            var claims = await context.Investigations
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
                            i.SubmittedAssessordEmail == userEmail &&
                            i.Status == CONSTANTS.CASE_STATUS.FINISHED &&
                            i.SubStatus == rejectStatus
                            ).ToListAsync();


            var response = claims
                .Select(a => new ClaimsInvestigationResponse
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
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                    Customer = a.CustomerDetail?.ImagePath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.CustomerDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    Name = a.CustomerDetail?.Name ??
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /></span>",
                    Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Status = a.ORIGIN.GetEnumDisplayName(),
                    ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = a.GetAssessorTimePending(false, true),
                    PolicyNum = a.GetPolicyNum(),
                    BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i></span>" :
                        a.BeneficiaryDetail.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.Vendor.DocumentUrl)))),
                    Agency = a.Vendor?.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime ?? DateTime.Now).TotalSeconds,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration,
                    CanDownload = CanDownload(a.Id, userEmail)
                })
                .ToList();
            return response;
        }
    }
}
