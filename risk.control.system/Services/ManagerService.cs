using System.Globalization;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IManagerService
    {
        Task<object> GetActiveCases(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        Task<List<ClaimsInvestigationResponse>> GetApprovedCases(string userEmail);
        Task<List<ClaimsInvestigationResponse>> GetRejectedCases(string userEmail);
    }
    internal class ManagerService : IManagerService
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;

        public ManagerService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            this.env = env;
        }
        public async Task<object> GetActiveCases(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var companyUser = await context.ClientCompanyApplicationUser
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var assignedToAssignerStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;
            var submittedToAssessorStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR;

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
                .Where(a => !a.Deleted && a.ClientCompanyId == companyUser.ClientCompanyId &&
                    a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                    (a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR));

            int totalRecords = query.Count(); // Get total count before pagination

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
                    a.CustomerDetail.PinCode.Code.ToLower().Contains(search) ||
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

            var data = query.AsEnumerable();
            int recordsFiltered = query.Count();

            var transformedData = data.Select(a => new
            {
                Id = a.Id,
                AutoAllocated = a.IsAutoAllocated,
                CustomerFullName = a.CustomerDetail?.Name ?? string.Empty,
                BeneficiaryFullName = a.BeneficiaryDetail?.Name ?? string.Empty,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = GetOwner(a),
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwnerImage(a))),
                CaseWithPerson = a.CaseOwner,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeCode = ClaimsInvestigationExtension.GetPincodeCode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentPath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ImagePath != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.CustomerDetail?.ImagePath))))
                        : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i> </span>",
                Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                Status = a.ORIGIN.GetEnumDisplayName(),
                SubStatus = a.SubStatus,
                Ready2Assign = a.IsReady2Assign,
                ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetManagerActiveTimePending(a),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath))))
                        : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name)
                        ? "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>"
                        : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).TotalSeconds,
                IsNewAssigned = a.IsNewAssignedToManager,
                PersonMapAddressUrl = string.Format(a.GetMap(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.SubStatus == assignedToAssignerStatus,
                                                      a.SubStatus == submittedToAssessorStatus), "400", "400")
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
            var idsToMarkViewed = pagedData.Where(x => x.IsNewAssigned).Select(x => x.Id).ToList();

            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNewAssignedToManager = false;

                await context.SaveChangesAsync(null, false); // mark as viewed
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
        private static string GetManagerActiveTimePending(InvestigationTask a)
        {
            if (a.CreatorSla == 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} days since created!\"></i>");
            }
            if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");

            else if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= 3 || DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");
            if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");

            if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours < 24 &&
                DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours == 0 && DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Minutes == 0 && DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        private byte[] GetOwnerImage(InvestigationTask a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var noDataImagefilePath = Path.Combine(env.WebRootPath, "img", "no-photo.jpg");
            var noDataimage = File.ReadAllBytes(noDataImagefilePath);

            if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR || a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                var agentProfile = context.Vendor.FirstOrDefault(u => u.VendorId == a.VendorId)?.DocumentImage;
                if (agentProfile != null)
                {
                    return agentProfile;
                }
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                var vendorImage = context.VendorApplicationUser.FirstOrDefault(v => v.Email == a.TaskedAgentEmail)?.ProfilePicture;
                if (vendorImage != null)
                {
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
                var company = context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == a.ClientCompanyId).DocumentImage;
                if (company != null)
                {
                    return company;
                }
            }
            return noDataimage;
        }
        private string GetOwner(InvestigationTask a)
        {
            if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                return a.Vendor.Email;
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                return a.TaskedAgentEmail;
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
                if (company != null)
                {
                    return company.Email;
                }
            }
            return string.Empty;
        }

        public async Task<List<ClaimsInvestigationResponse>> GetApprovedCases(string userEmail)
        {

            var companyUser = await context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(u => u.Email == userEmail);


            // Fetching the statuses once
            var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;

            var finishStatus = CONSTANTS.CASE_STATUS.FINISHED;

            var claims = await context.Investigations
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
                .Where(i => !i.Deleted && i.ClientCompanyId == companyUser.ClientCompanyId &&
                            (i.SubStatus == approvedStatus) &&
                            i.Status == finishStatus).ToListAsync();

            var response = claims.Select(a => new ClaimsInvestigationResponse
            {
                Id = a.Id,
                AutoAllocated = a.IsAutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = a.Vendor.Email,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentPath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ImagePath != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.CustomerDetail?.ImagePath))))
                        : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ??
                    "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /></span>",
                Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                Status = a.ORIGIN.GetEnumDisplayName(),
                ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetManagerTimeCompleted(a),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i></span>" :
                    a.BeneficiaryDetail.Name,
                Agency = a.Vendor?.Name,
                OwnerDetail = $"data:image/*;base64,{Convert.ToBase64String(a.Vendor.DocumentImage)}",
                TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime ?? DateTime.Now).TotalSeconds,
                PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration,
                CanDownload = CanDownload(a.Id, userEmail)
            }).ToList();
            return response;
        }
        private static string GetManagerTimeCompleted(InvestigationTask a)
        {
            if (a.CreatorSla == 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} days since created!\"></i>");
            }
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");

            else if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= 3 || DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");

            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours < 24 &&
                DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours == 0 && DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Minutes == 0 && DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Seconds} sec </span>");
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

        public async Task<List<ClaimsInvestigationResponse>> GetRejectedCases(string userEmail)
        {
            var companyUser = await context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            // Get the rejected status
            var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
            var finishStatus = CONSTANTS.CASE_STATUS.FINISHED;

            var claims = await context.Investigations
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
                .Where(i => !i.Deleted && i.ClientCompanyId == companyUser.ClientCompanyId &&
                            (i.SubStatus == rejectedStatus) &&
                            i.Status == finishStatus).ToListAsync();

            var response = claims.Select(a => new ClaimsInvestigationResponse
            {
                Id = a.Id,
                AutoAllocated = a.IsAutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = a.Vendor.Email,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentPath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ImagePath != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.CustomerDetail?.ImagePath))))
                        : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ??
                    "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /></span>",
                Policy = $"<span class='badge badge-light'>{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()}</span>",
                Status = $"ORIGIN of Claim: {a.ORIGIN.GetEnumDisplayName()}",
                ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetManagerTimeCompleted(a),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" :
                    a.BeneficiaryDetail.Name,
                OwnerDetail = $"data:image/*;base64,{Convert.ToBase64String(a.Vendor.DocumentImage)}",
                Agency = a.Vendor?.Name,
                TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime.Value).TotalSeconds,
                PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration,
                CanDownload = CanDownload(a.Id, userEmail)
            }).ToList();
            return response;
        }
    }
}
