using System.Security.Claims;

using Google.Api;

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
    public interface IInvestigationService
    {
        Task<int> GetAutoCount(string currentUserEmail);
        Task<object> GetAuto(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        InvestigationCreateModel Create(string currentUserEmail);
        InvestigationTask AddCasePolicy(string userEmail);
        Task<InvestigationTask> CreatePolicy(string userEmail, InvestigationTask claimsInvestigation, IFormFile? claimDocument);
        Task<InvestigationTask> EditPolicy(string userEmail, InvestigationTask claimsInvestigation, IFormFile? claimDocument);
        Task<CaseTransactionModel> GetClaimDetails(string currentUserEmail, long id);

    }
    public class InvestigationService : IInvestigationService
    {
        private readonly ApplicationDbContext context;
        private readonly INumberSequenceService numberService;

        public InvestigationService(ApplicationDbContext context, INumberSequenceService numberService)
        {
            this.context = context;
            this.numberService = numberService;
        }

        public InvestigationCreateModel Create(string currentUserEmail)
        {
            var companyUser = context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == currentUserEmail);
            var claim = new InvestigationTask
            {
                ClientCompany = companyUser.ClientCompany
            };
            bool userCanCreate = true;
            int availableCount = 0;
            var trial = companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial;
            if (trial)
            {
                var totalClaimsCreated = context.Investigations.Include(c => c.PolicyDetail).Where(c => !c.Deleted &&
                    c.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
                availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated.Count;

                if (totalClaimsCreated?.Count >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    userCanCreate = false;
                }
            }
            var model = new InvestigationCreateModel
            {
                InvestigationTask = claim,
                AllowedToCreate = userCanCreate,
                AutoAllocation = companyUser.ClientCompany.AutoAllocation,
                BeneficiaryDetail = new BeneficiaryDetail { },
                AvailableCount = availableCount,
                TotalCount = companyUser.ClientCompany.TotalCreatedClaimAllowed,
                Trial = trial
            };
            return model;
        }
        public InvestigationTask AddCasePolicy(string userEmail)
        {
            var createdStatus = context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var contractNumber = numberService.GetNumberSequence("PX");
            var model = new InvestigationTask
            {
                PolicyDetail = new PolicyDetail
                {
                    InsuranceType = InsuranceType.UNDERWRITING,
                    CaseEnablerId = context.CaseEnabler.FirstOrDefault().CaseEnablerId,
                    CauseOfLoss = "LOST IN ACCIDENT",
                    ClaimType = ClaimType.DEATH,
                    ContractIssueDate = DateTime.Now.AddDays(-10),
                    CostCentreId = context.CostCentre.FirstOrDefault().CostCentreId,
                    DateOfIncident = DateTime.Now.AddDays(-3),
                    InvestigationServiceTypeId = context.InvestigationServiceType.FirstOrDefault(i => i.InsuranceType == InsuranceType.UNDERWRITING).InvestigationServiceTypeId,
                    Comments = "SOMETHING FISHY",
                    SumAssuredValue = new Random().Next(10000, 99999),
                    ContractNumber = contractNumber
                },
                Status = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR
            };
            return model;
        }

        public async Task<InvestigationTask> CreatePolicy(string userEmail, InvestigationTask claimsInvestigation, IFormFile? claimDocument)
        {
            try
            {
                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    claimsInvestigation.PolicyDetail.DocumentImage = dataStream.ToArray();
                }

                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.ORIGIN = ORIGIN.USER;
                claimsInvestigation.UpdatedBy = userEmail;
                claimsInvestigation.Status = CONSTANTS.CASE_STATUS.INITIATED;
                claimsInvestigation.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR;
                claimsInvestigation.CreatorSla = currentUser.ClientCompany.CreatorSla;
                claimsInvestigation.ClientCompany = currentUser.ClientCompany;
                var aaddedClaimId = context.Investigations.Add(claimsInvestigation);

                return await context.SaveChangesAsync() > 0 ? claimsInvestigation : null!;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null!;
            }
        }
        public async Task<InvestigationTask> EditPolicy(string userEmail, InvestigationTask claimsInvestigation, IFormFile? claimDocument)
        {
            try
            {
                var existingPolicy = await context.Investigations
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.ClientCompany)
                        .FirstOrDefaultAsync(c => c.Id == claimsInvestigation.Id);
                existingPolicy.PolicyDetail.ContractIssueDate = claimsInvestigation.PolicyDetail.ContractIssueDate;
                existingPolicy.PolicyDetail.InvestigationServiceTypeId = claimsInvestigation.PolicyDetail.InvestigationServiceTypeId;
                existingPolicy.PolicyDetail.ClaimType = claimsInvestigation.PolicyDetail.ClaimType;
                existingPolicy.PolicyDetail.CostCentreId = claimsInvestigation.PolicyDetail.CostCentreId;
                existingPolicy.PolicyDetail.CaseEnablerId = claimsInvestigation.PolicyDetail.CaseEnablerId;
                existingPolicy.PolicyDetail.DateOfIncident = claimsInvestigation.PolicyDetail.DateOfIncident;
                existingPolicy.PolicyDetail.ContractNumber = claimsInvestigation.PolicyDetail.ContractNumber;
                existingPolicy.PolicyDetail.SumAssuredValue = claimsInvestigation.PolicyDetail.SumAssuredValue;
                existingPolicy.PolicyDetail.CauseOfLoss = claimsInvestigation.PolicyDetail.CauseOfLoss;
                existingPolicy.Updated = DateTime.Now;
                existingPolicy.UpdatedBy = userEmail;
                existingPolicy.ORIGIN = ORIGIN.USER;
                existingPolicy.PolicyDetail.InsuranceType = claimsInvestigation.PolicyDetail.InsuranceType;
                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    existingPolicy.PolicyDetail.DocumentImage = dataStream.ToArray();
                }

                context.Investigations.Update(existingPolicy);

                return await context.SaveChangesAsync() > 0 ? existingPolicy : null!;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null!;
            }
        }

        public async Task<CaseTransactionModel> GetClaimDetails(string currentUserEmail, long id)
        {
            var claim = await context.Investigations
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
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

            var location = claim.BeneficiaryDetail;
            var assignedStatus = context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var companyUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);

            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = claim,
                CaseIsValidToAssign = claim.IsValidCaseData(),
                Location = location,
                Assigned = claim.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser.ClientCompany.AutoAllocation
            };

            return model;
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
            var subStatuses = new[]
                {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                };

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
                    a.ClientCompanyId == companyUser.ClientCompanyId &&
                    (
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
                SubStatus = a.SubStatus,
                Ready2Assign = a.IsReady2Assign,
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.ORIGIN.GetEnumDisplayName(),
                Created = a.Created.ToString("dd-MM-yyyy"),
                ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ( {a.PolicyDetail.InvestigationServiceType.Name})",
                timePending = DateTime.Now.Subtract(a.Created),
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
    }
}
