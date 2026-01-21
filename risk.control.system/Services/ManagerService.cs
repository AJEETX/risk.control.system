using System.Globalization;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IManagerService
    {
        Task<object> GetActiveCases(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        Task<List<CaseInvestigationResponse>> GetApprovedCases(string userEmail);
        Task<List<CaseInvestigationResponse>> GetRejectedCases(string userEmail);
    }
    internal class ManagerService : IManagerService
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;

        public ManagerService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            this.env = env;
        }
        public async Task<object> GetActiveCases(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var companyUser = await context.ApplicationUser
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var assignedToAssignerStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;
            var submittedToAssessorStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR;

            var query = context.Investigations
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
                .Where(a => !a.Deleted && a.ClientCompanyId == companyUser.ClientCompanyId &&
                    a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                    (a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR));

            int totalRecords =await query.CountAsync(); // Get total count before pagination

            // Search filtering
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower(CultureInfo.InvariantCulture);
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.ToLower().Contains(search) ||
                    a.PolicyDetail.CauseOfLoss.ToLower().Contains(search) ||
                    a.PolicyDetail.InvestigationServiceType.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.DateOfBirth.ToString().ToLower().Contains(search) ||
                    a.CustomerDetail.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.PhoneNumber.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Code.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Name.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.PhoneNumber.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(caseType))
            {
                query = query.Where(c => c.PolicyDetail.InsuranceType == Enum.Parse<InsuranceType>(caseType));  // Assuming CaseType is the field in your data model
            }

            var data = query.AsEnumerable();
            int recordsFiltered =await query.CountAsync();

            var transformedData = data.Select(a => new
            {
                Id = a.Id,
                AutoAllocated = a.IsAutoAllocated,
                CustomerFullName = a.CustomerDetail?.Name ?? string.Empty,
                BeneficiaryFullName = a.BeneficiaryDetail?.Name ?? string.Empty,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = GetOwner(a),
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwnerImage(a))),
                CaseWithPerson = a.CaseOwner,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeCode = ClaimsInvestigationExtension.GetPincodeCode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentPath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ImagePath != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.CustomerDetail?.ImagePath))))
                        : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i> </span>",
                Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                Status = a.ORIGIN.GetEnumDisplayName(),
                SubStatus = a.SubStatus,
                Ready2Assign = a.IsReady2Assign,
                ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetManagerActiveTimePending(a),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath))))
                        : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name)
                        ? "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>"
                        : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).TotalSeconds,
                IsNewAssigned = a.IsNewAssignedToManager,
                PersonMapAddressUrl = string.Format(a.GetMap(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.SubStatus == assignedToAssignerStatus,
                                                      a.SubStatus == submittedToAssessorStatus), "400", "400")
            });

            // Apply Sorting AFTER Data Transformation
            if (!string.IsNullOrEmpty(orderDir))
            {
                bool isAsc = string.Equals(orderDir, "asc", StringComparison.OrdinalIgnoreCase);
                transformedData = orderColumn switch
                {
                    // Sort by Policy Number
                    1 => isAsc
                                                ? transformedData.OrderBy(a => a.PolicyId)
                                                : transformedData.OrderByDescending(a => a.PolicyId),
                    // Sort by Amount (Ensure proper sorting of numeric values)
                    2 => isAsc
                                                ? transformedData.OrderBy(a => a.Amount)
                                                : transformedData.OrderByDescending(a => a.Amount),
                    // Sort by Amount (Ensure proper sorting of numeric values)
                    3 => isAsc
                                                ? transformedData.OrderBy(a => a.PincodeCode)
                                                : transformedData.OrderByDescending(a => a.PincodeCode),
                    // Sort by Customer Full Name
                    6 => isAsc
                                                ? transformedData.OrderBy(a => a.CustomerFullName)
                                                : transformedData.OrderByDescending(a => a.CustomerFullName),
                    // Sort by Beneficiary Full Name
                    8 => isAsc
                                                ? transformedData.OrderBy(a => a.BeneficiaryFullName)
                                                : transformedData.OrderByDescending(a => a.BeneficiaryFullName),
                    // Sort by Status
                    9 => isAsc
                                                ? transformedData.OrderBy(a => a.ServiceType)
                                                : transformedData.OrderByDescending(a => a.ServiceType),
                    // Sort by Status
                    10 => isAsc
                                                ? transformedData.OrderBy(a => a.Location)
                                                : transformedData.OrderByDescending(a => a.Location),
                    // Sort by Created Date
                    11 => isAsc
                                                ? transformedData.OrderBy(a => DateTime.ParseExact(a.Created, "dd-MM-yyyy", null))
                                                : transformedData.OrderByDescending(a => DateTime.ParseExact(a.Created, "dd-MM-yyyy", null)),
                    // Sort by TimeElapsed
                    12 => isAsc
                                                ? transformedData.OrderBy(a => a.TimeElapsed)
                                                : transformedData.OrderByDescending(a => a.TimeElapsed),
                    // Default Sorting (if needed)
                    _ => isAsc 
                                                ? transformedData.OrderBy(a => a.TimeElapsed)
                                                : transformedData.OrderByDescending(a => a.TimeElapsed),
                };
            }
            // Apply Pagination
            var pagedData = transformedData.Skip(start).Take(length).ToList();
            // Prepare Response
            var idsToMarkViewed = pagedData.Where(x => x.IsNewAssigned).Select(x => x.Id).ToList();

            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate =await context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToListAsync();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNewAssignedToManager = false;

                await context.SaveChangesAsync(null, false); // mark as viewed
            }
            var response = new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = recordsFiltered,
                data = pagedData
            };

            return response;
        }
        private static string GetManagerActiveTimePending(InvestigationTask caseTask)
        {
            if (caseTask.CreatorSla == 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Days} days since created!\"></i>");
            }
            if (DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Days >= caseTask.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");

            else if (DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Days >= 3 || DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Days >= caseTask.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");
            if (DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");

            if (DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Hours < 24 &&
                DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Hours == 0 && DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Minutes == 0 && DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.AllocatedToAgencyTime.GetValueOrDefault()).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        private byte[] GetOwnerImage(InvestigationTask caseTask)
        {
            var noDataImagefilePath = Path.Combine(env.WebRootPath, "img", "no-photo.jpg");
            var noDataimage = File.ReadAllBytes(noDataImagefilePath);

            if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR || caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                var agent = context.Vendor.FirstOrDefault(u => u.VendorId == caseTask.VendorId);
                if (agent != null && !string.IsNullOrWhiteSpace(agent.DocumentUrl))
                {
                    var agentImagePath = Path.Combine(env.ContentRootPath, agent.DocumentUrl);
                    var agentProfile = File.ReadAllBytes(agentImagePath);
                    return agentProfile;
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                var vendor = context.ApplicationUser.FirstOrDefault(v => v.Email == caseTask.TaskedAgentEmail);
                if (vendor != null && !string.IsNullOrWhiteSpace(vendor.ProfilePictureUrl))
                {
                    var vendorImagePath = Path.Combine(env.ContentRootPath, vendor.ProfilePictureUrl);
                    var vendorImage = File.ReadAllBytes(vendorImagePath);
                    return vendorImage;
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                )
            {
                var company = context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == caseTask.ClientCompanyId);
                if (company != null && !string.IsNullOrWhiteSpace(company.DocumentUrl))
                {
                    var companyImagePath = Path.Combine(env.ContentRootPath, company.DocumentUrl);
                    var companyImage = File.ReadAllBytes(companyImagePath);
                    return companyImage;
                }
            }
            return noDataimage;
        }
        private string GetOwner(InvestigationTask caseTask)
        {
            if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                return caseTask.Vendor.Email;
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                return caseTask.TaskedAgentEmail;
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                )
            {
                var company = context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == caseTask.ClientCompanyId);
                if (company != null)
                {
                    return company.Email;
                }
            }
            return string.Empty;
        }

        public async Task<List<CaseInvestigationResponse>> GetApprovedCases(string userEmail)
        {

            var companyUser = await context.ApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(u => u.Email == userEmail);
            var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;

            var finishStatus = CONSTANTS.CASE_STATUS.FINISHED;

            var caseTasks = await context.Investigations
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

            var response = caseTasks.Select(a => new CaseInvestigationResponse
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
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ImagePath != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.CustomerDetail?.ImagePath))))
                        : Applicationsettings.NO_USER,
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
                BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i></span>" :
                    a.BeneficiaryDetail.Name,
                Agency = a.Vendor?.Name,
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.Vendor.DocumentUrl)))),
                TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime ?? DateTime.Now).TotalSeconds,
                PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration,
                CanDownload = CanDownload(a.Id, userEmail)
            }).ToList();
            return response;
        }
        private static string GetManagerTimeCompleted(InvestigationTask caseTask)
        {
            if (caseTask.CreatorSla == 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Days} days since created!\"></i>");
            }
            if (DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Days >= caseTask.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");

            else if (DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Days >= 3 || DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Days >= caseTask.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");
            if (DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");

            if (DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Hours < 24 &&
                DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Hours == 0 && DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Minutes == 0 && DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(caseTask.ProcessedByAssessorTime.GetValueOrDefault()).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
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

        public async Task<List<CaseInvestigationResponse>> GetRejectedCases(string userEmail)
        {
            var companyUser = await context.ApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            // Get the rejected status
            var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
            var finishStatus = CONSTANTS.CASE_STATUS.FINISHED;

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
                            (i.SubStatus == rejectedStatus) &&
                            i.Status == finishStatus).ToListAsync();

            var response = claims.Select(a => new CaseInvestigationResponse
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
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ImagePath != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.CustomerDetail?.ImagePath))))
                        : Applicationsettings.NO_USER,
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
                BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" :
                    a.BeneficiaryDetail.Name,
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.Vendor.DocumentUrl)))),
                Agency = a.Vendor?.Name,
                TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime.Value).TotalSeconds,
                PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration,
                CanDownload = CanDownload(a.Id, userEmail)
            }).ToList();
            return response;
        }
    }
}
