using System.Security.Claims;

using Google.Api;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using SkiaSharp;

using static risk.control.system.Helpers.Permissions;

namespace risk.control.system.Services
{
    public interface IVendorInvestigationService
    {
        Task<int> GetAutoCount(string currentUserEmail);
        Task<object> GetAuto(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        Task<object> GetActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        Task<CaseTransactionModel> GetClaimDetails(string currentUserEmail, long id);
        List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors);
        Task<CaseInvestigationVendorAgentModel> SelectVendorAgent(string userEmail, long selectedcase);
        Task<InvestigationTask> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, long claimsInvestigationId);
        Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, long claimsInvestigationId, string remarks, string? answer1, string? answer2, string? answer3, string? answer4);

    }
    public class VendorInvestigationService : IVendorInvestigationService
    {
        private readonly ApplicationDbContext context;
        private readonly INumberSequenceService numberService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ITimelineService timelineService;
        private readonly ICustomApiCLient customApiCLient;

        public VendorInvestigationService(ApplicationDbContext context, 
            INumberSequenceService numberService, 
            IWebHostEnvironment webHostEnvironment,
            ITimelineService timelineService,
            ICustomApiCLient customApiCLient)
        {
            this.context = context;
            this.numberService = numberService;
            this.webHostEnvironment = webHostEnvironment;
            this.timelineService = timelineService;
            this.customApiCLient = customApiCLient;
        }
        public async Task<CaseTransactionModel> GetClaimDetails(string currentUserEmail, long id)
        {
            var claim = await context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.InvestigationTimeline)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.ClientCompany)
                .Include(c => c.Vendor)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            var lastHistory = claim.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.Now - lastHistory.StatusChangedAt;
            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = claim,
                CaseIsValidToAssign = claim.IsValidCaseData(),
                Location = claim.BeneficiaryDetail,
                Assigned = claim.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                TimeTaken = timeTaken.ToString(@"hh\:mm\:ss") ?? "-",
                Withdrawable = (claim.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }
        public List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors)
        {
            // Get relevant status IDs in one query
            var relevantStatuses = new[]
                {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
                }; // Improves lookup performance

            // Fetch cases that match the criteria
            var vendorCaseCount = context.Investigations
                .Where(c => !c.Deleted &&
                            c.VendorId.HasValue &&
                            c.AssignedToAgency &&
                            relevantStatuses.Contains(c.SubStatus))
                .GroupBy(c => c.VendorId.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // Create the list of VendorIdWithCases
            return existingVendors
                .Select(vendorId => new VendorIdWithCases
                {
                    VendorId = vendorId,
                    CaseCount = vendorCaseCount.GetValueOrDefault(vendorId, 0)
                })
                .ToList();
        }
        public async Task<object> GetAuto(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var companyUser = await context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            if (companyUser == null)
                return null;
            var company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            // Fetching all relevant substatuses in a single query for efficiency

            var query = context.Investigations
                .Include(i=>i.PolicyDetail)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i=>i.CustomerDetail)
                .ThenInclude(i=>i.PinCode)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.Country)
                .Include(i=>i.BeneficiaryDetail)
                .ThenInclude(i => i.PinCode)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.Country)

                .Where(a => !a.Deleted &&
                    a.ClientCompanyId == companyUser.ClientCompanyId && a.CreatedUser == currentUserEmail &&
                    (
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY ||
                        a.SubStatus== CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER
                    )
                );

            int totalRecords = query.Count(); // Get total count before pagination

            // Search filtering
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.ToLower().Contains(search) ||
                    a.PolicyDetail.CauseOfLoss.ToLower().Contains(search) ||
                    a.PolicyDetail.LineOfBusiness.Name.ToLower().Contains(search) ||
                    a.PolicyDetail.InvestigationServiceType.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.DateOfBirth.ToString().ToLower().Contains(search) ||
                    a.CustomerDetail.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.ContactNumber.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Code.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Name.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.ContactNumber.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(caseType))
            {
                query = query.Where(c => c.PolicyDetail.InsuranceType.GetEnumDisplayName().ToLower() == caseType.ToLower());  // Assuming CaseType is the field in your data model
            }
            var data = query.AsEnumerable();
            int recordsFiltered = query.Count();


            // Calculate TimeElapsed and Transform Data
            var transformedData = data.Select(a => new
            {
                Id = a.Id,
                IsNew = a.IsNew,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                PolicyId = a.PolicyDetail.ContractNumber,
                AssignedToAgency = a.AssignedToAgency,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeCode = ClaimsInvestigationExtension.GetPincodeCode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-light\">customer name</span>",
                Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                IsUploaded = a.IsUploaded,
                Origin = a.ORIGIN.GetEnumDisplayName().ToLower(),
                SubStatus = a.SubStatus,
                Ready2Assign = a.IsReady2Assign,
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.ORIGIN.GetEnumDisplayName(),
                Created = a.Created.ToString("dd-MM-yyyy"),
                ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ( {a.PolicyDetail.InvestigationServiceType.Name})",
                timePending = GetDraftedTimePending(a),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-light\">beneficiary name</span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).TotalSeconds,
                BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "?" : a.BeneficiaryDetail.Name,
                CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.Name) ? "?" : a.CustomerDetail.Name,
                PersonMapAddressUrl = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail) != "..." ?
                        a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap : Applicationsettings.NO_MAP
            });

            // Apply Sorting AFTER Data Transformation
            if (!string.IsNullOrEmpty(orderDir))
            {
                switch (orderColumn)
                {
                    case 1: // Sort by Policy Number
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.PolicyId)
                            : transformedData.OrderByDescending(a => a.PolicyId);
                        break;

                    case 2: // Sort by Amount (Ensure proper sorting of numeric values)
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.Amount)
                            : transformedData.OrderByDescending(a => a.Amount);
                        break;

                    case 3: // Sort by Amount (Ensure proper sorting of numeric values)
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.PincodeCode)
                            : transformedData.OrderByDescending(a => a.PincodeCode);
                        break;

                    case 6: // Sort by Customer Full Name
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.CustomerFullName)
                            : transformedData.OrderByDescending(a => a.CustomerFullName);
                        break;

                    case 8: // Sort by Beneficiary Full Name
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.BeneficiaryFullName)
                            : transformedData.OrderByDescending(a => a.BeneficiaryFullName);
                        break;


                    case 9: // Sort by Status
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.ServiceType)
                            : transformedData.OrderByDescending(a => a.ServiceType);
                        break;

                    case 10: // Sort by Status
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.Location)
                            : transformedData.OrderByDescending(a => a.Location);
                        break;

                    case 11: // Sort by Created Date
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => DateTime.ParseExact(a.Created, "dd-MM-yyyy", null))
                            : transformedData.OrderByDescending(a => DateTime.ParseExact(a.Created, "dd-MM-yyyy", null));
                        break;

                    case 12: // Sort by TimeElapsed
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.TimeElapsed)
                            : transformedData.OrderByDescending(a => a.TimeElapsed);
                        break;

                    default: // Default Sorting (if needed)
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.TimeElapsed)
                            : transformedData.OrderByDescending(a => a.TimeElapsed);
                        break;
                }
            }
            // Apply Pagination
            var pagedData = transformedData.Skip(start).Take(length).ToList();
            // Prepare Response

            var idsToMarkViewed = pagedData.Where(x => x.IsNew).Select(x => x.Id).ToList();

            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNew = false;

                await context.SaveChangesAsync(); // mark as viewed
            }

            var response = new
            {
                draw = draw,
                AutoAllocatopn = company.AutoAllocation,
                recordsTotal = totalRecords,
                recordsFiltered = recordsFiltered,
                data = pagedData
            };

            return response;

        }
        string GetDraftedTimePending(InvestigationTask a)
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
        public async Task<object> GetActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var companyUser = await context.ClientCompanyApplicationUser
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            var openStatuses = new[] { CONSTANTS.CASE_STATUS.INITIATED, CONSTANTS.CASE_STATUS.INPROGRESS };

            var createdStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR;
            var assigned2AssignerStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;
            var withdrawnByCompanyStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY;
            var declinedByAgencyStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY;
            var requestedByCompanyStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;

            var subStatus = new[]
            {
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
            };
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
                .Where(a => !a.Deleted && a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                            a.ClientCompanyId == companyUser.ClientCompanyId && a.CreatedUser == currentUserEmail &&
                            !subStatus
                            .Contains(a.SubStatus));

            int totalRecords = query.Count(); // Get total count before pagination
            // Search filtering
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.ToLower().Contains(search) ||
                     a.PolicyDetail.CauseOfLoss.ToLower().Contains(search) ||
                    a.PolicyDetail.LineOfBusiness.Name.ToLower().Contains(search) ||
                    a.PolicyDetail.InvestigationServiceType.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.DateOfBirth.ToString().ToLower().Contains(search) ||
                    a.CustomerDetail.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.ContactNumber.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Code.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Name.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.ContactNumber.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(caseType))
            {
                query = query.Where(c => c.PolicyDetail.InsuranceType.GetEnumDisplayName().ToLower() == caseType.ToLower());  // Assuming CaseType is the field in your data model
            }

            var data = query.AsEnumerable();
            int recordsFiltered = query.Count();

            var transformedData = data.Select(a => new
            {
                Id = a.Id,
                IsNew = a.IsNew,
                CustomerFullName = a.CustomerDetail?.Name ?? "",
                BeneficiaryFullName = a.BeneficiaryDetail?.Name ?? "",
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = a.Vendor.Email,
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwnerImage(a))),
                CaseWithPerson = a.CaseOwner,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                Status = a.ORIGIN.GetEnumDisplayName(),
                SubStatus = a.SubStatus,
                Ready2Assign = a.IsReady2Assign,
                ServiceType = $"{a.PolicyDetail.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = a.GetCreatorTimePending(),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).TotalSeconds, // Calculate here
                PersonMapAddressUrl = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap
            }); // Materialize the list

            // Apply Sorting AFTER Data Transformation
            if (!string.IsNullOrEmpty(orderDir))
            {
                switch (orderColumn)
                {
                    case 0: // Sort by Policy Number
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.PolicyId)
                            : transformedData.OrderByDescending(a => a.PolicyId);
                        break;

                    case 1: // Sort by Amount (Ensure proper sorting of numeric values)
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.Amount)
                            : transformedData.OrderByDescending(a => a.Amount);
                        break;

                    case 6: // Sort by Customer Full Name
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.CustomerFullName)
                            : transformedData.OrderByDescending(a => a.CustomerFullName);
                        break;

                    case 8: // Sort by Beneficiary Full Name
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.BeneficiaryFullName)
                            : transformedData.OrderByDescending(a => a.BeneficiaryFullName);
                        break;


                    case 9: // Sort by Status
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.ServiceType)
                            : transformedData.OrderByDescending(a => a.ServiceType);
                        break;

                    case 10: // Sort by Status
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.SubStatus)
                            : transformedData.OrderByDescending(a => a.SubStatus);
                        break;

                    case 11: // Sort by Created Date
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => DateTime.ParseExact(a.Created, "dd-MM-yyyy", null))
                            : transformedData.OrderByDescending(a => DateTime.ParseExact(a.Created, "dd-MM-yyyy", null));
                        break;

                    case 13: // Sort by TimeElapsed
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.TimeElapsed)
                            : transformedData.OrderByDescending(a => a.TimeElapsed);
                        break;

                    default: // Default Sorting (if needed)
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.TimeElapsed)
                            : transformedData.OrderByDescending(a => a.TimeElapsed);
                        break;
                }
            }

            // Apply Pagination
            var pagedData = transformedData.Skip(start).Take(length).ToList();
            // Prepare Response

            var idsToMarkViewed = pagedData.Where(x => x.IsNew).Select(x => x.Id).ToList();

            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNew = false;

                await context.SaveChangesAsync(); // mark as viewed
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
        private byte[] GetOwnerImage(InvestigationTask a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");
            var noDataimage = File.ReadAllBytes(noDataImagefilePath);

            if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR || a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                ownerEmail = a.Vendor.Email;
                var agentProfile = context.Vendor.FirstOrDefault(u => u.Email == ownerEmail)?.DocumentImage;
                if (agentProfile != null)
                {
                    return agentProfile;
                }
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                var vendorImage = context.VendorApplicationUser.FirstOrDefault(v => v.Email == ownerDomain)?.ProfilePicture;
                if (vendorImage != null)
                {
                    return vendorImage;
                }
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
            {
                var companyImage = context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == a.ClientCompanyId)?.DocumentImage;
                if (companyImage != null)
                {
                    return companyImage;
                }
            }
            return noDataimage;
        }
        public string GetOwner(InvestigationTask a)
        {
            if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR || 
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                return a.Vendor.Email;
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                return a.CaseOwner;
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR
                )
            {
                var company = context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == a.ClientCompanyId);
                if (company != null)
                {
                    return company.Email;
                }
            }
            return string.Empty;
        }
        public async Task<int> GetAutoCount(string currentUserEmail)
        {
            var companyUser = await context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            if (companyUser == null)
                return 0;
            var company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            // Fetching all relevant substatuses in a single query for efficiency
            var subStatuses =  new[]
                {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                };

            var query = context.Investigations
                .Where(a => !a.Deleted &&
                    a.ClientCompanyId == companyUser.ClientCompanyId &&
                    (
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY  ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY  ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER
                ));

            int totalRecords = query.Count(); // Get total count before pagination
            return totalRecords;
        }

        public async Task<CaseInvestigationVendorAgentModel> SelectVendorAgent(string userEmail, long selectedcase)
        {
            var claimsAllocate2Agent = GetCases().FirstOrDefault(v => v.Id == selectedcase);

            var beneficiaryDetail = await context.BeneficiaryDetail
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.BeneficiaryDetailId == claimsAllocate2Agent.BeneficiaryDetail.BeneficiaryDetailId);

            var maskedCustomerContact = new string('*', claimsAllocate2Agent.CustomerDetail.ContactNumber.ToString().Length - 4) + claimsAllocate2Agent.CustomerDetail.ContactNumber.ToString().Substring(claimsAllocate2Agent.CustomerDetail.ContactNumber.ToString().Length - 4);
            claimsAllocate2Agent.CustomerDetail.ContactNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', beneficiaryDetail.ContactNumber.ToString().Length - 4) + beneficiaryDetail.ContactNumber.ToString().Substring(beneficiaryDetail.ContactNumber.ToString().Length - 4);
            claimsAllocate2Agent.BeneficiaryDetail.ContactNumber = maskedBeneficiaryContact;
            beneficiaryDetail.ContactNumber = maskedBeneficiaryContact;

            var model = new CaseInvestigationVendorAgentModel
            {
                CaseLocation = beneficiaryDetail,
                ClaimsInvestigation = claimsAllocate2Agent,
            };
            return model;
        }

        public async Task<InvestigationTask> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, long claimsInvestigationId)
        {
            try
            {
                var assignedToAgent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
                var claim = context.Investigations
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .Include(c => c.BeneficiaryDetail)
                    .Where(c => c.Id == claimsInvestigationId).FirstOrDefault();
                var agentUser = context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(u => u.Email == vendorAgentEmail);

                string drivingDistance, drivingDuration, drivingMap;
                float distanceInMeters;
                int durationInSeconds;
                string LocationLatitude = string.Empty;
                string LocationLongitude = string.Empty;
                if (claim.PolicyDetail?.InsuranceType == InsuranceType.UNDERWRITING)
                {
                    LocationLatitude = claim.CustomerDetail?.Latitude;
                    LocationLongitude = claim.CustomerDetail?.Longitude;
                }
                else
                {
                    LocationLatitude = claim.BeneficiaryDetail?.Latitude;
                    LocationLongitude = claim.BeneficiaryDetail?.Longitude;
                }
                (drivingDistance, distanceInMeters, drivingDuration, durationInSeconds, drivingMap) = await customApiCLient.GetMap(double.Parse(agentUser.AddressLatitude), double.Parse(agentUser.AddressLongitude), double.Parse(LocationLatitude), double.Parse(LocationLongitude));
                claim.AllocatingSupervisordEmail = currentUser;
                claim.CaseOwner = vendorAgentEmail;
                claim.TaskedAgentEmail = vendorAgentEmail;
                claim.IsNewAssignedToAgency = true;
                claim.Updated = DateTime.Now;
                claim.UpdatedBy = currentUser;
                claim.SubStatus = assignedToAgent;
                claim.SelectedAgentDrivingDistance = drivingDistance;
                claim.SelectedAgentDrivingDuration = drivingDuration;
                claim.SelectedAgentDrivingDistanceInMetres = distanceInMeters;
                claim.SelectedAgentDrivingDurationInSeconds = durationInSeconds;
                claim.SelectedAgentDrivingMap = drivingMap;
                claim.TaskToAgentTime = DateTime.Now;
                
                context.Investigations.Update(claim);
                var rows = await context.SaveChangesAsync();

                await timelineService.UpdateTaskStatus(claim.Id,currentUser);
                return claim;

            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, long claimsInvestigationId, string remarks, string? answer1, string? answer2, string? answer3, string? answer4)
        {
            try
            {
                var agent = context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(a => a.Email.Trim().ToLower() == userEmail.ToLower());

                var submitted2Supervisor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR;

                var claim = GetCases().Include(r=>r.InvestigationReport.InvestigationAgencyReport.CaseQuestionnaire)
                    .FirstOrDefault(c => c.Id == claimsInvestigationId);

                claim.Updated = DateTime.Now;
                claim.UpdatedBy = agent.FirstName + " " + agent.LastName + "(" + agent.Email + ")";
                claim.SubStatus = submitted2Supervisor;
                claim.SubmittedToSupervisorTime = DateTime.Now;
                var claimReport = claim.InvestigationReport.InvestigationAgencyReport;

                claimReport.CaseQuestionnaire.Answers[1] = answer1;
                claimReport.CaseQuestionnaire.Answers[2] = answer2;
                claimReport.CaseQuestionnaire.Answers[3] = answer3;
                claimReport.CaseQuestionnaire.Answers[4] = answer4;
                claimReport.AgentRemarks = remarks;
                claimReport.AgentRemarksUpdated = DateTime.Now;
                claimReport.AgentEmail = userEmail;

                await timelineService.UpdateTaskStatus(claim.Id, userEmail);

                var rows = await context.SaveChangesAsync();
                return (agent.Vendor, claim.PolicyDetail.ContractNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private IQueryable<InvestigationTask> GetCases()
        {
            var claim = context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.InvestigationTimeline)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.ClientCompany)
                .Include(c => c.Vendor)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode);
            return claim;
        }
    }
}
