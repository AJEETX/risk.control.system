using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services
{
    public interface IAssessorService
    {
        Task<object> GetInvestigationReports(string userEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");

        Task<object> GetReviews(string userEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");

        Task<object> GetApprovededCases(string userEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");

        Task<object> GetRejectedCases(string userEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
    }

    public class AssessorService : IAssessorService
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;
        private readonly IBase64FileService base64FileService;

        public AssessorService(ApplicationDbContext context, IWebHostEnvironment env, IBase64FileService base64FileService)
        {
            this.context = context;
            this.env = env;
            this.base64FileService = base64FileService;
        }

        public async Task<object> GetApprovededCases(string userEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;

            var approvedCases = await GetCompletedCases(userEmail, approvedStatus, draw, start, length, search, caseType, orderColumn, orderDir);

            return approvedCases;
        }

        public async Task<object> GetInvestigationReports(string userEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
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
                            i.Status != CONSTANTS.CASE_STATUS.FINISHED &&
                            (i.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                            i.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR));

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
            var pagedRawData = await query
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
                    a.IsNewSubmittedToCompany
                })
                .ToListAsync();

            // 6. Memory Processing (Images, Formatting, Methods)

            var finalDataTasks = pagedRawData.Select(async a =>
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
                    Id = a.Id,
                    AutoAllocated = a.IsAutoAllocated,
                    PolicyId = a.ContractNumber,
                    PolicyNum = a.PolicyNum,
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
                    timePending = CaseExtension.GetAssessorTimePending(a.investigation, true),
                    BeneficiaryName = a.BeneficiaryName,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration,
                    IsNewSubmittedToCompany = a.IsNewSubmittedToCompany,
                    TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime ?? DateTime.Now).TotalSeconds,
                    CanDownload = CanDownload(a.Id, userEmail)
                };
            });

            var finalData = (await Task.WhenAll(finalDataTasks));

            var idsToUpdate = finalData.Where(x => x.IsNewSubmittedToCompany).Select(x => x.Id).ToList();
            if (idsToUpdate.Any())
            {
                await context.Investigations
                    .Where(x => idsToUpdate.Contains(x.Id))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.IsNew, false));
            }
            return new
            {
                Draw = draw,
                RecordsTotal = recordsTotal,
                RecordsFiltered = recordsFiltered,
                Data = finalData
            };
        }

        public async Task<object> GetReviews(string userEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var companyUser = await context.ApplicationUser
                 .Include(c => c.Country)
                 .FirstOrDefaultAsync(c => c.Email == userEmail);

            var query = context.Investigations
                .AsNoTracking()
                .Where(i => !i.Deleted &&
                            i.ClientCompanyId == companyUser.ClientCompanyId &&
                            i.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                            i.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR &&
                            i.IsQueryCase &&
                            i.RequestedAssessordEmail == userEmail);

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
                    a.IsNewSubmittedToCompany,
                    a.AssignedToAgency,
                    a.IsReady2Assign,
                })
                .AsNoTracking()
                .ToListAsync();

            var finalDataTasks = pagedData
                .Select(async a =>
                {
                    var culture = CustomExtensions.GetCultureByCountry(companyUser.Country.Code.ToUpper());
                    var isUW = a.InsuranceType == InsuranceType.UNDERWRITING;
                    var pincode = ClaimsInvestigationExtension.GetPincodeOfInterest(isUW, a.customerPincode, a.beneficiaryPincode);
                    var customerAddress = a.customerAddressline + ',' + a.customerDistrict + ',' + a.customerState;
                    var beneficiaryAddress = a.beneficiaryAddressline + ',' + a.beneficiaryDistrict + ',' + a.beneficiaryState;
                    var pincodeName = isUW ? customerAddress : beneficiaryAddress;

                    var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                    var customerTask = base64FileService.GetBase64FileAsync(a.customerImagePath, Applicationsettings.NO_USER);
                    var beneficiaryTask = base64FileService.GetBase64FileAsync(a.beneficiaryImagePath, Applicationsettings.NO_USER);
                    var ownerDetailTask = base64FileService.GetBase64FileAsync(a.VendorDocumentUrl, Applicationsettings.NO_USER);

                    return new CaseInvestigationResponse
                    {
                        Id = a.Id,
                        AutoAllocated = a.IsAutoAllocated,
                        CustomerFullName = a.CustomerName,
                        BeneficiaryFullName = a.BeneficiaryName ?? "",
                        PolicyId = a.ContractNumber,
                        Amount = string.Format(CustomExtensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.SumAssuredValue),
                        AssignedToAgency = a.AssignedToAgency,
                        Agent = a.VendorEmail,
                        OwnerDetail = await ownerDetailTask,
                        Pincode = pincode,
                        PincodeName = pincodeName,
                        Document = await documentTask,
                        Customer = await customerTask,
                        Name = a.CustomerName ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                        Policy = a.InsuranceType.GetEnumDisplayName(),
                        Status = a.ORIGIN.GetEnumDisplayName(),
                        SubStatus = a.SubStatus,
                        Ready2Assign = a.IsReady2Assign,
                        ServiceType = $"{a.InsuranceType.GetEnumDisplayName()} ({a.ServiceTypeName})",
                        Service = a.ServiceTypeName,
                        Location = a.SubStatus,
                        Created = a.Created.ToString("dd-MM-yyyy"),
                        timePending = a.investigation.GetAssessorTimePending(false, false, a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR, a.investigation.IsQueryCase),
                        Withdrawable = false,
                        PolicyNum = a.PolicyNum,
                        BeneficiaryPhoto = await beneficiaryTask,
                        BeneficiaryName = a.BeneficiaryName,
                        TimeElapsed = DateTime.Now.Subtract(a.investigation.EnquiredByAssessorTime ?? DateTime.Now).TotalSeconds,
                        PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                        Distance = a.SelectedAgentDrivingDistance,
                        Duration = a.SelectedAgentDrivingDuration
                    };
                });

            var finalData = (await Task.WhenAll(finalDataTasks));

            return new
            {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                Data = finalData
            };
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
                            i.SubmittedAssessordEmail == userEmail &&
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
                    Id = a.Id,
                    AutoAllocated = a.IsAutoAllocated,
                    PolicyNum = a.PolicyNum,
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
                Data = finalData
            };
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
    }
}