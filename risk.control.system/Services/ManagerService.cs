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
        Task<object> GetApprovedCases(string userEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        Task<object> GetRejectedCases(string userEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
    }
    internal class ManagerService : IManagerService
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;
        private readonly IBase64FileService base64FileService;

        public ManagerService(ApplicationDbContext context, IWebHostEnvironment env, IBase64FileService base64FileService)
        {
            this.context = context;
            this.env = env;
            this.base64FileService = base64FileService;
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
                    a.CustomerDetail.PinCode.Code.ToString().Contains(search) ||
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

            int recordsFiltered =await query.CountAsync();
            bool isAsc = orderDir == "asc";
            query = orderColumn switch
            {
                0 => isAsc ? query.OrderBy(a => a.PolicyDetail.ContractNumber) : query.OrderByDescending(a => a.PolicyDetail.ContractNumber),
                1 => isAsc ? query.OrderBy(a => (double)a.PolicyDetail.SumAssuredValue) : query.OrderByDescending(a => (double)a.PolicyDetail.SumAssuredValue),
                3 => isAsc
                    ? query.OrderBy(a =>
                        a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                            ? a.CustomerDetail.PinCode.Code
                            : a.BeneficiaryDetail.PinCode.Code)
                    : query.OrderByDescending(a =>
                        a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                            ? a.CustomerDetail.PinCode.Code
                            : a.BeneficiaryDetail.PinCode.Code),
                6 => isAsc ? query.OrderBy(a => a.CustomerDetail.Name) : query.OrderByDescending(a => a.CustomerDetail.Name),
                8 => isAsc ? query.OrderBy(a => a.BeneficiaryDetail.Name) : query.OrderByDescending(a => a.BeneficiaryDetail.Name),
                9 => isAsc ? query.OrderBy(a => $"{a.PolicyDetail.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})") : query.OrderByDescending(a => $"{a.PolicyDetail.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})"),
                10 => isAsc ? query.OrderBy(a => a.SubStatus) : query.OrderByDescending(a => a.SubStatus),
                11 => isAsc ? query.OrderBy(a => a.Created) : query.OrderByDescending(a => a.Created),
                _ => isAsc ? query.OrderBy(a => a.Updated) : query.OrderByDescending(a => a.Updated)
            };

            var pagedList = await query
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
                .Skip(start).Take(length).ToListAsync();

            var transformedData = pagedList.Select(async a => {
                    var isUW = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING;
                    var culture = CustomExtensions.GetCultureByCountry(companyUser.Country.Code.ToUpper());
                    var policyNumber = a.GetPolicyNum();
                    var investigationService = a.PolicyDetail.InvestigationServiceType.Name;
                    var serviceType = $"{a.PolicyDetail.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})";
                    var personMapAddressUrl = isUW ? string.Format(a.CustomerDetail.CustomerLocationMap, "400", "400") : string.Format(a.BeneficiaryDetail.BeneficiaryLocationMap, "400", "400");
                    var pincode = ClaimsInvestigationExtension.GetPincode(isUW, a.CustomerDetail, a.BeneficiaryDetail);
                    var pincodeName = ClaimsInvestigationExtension.GetPincodeName(isUW, a.CustomerDetail, a.BeneficiaryDetail);
                    var customerName = a.CustomerDetail.Name ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>";
                    var beneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" : a.BeneficiaryDetail.Name;

                // Fetch files in parallel for this row
                var docTask = base64FileService.GetBase64FileAsync(a.PolicyDetail.DocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                    var custTask = base64FileService.GetBase64FileAsync(a.CustomerDetail.ImagePath, Applicationsettings.NO_USER);
                    var beneTask = base64FileService.GetBase64FileAsync(a.BeneficiaryDetail.ImagePath, Applicationsettings.NO_USER);
                    var ownerImageTask = base64FileService.GetBase64FileAsync(await GetOwnerImage(a));
                    var ownerDetailTask = GetOwner(a);
                    await Task.WhenAll(docTask, custTask, beneTask, ownerImageTask, ownerDetailTask);
                return new
                {
                    Id = a.Id,
                    AutoAllocated = a.IsAutoAllocated,
                    CustomerFullName = a.CustomerDetail?.Name ?? string.Empty,
                    BeneficiaryFullName = a.BeneficiaryDetail?.Name ?? string.Empty,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(culture, "{0:c}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Agent = await ownerImageTask,
                    OwnerDetail = await ownerDetailTask,
                    CaseWithPerson = a.CaseOwner,
                    Pincode = pincode,
                    PincodeCode = pincode,
                    PincodeName = pincodeName,
                    Document = await docTask,
                    Customer = await custTask,
                    Name = customerName,
                    Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Status = a.ORIGIN.GetEnumDisplayName(),
                    SubStatus = a.SubStatus,
                    Ready2Assign = a.IsReady2Assign,
                    ServiceType = serviceType,
                    Service = investigationService,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = GetManagerActiveTimePending(a),
                    PolicyNum = policyNumber,
                    BeneficiaryPhoto = await beneTask,
                    BeneficiaryName = beneficiaryName,
                    TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).TotalSeconds,
                    IsNewAssigned = a.IsNewAssignedToManager,
                    PersonMapAddressUrl = string.Format(a.GetMap(isUW, a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                                                          a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR), "400", "400")
                };
            });
            var data = await Task.WhenAll(transformedData);

            // Prepare Response
            var idsToMarkViewed = data.Where(x => x.IsNewAssigned).Select(x => x.Id).ToList();

            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate =await context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToListAsync();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNewAssignedToManager = false;

                await context.SaveChangesAsync(null, false); // mark as viewed
            }
            return new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = recordsFiltered,
                data = data
            };
        }

        public async Task<object> GetApprovedCases(string userEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
            var approvedCases = await GetCompletedCases(userEmail, approvedStatus, draw, start, length, search, caseType, orderColumn, orderDir);
            return approvedCases;
        }

        public async Task<object> GetRejectedCases(string userEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
            var rejectedCases = await GetCompletedCases(userEmail, rejectedStatus, draw, start, length, search, caseType, orderColumn, orderDir);
            return rejectedCases;
        }

        private async Task<object> GetCompletedCases(string userEmail, string subStatus, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            // 1. Get User Context (Minimal fetch)
            var companyUser = await context.ApplicationUser
                .AsNoTracking()
                .Where(u => u.Email == userEmail)
                .Select(u => new { u.ClientCompanyId, CountryCode = u.Country.Code.ToUpper() })
                .FirstOrDefaultAsync();

            // 2. Base Query (IQueryable - Not executed yet)
            var query = context.Investigations
                .AsNoTracking()
                .Where(i => !i.Deleted &&
                            i.ClientCompanyId == companyUser.ClientCompanyId &&
                            i.Status == CONSTANTS.CASE_STATUS.FINISHED &&
                            i.SubStatus == subStatus);

            int recordsTotal = await query.CountAsync();

            // 3. Server-Side Searching
            if (!string.IsNullOrWhiteSpace(search))
            {
                // Add fields you want to be searchable in the UI
                query = query.Where(i =>
                    i.PolicyDetail.ContractNumber.Contains(search) ||
                    i.CustomerDetail.Name.Contains(search) ||
                    i.BeneficiaryDetail.Name.Contains(search) ||
                    i.Vendor.Name.Contains(search));
            }
            if (!string.IsNullOrEmpty(caseType))
            {
                query = query.Where(c => c.PolicyDetail.InsuranceType == Enum.Parse<InsuranceType>(caseType));  // Assuming CaseType is the field in your data model
            }
            int recordsFiltered = await query.CountAsync();

            // 4. Server-Side Sorting
            bool isAsc = orderDir == "asc";
            query = orderColumn switch
            {
                1 => isAsc ? query.OrderBy(i => i.PolicyDetail.ContractNumber) : query.OrderByDescending(i => i.PolicyDetail.ContractNumber),
                2 => isAsc ? query.OrderBy(i => i.PolicyDetail.SumAssuredValue) : query.OrderByDescending(i => i.PolicyDetail.SumAssuredValue),
                3 => isAsc ? query.OrderBy(i => i.Vendor.Name) : query.OrderByDescending(i => i.Vendor.Name),
                4 => isAsc ? query.OrderBy(i => i.Created) : query.OrderByDescending(i => i.Created),
                _ => query.OrderByDescending(i => i.Created) // Default sort
            };

            // 5. Server-Side Paging & Projection
            // This fetches ONLY what is needed for the current page
            var pagedData = await query
                .Skip(start)
                .Take(length)
                .Select(a => new
                {
                    investigation = a,
                    a.Id,
                    a.IsAutoAllocated,
                    a.PolicyDetail,
                    a.CustomerDetail,
                    a.BeneficiaryDetail,
                    a.Vendor,
                    a.SubStatus,
                    a.ORIGIN,
                    a.Created,
                    a.ProcessedByAssessorTime,
                    a.SelectedAgentDrivingMap,
                    a.SelectedAgentDrivingDistance,
                    a.SelectedAgentDrivingDuration,
                    ServiceTypeName = a.PolicyDetail.InvestigationServiceType.Name
                })
                .ToListAsync();

            // 6. Memory Processing (Images, Formatting, Methods)

            var responseList = pagedData.Select(async a =>
            {
                var culture = CustomExtensions.GetCultureByCountry(companyUser.CountryCode.ToUpper());

                // Run file operations in parallel for this specific row
                var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDetail.DocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerTask = base64FileService.GetBase64FileAsync(a.CustomerDetail?.ImagePath, Applicationsettings.NO_USER);
                var beneficiaryTask = base64FileService.GetBase64FileAsync(a.BeneficiaryDetail?.ImagePath, Applicationsettings.NO_USER);
                var ownerDetailTask = base64FileService.GetBase64FileAsync(a.Vendor?.DocumentUrl, Applicationsettings.NO_USER);

                await Task.WhenAll(documentTask, customerTask, beneficiaryTask);
                return new CaseInvestigationResponse
                {
                    Id = a.Id,
                    AutoAllocated = a.IsAutoAllocated,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(culture, "{0:C}", a.PolicyDetail.SumAssuredValue),
                    Agent = a.Vendor?.Email,
                    Agency = a.Vendor?.Name,

                    // Helper Methods (Calculated in Memory)
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),

                    // Images (See Note Below regarding File.ReadAllBytes)
                    Document = await documentTask,
                    Customer = await customerTask,
                    BeneficiaryPhoto = await beneficiaryTask,
                    OwnerDetail = await ownerDetailTask,

                    Name = a.CustomerDetail?.Name ?? "N/A",
                    Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Status = a.ORIGIN.GetEnumDisplayName(),
                    ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.ServiceTypeName})",
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = CaseExtension.GetAssessorTime(a.investigation, false, true),
                    BeneficiaryName = a.BeneficiaryDetail.Name,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration,

                    TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime ?? DateTime.Now).TotalSeconds,
                    CanDownload = CanDownload(a.Id, userEmail)
                };
            }).ToList();

            return new
            {
                Draw = draw,
                RecordsTotal = recordsTotal,
                RecordsFiltered = recordsFiltered,
                Data = responseList
            };
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
        private async Task<string> GetOwnerImage(InvestigationTask caseTask)
        {
            var noDataImagefilePath = Path.Combine(env.WebRootPath, "img", "no-photo.jpg");

            if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR || caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                var agent =await context.Vendor.FirstOrDefaultAsync(u => u.VendorId == caseTask.VendorId);
                if (agent != null && !string.IsNullOrWhiteSpace(agent.DocumentUrl))
                {
                    return agent.DocumentUrl;
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                var vendor =await context.ApplicationUser.FirstOrDefaultAsync(v => v.Email == caseTask.TaskedAgentEmail);
                if (vendor != null && !string.IsNullOrWhiteSpace(vendor.ProfilePictureUrl))
                {
                    return vendor.ProfilePictureUrl;
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
                var company =await context.ClientCompany.FirstOrDefaultAsync(v => v.ClientCompanyId == caseTask.ClientCompanyId);
                if (company != null && !string.IsNullOrWhiteSpace(company.DocumentUrl))
                {
                    return company.DocumentUrl;
                }
            }
            return noDataImagefilePath;
        }
        private async Task<string> GetOwner(InvestigationTask caseTask)
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
                var company =await context.ClientCompany.FirstOrDefaultAsync(v => v.ClientCompanyId == caseTask.ClientCompanyId);
                if (company != null)
                {
                    return company.Email;
                }
            }
            return string.Empty;
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
    }
}
