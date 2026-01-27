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
        Task<List<CaseInvestigationResponse>> GetOpenCases(string userEmail);
        Task<List<CaseInvestigationAgencyResponse>> GetAgentReports(string userEmail);
        Task<List<CaseInvestigationAgencyResponse>> GetCompletedCases(string userEmail, string userClaim);
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
                var culture = Extensions.GetCultureByCountry(vendorUser.Country.Code);
                var ownerDetailTask = base64FileService.GetBase64FileAsync(a.ClientCompany.DocumentUrl);
                var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDetail.DocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerPhotoTask = base64FileService.GetBase64FileAsync(ClaimsInvestigationExtension.GetPersonPhoto(
                    a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail));
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
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    AssignedToAgency = a.AssignedToAgency,
                    Document = await documentTask,
                    Customer = await customerPhotoTask,
                    Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                    Policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                    Status = a.Status,
                    ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = GetSupervisorNewTimePending(a),
                    PolicyNum = GetPolicyNumForAgency(a, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR),
                    BeneficiaryPhoto = await beneficiaryPhotoTask,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" : a.BeneficiaryDetail.Name,
                    TimeElapsed = a.AllocatedToAgencyTime.HasValue ? DateTime.Now.Subtract(a.AllocatedToAgencyTime.Value).TotalSeconds : 0,
                    IsNewAssigned = a.IsNewAssignedToAgency,
                    IsQueryCase = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR,
                    PersonMapAddressUrl = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ?
                        string.Format(a.CustomerDetail.CustomerLocationMap, "400", "400") : string.Format(a.BeneficiaryDetail.BeneficiaryLocationMap, "400", "400"),
                    AddressLocationInfo = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.AddressLocationInfo : a.BeneficiaryDetail.AddressLocationInfo
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

        public async Task<List<CaseInvestigationResponse>> GetOpenCases(string userEmail)
        {
            var vendorUser = await context.ApplicationUser
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);
            List<InvestigationTask> caseTasks = null;
            if (vendorUser.Role.ToString() == SUPERVISOR.DISPLAY_NAME)
            {
                caseTasks = await context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Where(a => a.Status == CONSTANTS.CASE_STATUS.INPROGRESS)
                .Where(a => a.VendorId == vendorUser.VendorId)
                .Where(a => (a.AllocatingSupervisordEmail == userEmail &&
                            a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT) ||
                            ((a.SubmittingSupervisordEmail == userEmail) &&
                            (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                             a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR))).ToListAsync();
            }
            else
            {
                caseTasks = await context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Where(a => a.Status == CONSTANTS.CASE_STATUS.INPROGRESS)
                .Where(a => a.VendorId == vendorUser.VendorId &&
                            (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ||
                            a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                             a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)).ToListAsync();
            }

            var response = caseTasks?.Select(a => new CaseInvestigationResponse
            {
                Id = a.Id,
                AssignedToAgency = a.IsNewSubmittedToAgent,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                Agent = GetOwnerEmail(a),
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwner(a))),
                CaseWithPerson = IsCaseWithAgent(a),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Company = a.ClientCompany.Name,
                Document = a.PolicyDetail.DocumentPath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) : Applicationsettings.NO_POLICY_IMAGE,
                Customer =
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail))))),
                Name = a.CustomerDetail.Name,
                Policy = $"<span class='badge badge-light'>{a.PolicyDetail.InsuranceType.GetEnumDisplayName()}</span>",
                Status = a.Status,
                ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetSupervisorOpenTimePending(a),
                PolicyNum = a.PolicyDetail.ContractNumber,
                BeneficiaryPhoto = a.BeneficiaryDetail.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ? "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = GetTimeElapsed(a),
                PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration
            })?.ToList();

            var idsToMarkViewed = response.Where(x => x.IsNewAssigned.GetValueOrDefault()).Select(x => x.Id).ToList();
            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate =await context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToListAsync();

                foreach (var entity in entitiesToUpdate)
                    if (entity.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
                    {
                        entity.IsNewSubmittedToAgent = false;
                    }
                    else if (entity.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
                    {
                        entity.IsNewSubmittedToCompany = false;
                    }

                await context.SaveChangesAsync(null, false); // mark as viewed
            }

            return response;
        }

        public async Task<List<CaseInvestigationAgencyResponse>> GetCompletedCases(string userEmail, string userClaim)
        {
            var agencyUser = await context.ApplicationUser
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            var finishedStatus = CONSTANTS.CASE_STATUS.FINISHED;
            var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
            var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

            var caseTasks = context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Where(a => a.VendorId == agencyUser.VendorId && a.Status == finishedStatus && (a.SubStatus == approvedStatus || a.SubStatus == rejectedStatus));

            if (agencyUser.Role.ToString() == SUPERVISOR.DISPLAY_NAME)
            {
                caseTasks = caseTasks
                    .Where(a => a.SubmittedAssessordEmail == userEmail);
            }
            var responseData = await caseTasks.ToListAsync();
            var response = responseData
                .Select(a => new CaseInvestigationAgencyResponse
                {
                    Id = a.Id,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(agencyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.ClientCompany.DocumentUrl)))),
                    Document = a.PolicyDetail.DocumentPath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) : Applicationsettings.NO_POLICY_IMAGE,
                    Customer =
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail))))),
                    Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                    Policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                    Status = a.Status,
                    ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = GetSupervisorCompletedTimePending(a),
                    PolicyNum = a.PolicyDetail.ContractNumber,
                    BeneficiaryPhoto = a.BeneficiaryDetail.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                                    "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>" :
                                    a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime.Value).TotalSeconds,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration,
                    CanDownload = CanDownload(a.Id, userClaim)
                })
                .ToList();
            return response;
        }
        public async Task<List<CaseInvestigationAgencyResponse>> GetAgentReports(string userEmail)
        {

            // Fetch the vendor user along with the related Vendor and Country info in one query
            var vendorUser = await context.ApplicationUser
                .Include(v => v.Country)
                .Include(u => u.Vendor)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            // Filter the claims based on the vendor ID and required status
            var caseTasks = context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Where(a => a.VendorId == vendorUser.VendorId &&
                            a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var responseData = await caseTasks.ToListAsync();
            var response = responseData.Select(a =>
                new CaseInvestigationAgencyResponse
                {
                    Id = a.Id,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.ClientCompany.DocumentUrl)))),
                    Document = a.PolicyDetail.DocumentPath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) : Applicationsettings.NO_POLICY_IMAGE,
                    Customer =
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail))))),
                    Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                    Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Status = a.SubStatus,
                    ServiceType = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    RawStatus = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = GetSupervisorReportTimePending(a),
                    PolicyNum = a.PolicyDetail.ContractNumber,
                    BeneficiaryPhoto = a.BeneficiaryDetail.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                            "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" :
                            a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.SubmittedToSupervisorTime.Value).TotalSeconds,
                    IsNewAssigned = a.IsNewSubmittedToAgency,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                }).ToList();
            var idsToMarkViewed = response.Where(x => x.IsNewAssigned.GetValueOrDefault()).Select(x => x.Id).ToList();
            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate =await context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToListAsync();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNewSubmittedToAgency = false;

                await context.SaveChangesAsync(null, false); // mark as viewed
            }
            return response;
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
        private string GetOwnerEmail(InvestigationTask caseTask)
        {
            string ownerEmail = string.Empty;
            var allocated2agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;

            if (caseTask.SubStatus == allocated2agent)
            {
                ownerEmail = caseTask.TaskedAgentEmail;
                var agencyUser = context.ApplicationUser.FirstOrDefault(u => u.Email == ownerEmail);
                if (agencyUser != null && !string.IsNullOrWhiteSpace(agencyUser.Email))
                {
                    return agencyUser?.Email;
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR || caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                var companyUser = context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == caseTask.ClientCompanyId);
                if (companyUser != null && !string.IsNullOrWhiteSpace(companyUser.Email))
                {
                    return companyUser.Email;
                }
            }
            return "noDataimage";
        }
        private byte[] GetOwner(InvestigationTask caseTask)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var allocated2agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
            var noDataImagefilePath = Path.Combine(env.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);

            if (caseTask.SubStatus == allocated2agent)
            {
                ownerEmail = caseTask.TaskedAgentEmail;
                var agencyUser = context.ApplicationUser.FirstOrDefault(u => u.Email == ownerEmail);
                if (agencyUser != null && !string.IsNullOrWhiteSpace(agencyUser?.ProfilePictureUrl))
                {
                    var agencyUserImagePath = Path.Combine(env.ContentRootPath, agencyUser.ProfilePictureUrl);
                    if (System.IO.File.Exists(agencyUserImagePath))
                    {
                        return System.IO.File.ReadAllBytes(agencyUserImagePath);
                    }
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR || caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                var companyUser = context.ClientCompany.FirstOrDefault(u => u.ClientCompanyId == caseTask.ClientCompanyId);
                if (companyUser != null && !string.IsNullOrWhiteSpace(companyUser?.DocumentUrl))
                {
                    var companyUserImagePath = Path.Combine(env.ContentRootPath, companyUser.DocumentUrl);
                    if (System.IO.File.Exists(companyUserImagePath))
                    {
                        return System.IO.File.ReadAllBytes(companyUserImagePath);
                    }
                }
            }
            return noDataimage;
        }
        private static string GetSupervisorReportTimePending(InvestigationTask caseTask)
        {
            DateTime timeToCompare = caseTask.SubmittedToSupervisorTime.Value;

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
