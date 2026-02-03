using System.Globalization;

using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Api
{
    public interface IInvestigationService
    {
        Task<int> GetAutoCount(string currentUserEmail);

        Task<object> GetAuto(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");

        Task<object> GetActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");

        Task<FilesDataResponse> GetFilesData(string userEmail, bool isManager, int draw, int start, int length, int orderColumn, string orderDir, int uploadId = 0, string searchTerm = null);

        Task<(object, bool)> GetFileById(string userEmail, bool isManager, int uploadId);
    }

    internal class InvestigationService : IInvestigationService
    {
        private readonly ApplicationDbContext context;
        private readonly IBase64FileService base64FileService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public InvestigationService(ApplicationDbContext context,
            IBase64FileService base64FileService,
            IWebHostEnvironment webHostEnvironment)
        {
            this.context = context;
            this.base64FileService = base64FileService;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task<object> GetAuto(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var companyUser = await context.ApplicationUser.Include(c => c.Country).Include(c => c.ClientCompany)
                .Where(c => c.Email == currentUserEmail)
                .FirstOrDefaultAsync();

            var query = context.Investigations.Where(a => !a.Deleted && a.ClientCompanyId == companyUser.ClientCompanyId && a.CreatedUser == currentUserEmail);

            query = query.Where(a => CONSTANTS.CreatedAndDraftStatuses.Contains(a.SubStatus));

            int totalRecords = await query.CountAsync();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower(CultureInfo.InvariantCulture);
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.ToLower().Contains(search) ||
                    a.PolicyDetail.CauseOfLoss.ToLower().Contains(search) ||
                    a.PolicyDetail.InvestigationServiceType.Name.ToLower().Contains(search) ||
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

            // 4. Apply Sorting (In SQL)
            bool isAsc = string.Equals(orderDir, "asc", StringComparison.OrdinalIgnoreCase);
            query = orderColumn switch
            {
                1 => isAsc ? query.OrderBy(a => a.PolicyDetail.ContractNumber) : query.OrderByDescending(a => a.PolicyDetail.ContractNumber),
                2 => isAsc ? query.OrderBy(a => (double)a.PolicyDetail.SumAssuredValue) : query.OrderByDescending(a => (double)a.PolicyDetail.SumAssuredValue),
                3 => isAsc
                    ? query.OrderBy(a =>
                        a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                            ? a.CustomerDetail.PinCode.Code
                            : a.BeneficiaryDetail.PinCode.Code)
                    : query.OrderByDescending(a =>
                        a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                            ? a.CustomerDetail.PinCode.Code
                            : a.BeneficiaryDetail.PinCode.Code),
                4 => isAsc ? query.OrderBy(a => a.ClientCompany.Name) : query.OrderByDescending(a => a.ClientCompany.Name),
                5 => isAsc ? query.OrderBy(a => a.PolicyDetail.InsuranceType) : query.OrderByDescending(a => a.PolicyDetail.InsuranceType),
                6 => isAsc ? query.OrderBy(a => a.CustomerDetail.Name) : query.OrderByDescending(a => a.CustomerDetail.Name),
                7 => isAsc ? query.OrderBy(a => a.Status) : query.OrderByDescending(a => a.Status),
                8 => isAsc ? query.OrderBy(a => a.BeneficiaryDetail.Name) : query.OrderByDescending(a => a.BeneficiaryDetail.Name),
                9 => isAsc ? query.OrderBy(a => a.PolicyDetail.InvestigationServiceType.Name) : query.OrderByDescending(a => a.PolicyDetail.InvestigationServiceType.Name),
                10 => isAsc ? query.OrderBy(a => a.ORIGIN) : query.OrderByDescending(a => a.ORIGIN),
                11 => isAsc ? query.OrderBy(a => a.Created) : query.OrderByDescending(a => a.Created),
                12 => isAsc ? query.OrderBy(a => a.Updated) : query.OrderByDescending(a => a.Updated),
                // Default fallback (Usually ID or Created Date)
                _ => isAsc ? query.OrderByDescending(a => a.Id) : query.OrderBy(a => a.Id)
            };
            // 5. Paginate and Project (Only fetch what you need)
            var pagedRawData = await query
                .Skip(start)
                .Take(length)
                .Select(a => new
                {
                    investigation = a,
                    a.Id,
                    policyId = a.PolicyDetail.ContractNumber,
                    PolicyNum = a.GetPolicyNum(a.PolicyDetail.ContractNumber),
                    InsuranceType = a.PolicyDetail.InsuranceType,
                    ServiceTypeName = a.PolicyDetail.InvestigationServiceType.Name,
                    ContractNumber = a.PolicyDetail.ContractNumber,
                    PolicyDocumentPath = a.PolicyDetail.DocumentPath,
                    SumAssuredValue = a.PolicyDetail.SumAssuredValue,
                    a.IsNew,
                    a.IsReady2Assign,
                    a.IsUploaded,
                    a.SubStatus,
                    a.Created,
                    a.Updated,
                    a.ORIGIN,
                    CustomerName = a.CustomerDetail != null ? a.CustomerDetail.Name : null,
                    customerImagePath = a.CustomerDetail != null ? a.CustomerDetail.ImagePath : Applicationsettings.NO_USER,
                    CustomerLocationMap = a.CustomerDetail.CustomerLocationMap,
                    customerAddressline = a.CustomerDetail != null ? a.CustomerDetail.Addressline : string.Empty,
                    customerDistrict = a.CustomerDetail != null ? a.CustomerDetail.District.Name : string.Empty,
                    customerState = a.CustomerDetail != null ? a.CustomerDetail.State.Name : string.Empty,
                    customerPincode = a.CustomerDetail != null ? a.CustomerDetail.PinCode.Code : 0,
                    BeneficiaryName = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.Name : null,
                    beneficiaryImagePath = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.ImagePath : Applicationsettings.NO_USER,
                    BeneficiaryLocationMap = a.BeneficiaryDetail.BeneficiaryLocationMap,
                    beneficiaryAddressline = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.Addressline : string.Empty,
                    beneficiaryDistrict = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.District.Name : string.Empty,
                    beneficiaryState = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.State.Name : string.Empty,
                    beneficiaryPincode = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.PinCode.Code : 0,
                }).ToListAsync();

            int recordsFiltered = await query.CountAsync();

            var finalDataTasks = pagedRawData.Select(async i =>
            {
                var isUW = i.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = CustomExtensions.GetCultureByCountry(companyUser.Country.Code.ToUpper());

                var policyId = i.policyId;
                var amount = string.Format(culture, "{0:C}", i.SumAssuredValue);
                var pincodeCode = isUW ? i.customerPincode : i.beneficiaryPincode;
                var customerAddress = i.IsReady2Assign ? i.customerAddressline + ',' + i.customerDistrict + ',' + i.customerState : null;
                var beneficiaryAddress = i.IsReady2Assign ? i.beneficiaryAddressline + ',' + i.beneficiaryDistrict + ',' + i.beneficiaryState : null;
                var address = isUW ? customerAddress : beneficiaryAddress;
                var pincodeName = i.IsReady2Assign ? (isUW ? customerAddress : beneficiaryAddress) : null;
                var customerName = string.IsNullOrWhiteSpace(i.CustomerName) ? "<span class=\"badge badge-light\">customer name</span>" : i.CustomerName;
                var policyName = i.InsuranceType.GetEnumDisplayName();
                var Origin = i.ORIGIN.GetEnumDisplayName();
                var ready2Assign = i.IsReady2Assign;
                var investigationService = i.ServiceTypeName;
                var serviceType = $"{i.InsuranceType.GetEnumDisplayName()} ({investigationService})";
                var timePending = GetDraftedTimePending(i.investigation);
                var policyNumber = i.PolicyNum;
                var beneficiaryName = string.IsNullOrWhiteSpace(i.BeneficiaryName) ?
                    "<span class=\"badge badge-light\">beneficiary name</span>" : i.BeneficiaryName;
                var timeElapsed = DateTime.Now.Subtract(i.Updated.GetValueOrDefault()).TotalSeconds;
                var personMapAddressUrl = pincodeName == null ? Applicationsettings.NO_MAP : (isUW ? i.CustomerLocationMap : i.BeneficiaryLocationMap);

                // Run file operations in parallel for this specific row
                var documentTask = base64FileService.GetBase64FileAsync(i.PolicyDocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerTask = base64FileService.GetBase64FileAsync(i.customerImagePath, Applicationsettings.NO_USER);
                var beneficiaryTask = base64FileService.GetBase64FileAsync(i.beneficiaryImagePath, Applicationsettings.NO_USER);

                await Task.WhenAll(documentTask, customerTask, beneficiaryTask);
                return new CaseAutoAllocationResponse
                {
                    Id = i.Id,
                    IsNew = i.IsNew,
                    Amount = amount,
                    PolicyId = policyId,
                    PolicyNum = i.PolicyNum,
                    AssignedToAgency = i.IsNew,
                    PincodeCode = pincodeCode,
                    Document = await documentTask,
                    Customer = await customerTask,
                    Name = customerName,
                    Policy = policyName,
                    IsUploaded = i.IsUploaded,
                    Origin = Origin,
                    SubStatus = i.SubStatus,
                    Ready2Assign = ready2Assign,
                    Service = investigationService,
                    Location = i.ORIGIN.GetEnumDisplayName(),
                    Created = i.Created.ToString("dd-MM-yyyy"),
                    ServiceType = serviceType,
                    TimePending = timePending,
                    BeneficiaryPhoto = await beneficiaryTask,
                    BeneficiaryName = beneficiaryName,
                    TimeElapsed = timeElapsed,
                    BeneficiaryFullName = i.BeneficiaryName ?? "?",
                    CustomerFullName = i.CustomerName ?? "?",
                    PersonMapAddressUrl = personMapAddressUrl
                };
            });

            // 2. Await all rows
            var finalData = (await Task.WhenAll(finalDataTasks));

            // 7. Bulk Update "IsNew" (Fastest way)
            var idsToUpdate = finalData.Where(x => x.IsNew).Select(x => x.Id).ToList();

            if (idsToUpdate.Any())
            {
                await context.Investigations
                    .Where(x => idsToUpdate.Contains(x.Id))

                    .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.IsNew, false));
            }

            return new
            {
                draw,
                AutoAllocatopn = companyUser.ClientCompany.AutoAllocation,
                recordsTotal = totalRecords,
                recordsFiltered,
                data = finalData
            };
        }

        public async Task<object> GetActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var companyUser = await context.ApplicationUser
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            var query = context.Investigations.Where(a => !a.Deleted && a.CreatedUser == currentUserEmail);

            query = query.Where(q => q.Status == CONSTANTS.CASE_STATUS.INPROGRESS && !CONSTANTS.ActiveSubStatuses.Contains(q.SubStatus));

            int totalRecords = query.Count(); // Get total count before pagination
                                              // Search filtering
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
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
                .Skip(start).Take(length)
                .Select(a => new
                {
                    investigation = a,
                    a.Id,
                    policyId = a.PolicyDetail.ContractNumber,
                    PolicyNum = a.GetPolicyNum(a.PolicyDetail.ContractNumber),
                    InsuranceType = a.PolicyDetail.InsuranceType,
                    ServiceTypeName = a.PolicyDetail.InvestigationServiceType.Name,
                    serviceType = $"{a.PolicyDetail.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                    ContractNumber = a.PolicyDetail.ContractNumber,
                    PolicyDocumentPath = a.PolicyDetail.DocumentPath,
                    SumAssuredValue = a.PolicyDetail.SumAssuredValue,
                    a.IsNew,
                    a.IsAutoAllocated,
                    a.AssignedToAgency,
                    a.IsUploaded,
                    a.SubStatus,
                    a.Created,
                    a.Updated,
                    a.ORIGIN,
                    a.CaseOwner,
                    CustomerName = a.CustomerDetail.Name,
                    customerImagePath = a.CustomerDetail.ImagePath,
                    customerAddressline = a.CustomerDetail.Addressline,
                    customerDistrict = a.CustomerDetail.District.Name,
                    customerState = a.CustomerDetail.State.Name,
                    customerPincode = a.CustomerDetail.PinCode.Code,
                    a.CustomerDetail.CustomerLocationMap,
                    BeneficiaryName = a.BeneficiaryDetail.Name,
                    beneficiaryImagePath = a.BeneficiaryDetail.ImagePath,
                    beneficiaryAddressline = a.BeneficiaryDetail.Addressline,
                    beneficiaryDistrict = a.BeneficiaryDetail.District.Name,
                    beneficiaryState = a.BeneficiaryDetail.State.Name,
                    beneficiaryPincode = a.BeneficiaryDetail.PinCode.Code,
                    a.BeneficiaryDetail.BeneficiaryLocationMap
                }).ToListAsync();

            // 6. Transform & Async File I/O
            var finalTasks = pagedList.Select(async a =>
            {
                var isUW = a.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = CustomExtensions.GetCultureByCountry(companyUser.Country.Code.ToUpper());
                var policyNumber = a.PolicyNum;
                var investigationService = a.ServiceTypeName;
                var serviceType = a.serviceType;
                var personMapAddressUrl = isUW ? string.Format(a.CustomerLocationMap, "400", "400") : string.Format(a.BeneficiaryLocationMap, "400", "400");
                var pincode = ClaimsInvestigationExtension.GetPincodeOfInterest(isUW, a.customerPincode, a.beneficiaryPincode);
                var customerAddress = a.customerAddressline + ',' + a.customerDistrict + ',' + a.customerState;
                var beneficiaryAddress = a.beneficiaryAddressline + ',' + a.beneficiaryDistrict + ',' + a.beneficiaryState;
                var pincodeName = isUW ? customerAddress : beneficiaryAddress;
                var customerName = a.CustomerName ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>";
                var beneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryName) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" : a.BeneficiaryName;

                // Fetch files in parallel for this row
                var docTask = base64FileService.GetBase64FileAsync(a.PolicyDocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var custTask = base64FileService.GetBase64FileAsync(a.customerImagePath, Applicationsettings.NO_USER);
                var beneTask = base64FileService.GetBase64FileAsync(a.beneficiaryImagePath, Applicationsettings.NO_USER);
                var ownerImageTask = GetOwnerImage(a.Id);
                var ownerDetailTask = GetOwner(a.Id);

                await Task.WhenAll(docTask, custTask, beneTask, ownerImageTask, ownerDetailTask);
                return new ActiveCaseResponse
                {
                    PolicyNum = policyNumber,
                    AutoAllocated = a.IsAutoAllocated,
                    Id = a.Id,
                    IsNew = a.IsNew,
                    PolicyId = a.policyId,
                    Amount = string.Format(culture, "{0:c}", a.SumAssuredValue),
                    CustomerFullName = a.CustomerName ?? "",
                    BeneficiaryFullName = a.BeneficiaryName ?? "",
                    Document = await docTask,
                    Customer = await custTask,
                    AssignedToAgency = a.AssignedToAgency,
                    Agent = await ownerDetailTask,
                    OwnerDetail = await ownerImageTask,
                    CaseWithPerson = a.CaseOwner,
                    BeneficiaryPhoto = await beneTask,
                    SubStatus = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    Location = a.SubStatus,
                    TimePending = a.investigation.GetCreatorTimePending(),
                    TimeElapsed = DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).TotalSeconds,
                    Service = investigationService,
                    ServiceType = serviceType,
                    PersonMapAddressUrl = personMapAddressUrl,
                    Pincode = pincode,
                    PincodeName = pincodeName,
                    Name = customerName,
                    BeneficiaryName = beneficiaryName,
                    Policy = a.InsuranceType.GetEnumDisplayName(),
                    Status = a.ORIGIN.GetEnumDisplayName(),
                };
            });

            var data = await Task.WhenAll(finalTasks);

            // 7. Bulk Update Viewed Status
            var idsToUpdate = data.Where(x => x.IsNew).Select(x => x.Id).ToList();
            if (idsToUpdate.Any())
            {
                await context.Investigations
                    .Where(x => idsToUpdate.Contains(x.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsNew, false));
            }

            return new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered,
                data
            };
        }

        public async Task<int> GetAutoCount(string currentUserEmail)
        {
            var companyUser = await context.ApplicationUser.Include(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            if (companyUser == null)
                return 0;
            var company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            // Fetching all relevant substatuses in a single query for efficiency
            var subStatuses = new[]
                {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                };

            var query = context.Investigations.Where(a => !a.Deleted && a.ClientCompanyId == companyUser.ClientCompanyId && subStatuses.Contains(a.SubStatus));

            return await query.CountAsync(); // Get total count before pagination
        }

        public async Task<FilesDataResponse> GetFilesData(string userEmail, bool isManager, int draw, int start, int length, int orderColumn, string orderDir, int uploadId = 0, string searchTerm = null)
        {
            var companyUser = await context.ApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);

            var query = context.FilesOnFileSystem.AsNoTracking().Where(f => f.CompanyId == companyUser.ClientCompanyId && !f.Deleted);

            var totalReadyToAssign = await GetAutoCount(userEmail);
            var maxAssignReadyAllowedByCompany = companyUser.ClientCompany.TotalToAssignMaxAllowed;

            if (uploadId > 0)
            {
                var file = await context.FilesOnFileSystem.Include(c => c.CaseIds).FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
                if (file == null)
                {
                    return (null!);
                }
                if (file.CaseIds != null && file.CaseIds.Count > 0)
                {
                    totalReadyToAssign += file.CaseIds.Count;
                }
            }

            if (!isManager)
                query = query.Where(f => f.UploadedBy == userEmail);

            int recordsTotal = await query.CountAsync();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(f =>
                    f.UserSequenceNumber.ToString().Contains(searchTerm) ||
                    f.CompanySequenceNumber.ToString().Contains(searchTerm) ||
                    f.Name.ToLower().Contains(searchTerm) ||
                    f.Description.ToLower().Contains(searchTerm) ||
                    f.Status.ToLower().Contains(searchTerm) ||
                    f.Message.ToLower().Contains(searchTerm) ||
                    f.UploadedBy.ToLower().Contains(searchTerm));
            }

            int recordsFiltered = await query.CountAsync();
            bool asc = orderDir == "asc";

            // ✅ ONLY SQL-SAFE SORTING
            query = orderColumn switch
            {
                1 => asc ? query.OrderBy(f => f.UserSequenceNumber)
                         : query.OrderByDescending(f => f.UserSequenceNumber),

                2 => asc ? query.OrderBy(f => f.Status)
                         : query.OrderByDescending(f => f.Status),

                3 => asc ? query.OrderBy(f => f.Name)
                        : query.OrderByDescending(f => f.Name),

                5 => asc ? query.OrderBy(f => f.CreatedOn)
                         : query.OrderByDescending(f => f.CreatedOn),

                6 => asc
                        ? query.OrderBy(f => f.TimeTakenSeconds)
                        : query.OrderByDescending(f => f.TimeTakenSeconds),

                7 => asc ? query.OrderBy(f => f.DirectAssign)
                         : query.OrderByDescending(f => f.DirectAssign),

                8 => asc ? query.OrderBy(f => f.Message)
                        : query.OrderByDescending(f => f.Message),

                _ => query.OrderByDescending(f => f.CreatedOn)
            };
            var page = await query
                .Skip(start)
                .Take(length)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Description,
                    f.FileType,
                    f.CreatedOn,
                    f.UploadedBy,
                    f.Status,
                    f.Message,
                    f.Icon,
                    f.RecordCount,
                    f.Completed,
                    f.DirectAssign,
                    f.CompanySequenceNumber,
                    f.UserSequenceNumber,
                    f.CompletedOn,
                    f.ErrorByteData,
                    f.TimeTakenSeconds
                }).ToListAsync();

            var data = page.Select(file => new
            {
                file.Id,
                SequenceNumber = isManager ? file.CompanySequenceNumber : file.UserSequenceNumber,
                file.Name,
                file.Description,
                file.FileType,
                CreatedOn = file.CreatedOn.ToString("dd-MMM-yyyy HH:mm:ss"),
                file.UploadedBy,
                Status = file.Status,
                file.Message,
                //Message = file.Message == "Upload In progress" ? file.Icon : file.Message,
                Icon = file.Icon, // or use some other status representation
                IsManager = isManager,
                file.RecordCount,
                file.Completed,
                file.DirectAssign,
                hasError = (file.CompletedOn != null && file.ErrorByteData != null) ? true : false,
                UploadedType = file.DirectAssign ? "<i class='fas fa-random i-assign'></i>" : "<i class='fas fa-upload i-upload'></i>",
                file.TimeTakenSeconds,
                TimeTaken = file.TimeTakenSeconds > 0 ? $" {file.TimeTakenSeconds} sec" : "<i class='fas fa-sync fa-spin i-grey'></i>",
            }).ToList();
            return new FilesDataResponse
            {
                Draw = draw,
                RecordsTotal = recordsTotal,
                RecordsFiltered = recordsFiltered,
                MaxAssignReadyAllowed = companyUser.ClientCompany.TotalCreatedClaimAllowed >= totalReadyToAssign,
                Data = data
            };
        }

        public async Task<(object, bool)> GetFileById(string userEmail, bool isManager, int uploadId)
        {
            var companyUser = await context.ApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
            var file = await context.FilesOnFileSystem.Include(c => c.CaseIds).FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
            if (file == null)
            {
                return (null!, false);
            }
            var totalReadyToAssign = await GetAutoCount(userEmail);
            var totalForAssign = totalReadyToAssign + file.CaseIds?.Count;
            var maxAssignReadyAllowedByCompany = companyUser.ClientCompany.TotalToAssignMaxAllowed;

            var result = new
            {
                file.Id,
                SequenceNumber = isManager ? file.CompanySequenceNumber : file.UserSequenceNumber,
                file.Name,
                file.Description,
                file.FileType,
                CreatedOn = file.CreatedOn.ToString("dd-MMM-yyyy HH:mm:ss"),
                file.UploadedBy,
                Status = file.Status,
                file.Completed,
                Message = file.Message,
                Icon = file.Icon, // or use some other status representation
                IsManager = isManager,
                file.RecordCount,
                file.DirectAssign,
                hasError = (file.CompletedOn != null && file.ErrorByteData != null) ? true : false,
                UploadedType = file.DirectAssign ? "<i class='fas fa-random i-assign'></i>" : "<i class='fas fa-upload i-upload'></i>",
                file.TimeTakenSeconds,
                TimeTaken = file.TimeTakenSeconds > 0 ? $" {file.TimeTakenSeconds} sec" : "<i class='fas fa-sync fa-spin i-grey'></i>",
            };//<i class='fas fa-sync fa-spin'></i>
            return (new { result }, maxAssignReadyAllowedByCompany >= totalForAssign);
        }

        private static string GetDraftedTimePending(InvestigationTask a)
        {
            if (a.CreatorSla == 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days} days since created!\"></i>");
            }
            if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days} day</span>");
            else if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days >= 3 || DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days} day</span>");
            if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days} day</span>");

            if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Hours < 24 &&
                DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Hours == 0 && DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Minutes == 0 && DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

        private async Task<string> GetOwnerImage(long id)
        {
            string base64StringImage;
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");
            var caseTask = await context.Investigations.FirstOrDefaultAsync(c => c.Id == id);
            if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR || caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                var agencyUser = await context.Vendor.FirstOrDefaultAsync(u => u.VendorId == caseTask.VendorId);
                if (agencyUser != null && !string.IsNullOrWhiteSpace(agencyUser.DocumentUrl))
                {
                    var agentImagePath = Path.Combine(webHostEnvironment.ContentRootPath, agencyUser.DocumentUrl);
                    var agentImageByte = await System.IO.File.ReadAllBytesAsync(agentImagePath);
                    return string.Format("data:image/*;base64,{0}", Convert.ToBase64String(agentImageByte));
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                var agent = await context.ApplicationUser.FirstOrDefaultAsync(v => v.Email == caseTask.TaskedAgentEmail);
                if (agent != null && !string.IsNullOrWhiteSpace(agent.ProfilePictureUrl))
                {
                    var agentImagePath = Path.Combine(webHostEnvironment.ContentRootPath, agent.ProfilePictureUrl);
                    var agentImageByte = await File.ReadAllBytesAsync(agentImagePath);
                    return string.Format("data:image/*;base64,{0}", Convert.ToBase64String(agentImageByte));
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
                    var companyImagePath = Path.Combine(webHostEnvironment.ContentRootPath, company.DocumentUrl);
                    var companyImageByte = await File.ReadAllBytesAsync(companyImagePath);
                    return string.Format("data:image/*;base64,{0}", Convert.ToBase64String(companyImageByte));
                }
            }
            return noDataImagefilePath;
        }

        private async Task<string> GetOwner(long caseId)
        {
            var caseTask = await context.Investigations.FirstOrDefaultAsync(c => c.Id == caseId);

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
    }
}