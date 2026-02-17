using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Api
{
    public interface IAgencyInvestigationService
    {
        Task<DataTableResponse<CaseInvestigationAgencyResponse>> GetNewCases(string userEmail, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc");

        Task<DataTableResponse<CaseInvestigationResponse>> GetOpenCases(string userEmail, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc");

        Task<DataTableResponse<CaseInvestigationAgencyResponse>> GetAgentReports(string userEmail, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc");

        Task<DataTableResponse<CaseInvestigationAgencyResponse>> GetCompletedCases(string userEmail, string userClaim, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc");
    }

    internal class AgencyInvestigationService : IAgencyInvestigationService
    {
        private readonly ApplicationDbContext context;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IBase64FileService base64FileService;
        private readonly IWeatherInfoService weatherInfoService;
        private readonly IWebHostEnvironment env;

        public AgencyInvestigationService(ApplicationDbContext context,
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IBase64FileService base64FileService,
            IWeatherInfoService weatherInfoService,
            IWebHostEnvironment env)
        {
            this.context = context;
            this._contextFactory = contextFactory;
            this.base64FileService = base64FileService;
            this.weatherInfoService = weatherInfoService;
            this.env = env;
        }

        public async Task<DataTableResponse<CaseInvestigationAgencyResponse>> GetNewCases(string userEmail, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc")
        {
            var vendorUser = await context.ApplicationUser
                .AsNoTracking()
                .Include(v => v.Country)
                .FirstOrDefaultAsync(x => x.Email == userEmail);

            if (vendorUser == null)
            {
                return new DataTableResponse<CaseInvestigationAgencyResponse>
                {
                    Draw = draw,
                    Data = new(),
                    RecordsTotal = 0,
                    RecordsFiltered = 0
                };
            }

            // -----------------------------
            // BASE QUERY
            // -----------------------------
            var query = context.Investigations
                .AsNoTracking()
                .Where(a =>
                    a.VendorId == vendorUser.VendorId &&
                    (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR ||
                     a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR));

            int recordsTotal = await query.CountAsync();

            // -----------------------------
            // SEARCH (SQL SAFE)
            // -----------------------------
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.Contains(search) ||
                    a.ClientCompany.Name.Contains(search) ||
                     (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name.Contains(search) : a.BeneficiaryDetail.Name.Contains(search)) ||
                     (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.PinCode.Code.ToString().Contains(search) : a.BeneficiaryDetail.PinCode.ToString().Contains(search)) ||
                     (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.PinCode.Name.ToLower().ToString().Contains(search) : a.BeneficiaryDetail.PinCode.Name.ToLower().Contains(search)) ||
                     (a.PolicyDetail.InvestigationServiceType.Name.Contains(search)) ||
                    a.PolicyDetail.InsuranceType.GetEnumDisplayName().ToLower().Contains(search));
            }

            int recordsFiltered = await query.CountAsync();

            // -----------------------------
            // SORTING (SQL ONLY)
            // -----------------------------
            bool asc = orderDir == "asc";

            query = orderColumn switch
            {
                1 => asc ? query.OrderBy(x => x.PolicyDetail.ContractNumber) : query.OrderByDescending(x => x.PolicyDetail.ContractNumber),
                2 => asc ? query.OrderBy(x => (double)x.PolicyDetail.SumAssuredValue)
                         : query.OrderByDescending(x => (double)x.PolicyDetail.SumAssuredValue),
                3 => asc ? query.OrderBy(x => x.ClientCompany.Name)
                         : query.OrderByDescending(x => x.ClientCompany.Name),
                4 => asc ? query.OrderBy(x => (x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING) ? x.CustomerDetail.PinCode.Code : x.BeneficiaryDetail.PinCode.Code)
                            : query.OrderByDescending(x => (x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING) ? x.CustomerDetail.PinCode.Code : x.BeneficiaryDetail.PinCode.Code),
                7 => asc
                        ? query.OrderBy(x =>
                            x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                                ? x.CustomerDetail.Name
                                : x.BeneficiaryDetail.Name)
                        : query.OrderByDescending(x =>
                            x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                                ? x.CustomerDetail.PinCode.Code
                                : x.BeneficiaryDetail.PinCode.Code),
                8 => asc ? query.OrderBy(x => x.PolicyDetail.InsuranceType.GetEnumDisplayName())
                                                         : query.OrderByDescending(x => x.PolicyDetail.InsuranceType.GetEnumDisplayName()),
                9 => asc ? query.OrderBy(x => x.PolicyDetail.InvestigationServiceType.Name)
                                         : query.OrderByDescending(x => x.PolicyDetail.InvestigationServiceType.Name),
                11 => asc ? query.OrderBy(x => x.Created)
                : query.OrderByDescending(x => x.Created),
                12 => asc
                        ? query.OrderBy(a =>
                            a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR
                                ? a.AllocatedToAgencyTime
                                : a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
                                    ? a.ReviewByAssessorTime
                                    : a.Created)
                        : query.OrderByDescending(a =>
                            a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR
                                ? a.AllocatedToAgencyTime
                                : a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
                                    ? a.ReviewByAssessorTime
                                    : a.Created),
                _ => query.OrderByDescending(x => x.Created)
            };

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
                    a.Status,
                    a.SubStatus,
                    a.Created,
                    a.Updated,
                    a.ORIGIN,
                    a.IsNewAssignedToAgency,
                    a.AssignedToAgency,
                    a.ReviewByAssessorTime,
                    CustomerName = a.CustomerDetail != null ? a.CustomerDetail.Name : null,
                    customerImagePath = a.CustomerDetail != null ? a.CustomerDetail.ImagePath : Applicationsettings.NO_USER,
                    CustomerLocationMap = a.CustomerDetail.CustomerLocationMap,
                    CustomerDetailAddressLocationInfo = a.CustomerDetail.AddressLocationInfo,
                    customerAddressline = a.CustomerDetail != null ? a.CustomerDetail.Addressline : string.Empty,
                    customerDistrict = a.CustomerDetail != null ? a.CustomerDetail.District.Name : string.Empty,
                    customerState = a.CustomerDetail != null ? a.CustomerDetail.State.Name : string.Empty,
                    customerPincode = a.CustomerDetail != null ? a.CustomerDetail.PinCode.Code : 0,
                    CustomerDetailLatitude = a.CustomerDetail.Latitude,
                    CustomerDetailLongitude = a.CustomerDetail.Longitude,
                    BeneficiaryName = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.Name : null,
                    beneficiaryImagePath = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.ImagePath : Applicationsettings.NO_USER,
                    BeneficiaryLocationMap = a.BeneficiaryDetail.BeneficiaryLocationMap,
                    beneficiaryAddressline = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.Addressline : string.Empty,
                    beneficiaryDistrict = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.District.Name : string.Empty,
                    beneficiaryState = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.State.Name : string.Empty,
                    beneficiaryPincode = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.PinCode.Code : 0,
                    BeneficiaryDetailLatitude = a.BeneficiaryDetail.Latitude,
                    BeneficiaryDetailLongitude = a.BeneficiaryDetail.Longitude,
                    BeneficiaryAddressLocationInfo = a.BeneficiaryDetail.AddressLocationInfo,
                    a.AllocatedToAgencyTime,
                    ClientCompanyDocumentUrl = a.ClientCompany.DocumentUrl,
                    ClientCompanyName = a.ClientCompany.Name,
                    GetTimeElapsed = DateTime.UtcNow.Subtract(
                        (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR && a.AllocatedToAgencyTime.HasValue) ?
                        a.AllocatedToAgencyTime.Value :
                        (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR && a.ReviewByAssessorTime.HasValue) ?
                        a.ReviewByAssessorTime.Value : a.Created).TotalSeconds
                }).ToListAsync();

            // -----------------------------
            // FINAL PROJECTION
            // -----------------------------
            var finalDataTasks = pagedRawData.Select(async a =>
            {
                var isUW = a.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = CustomExtensions.GetCultureByCountry(vendorUser.Country.Code);
                var pincode = isUW ? a.customerPincode : a.beneficiaryPincode;
                var customerAddress = a.customerAddressline + ',' + a.customerDistrict + ',' + a.customerState;
                var beneficiaryAddress = a.beneficiaryAddressline + ',' + a.beneficiaryDistrict + ',' + a.beneficiaryState;
                var pincodeName = isUW ? customerAddress : beneficiaryAddress;
                var personName = isUW ? a.CustomerName : a.BeneficiaryName;
                var policy = a.InsuranceType.GetEnumDisplayName();
                var serviceType = a.InsuranceType.GetEnumDisplayName();
                var service = a.ServiceTypeName;
                var timePending = GetSupervisorNewTimePending(a.investigation);
                var beneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" : a.BeneficiaryName;
                var isQueryCase = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
                //var timeElapsed = (!isQueryCase && a.AllocatedToAgencyTime.HasValue) ? DateTime.UtcNow.Subtract(a.AllocatedToAgencyTime.Value).TotalSeconds :
                //    DateTime.UtcNow.Subtract(a.ReviewByAssessorTime.Value).TotalSeconds;
                var personMapAddressUrl = isUW ?
                        string.Format(a.CustomerLocationMap, "400", "400") : string.Format(a.BeneficiaryLocationMap, "400", "400");
                var addressLocationInfoTask = isUW ?
                    weatherInfoService.GetWeatherAsync(a.CustomerDetailLatitude, a.CustomerDetailLongitude) :
                        weatherInfoService.GetWeatherAsync(a.BeneficiaryDetailLatitude, a.BeneficiaryDetailLongitude);

                var ownerDetailTask = base64FileService.GetBase64FileAsync(a.ClientCompanyDocumentUrl);
                var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerPhotoTask = base64FileService.GetBase64FileAsync(isUW ? a.customerImagePath : a.beneficiaryImagePath);
                var beneficiaryPhotoTask = base64FileService.GetBase64FileAsync(a.beneficiaryImagePath, Applicationsettings.NO_USER);

                // Wait for all images for THIS case to load
                await Task.WhenAll(ownerDetailTask, documentTask, customerPhotoTask, beneficiaryPhotoTask, addressLocationInfoTask);

                return new CaseInvestigationAgencyResponse
                {
                    Id = a.Id,
                    PolicyId = a.policyId,
                    PolicyNum = a.PolicyNum,
                    Amount = string.Format(culture, "{0:C}", a.SumAssuredValue),
                    Company = a.ClientCompanyName,
                    OwnerDetail = await ownerDetailTask,
                    Pincode = pincode.ToString(),
                    PincodeName = pincodeName,
                    AssignedToAgency = a.AssignedToAgency,
                    Document = await documentTask,
                    Customer = await customerPhotoTask,
                    Name = personName,
                    Policy = policy,
                    Status = a.Status,
                    ServiceType = serviceType,
                    Service = service,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = timePending,
                    BeneficiaryPhoto = await beneficiaryPhotoTask,
                    BeneficiaryName = beneficiaryName,
                    TimeElapsed = a.GetTimeElapsed,
                    IsNewAssigned = a.IsNewAssignedToAgency,
                    IsQueryCase = isQueryCase,
                    PersonMapAddressUrl = personMapAddressUrl,
                    AddressLocationInfo = await addressLocationInfoTask
                };
            });

            var finalData = (await Task.WhenAll(finalDataTasks));
            //if (orderColumn == 12)
            //{
            //    finalData = asc
            //        ? finalData.OrderBy(x => x.TimeElapsed).ToArray()
            //        : finalData.OrderByDescending(x => x.TimeElapsed).ToArray();
            //}
            // -----------------------------
            // MARK VIEWED (BULK UPDATE)
            // -----------------------------
            var newIds = finalData.Where(x => x.IsNewAssigned.HasValue).Select(x => x.Id).ToList();

            if (newIds.Any())
            {
                await context.Investigations
                    .Where(x => newIds.Contains(x.Id))
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(p => p.IsNewAssignedToAgency, false));
            }

            return new DataTableResponse<CaseInvestigationAgencyResponse>
            {
                Draw = draw,
                RecordsTotal = recordsTotal,
                RecordsFiltered = recordsFiltered,
                Data = finalData.ToList()
            };
        }

        public async Task<DataTableResponse<CaseInvestigationResponse>> GetOpenCases(string userEmail, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc")
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var vendorUser = await _context.ApplicationUser
                .AsNoTracking()
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (vendorUser == null)
                return new DataTableResponse<CaseInvestigationResponse>();

            var query = _context.Investigations
                .AsNoTracking()
                .Where(a =>
                    a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                    a.VendorId == vendorUser.VendorId);

            if (vendorUser.Role.ToString() == SUPERVISOR.DISPLAY_NAME)
            {
                query = query.Where(a =>
                    (a.AllocatingSupervisordEmail == userEmail &&
                     a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
                    ||
                    (a.SubmittingSupervisordEmail == userEmail &&
                     (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                      a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR))
                );
            }
            else
            {
                query = query.Where(a =>
                    a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ||
                    a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                    a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR
                );
            }
            int recordsTotal = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.Contains(search) ||
                    (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name.Contains(search) : a.BeneficiaryDetail.Name.Contains(search)) ||
                     (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.PinCode.Code.ToString().Contains(search) : a.BeneficiaryDetail.PinCode.ToString().Contains(search)) ||
                     (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.PinCode.Name.ToLower().ToString().Contains(search) : a.BeneficiaryDetail.PinCode.Name.ToLower().Contains(search)) ||
                    a.ClientCompany.Name.Contains(search) ||
                    a.PolicyDetail.InsuranceType.GetEnumDisplayName().Contains(search) ||
                    a.PolicyDetail.InvestigationServiceType.Name.Contains(search)
                );
            }
            int recordsFiltered = await query.CountAsync();

            bool asc = orderDir == "asc";
            query = orderColumn switch
            {
                1 => asc ? query.OrderBy(x => x.PolicyDetail.SumAssuredValue) : query.OrderByDescending(x => x.PolicyDetail.SumAssuredValue),

                2 => asc
                    ? query.OrderBy(x => x.CustomerDetail.Name)
                    : query.OrderByDescending(x => x.CustomerDetail.Name),

                3 => asc
                    ? query.OrderBy(x => x.ClientCompany.Name)
                    : query.OrderByDescending(x => x.ClientCompany.Name),
                4 => asc
                        ? query.OrderBy(x => x.SelectedAgentDrivingDistance)
                        : query.OrderByDescending(x => x.SelectedAgentDrivingDistance),
                5 => asc
                ? query.OrderBy(x => x.SelectedAgentDrivingDuration)
                : query.OrderByDescending(x => x.SelectedAgentDrivingDuration),

                8 => asc
                ? query.OrderBy(x => x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? x.CustomerDetail.Name : x.BeneficiaryDetail.Name)
                : query.OrderByDescending(x => x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? x.BeneficiaryDetail.Name : x.CustomerDetail.Name),

                9 => asc
                ? query.OrderBy(x => x.PolicyDetail.InsuranceType)
                : query.OrderByDescending(x => x.PolicyDetail.InsuranceType),
                10 => asc
                ? query.OrderBy(x => x.PolicyDetail.InvestigationServiceType.Name)
                : query.OrderByDescending(x => x.PolicyDetail.InvestigationServiceType.Name),
                11 => asc
                ? query.OrderBy(x => x.Created)
                : query.OrderByDescending(x => x.Created),
                12 => asc
                ? query.OrderBy(x =>
                    x.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ? x.TaskToAgentTime :
                    x.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ? x.SubmittedToAssessorTime :
                    x.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ? x.EnquiryReplyByAgencyTime :
                    x.Created)  // fallback
                : query.OrderByDescending(x =>
                    x.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ? x.TaskToAgentTime :
                    x.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ? x.SubmittedToAssessorTime :
                    x.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ? x.EnquiryReplyByAgencyTime :
                    x.Created),  // fallback,

                _ => query.OrderByDescending(x => x.Created)
            };

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
                    a.Status,
                    a.SubStatus,
                    a.Created,
                    a.Updated,
                    a.ORIGIN,
                    a.IsNewAssignedToAgency,
                    a.IsNewSubmittedToAgent,
                    a.AssignedToAgency,
                    CustomerName = a.CustomerDetail != null ? a.CustomerDetail.Name : null,
                    customerImagePath = a.CustomerDetail != null ? a.CustomerDetail.ImagePath : Applicationsettings.NO_USER,
                    CustomerLocationMap = a.CustomerDetail.CustomerLocationMap,
                    CustomerDetailAddressLocationInfo = a.CustomerDetail.AddressLocationInfo,
                    customerAddressline = a.CustomerDetail != null ? a.CustomerDetail.Addressline : string.Empty,
                    customerDistrict = a.CustomerDetail != null ? a.CustomerDetail.District.Name : string.Empty,
                    customerState = a.CustomerDetail != null ? a.CustomerDetail.State.Name : string.Empty,
                    customerPincode = a.CustomerDetail != null ? a.CustomerDetail.PinCode.Code : 0,
                    CustomerDetailLatitude = a.CustomerDetail.Latitude,
                    CustomerDetailLongitude = a.CustomerDetail.Longitude,
                    BeneficiaryName = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.Name : null,
                    beneficiaryImagePath = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.ImagePath : Applicationsettings.NO_USER,
                    BeneficiaryLocationMap = a.BeneficiaryDetail.BeneficiaryLocationMap,
                    beneficiaryAddressline = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.Addressline : string.Empty,
                    beneficiaryDistrict = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.District.Name : string.Empty,
                    beneficiaryState = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.State.Name : string.Empty,
                    beneficiaryPincode = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.PinCode.Code : 0,
                    BeneficiaryDetailLatitude = a.BeneficiaryDetail.Latitude,
                    BeneficiaryDetailLongitude = a.BeneficiaryDetail.Longitude,
                    BeneficiaryAddressLocationInfo = a.BeneficiaryDetail.AddressLocationInfo,
                    a.AllocatedToAgencyTime,
                    ClientCompanyDocumentUrl = a.ClientCompany.DocumentUrl,
                    ClientCompanyName = a.ClientCompany.Name,
                    a.SelectedAgentDrivingMap,
                    a.SelectedAgentDrivingDistance,
                    a.SelectedAgentDrivingDuration
                }).ToListAsync();

            // -------------------------
            // Projection (ALL fields)
            // -------------------------
            var finalDataTasks = pagedRawData.Select(async a =>
            {
                var isUW = a.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = CustomExtensions.GetCultureByCountry(vendorUser.Country.Code);
                var pincode = isUW ? a.customerPincode : a.beneficiaryPincode;
                var customerAddress = a.customerAddressline + ',' + a.customerDistrict + ',' + a.customerState;
                var beneficiaryAddress = a.beneficiaryAddressline + ',' + a.beneficiaryDistrict + ',' + a.beneficiaryState;
                var pincodeName = isUW ? customerAddress : beneficiaryAddress;
                var policy = $"<span class='badge badge-light'>{a.InsuranceType.GetEnumDisplayName()}</span>";
                var beneficiaryName = a.BeneficiaryName;

                var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerPhotoTask = base64FileService.GetBase64FileAsync(isUW ? a.customerImagePath : a.beneficiaryImagePath);
                var beneficiaryPhotoTask = base64FileService.GetBase64FileAsync(a.beneficiaryImagePath, Applicationsettings.NO_USER);
                var ownerImageTask = base64FileService.GetBase64FileAsync(await GetOwner(a.investigation), Applicationsettings.NO_USER);
                var ownerEmailTask = GetOwnerEmail(a.investigation);

                // Wait for all images for THIS case to load
                await Task.WhenAll(documentTask, customerPhotoTask, beneficiaryPhotoTask, ownerImageTask, ownerEmailTask);
                return new CaseInvestigationResponse
                {
                    Id = a.Id,
                    AssignedToAgency = a.IsNewSubmittedToAgent,
                    PolicyId = a.policyId,
                    PolicyNum = a.policyId,
                    Amount = string.Format(culture, "{0:C}", a.SumAssuredValue),
                    Agent = await ownerEmailTask,
                    OwnerDetail = await ownerImageTask,
                    CaseWithPerson = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    Pincode = pincode.ToString(),
                    PincodeName = pincodeName,
                    Company = a.ClientCompanyName,
                    Document = await documentTask,
                    Customer = await customerPhotoTask,
                    Name = a.CustomerName,
                    Policy = policy,
                    Status = a.Status,
                    ServiceType = a.InsuranceType.GetEnumDisplayName(),
                    Service = a.ServiceTypeName,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = GetSupervisorOpenTimePending(a.investigation),
                    TimeElapsed = GetTimeElapsed(a.investigation),
                    BeneficiaryPhoto = await beneficiaryPhotoTask,
                    BeneficiaryName = beneficiaryName,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                };
            });

            var finalData = (await Task.WhenAll(finalDataTasks));

            // -------------------------
            // Mark as viewed
            // -------------------------
            var idsToMarkViewed = finalData
                .Where(x => x.AssignedToAgency)
                .Select(x => x.Id)
                .ToList();

            if (idsToMarkViewed.Any())
            {
                await _context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.IsNewSubmittedToAgent, false)
                        .SetProperty(p => p.IsNewSubmittedToCompany, false));
            }

            return new DataTableResponse<CaseInvestigationResponse>
            {
                Draw = draw,
                RecordsTotal = recordsTotal,
                RecordsFiltered = recordsFiltered,
                Data = finalData.ToList()
            };
        }

        private static double GetTimeElapsed(InvestigationTask caseTask)
        {
            var timeElapsed = DateTime.UtcNow.Subtract(caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ?
                caseTask.TaskToAgentTime.Value :
                                                     caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ?
                                                     caseTask.SubmittedToAssessorTime.Value :
                                                     caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ?
                                                     caseTask.EnquiryReplyByAgencyTime.Value : caseTask.Created).TotalSeconds;
            return timeElapsed;
        }

        public async Task<DataTableResponse<CaseInvestigationAgencyResponse>> GetCompletedCases(string userEmail, string userClaim, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc")
        {
            var vendorUser = await context.ApplicationUser
                .AsNoTracking()
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);
            if (vendorUser == null)
            {
                return new DataTableResponse<CaseInvestigationAgencyResponse>
                {
                    Draw = draw,
                    Data = new(),
                    RecordsTotal = 0,
                    RecordsFiltered = 0
                };
            }
            var finishedStatus = CONSTANTS.CASE_STATUS.FINISHED;
            var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
            var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

            var query = context.Investigations
                .AsNoTracking()
                .Where(a =>
                    a.VendorId == vendorUser.VendorId &&
                    a.Status == finishedStatus && (a.SubStatus == approvedStatus || a.SubStatus == rejectedStatus));

            if (vendorUser.Role.ToString() == SUPERVISOR.DISPLAY_NAME)
            {
                query = query
                    .Where(a => a.SubmittedAssessordEmail == userEmail);
            }
            int recordsTotal = await query.CountAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.Contains(search) ||
                    a.ClientCompany.Name.Contains(search) ||
                     (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name.Contains(search) : a.BeneficiaryDetail.Name.Contains(search)) ||
                     (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.PinCode.Code.ToString().Contains(search) : a.BeneficiaryDetail.PinCode.ToString().Contains(search)) ||
                     (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.PinCode.Name.ToLower().ToString().Contains(search) : a.BeneficiaryDetail.PinCode.Name.ToLower().Contains(search)) ||
                     (a.PolicyDetail.InvestigationServiceType.Name.Contains(search)) ||
                    a.PolicyDetail.InsuranceType.GetEnumDisplayName().ToLower().Contains(search));
            }

            int recordsFiltered = await query.CountAsync();
            bool asc = orderDir == "asc";

            query = orderColumn switch
            {
                1 => asc ? query.OrderBy(x => x.PolicyDetail.ContractNumber) : query.OrderByDescending(x => x.PolicyDetail.ContractNumber),
                2 => asc ? query.OrderBy(x => (double)x.PolicyDetail.SumAssuredValue)
                         : query.OrderByDescending(x => (double)x.PolicyDetail.SumAssuredValue),
                3 => asc ? query.OrderBy(x => x.ClientCompany.Name)
                         : query.OrderByDescending(x => x.ClientCompany.Name),
                4 => asc ? query.OrderBy(x => (x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING) ? x.CustomerDetail.PinCode.Code : x.BeneficiaryDetail.PinCode.Code)
                            : query.OrderByDescending(x => (x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING) ? x.CustomerDetail.PinCode.Code : x.BeneficiaryDetail.PinCode.Code),
                7 => asc
                        ? query.OrderBy(x =>
                            x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                                ? x.CustomerDetail.Name
                                : x.BeneficiaryDetail.Name)
                        : query.OrderByDescending(x =>
                            x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                                ? x.CustomerDetail.PinCode.Code
                                : x.BeneficiaryDetail.PinCode.Code),
                8 => asc ? query.OrderBy(x => x.PolicyDetail.InsuranceType.GetEnumDisplayName())
                                                         : query.OrderByDescending(x => x.PolicyDetail.InsuranceType.GetEnumDisplayName()),
                9 => asc ? query.OrderBy(x => x.PolicyDetail.InvestigationServiceType.Name)
                                         : query.OrderByDescending(x => x.PolicyDetail.InvestigationServiceType.Name),
                10 => asc ? query.OrderBy(x => x.Created)
                : query.OrderByDescending(x => x.Created),
                11 => asc
                        ? query.OrderBy(x => x.AllocatedToAgencyTime == null)
                               .ThenByDescending(x => x.AllocatedToAgencyTime)
                        : query.OrderBy(x => x.AllocatedToAgencyTime == null)
                               .ThenBy(x => x.AllocatedToAgencyTime),
                _ => query.OrderByDescending(x => x.Created)
            };

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
                    a.Status,
                    a.SubStatus,
                    a.Created,
                    a.Updated,
                    a.ORIGIN,
                    a.IsNewAssignedToAgency,
                    a.AssignedToAgency,
                    a.ProcessedByAssessorTime,
                    CustomerName = a.CustomerDetail != null ? a.CustomerDetail.Name : null,
                    customerImagePath = a.CustomerDetail != null ? a.CustomerDetail.ImagePath : Applicationsettings.NO_USER,
                    CustomerLocationMap = a.CustomerDetail.CustomerLocationMap,
                    CustomerDetailAddressLocationInfo = a.CustomerDetail.AddressLocationInfo,
                    customerAddressline = a.CustomerDetail != null ? a.CustomerDetail.Addressline : string.Empty,
                    customerDistrict = a.CustomerDetail != null ? a.CustomerDetail.District.Name : string.Empty,
                    customerState = a.CustomerDetail != null ? a.CustomerDetail.State.Name : string.Empty,
                    customerPincode = a.CustomerDetail != null ? a.CustomerDetail.PinCode.Code : 0,
                    CustomerDetailLatitude = a.CustomerDetail.Latitude,
                    CustomerDetailLongitude = a.CustomerDetail.Longitude,
                    BeneficiaryName = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.Name : null,
                    beneficiaryImagePath = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.ImagePath : Applicationsettings.NO_USER,
                    BeneficiaryLocationMap = a.BeneficiaryDetail.BeneficiaryLocationMap,
                    beneficiaryAddressline = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.Addressline : string.Empty,
                    beneficiaryDistrict = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.District.Name : string.Empty,
                    beneficiaryState = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.State.Name : string.Empty,
                    beneficiaryPincode = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.PinCode.Code : 0,
                    BeneficiaryDetailLatitude = a.BeneficiaryDetail.Latitude,
                    BeneficiaryDetailLongitude = a.BeneficiaryDetail.Longitude,
                    BeneficiaryAddressLocationInfo = a.BeneficiaryDetail.AddressLocationInfo,
                    a.AllocatedToAgencyTime,
                    ClientCompanyDocumentUrl = a.ClientCompany.DocumentUrl,
                    ClientCompanyName = a.ClientCompany.Name,
                    a.SelectedAgentDrivingDistance,
                    a.SelectedAgentDrivingDuration
                }).ToListAsync();
            var finalData = pagedRawData.Select(async a =>
            {
                var isUW = a.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = CustomExtensions.GetCultureByCountry(vendorUser.Country.Code);
                var pincode = isUW ? a.customerPincode : a.beneficiaryPincode;
                var customerAddress = a.customerAddressline + ',' + a.customerDistrict + ',' + a.customerState;
                var beneficiaryAddress = a.beneficiaryAddressline + ',' + a.beneficiaryDistrict + ',' + a.beneficiaryState;
                var pincodeName = isUW ? customerAddress : beneficiaryAddress;
                var personName = isUW ? a.CustomerName : a.BeneficiaryName;
                var policy = a.InsuranceType.GetEnumDisplayName();
                var serviceType = a.InsuranceType.GetEnumDisplayName();
                var service = a.ServiceTypeName;
                var timePending = GetSupervisorCompletedTime(a.investigation);
                var policyNum = a.PolicyNum;
                var beneficiaryName = a.BeneficiaryName;
                var timeElapsed = a.AllocatedToAgencyTime.HasValue ? DateTime.UtcNow.Subtract(a.AllocatedToAgencyTime.Value).TotalSeconds : 0;
                var isQueryCase = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
                var personMapAddressUrl = isUW ? string.Format(a.CustomerLocationMap, "400", "400") : string.Format(a.BeneficiaryLocationMap, "400", "400");
                var addressLocationInfoTask = isUW ?
                    weatherInfoService.GetWeatherAsync(a.CustomerDetailLatitude, a.CustomerDetailLongitude) :
                        weatherInfoService.GetWeatherAsync(a.BeneficiaryDetailLatitude, a.BeneficiaryDetailLongitude);

                var ownerDetailTask = base64FileService.GetBase64FileAsync(a.ClientCompanyDocumentUrl);
                var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerPhotoTask = base64FileService.GetBase64FileAsync(isUW ? a.customerImagePath : a.beneficiaryImagePath);
                var beneficiaryPhotoTask = base64FileService.GetBase64FileAsync(a.beneficiaryImagePath, Applicationsettings.NO_USER);

                // Wait for all images for THIS case to load
                await Task.WhenAll(ownerDetailTask, documentTask, customerPhotoTask, beneficiaryPhotoTask);

                return new CaseInvestigationAgencyResponse
                {
                    Id = a.Id,
                    PolicyId = a.policyId,
                    Amount = string.Format(culture, "{0:C}", a.SumAssuredValue),
                    Company = a.ClientCompanyName,
                    OwnerDetail = await ownerDetailTask,
                    Pincode = pincode.ToString(),
                    PincodeName = pincodeName,
                    AssignedToAgency = a.AssignedToAgency,
                    Document = await documentTask,
                    Customer = await customerPhotoTask,
                    Name = personName,
                    Policy = policy,
                    Status = a.Status,
                    ServiceType = serviceType,
                    Service = service,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = timePending,
                    PolicyNum = policyNum,
                    BeneficiaryPhoto = await beneficiaryPhotoTask,
                    BeneficiaryName = beneficiaryName,
                    TimeElapsed = timeElapsed,
                    IsNewAssigned = a.IsNewAssignedToAgency,
                    IsQueryCase = isQueryCase,
                    PersonMapAddressUrl = personMapAddressUrl,
                    AddressLocationInfo = await addressLocationInfoTask,
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                };
            });
            var data = await Task.WhenAll(finalData);

            return new DataTableResponse<CaseInvestigationAgencyResponse>
            {
                Draw = draw,
                RecordsTotal = recordsTotal,
                RecordsFiltered = recordsFiltered,
                Data = data.ToList()
            };
        }

        public async Task<DataTableResponse<CaseInvestigationAgencyResponse>> GetAgentReports(string userEmail, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc")
        {
            var vendorUser = await context.ApplicationUser
                            .AsNoTracking()
                            .Include(v => v.Country)
                            .FirstOrDefaultAsync(x => x.Email == userEmail);

            if (vendorUser == null)
            {
                return new DataTableResponse<CaseInvestigationAgencyResponse>
                {
                    Draw = draw,
                    Data = new(),
                    RecordsTotal = 0,
                    RecordsFiltered = 0
                };
            }

            var query = context.Investigations
                .AsNoTracking()
                .Where(a =>
                    a.VendorId == vendorUser.VendorId &&
                    a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            int recordsTotal = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.Contains(search) ||
                    a.ClientCompany.Name.Contains(search) ||
                     (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name.Contains(search) : a.BeneficiaryDetail.Name.Contains(search)) ||
                     (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.PinCode.Code.ToString().Contains(search) : a.BeneficiaryDetail.PinCode.ToString().Contains(search)) ||
                     (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.PinCode.Name.ToLower().ToString().Contains(search) : a.BeneficiaryDetail.PinCode.Name.ToLower().Contains(search)) ||
                     (a.PolicyDetail.InvestigationServiceType.Name.Contains(search)) ||
                    a.PolicyDetail.InsuranceType.GetEnumDisplayName().ToLower().Contains(search));
            }

            int recordsFiltered = await query.CountAsync();

            // -----------------------------
            // SORTING (SQL ONLY)
            // -----------------------------
            bool asc = orderDir == "asc";

            query = orderColumn switch
            {
                1 => asc ? query.OrderBy(x => x.PolicyDetail.ContractNumber) : query.OrderByDescending(x => x.PolicyDetail.ContractNumber),
                2 => asc ? query.OrderBy(x => (double)x.PolicyDetail.SumAssuredValue)
                         : query.OrderByDescending(x => (double)x.PolicyDetail.SumAssuredValue),
                3 => asc ? query.OrderBy(x => x.ClientCompany.Name)
                         : query.OrderByDescending(x => x.ClientCompany.Name),
                4 => asc ? query.OrderBy(x => (x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING) ? x.CustomerDetail.PinCode.Code : x.BeneficiaryDetail.PinCode.Code)
                            : query.OrderByDescending(x => (x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING) ? x.CustomerDetail.PinCode.Code : x.BeneficiaryDetail.PinCode.Code),
                7 => asc
                        ? query.OrderBy(x =>
                            x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                                ? x.CustomerDetail.Name
                                : x.BeneficiaryDetail.Name)
                        : query.OrderByDescending(x =>
                            x.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                                ? x.CustomerDetail.PinCode.Code
                                : x.BeneficiaryDetail.PinCode.Code),
                8 => asc ? query.OrderBy(x => x.PolicyDetail.InsuranceType.GetEnumDisplayName())
                                                         : query.OrderByDescending(x => x.PolicyDetail.InsuranceType.GetEnumDisplayName()),
                9 => asc ? query.OrderBy(x => x.PolicyDetail.InvestigationServiceType.Name)
                                         : query.OrderByDescending(x => x.PolicyDetail.InvestigationServiceType.Name),
                10 => asc ? query.OrderBy(x => x.Created)
                : query.OrderByDescending(x => x.Created),
                11 => asc
                        ? query.OrderBy(x => x.SubmittedToSupervisorTime == null)
                               .ThenByDescending(x => x.SubmittedToSupervisorTime)
                        : query.OrderBy(x => x.SubmittedToSupervisorTime == null)
                               .ThenBy(x => x.SubmittedToSupervisorTime),
                _ => query.OrderByDescending(x => x.Created)
            };
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
                    a.Status,
                    a.SubStatus,
                    a.Created,
                    a.ORIGIN,
                    a.IsNewAssignedToAgency,
                    a.AssignedToAgency,
                    a.SubmittedToSupervisorTime,
                    CustomerName = a.CustomerDetail != null ? a.CustomerDetail.Name : null,
                    customerImagePath = a.CustomerDetail != null ? a.CustomerDetail.ImagePath : Applicationsettings.NO_USER,
                    CustomerLocationMap = a.CustomerDetail.CustomerLocationMap,
                    CustomerDetailAddressLocationInfo = a.CustomerDetail.AddressLocationInfo,
                    customerAddressline = a.CustomerDetail != null ? a.CustomerDetail.Addressline : string.Empty,
                    customerDistrict = a.CustomerDetail != null ? a.CustomerDetail.District.Name : string.Empty,
                    customerState = a.CustomerDetail != null ? a.CustomerDetail.State.Name : string.Empty,
                    customerPincode = a.CustomerDetail != null ? a.CustomerDetail.PinCode.Code : 0,
                    CustomerDetailLatitude = a.CustomerDetail.Latitude,
                    CustomerDetailLongitude = a.CustomerDetail.Longitude,
                    BeneficiaryName = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.Name : null,
                    beneficiaryImagePath = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.ImagePath : Applicationsettings.NO_USER,
                    BeneficiaryLocationMap = a.BeneficiaryDetail.BeneficiaryLocationMap,
                    beneficiaryAddressline = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.Addressline : string.Empty,
                    beneficiaryDistrict = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.District.Name : string.Empty,
                    beneficiaryState = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.State.Name : string.Empty,
                    beneficiaryPincode = a.BeneficiaryDetail != null ? a.BeneficiaryDetail.PinCode.Code : 0,
                    BeneficiaryDetailLatitude = a.BeneficiaryDetail.Latitude,
                    BeneficiaryDetailLongitude = a.BeneficiaryDetail.Longitude,
                    BeneficiaryAddressLocationInfo = a.BeneficiaryDetail.AddressLocationInfo,
                    ClientCompanyDocumentUrl = a.ClientCompany.DocumentUrl,
                    ClientCompanyName = a.ClientCompany.Name,
                    a.SelectedAgentDrivingDistance,
                    a.SelectedAgentDrivingDuration
                }).ToListAsync();

            var data = await Task.WhenAll(pagedRawData.Select(async a =>
            {
                var isUW = a.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = CustomExtensions.GetCultureByCountry(vendorUser.Country.Code);
                var pincode = isUW ? a.customerPincode : a.beneficiaryPincode;
                var customerAddress = a.customerAddressline + ',' + a.customerDistrict + ',' + a.customerState;
                var beneficiaryAddress = a.beneficiaryAddressline + ',' + a.beneficiaryDistrict + ',' + a.beneficiaryState;
                var pincodeName = isUW ? customerAddress : beneficiaryAddress;
                var personName = isUW ? a.CustomerName : a.BeneficiaryName;
                var policy = a.InsuranceType.GetEnumDisplayName();
                var serviceType = a.InsuranceType.GetEnumDisplayName();
                var service = a.ServiceTypeName;
                var timePending = GetSupervisorSubmittedByAgent(a.investigation);
                var policyNum = a.PolicyNum;
                var beneficiaryName = a.BeneficiaryName;
                var timeElapsed = a.SubmittedToSupervisorTime.HasValue ? DateTime.UtcNow.Subtract(a.SubmittedToSupervisorTime.Value).TotalSeconds : 0;
                var isQueryCase = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
                var personMapAddressUrl = isUW ?
                        string.Format(a.CustomerLocationMap, "400", "400") : string.Format(a.BeneficiaryLocationMap, "400", "400");
                var addressLocationInfoTask = isUW ?
                    weatherInfoService.GetWeatherAsync(a.CustomerDetailLatitude, a.CustomerDetailLongitude) :
                        weatherInfoService.GetWeatherAsync(a.BeneficiaryDetailLatitude, a.BeneficiaryDetailLongitude);

                var ownerDetailTask = base64FileService.GetBase64FileAsync(a.ClientCompanyDocumentUrl);
                var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerPhotoTask = base64FileService.GetBase64FileAsync(isUW ? a.customerImagePath : a.beneficiaryImagePath);
                var beneficiaryPhotoTask = base64FileService.GetBase64FileAsync(a.beneficiaryImagePath, Applicationsettings.NO_USER);

                // Wait for all images for THIS case to load
                await Task.WhenAll(ownerDetailTask, documentTask, customerPhotoTask, beneficiaryPhotoTask, addressLocationInfoTask);

                return new CaseInvestigationAgencyResponse
                {
                    Id = a.Id,
                    PolicyId = a.policyId,
                    Amount = string.Format(culture, "{0:C}", a.SumAssuredValue),
                    Company = a.ClientCompanyName,
                    OwnerDetail = await ownerDetailTask,
                    Pincode = pincode.ToString(),
                    PincodeName = pincodeName,
                    AssignedToAgency = a.AssignedToAgency,
                    Document = await documentTask,
                    Customer = await customerPhotoTask,
                    Name = personName,
                    Policy = policy,
                    Status = a.Status,
                    ServiceType = serviceType,
                    Service = service,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = timePending,
                    PolicyNum = policyNum,
                    BeneficiaryPhoto = await beneficiaryPhotoTask,
                    BeneficiaryName = beneficiaryName,
                    TimeElapsed = timeElapsed,
                    IsNewAssigned = a.IsNewAssignedToAgency,
                    IsQueryCase = isQueryCase,
                    PersonMapAddressUrl = personMapAddressUrl,
                    AddressLocationInfo = await addressLocationInfoTask,
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                };
            }));

            // -----------------------------
            // MARK VIEWED (BULK UPDATE)
            // -----------------------------
            var newIds = data.Where(x => x.IsNewAssigned.HasValue).Select(x => x.Id).ToList();

            if (newIds.Any())
            {
                await context.Investigations
                    .Where(x => newIds.Contains(x.Id))
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(p => p.IsNewAssignedToAgency, false));
            }

            return new DataTableResponse<CaseInvestigationAgencyResponse>
            {
                Draw = draw,
                RecordsTotal = recordsTotal,
                RecordsFiltered = recordsFiltered,
                Data = data.ToList()
            };
        }

        private static string GetSupervisorCompletedTime(InvestigationTask caseTask)
        {
            DateTime timeToCompare = caseTask.ProcessedByAssessorTime.Value;

            if (DateTime.UtcNow.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.UtcNow.Subtract(timeToCompare).Hours < 24 &&
                DateTime.UtcNow.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.UtcNow.Subtract(timeToCompare).Hours == 0 && DateTime.UtcNow.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.UtcNow.Subtract(timeToCompare).Minutes == 0 && DateTime.UtcNow.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

        private static string GetSupervisorSubmittedByAgent(InvestigationTask caseTask)
        {
            DateTime timeToCompare = caseTask.SubmittedToSupervisorTime.Value;

            if (DateTime.UtcNow.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.UtcNow.Subtract(timeToCompare).Hours < 24 &&
                DateTime.UtcNow.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.UtcNow.Subtract(timeToCompare).Hours == 0 && DateTime.UtcNow.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.UtcNow.Subtract(timeToCompare).Minutes == 0 && DateTime.UtcNow.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

        private static string GetSupervisorNewTimePending(InvestigationTask caseTask)
        {
            DateTime timeToCompare = caseTask.AllocatedToAgencyTime.Value;

            if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                timeToCompare = caseTask.EnquiredByAssessorTime.GetValueOrDefault();
            }

            if (DateTime.UtcNow.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.UtcNow.Subtract(timeToCompare).Hours < 24 &&
                DateTime.UtcNow.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.UtcNow.Subtract(timeToCompare).Hours == 0 && DateTime.UtcNow.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.UtcNow.Subtract(timeToCompare).Minutes == 0 && DateTime.UtcNow.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

        private static string GetSupervisorOpenTimePending(InvestigationTask caseTask)
        {
            DateTime timeToCompare = caseTask.TaskToAgentTime.Value;
            if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                timeToCompare = caseTask.TaskToAgentTime.GetValueOrDefault();
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
            {
                timeToCompare = caseTask.SubmittedToAssessorTime.GetValueOrDefault();
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                timeToCompare = caseTask.EnquiryReplyByAgencyTime.GetValueOrDefault();
            }

            if (DateTime.UtcNow.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.UtcNow.Subtract(timeToCompare).Hours < 24 &&
                DateTime.UtcNow.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.UtcNow.Subtract(timeToCompare).Hours == 0 && DateTime.UtcNow.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.UtcNow.Subtract(timeToCompare).Minutes == 0 && DateTime.UtcNow.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.UtcNow.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

        private async Task<string> GetOwnerEmail(InvestigationTask caseTask)
        {
            string ownerEmail = string.Empty;
            var allocated2agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
            await using var _context = _contextFactory.CreateDbContext();
            if (caseTask.SubStatus == allocated2agent)
            {
                ownerEmail = caseTask.TaskedAgentEmail;
                var agencyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == ownerEmail);
                if (agencyUser != null && !string.IsNullOrWhiteSpace(agencyUser.Email))
                {
                    return agencyUser?.Email;
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR || caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                var companyUser = await _context.ClientCompany.FirstOrDefaultAsync(v => v.ClientCompanyId == caseTask.ClientCompanyId);
                if (companyUser != null && !string.IsNullOrWhiteSpace(companyUser.Email))
                {
                    return companyUser.Email;
                }
            }
            return "...";
        }

        private async Task<string> GetOwner(InvestigationTask caseTask)
        {
            string ownerEmail = string.Empty;
            var allocated2agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
            var noDataImagefilePath = Path.Combine(env.WebRootPath, "img", "no-photo.jpg");
            await using var _context = _contextFactory.CreateDbContext();

            if (caseTask.SubStatus == allocated2agent)
            {
                ownerEmail = caseTask.TaskedAgentEmail;
                var agencyUser = _context.ApplicationUser.FirstOrDefault(u => u.Email == ownerEmail);
                if (agencyUser != null && !string.IsNullOrWhiteSpace(agencyUser?.ProfilePictureUrl))
                {
                    return agencyUser.ProfilePictureUrl;
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR || caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                var companyUser = _context.ClientCompany.FirstOrDefault(u => u.ClientCompanyId == caseTask.ClientCompanyId);
                if (companyUser != null && !string.IsNullOrWhiteSpace(companyUser?.DocumentUrl))
                {
                    return companyUser.DocumentUrl;
                }
            }
            return noDataImagefilePath;
        }
    }
}