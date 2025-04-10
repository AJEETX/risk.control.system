using static risk.control.system.AppConstant.Applicationsettings;
using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.Helpers.Permissions;

namespace risk.control.system.Services
{
    public interface ICreatorService
    {
        ClaimTransactionModel Create(string currentUserEmail);
        Task<object> GetAuto(string currentUserEmail, int draw, int start, int length, string search = "",string caseType= "" ,int orderColumn = 0, string orderDir = "asc");
        Task<object> GetActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        Task<int> GetAutoCount(string currentUserEmail);
    }
    public class CreatorService : ICreatorService
    {
        private const string UNDERWRITING = "underwriting";
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IClaimsService claimsService;

        public CreatorService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IClaimsService claimsService)
        {
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.claimsService = claimsService;
        }
        public ClaimTransactionModel Create(string currentUserEmail)
        {
            var companyUser = context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == currentUserEmail);
            var claim = new ClaimsInvestigation
            {
                PolicyDetail = new PolicyDetail
                {
                    LineOfBusinessId = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId
                },
                ClientCompany = companyUser.ClientCompany
            };
            bool userCanCreate = true;
            int availableCount = 0;
            var trial = companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial;
            if (trial)
            {
                var totalClaimsCreated = context.ClaimsInvestigation.Include(c => c.PolicyDetail).Where(c => !c.Deleted && 
                    c.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
                availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated.Count;

                if (totalClaimsCreated?.Count >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    userCanCreate = false;
                }
            }
            var model = new ClaimTransactionModel
            {
                ClaimsInvestigation = claim,
                Log = null,
                AllowedToCreate = userCanCreate,
                AutoAllocation = companyUser.ClientCompany.AutoAllocation,
                Location = new BeneficiaryDetail { },
                AvailableCount = availableCount,
                TotalCount = companyUser.ClientCompany.TotalCreatedClaimAllowed,
                Trial = trial
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
            var subStatuses = context.InvestigationCaseSubStatus
                .Where(i => new[]
                {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                }.Contains(i.Name.ToUpper()))
                .ToDictionary(i => i.Name.ToUpper(), i => i.InvestigationCaseSubStatusId);
            
            var query = claimsService.GetClaims()
                .Where(a => !a.Deleted &&
                    a.ClientCompanyId == companyUser.ClientCompanyId &&
                    (
                        (a.UserEmailActioned == companyUser.Email && a.UserEmailActionedTo == companyUser.Email && a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR]) ||
                        (a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY] && a.UserEmailActionedTo == string.Empty && a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}") ||
                        (a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY] && a.UserEmailActionedTo == companyUser.Email && a.UserEmailActioned == companyUser.Email && a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}") ||
                        (a.UserEmailActioned == companyUser.Email && a.UserEmailActionedTo == companyUser.Email && a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER])
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
                query = query.Where(c => c.PolicyDetail.LineOfBusiness.Name.ToLower() == caseType.ToLower());  // Assuming CaseType is the field in your data model
            }
            var data = query.AsEnumerable();
            int recordsFiltered = query.Count();

            var newClaimsAssigned = new List<ClaimsInvestigation>();

            // Process claims and update AutoNew
            foreach (var item in query)
            {
                item.AutoNew += 1;
                if (item.AutoNew <= 1)
                {
                    newClaimsAssigned.Add(item);
                }
            }

            if (newClaimsAssigned.Any())
            {
                context.ClaimsInvestigation.UpdateRange(newClaimsAssigned);
                context.SaveChanges();
            }

            var underWritingLineOfBusiness = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;
            // Calculate TimeElapsed and Transform Data
            var transformedData = data.Select(a => new
            {
                Id = a.ClaimsInvestigationId,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                PolicyId = a.PolicyDetail.ContractNumber,
                AssignedToAgency = a.AssignedToAgency,
                AutoAllocated = a.AutoAllocated,
                Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ? a.UserEmailActionedTo : a.UserRoleActionedTo,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeCode = ClaimsInvestigationExtension.GetPincodeCode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-light\">customer name</span>",
                Policy = a.PolicyDetail?.LineOfBusiness.Name,
                Status = a.STATUS.GetEnumDisplayName(),
                SubStatus = a.InvestigationCaseSubStatus.Name,
                Ready2Assign = a.IsValidCaseData(),
                ServiceType = $"{a.PolicyDetail?.LineOfBusiness.Name} ( {a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.ORIGIN.GetEnumDisplayName(),
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetCreatorAutoTimePending(a, a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER]),
                Withdrawable = !a.NotWithdrawable,
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-light\">beneficiary name</span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).TotalSeconds,
                IsNewAssigned = a.AutoNew <= 1,
                BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "?" : a.BeneficiaryDetail.Name,
                CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.Name) ? "?" : a.CustomerDetail.Name,
                PersonMapAddressUrl = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail) != "..." ?
                        a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness ? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap : Applicationsettings.NO_MAP
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

        public async Task<object> GetActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var companyUser = await context.ClientCompanyApplicationUser
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            if (companyUser == null) return null;

            var openStatuses =await context.InvestigationCaseStatus
                .Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED))
                .Select(i => i.InvestigationCaseStatusId)
                .ToListAsync();

            var createdStatus = context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assigned2AssignerStatus = context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var withdrawnByCompanyStatus = context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);
            var declinedByAgencyStatus = context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);

            if (createdStatus == null || assigned2AssignerStatus == null || withdrawnByCompanyStatus == null || declinedByAgencyStatus == null)
                return null;

            var query = claimsService.GetClaims()
                .Where(a => !a.Deleted && openStatuses.Contains(a.InvestigationCaseStatusId) &&
                            a.ClientCompanyId == companyUser.ClientCompanyId &&
                            !new[] { createdStatus.InvestigationCaseSubStatusId, withdrawnByCompanyStatus.InvestigationCaseSubStatusId,
                            declinedByAgencyStatus.InvestigationCaseSubStatusId, assigned2AssignerStatus.InvestigationCaseSubStatusId }
                            .Contains(a.InvestigationCaseSubStatusId));

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
                query = query.Where(c => c.PolicyDetail.LineOfBusiness.Name.ToLower() == caseType.ToLower());  // Assuming CaseType is the field in your data model
            }

            var data = query.AsEnumerable();
            int recordsFiltered = query.Count();

            var newClaims = new List<ClaimsInvestigation>();

            foreach (var claim in query)
            {
                var userHasReviewClaimLogs = context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase && c.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")?.ToList();

                int? reviewLogCount = 0;
                if (userHasReviewClaimLogs != null && userHasReviewClaimLogs.Count > 0)
                {
                    reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o => o.HopCount).First().HopCount;
                }
                var userHasClaimLog = context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.UserEmailActioned == companyUser.Email && c.HopCount >= reviewLogCount);

                if (userHasClaimLog)
                {
                    claim.ActiveView += 1;
                    if (claim.ActiveView <= 1)
                    {
                        newClaims.Add(claim);
                    }
                }
            }

            if (newClaims.Any())
            {
                context.ClaimsInvestigation.UpdateRange(newClaims);
                context.SaveChanges();
            }

            var underWritingLineOfBusiness = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;
            // Calculate TimeElapsed and Transform Data
            var transformedData = data.Select(a => new
            {
                Id = a.ClaimsInvestigationId,
                AutoAllocated = a.AutoAllocated,
                CustomerFullName = a.CustomerDetail?.Name ?? "",
                BeneficiaryFullName = a.BeneficiaryDetail?.Name ?? "",
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ? a.UserEmailActionedTo : a.UserRoleActionedTo,
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwner(a))),
                CaseWithPerson = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : NO_USER,
                Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                Policy = a.PolicyDetail?.LineOfBusiness.Name,
                Status = a.ORIGIN.GetEnumDisplayName(),
                SubStatus = a.InvestigationCaseSubStatus.Name,
                Ready2Assign = a.IsReady2Assign,
                ServiceType = $"{a.PolicyDetail.LineOfBusiness.Name} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.InvestigationCaseSubStatus.Name,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = a.GetCreatorTimePending(true),
                Withdrawable = !a.NotWithdrawable,
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) : NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).TotalSeconds, // Calculate here
                IsNewAssigned = a.ActiveView <= 1,
                PersonMapAddressUrl = a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness ? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap
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

            var response = new
            {
                draw = draw,
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
            var subStatuses = context.InvestigationCaseSubStatus
                .Where(i => new[]
                {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                }.Contains(i.Name.ToUpper()))
                .ToDictionary(i => i.Name.ToUpper(), i => i.InvestigationCaseSubStatusId);

            var query = claimsService.GetClaims()
                .Where(a => !a.Deleted &&
                    a.ClientCompanyId == companyUser.ClientCompanyId &&
                    (
                        (a.UserEmailActioned == companyUser.Email && a.UserEmailActionedTo == companyUser.Email && a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR]) ||
                        (a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY] && a.UserEmailActionedTo == string.Empty && a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}") ||
                        (a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY] && a.UserEmailActionedTo == companyUser.Email && a.UserEmailActioned == companyUser.Email && a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}") ||
                        (a.UserEmailActioned == companyUser.Email && a.UserEmailActionedTo == companyUser.Email && a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER])
                    )
                );

            int totalRecords = query.Count(); // Get total count before pagination
            return totalRecords;
        }

        private byte[] GetOwner(ClaimsInvestigation a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var allocated2agent = context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            if (!string.IsNullOrWhiteSpace(a.UserEmailActionedTo) && a.InvestigationCaseSubStatusId == allocated2agent.InvestigationCaseSubStatusId)
            {
                ownerEmail = a.UserEmailActionedTo;
                var agentProfile = context.VendorApplicationUser.FirstOrDefault(u => u.Email == ownerEmail)?.ProfilePicture;
                if (agentProfile == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return agentProfile;
            }
            else if (string.IsNullOrWhiteSpace(a.UserEmailActionedTo) &&
                !string.IsNullOrWhiteSpace(a.UserRoleActionedTo)
                && a.AssignedToAgency)
            {
                ownerDomain = a.UserRoleActionedTo;
                var vendorImage = context.Vendor.FirstOrDefault(v => v.Email == ownerDomain)?.DocumentImage;
                if (vendorImage == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return vendorImage;
            }
            else
            {
                ownerDomain = a.UserRoleActionedTo;
                var companyImage = context.ClientCompany.FirstOrDefault(v => v.Email == ownerDomain)?.DocumentImage;
                if (companyImage == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return companyImage;
            }

        }
        private static string GetCreatorAutoTimePending(ClaimsInvestigation a, bool assigned = false)
        {
            if (DateTime.Now.Subtract(a.Updated.Value).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.Updated.Value).Days} days since created!\"></i>");

            else if (DateTime.Now.Subtract(a.Updated.Value).Days >= 3 || DateTime.Now.Subtract(a.Updated.Value).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Caution : {DateTime.Now.Subtract(a.Updated.Value).Days} day since created.\"></i>");
            if (DateTime.Now.Subtract(a.Updated.Value).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Days} day</span>");

            if (DateTime.Now.Subtract(a.Updated.Value).Hours < 24 &&
                DateTime.Now.Subtract(a.Updated.Value).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(a.Updated.Value).Hours == 0 && DateTime.Now.Subtract(a.Updated.Value).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(a.Updated.Value).Minutes == 0 && DateTime.Now.Subtract(a.Updated.Value).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

    }
}
