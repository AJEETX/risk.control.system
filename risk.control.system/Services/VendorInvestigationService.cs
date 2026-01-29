using Amazon.Auth.AccessControlPolicy;
using Google.Api;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IVendorInvestigationService
    {
        Task<DataTableResponse<CaseInvestigationAgencyResponse>> GetNewCases(string userEmail, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc");
        Task<DataTableResponse<CaseInvestigationResponse>> GetOpenCases(string userEmail, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc");
        Task<DataTableResponse<CaseInvestigationAgencyResponse>> GetAgentReports(string userEmail, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc");
        Task<DataTableResponse<CaseInvestigationAgencyResponse>> GetCompletedCases(string userEmail, string userClaim, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc");
    }
    internal class VendorInvestigationService : IVendorInvestigationService
    {
        private readonly ApplicationDbContext context;
        private readonly IBase64FileService base64FileService;
        private readonly IWeatherInfoService weatherInfoService;
        private readonly IWebHostEnvironment env;

        public VendorInvestigationService(ApplicationDbContext context,
            IBase64FileService base64FileService,
            IWeatherInfoService weatherInfoService,
            IWebHostEnvironment env)
        {
            this.context = context;
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
                2 => asc ? query.OrderBy(x =>(double) x.PolicyDetail.SumAssuredValue)
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

            // -----------------------------
            // PAGING
            // -----------------------------
            var pageCases = await query
                .Skip(start)
                .Take(length)
                .Include(a => a.ClientCompany)
                .Include(a => a.PolicyDetail).ThenInclude(p => p.InvestigationServiceType)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.PinCode)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.District)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.State)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.PinCode)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.District)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.State)
                .ToListAsync();

            // -----------------------------
            // WEATHER (PARALLEL)
            // -----------------------------
            await Task.WhenAll(pageCases.Select(a =>
            {
                if (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING && a.CustomerDetail != null)
                {
                    return weatherInfoService.GetWeatherAsync(
                        a.CustomerDetail.Latitude,
                        a.CustomerDetail.Longitude)
                        .ContinueWith(t => a.CustomerDetail.AddressLocationInfo = t.Result);
                }

                if (a.BeneficiaryDetail != null)
                {
                    return weatherInfoService.GetWeatherAsync(
                        a.BeneficiaryDetail.Latitude,
                        a.BeneficiaryDetail.Longitude)
                        .ContinueWith(t => a.BeneficiaryDetail.AddressLocationInfo = t.Result);
                }

                return Task.CompletedTask;
            }));


            // -----------------------------
            // FINAL PROJECTION
            // -----------------------------
            var data = await Task.WhenAll(pageCases.Select(async a =>
            {
                var isUnderWriting = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = CustomExtensions.GetCultureByCountry(vendorUser.Country.Code);
                var pincode = ClaimsInvestigationExtension.GetPincode(isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail);
                var pincodeName = ClaimsInvestigationExtension.GetPincodeName(isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail);
                var personName = isUnderWriting ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name;
                var policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName();
                var serviceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName();
                var service = a.PolicyDetail.InvestigationServiceType.Name;
                var timePending = GetSupervisorNewTimePending(a);
                var policyNum = GetPolicyNumForAgency(a, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);
                var beneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" : a.BeneficiaryDetail.Name;
                var timeElapsed = a.AllocatedToAgencyTime.HasValue ? DateTime.Now.Subtract(a.AllocatedToAgencyTime.Value).TotalSeconds : 0;
                var isQueryCase = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
                var personMapAddressUrl = isUnderWriting ?
                        string.Format(a.CustomerDetail.CustomerLocationMap, "400", "400") : string.Format(a.BeneficiaryDetail.BeneficiaryLocationMap, "400", "400");
                var addressLocationInfo = isUnderWriting ? a.CustomerDetail.AddressLocationInfo : a.BeneficiaryDetail.AddressLocationInfo;

                var ownerDetailTask = base64FileService.GetBase64FileAsync(a.ClientCompany.DocumentUrl);
                var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDetail.DocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerPhotoTask = base64FileService.GetBase64FileAsync(ClaimsInvestigationExtension.GetPersonPhoto(
                    isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail));
                var beneficiaryPhotoTask = base64FileService.GetBase64FileAsync(a.BeneficiaryDetail?.ImagePath, Applicationsettings.NO_USER);
                
                
                // Wait for all images for THIS case to load
                await Task.WhenAll(ownerDetailTask, documentTask, customerPhotoTask, beneficiaryPhotoTask);

                return new CaseInvestigationAgencyResponse
                {
                     Id = a.Id,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(culture, "{0:C}", a.PolicyDetail.SumAssuredValue),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = await ownerDetailTask,
                    Pincode = pincode,
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
                    AddressLocationInfo = addressLocationInfo
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

        public async Task<DataTableResponse<CaseInvestigationResponse>> GetOpenCases(string userEmail, int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc")
        {
            var vendorUser = await context.ApplicationUser
                .AsNoTracking()
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (vendorUser == null)
                return new DataTableResponse<CaseInvestigationResponse>();

            IQueryable<InvestigationTask> query = context.Investigations
                .AsNoTracking()
                .Where(a =>
                    a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                    a.VendorId == vendorUser.VendorId);

            int recordsTotal = await query.CountAsync();

            // -------------------------
            // Role-based filtering
            // -------------------------
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

            // -------------------------
            // Search (SQL)
            // -------------------------
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.Contains(search) ||
                    a.CustomerDetail.Name.Contains(search) ||
                    a.ClientCompany.Name.Contains(search) ||
                    a.PolicyDetail.InvestigationServiceType.Name.Contains(search)
                );
            }
            int recordsFiltered = await query.CountAsync();

            // -------------------------
            // Sorting
            // -------------------------
            query = orderColumn switch
            {
                0 => orderDir == "asc"
                    ? query.OrderBy(x => x.Created)
                    : query.OrderByDescending(x => x.Created),

                1 => orderDir == "asc"
                    ? query.OrderBy(x => x.PolicyDetail.ContractNumber)
                    : query.OrderByDescending(x => x.PolicyDetail.ContractNumber),

                2 => orderDir == "asc"
                    ? query.OrderBy(x => x.CustomerDetail.Name)
                    : query.OrderByDescending(x => x.CustomerDetail.Name),

                3 => orderDir == "asc"
                    ? query.OrderBy(x => x.ClientCompany.Name)
                    : query.OrderByDescending(x => x.ClientCompany.Name),

                _ => query.OrderByDescending(x => x.Created)
            };

            // -------------------------
            // Paging (SQL)
            // -------------------------
            var pageCases = await query
                .Skip(start)
                .Take(length)
                .Include(a => a.ClientCompany)
                .Include(a => a.PolicyDetail).ThenInclude(p => p.InvestigationServiceType)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.PinCode)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.District)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.State)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.PinCode)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.District)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.State)
                .ToListAsync();


            // -------------------------
            // Projection (ALL fields)
            // -------------------------
            var data = await Task.WhenAll(pageCases.Select(async a =>
            {
                var isUnderWriting = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = CustomExtensions.GetCultureByCountry(vendorUser.Country.Code);
                var pinCode = ClaimsInvestigationExtension.GetPincode(isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail);
                var pincodeName = ClaimsInvestigationExtension.GetPincodeName(isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail);
                var policy = $"<span class='badge badge-light'>{a.PolicyDetail.InsuranceType.GetEnumDisplayName()}</span>";
                var beneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" : a.BeneficiaryDetail.Name;
                var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDetail.DocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerPhotoTask = base64FileService.GetBase64FileAsync(ClaimsInvestigationExtension.GetPersonPhoto(
                    isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail));
                var beneficiaryPhotoTask = base64FileService.GetBase64FileAsync(a.BeneficiaryDetail?.ImagePath, Applicationsettings.NO_USER);
                var ownerImageTask = base64FileService.GetBase64FileAsync(await GetOwner(a), Applicationsettings.NO_USER);
                var ownerEmailTask = GetOwnerEmail(a);

                // Wait for all images for THIS case to load
                await Task.WhenAll(documentTask, customerPhotoTask, beneficiaryPhotoTask, ownerImageTask, ownerEmailTask);
                return new CaseInvestigationResponse
                {
                    Id = a.Id,
                    AssignedToAgency = a.IsNewSubmittedToAgent,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    PolicyNum = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(culture, "{0:C}", a.PolicyDetail.SumAssuredValue),
                    Agent = await ownerEmailTask,
                    OwnerDetail = await ownerImageTask,
                    CaseWithPerson = IsCaseWithAgent(a),
                    Pincode = pinCode,
                    PincodeName = pincodeName,
                    Company = a.ClientCompany.Name,
                    Document = await documentTask,
                    Customer = await customerPhotoTask,
                    Name = a.CustomerDetail.Name,
                    Policy = policy,
                    Status = a.Status,
                    ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = GetSupervisorOpenTimePending(a),
                    TimeElapsed = GetTimeElapsed(a),
                    BeneficiaryPhoto = await beneficiaryPhotoTask,
                    BeneficiaryName = beneficiaryName,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                };
        }));
            // -------------------------
            // Mark as viewed
            // -------------------------
            var idsToMarkViewed = data
                .Where(x => x.AssignedToAgency)
                .Select(x => x.Id)
                .ToList();

            if (idsToMarkViewed.Any())
            {
                await context.Investigations
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
                Data = data.ToList()
            };
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

            // -----------------------------
            // PAGING
            // -----------------------------
            var pageCases = await query
                .Skip(start)
                .Take(length)
                .Include(a => a.ClientCompany)
                .Include(a => a.PolicyDetail).ThenInclude(p => p.InvestigationServiceType)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.PinCode)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.District)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.State)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.PinCode)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.District)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.State)
                .ToListAsync();

            // -----------------------------
            // WEATHER (PARALLEL)
            // -----------------------------
            await Task.WhenAll(pageCases.Select(a =>
            {
                if (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING && a.CustomerDetail != null)
                {
                    return weatherInfoService.GetWeatherAsync(
                        a.CustomerDetail.Latitude,
                        a.CustomerDetail.Longitude)
                        .ContinueWith(t => a.CustomerDetail.AddressLocationInfo = t.Result);
                }

                if (a.BeneficiaryDetail != null)
                {
                    return weatherInfoService.GetWeatherAsync(
                        a.BeneficiaryDetail.Latitude,
                        a.BeneficiaryDetail.Longitude)
                        .ContinueWith(t => a.BeneficiaryDetail.AddressLocationInfo = t.Result);
                }

                return Task.CompletedTask;
            }));

            var data = await Task.WhenAll(pageCases.Select(async a =>
            {
                var isUnderWriting = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = CustomExtensions.GetCultureByCountry(vendorUser.Country.Code);
                var pincode = ClaimsInvestigationExtension.GetPincode(isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail);
                var pincodeName = ClaimsInvestigationExtension.GetPincodeName(isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail);
                var personName = isUnderWriting ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name;
                var policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName();
                var serviceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName();
                var service = a.PolicyDetail.InvestigationServiceType.Name;
                var timePending = GetSupervisorNewTimePending(a);
                var policyNum = GetPolicyNumForAgency(a, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);
                var beneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" : a.BeneficiaryDetail.Name;
                var timeElapsed = a.AllocatedToAgencyTime.HasValue ? DateTime.Now.Subtract(a.AllocatedToAgencyTime.Value).TotalSeconds : 0;
                var isQueryCase = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
                var personMapAddressUrl = isUnderWriting ?
                        string.Format(a.CustomerDetail.CustomerLocationMap, "400", "400") : string.Format(a.BeneficiaryDetail.BeneficiaryLocationMap, "400", "400");
                var addressLocationInfo = isUnderWriting ? a.CustomerDetail.AddressLocationInfo : a.BeneficiaryDetail.AddressLocationInfo;

                var ownerDetailTask = base64FileService.GetBase64FileAsync(a.ClientCompany.DocumentUrl);
                var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDetail.DocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerPhotoTask = base64FileService.GetBase64FileAsync(ClaimsInvestigationExtension.GetPersonPhoto(
                    isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail));
                var beneficiaryPhotoTask = base64FileService.GetBase64FileAsync(a.BeneficiaryDetail?.ImagePath, Applicationsettings.NO_USER);


                // Wait for all images for THIS case to load
                await Task.WhenAll(ownerDetailTask, documentTask, customerPhotoTask, beneficiaryPhotoTask);

                return new CaseInvestigationAgencyResponse
                {
                    Id = a.Id,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(culture, "{0:C}", a.PolicyDetail.SumAssuredValue),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = await ownerDetailTask,
                    Pincode = pincode,
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
                    AddressLocationInfo = addressLocationInfo
                };
            }));

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
                        ? query.OrderBy(x => x.AllocatedToAgencyTime == null)
                               .ThenByDescending(x => x.AllocatedToAgencyTime)
                        : query.OrderBy(x => x.AllocatedToAgencyTime == null)
                               .ThenBy(x => x.AllocatedToAgencyTime),
                _ => query.OrderByDescending(x => x.Created)
            };

            // -----------------------------
            // PAGING
            // -----------------------------
            var pageCases = await query
                .Skip(start)
                .Take(length)
                .Include(a => a.ClientCompany)
                .Include(a => a.PolicyDetail).ThenInclude(p => p.InvestigationServiceType)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.PinCode)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.District)
                .Include(a => a.CustomerDetail).ThenInclude(p => p.State)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.PinCode)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.District)
                .Include(a => a.BeneficiaryDetail).ThenInclude(p => p.State)
                .ToListAsync();

            // -----------------------------
            // WEATHER (PARALLEL)
            // -----------------------------
            await Task.WhenAll(pageCases.Select(a =>
            {
                if (a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING && a.CustomerDetail != null)
                {
                    return weatherInfoService.GetWeatherAsync(
                        a.CustomerDetail.Latitude,
                        a.CustomerDetail.Longitude)
                        .ContinueWith(t => a.CustomerDetail.AddressLocationInfo = t.Result);
                }

                if (a.BeneficiaryDetail != null)
                {
                    return weatherInfoService.GetWeatherAsync(
                        a.BeneficiaryDetail.Latitude,
                        a.BeneficiaryDetail.Longitude)
                        .ContinueWith(t => a.BeneficiaryDetail.AddressLocationInfo = t.Result);
                }

                return Task.CompletedTask;
            }));

            var data = await Task.WhenAll(pageCases.Select(async a =>
            {
                var isUnderWriting = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = CustomExtensions.GetCultureByCountry(vendorUser.Country.Code);
                var pincode = ClaimsInvestigationExtension.GetPincode(isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail);
                var pincodeName = ClaimsInvestigationExtension.GetPincodeName(isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail);
                var personName = isUnderWriting ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name;
                var policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName();
                var serviceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName();
                var service = a.PolicyDetail.InvestigationServiceType.Name;
                var timePending = GetSupervisorNewTimePending(a);
                var policyNum = GetPolicyNumForAgency(a, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);
                var beneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" : a.BeneficiaryDetail.Name;
                var timeElapsed = a.AllocatedToAgencyTime.HasValue ? DateTime.Now.Subtract(a.AllocatedToAgencyTime.Value).TotalSeconds : 0;
                var isQueryCase = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
                var personMapAddressUrl = isUnderWriting ?
                        string.Format(a.CustomerDetail.CustomerLocationMap, "400", "400") : string.Format(a.BeneficiaryDetail.BeneficiaryLocationMap, "400", "400");
                var addressLocationInfo = isUnderWriting ? a.CustomerDetail.AddressLocationInfo : a.BeneficiaryDetail.AddressLocationInfo;

                var ownerDetailTask = base64FileService.GetBase64FileAsync(a.ClientCompany.DocumentUrl);
                var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDetail.DocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerPhotoTask = base64FileService.GetBase64FileAsync(ClaimsInvestigationExtension.GetPersonPhoto(
                    isUnderWriting, a.CustomerDetail, a.BeneficiaryDetail));
                var beneficiaryPhotoTask = base64FileService.GetBase64FileAsync(a.BeneficiaryDetail?.ImagePath, Applicationsettings.NO_USER);


                // Wait for all images for THIS case to load
                await Task.WhenAll(ownerDetailTask, documentTask, customerPhotoTask, beneficiaryPhotoTask);

                return new CaseInvestigationAgencyResponse
                {
                    Id = a.Id,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(culture, "{0:C}", a.PolicyDetail.SumAssuredValue),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = await ownerDetailTask,
                    Pincode = pincode,
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
                    AddressLocationInfo = addressLocationInfo
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
        private static string GetSupervisorNewTimePending(InvestigationTask caseTask)
        {
            DateTime timeToCompare = caseTask.AllocatedToAgencyTime.Value;

            if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                timeToCompare = caseTask.EnquiredByAssessorTime.GetValueOrDefault();
            }

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        private static string GetPolicyNumForAgency(InvestigationTask caseTask, string enquiryStatus)
        {
            if (caseTask is not null)
            {
                var isRequested = caseTask.SubStatus == enquiryStatus;
                if (isRequested)
                {
                    return string.Join("", caseTask.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"ENQUIRY\"></i>");
                }

            }
            return string.Join("", caseTask.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style-none\"></i>");
        }

        private static double GetTimeElapsed(InvestigationTask caseTask)
        {

            var timeElapsed = DateTime.Now.Subtract(caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ? caseTask.TaskToAgentTime.Value :
                                                     caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ? caseTask.SubmittedToAssessorTime.Value :
                                                     caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ?
                                                     caseTask.EnquiryReplyByAssessorTime.Value : caseTask.Created).TotalSeconds;
            return timeElapsed;
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

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        private static bool IsCaseWithAgent(InvestigationTask caseTask)
        {
            return (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
        }
        private async Task<string> GetOwnerEmail(InvestigationTask caseTask)
        {
            string ownerEmail = string.Empty;
            var allocated2agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;

            if (caseTask.SubStatus == allocated2agent)
            {
                ownerEmail = caseTask.TaskedAgentEmail;
                var agencyUser =await context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == ownerEmail);
                if (agencyUser != null && !string.IsNullOrWhiteSpace(agencyUser.Email))
                {
                    return agencyUser?.Email;
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR || caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                var companyUser =await context.ClientCompany.FirstOrDefaultAsync(v => v.ClientCompanyId == caseTask.ClientCompanyId);
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

            if (caseTask.SubStatus == allocated2agent)
            {
                ownerEmail = caseTask.TaskedAgentEmail;
                var agencyUser = context.ApplicationUser.FirstOrDefault(u => u.Email == ownerEmail);
                if (agencyUser != null && !string.IsNullOrWhiteSpace(agencyUser?.ProfilePictureUrl))
                {
                    return agencyUser.ProfilePictureUrl;
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR || caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                var companyUser = context.ClientCompany.FirstOrDefault(u => u.ClientCompanyId == caseTask.ClientCompanyId);
                if (companyUser != null && !string.IsNullOrWhiteSpace(companyUser?.DocumentUrl))
                {
                    return companyUser.DocumentUrl;
                }
            }
            return noDataImagefilePath;
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
        private static string GetSupervisorCompletedTimePending(InvestigationTask caseTask)
        {
            DateTime timeToCompare = caseTask.ProcessedByAssessorTime.Value;

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

    }
}
