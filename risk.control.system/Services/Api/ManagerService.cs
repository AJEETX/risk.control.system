using System.Globalization;

using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Api
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

            int totalRecords = await query.CountAsync(); // Get total count before pagination

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

            int recordsFiltered = await query.CountAsync();
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
                .Skip(start)
                .Take(length)
                .Select(a => new
                {
                    investigation = a,
                    CaseOwner = a.CaseOwner,
                    a.Id,
                    a.IsAutoAllocated,
                    PolicyNum = a.GetPolicyNum(a.PolicyDetail.ContractNumber),
                    InsuranceType = a.PolicyDetail.InsuranceType,
                    ServiceType = $"{a.PolicyDetail.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                    ServiceTypeName = a.PolicyDetail.InvestigationServiceType.Name,
                    ContractNumber = a.PolicyDetail.ContractNumber,
                    PolicyDocumentPath = a.PolicyDetail.DocumentPath,
                    SumAssuredValue = a.PolicyDetail.SumAssuredValue,
                    CustomerName = a.CustomerDetail.Name,
                    customerImagePath = a.CustomerDetail.ImagePath,
                    customerAddressline = a.CustomerDetail.Addressline,
                    customerDistrict = a.CustomerDetail.District.Name,
                    customerState = a.CustomerDetail.State.Name,
                    customerPincode = a.CustomerDetail.PinCode.Code,
                    BeneficiaryName = a.BeneficiaryDetail.Name,
                    beneficiaryImagePath = a.BeneficiaryDetail.ImagePath,
                    beneficiaryAddressline = a.BeneficiaryDetail.Addressline,
                    beneficiaryDistrict = a.BeneficiaryDetail.District.Name,
                    beneficiaryState = a.BeneficiaryDetail.State.Name,
                    beneficiaryPincode = a.BeneficiaryDetail.PinCode.Code,
                    a.Vendor,
                    VendorDocumentUrl = a.Vendor.DocumentUrl,
                    VendorName = a.Vendor.Name,
                    VendorEmail = a.Vendor.Email,
                    IsReady2Assign = a.IsReady2Assign,
                    a.SubStatus,
                    a.ORIGIN,
                    a.Created,
                    a.AssignedToAgency,
                    a.AllocatedToAgencyTime,
                    a.IsNewAssignedToManager,
                    a.ProcessedByAssessorTime,
                    a.SelectedAgentDrivingMap,
                    a.SelectedAgentDrivingDistance,
                    a.SelectedAgentDrivingDuration,
                    CustomerLocationMap = a.CustomerDetail.CustomerLocationMap,
                    BeneficiaryLocationMap = a.BeneficiaryDetail.BeneficiaryLocationMap
                }).ToListAsync();

            var transformedData = pagedList.Select(async a =>
            {
                var isUW = a.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = CustomExtensions.GetCultureByCountry(companyUser.Country.Code.ToUpper());
                var pincode = ClaimsInvestigationExtension.GetPincodeOfInterest(isUW, a.customerPincode, a.beneficiaryPincode);
                var customerAddress = a.customerAddressline + ',' + a.customerDistrict + ',' + a.customerState;
                var beneficiaryAddress = a.beneficiaryAddressline + ',' + a.beneficiaryDistrict + ',' + a.beneficiaryState;
                var pincodeName = isUW ? customerAddress : beneficiaryAddress;
                var customerName = a.CustomerName ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>";
                var beneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryName) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" : a.BeneficiaryName;
                var PersonMapAddressUrl = string.Format(a.investigation.GetMap(isUW, a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                                                          a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR), "400", "400");

                // Fetch files in parallel for this row
                var docTask = base64FileService.GetBase64FileAsync(a.PolicyDocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var custTask = base64FileService.GetBase64FileAsync(a.customerImagePath, Applicationsettings.NO_USER);
                var beneTask = base64FileService.GetBase64FileAsync(a.beneficiaryImagePath, Applicationsettings.NO_USER);
                var ownerImageTask = base64FileService.GetBase64FileAsync(await GetOwnerImage(a.investigation));
                var ownerDetailTask = GetOwner(a.investigation);
                await Task.WhenAll(docTask, custTask, beneTask, ownerImageTask, ownerDetailTask);
                return new
                {
                    Id = a.Id,
                    AutoAllocated = a.IsAutoAllocated,
                    CustomerFullName = a.CustomerName ?? string.Empty,
                    BeneficiaryFullName = a.BeneficiaryName ?? string.Empty,
                    PolicyId = a.ContractNumber,
                    Amount = string.Format(culture, "{0:c}", a.SumAssuredValue),
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
                    Policy = a.InsuranceType.GetEnumDisplayName(),
                    Status = a.ORIGIN.GetEnumDisplayName(),
                    SubStatus = a.SubStatus,
                    Ready2Assign = a.IsReady2Assign,
                    ServiceType = a.ServiceType,
                    Service = a.ServiceTypeName,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = GetManagerActiveTimePending(a.investigation),
                    PolicyNum = a.PolicyNum,
                    BeneficiaryPhoto = await beneTask,
                    BeneficiaryName = beneficiaryName,
                    TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).TotalSeconds,
                    IsNewAssigned = a.IsNewAssignedToManager,
                    PersonMapAddressUrl = PersonMapAddressUrl
                };
            });
            var data = await Task.WhenAll(transformedData);

            // Prepare Response
            var idsToMarkViewed = data.Where(x => x.IsNewAssigned).Select(x => x.Id).ToList();

            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = await context.Investigations
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
                    PolicyNum = a.GetPolicyNum(a.PolicyDetail.ContractNumber),
                    InsuranceType = a.PolicyDetail.InsuranceType,
                    ServiceTypeName = a.PolicyDetail.InvestigationServiceType.Name,
                    ContractNumber = a.PolicyDetail.ContractNumber,
                    PolicyDocumentPath = a.PolicyDetail.DocumentPath,
                    SumAssuredValue = a.PolicyDetail.SumAssuredValue,
                    CustomerName = a.CustomerDetail.Name,
                    customerImagePath = a.CustomerDetail.ImagePath,
                    customerAddressline = a.CustomerDetail.Addressline,
                    customerDistrict = a.CustomerDetail.District.Name,
                    customerState = a.CustomerDetail.State.Name,
                    customerPincode = a.CustomerDetail.PinCode.Code,
                    BeneficiaryName = a.BeneficiaryDetail.Name,
                    beneficiaryImagePath = a.BeneficiaryDetail.ImagePath,
                    beneficiaryAddressline = a.BeneficiaryDetail.Addressline,
                    beneficiaryDistrict = a.BeneficiaryDetail.District.Name,
                    beneficiaryState = a.BeneficiaryDetail.State.Name,
                    beneficiaryPincode = a.BeneficiaryDetail.PinCode.Code,
                    a.Vendor,
                    VendorDocumentUrl = a.Vendor.DocumentUrl,
                    VendorName = a.Vendor.Name,
                    VendorEmail = a.Vendor.Email,
                    a.SubStatus,
                    a.ORIGIN,
                    a.Created,
                    a.ProcessedByAssessorTime,
                    a.SelectedAgentDrivingMap,
                    a.SelectedAgentDrivingDistance,
                    a.SelectedAgentDrivingDuration,
                })
                .ToListAsync();

            // 6. Memory Processing (Images, Formatting, Methods)

            var finalDataTasks = pagedData.Select(async a =>
            {
                var culture = CustomExtensions.GetCultureByCountry(companyUser.CountryCode.ToUpper());
                var isUW = a.InsuranceType == InsuranceType.UNDERWRITING;
                var pincode = ClaimsInvestigationExtension.GetPincodeOfInterest(isUW, a.customerPincode, a.beneficiaryPincode);
                var customerAddress = a.customerAddressline + ',' + a.customerDistrict + ',' + a.customerState;
                var beneficiaryAddress = a.beneficiaryAddressline + ',' + a.beneficiaryDistrict + ',' + a.beneficiaryState;
                var pincodeName = isUW ? customerAddress : beneficiaryAddress;

                // Run file operations in parallel for this specific row
                var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerTask = base64FileService.GetBase64FileAsync(a.customerImagePath, Applicationsettings.NO_USER);
                var beneficiaryTask = base64FileService.GetBase64FileAsync(a.beneficiaryImagePath, Applicationsettings.NO_USER);
                var ownerDetailTask = base64FileService.GetBase64FileAsync(a.VendorDocumentUrl, Applicationsettings.NO_USER);

                await Task.WhenAll(documentTask, customerTask, beneficiaryTask);
                return new CaseInvestigationResponse
                {
                    PolicyNum = a.PolicyNum,
                    Id = a.Id,
                    AutoAllocated = a.IsAutoAllocated,
                    PolicyId = a.ContractNumber,
                    Amount = string.Format(culture, "{0:C}", a.SumAssuredValue),
                    Agent = a.VendorEmail,
                    Agency = a.VendorName,

                    // Helper Methods (Calculated in Memory)
                    Pincode = pincode,
                    PincodeName = pincodeName,

                    // Images (See Note Below regarding File.ReadAllBytes)
                    Document = await documentTask,
                    Customer = await customerTask,
                    BeneficiaryPhoto = await beneficiaryTask,
                    OwnerDetail = await ownerDetailTask,

                    Name = a.CustomerName ?? "N/A",
                    Policy = a.InsuranceType.GetEnumDisplayName(),
                    Status = a.ORIGIN.GetEnumDisplayName(),
                    ServiceType = $"{a.InsuranceType.GetEnumDisplayName()} ({a.ServiceTypeName})",
                    Service = a.ServiceTypeName,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = CaseExtension.GetAssessorTime(a.investigation, false, true),
                    BeneficiaryName = a.BeneficiaryName,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration,

                    TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime ?? DateTime.Now).TotalSeconds,
                    CanDownload = CanDownload(a.Id, userEmail)
                };
            });
            var finalData = (await Task.WhenAll(finalDataTasks));

            return new
            {
                Draw = draw,
                RecordsTotal = recordsTotal,
                RecordsFiltered = recordsFiltered,
                Data = finalData.ToArray()
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
                var agent = await context.Vendor.FirstOrDefaultAsync(u => u.VendorId == caseTask.VendorId);
                if (agent != null && !string.IsNullOrWhiteSpace(agent.DocumentUrl))
                {
                    return agent.DocumentUrl;
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                var vendor = await context.ApplicationUser.FirstOrDefaultAsync(v => v.Email == caseTask.TaskedAgentEmail);
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
                var company = await context.ClientCompany.FirstOrDefaultAsync(v => v.ClientCompanyId == caseTask.ClientCompanyId);
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
                var company = await context.ClientCompany.FirstOrDefaultAsync(v => v.ClientCompanyId == caseTask.ClientCompanyId);
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