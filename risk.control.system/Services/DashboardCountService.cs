using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IDashboardCountService
    {
        Task<DashboardData> GetCreatorCount(string userEmail, string role);
        Task<DashboardData> GetAssessorCount(string userEmail, string role);
        Task<DashboardData> GetCompanyAdminCount(string userEmail, string role);
        Task<DashboardData> GetManagerCount(string userEmail, string role);
        Task<DashboardData> GetSupervisorCount(string userEmail, string role);
        Task<DashboardData> GetAgentCount(string userEmail, string role);
        Task<DashboardData> GetSuperAdminCount(string userEmail, string role);
    }

    internal class DashboardCountService : IDashboardCountService
    {
        private readonly ApplicationDbContext _context;

        private const string allocated = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;
        private const string assigned2Agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
        private const string submitted2Supervisor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR;
        private const string submitted2Assessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR;
        private const string reply2Assessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR;
        private const string requestedAssessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
        private const string rejectd = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
        private const string approved = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
        private const string finished = CONSTANTS.CASE_STATUS.FINISHED;
        public DashboardCountService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task<DashboardData> GetAgentCount(string userEmail, string role)
        {
            var vendorUser = await _context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(c => c.Email == userEmail);
            var taskCount =await GetCases().CountAsync(c => c.VendorId == vendorUser.VendorId &&
            c.SubStatus == assigned2Agent &&
            c.TaskedAgentEmail == userEmail);

            var agentSubmittedCount =await GetCases().Distinct().CountAsync(t => t.TaskedAgentEmail == userEmail && t.SubStatus != assigned2Agent);

            var data = new DashboardData();
            data.FirstBlockName = "Tasks";
            data.FirstBlockCount = taskCount;
            data.FirstBlockUrl = "/Agent/Index";

            data.SecondBlockName = "Submitted";
            data.SecondBlockCount = agentSubmittedCount;
            data.SecondBlockUrl = "/Agent/Submitted";

            return data;
        }
        public async Task<DashboardData> GetSuperAdminCount(string userEmail, string role)
        {
            var allCompaniesCount = await _context.ClientCompany.CountAsync(c => !c.Deleted);
            var allAgenciesCount = await _context.Vendor.CountAsync(v => !v.Deleted);
            var AllUsersCount = await _context.ApplicationUser.CountAsync(u => !u.Deleted && u.Email != userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "Companies";
            data.FirstBlockCount = allCompaniesCount;
            data.FirstBlockUrl = "/ClientCompany/Companies";

            data.SecondBlockName = "Agencies";
            data.SecondBlockCount = allAgenciesCount;
            data.SecondBlockUrl = "/Vendors/Agencies";

            data.ThirdBlockName = "Users";
            data.ThirdBlockCount = AllUsersCount;
            data.ThirdBlockUrl = "/User";

            return data;
        }

        public async Task<DashboardData> GetSupervisorCount(string userEmail, string role)
        {

            var claimsAllocate = await GetAgencyAllocateCount(userEmail);
            var claimsVerified = await GetAgencyVerifiedCount(userEmail);
            var claimsActiveCount = await GetSuperVisorActiveCount(userEmail);

            var claimsCompleted = await GetAgencyyCompleted(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "Allocate/Enquiry";
            data.FirstBlockCount = claimsAllocate;
            data.FirstBlockUrl = "/VendorInvestigation/Allocate";

            data.SecondBlockName = "Submit(report)";
            data.SecondBlockCount = claimsVerified;
            data.SecondBlockUrl = "/VendorInvestigation/CaseReport";

            data.ThirdBlockName = "Active";
            data.ThirdBlockCount = claimsActiveCount;
            data.ThirdBlockUrl = "/VendorInvestigation/Open";

            data.LastBlockName = "Completed";
            data.LastBlockCount = claimsCompleted;
            data.LastBlockUrl = "/VendorInvestigation/Completed";

            return data;
        }
        public async Task<DashboardData> GetCreatorCount(string userEmail, string role)
        {
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var data = new DashboardData
            {
                AutoAllocation = company.AutoAllocation,
                BulkUpload = company.BulkUpload
            };


            data.FirstBlockName = "ADD/ASSIGN";
            data.FirstBlockUrl = "/CaseCreateEdit/New";

            var claimCount = await GetCreatorAssignAuto(userEmail, InsuranceType.CLAIM);
            var underWritingCount = await GetCreatorAssignAuto(userEmail, InsuranceType.UNDERWRITING);
            data.FirstBlockCount = claimCount;
            data.SecondBlockCount = underWritingCount;

            var filesUploadCount = await _context.FilesOnFileSystem.CountAsync(f => f.CompanyId == company.ClientCompanyId && !f.Deleted && f.UploadedBy == companyUser.Email && !f.DirectAssign);
            var filesUploadAssignCount = await _context.FilesOnFileSystem.CountAsync(f => f.CompanyId == company.ClientCompanyId && !f.Deleted && f.UploadedBy == companyUser.Email && f.DirectAssign);
            data.BulkUploadBlockName = "UPLOAD  ";

            data.BulkUploadBlockUrl = "/CaseUpload/Uploads";

            data.BulkUploadBlockCount = filesUploadCount;
            data.BulkUploadAssignCount = filesUploadAssignCount;

            data.ThirdBlockName = "ACTIVE";
            var claimsActive = await GetCreatorActive(userEmail, InsuranceType.CLAIM);
            var underWritingActive = await GetCreatorActive(userEmail, InsuranceType.UNDERWRITING);
            data.ThirdBlockCount = claimsActive;
            data.LastBlockCount = underWritingActive;
            data.ThirdBlockUrl = "/CaseActive/Active";


            return data;
        }

        public async Task<DashboardData> GetManagerCount(string userEmail, string role)
        {
            var claimsReject = await GetManagerReject(userEmail, InsuranceType.CLAIM);
            var undewrwritingReject = await GetManagerReject(userEmail, InsuranceType.UNDERWRITING);

            var claimsCompleted = await GetCompanyManagerApproved(userEmail, InsuranceType.CLAIM);
            var underwritingCompleted = await GetCompanyManagerApproved(userEmail, InsuranceType.UNDERWRITING);

            var activeClaims = await GetManagerActive(userEmail, InsuranceType.CLAIM);
            var activeUnderwritings = await GetManagerActive(userEmail, InsuranceType.UNDERWRITING);

            var empanelledAgenciesCount = await GetEmpanelledAgencies(userEmail);
            var availableAgenciesCount = await GetAvailableAgencies(userEmail);

            var data = new DashboardData();
            
            data.SecondBBlockName = "Active";
            data.SecondBBlockUrl = "/Manager/Active";
            data.SecondBBlockCount = activeClaims;
            data.SecondBlockCount = activeUnderwritings;


            data.ThirdBlockName = "Approved";
            data.ThirdBlockCount = claimsCompleted;
            data.ApprovedUnderwritingCount = underwritingCompleted;
            data.ThirdBlockUrl = "/Manager/Approved";

            data.LastBlockName = "Rejected";
            data.LastBlockCount = claimsReject;
            data.RejectedUnderwritingCount = undewrwritingReject;
            data.LastBlockUrl = "/Manager/Rejected";

            data.FifthBlockName = "Empanelled Agencies";
            data.FifthBlockCount = empanelledAgenciesCount;
            data.FifthBlockUrl = "/Vendors/EmpanelledVendors";

            data.SixthBlockName = "Available Agencies";
            data.SixthBlockCount = availableAgenciesCount;
            data.SixthBlockUrl = "/Vendors/AvailableVendors";

            return data;
        }
        public async Task<DashboardData> GetAssessorCount(string userEmail, string role)
        {

            var claimsAssessor = await GetAssessorAssess(userEmail, InsuranceType.CLAIM);
            var underwritingAssessor = await GetAssessorAssess(userEmail, InsuranceType.UNDERWRITING);

            var claimsReview = await GetAssessorReview(userEmail, InsuranceType.CLAIM);
            var underwritingReview = await GetAssessorReview(userEmail, InsuranceType.UNDERWRITING);


            var claimsReject = await GetAssessorReject(userEmail, InsuranceType.CLAIM);
            var underwritingReject = await GetAssessorReject(userEmail, InsuranceType.UNDERWRITING);

            var claimsCompleted = await GetCompanyCompleted(userEmail, InsuranceType.CLAIM);
            var underwritingCompleted = await GetCompanyCompleted(userEmail, InsuranceType.UNDERWRITING);

            var data = new DashboardData();
            data.FirstBlockName = "Assess (report)";
            data.FirstBlockCount = claimsAssessor;
            data.UnderwritingCount = underwritingAssessor;
            data.FirstBlockUrl = "/Assessor/Assessor";

            data.SecondBlockName = "Enquiry";
            data.SecondBlockCount = claimsReview;
            data.SecondBBlockCount = underwritingReview;
            data.SecondBlockUrl = "/Assessor/Review";

            data.ThirdBlockName = "Approved";
            data.ApprovedClaimgCount = claimsCompleted;
            data.ApprovedUnderwritingCount = underwritingCompleted;
            data.ThirdBlockUrl = "/Assessor/Approved";

            data.LastBlockName = "Rejected";
            data.RejectedClaimCount = claimsReject;
            data.RejectedUnderwritingCount = underwritingReject;
            data.LastBlockUrl = "/Assessor/Rejected";

            return data;
        }

        public async Task<DashboardData> GetCompanyAdminCount(string userEmail, string role)
        {
            var companyUsersCount = await GetCompanyUsers(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "All Users";
            data.FirstBlockCount = companyUsersCount;
            data.FirstBlockUrl = "/Company/Users";

            return data;
        }
        private async Task<int> GetAvailableAgencies(string userEmail)
        {
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            var company = await _context.ClientCompany
               .Include(c => c.EmpanelledVendors)
               .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var availableVendors =await _context.Vendor
                .CountAsync(v => !company.EmpanelledVendors.Contains(v) && v.CountryId == companyUser.CountryId && !v.Deleted);
            return availableVendors;
        }
        private async Task<int> GetEmpanelledAgencies(string userEmail)
        {
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            var empAgencies = await _context.ClientCompany.Include(c => c.EmpanelledVendors).FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var count = empAgencies.EmpanelledVendors.Count(v => v.Status == VendorStatus.ACTIVE && !v.Deleted);
            return count;
        }
        private async Task<int> GetCompanyUsers(string userEmail)
        {
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);

            var allCompanyUserCount = await _context.ApplicationUser.CountAsync(u => u.ClientCompanyId == companyUser.ClientCompanyId && !u.Deleted && u.Email != userEmail);

            return allCompanyUserCount;
        }
        private async Task<int> GetSuperVisorActiveCount(string userEmail)
        {
            var claims = GetAgencyClaims();
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            if (vendorUser.IsVendorAdmin)
            {
                return await claims.CountAsync(a => a.VendorId == vendorUser.VendorId &&
            a.Status != finished &&
                 (a.SubStatus == assigned2Agent ||
                a.SubStatus == submitted2Assessor ||
                 a.SubStatus == reply2Assessor));
            }
            var count =await claims.CountAsync(a => a.VendorId == vendorUser.VendorId &&
            a.Status != finished &&
                 ((a.SubStatus == assigned2Agent && a.AllocatingSupervisordEmail == userEmail) ||
                (a.SubStatus == submitted2Assessor && a.SubmittingSupervisordEmail == userEmail) ||
                 (a.SubStatus == reply2Assessor && a.SubmittingSupervisordEmail == userEmail)));
            return count;
        }

        private async Task<int> GetAgencyVerifiedCount(string userEmail)
        {
            var agencyCases = GetAgencyClaims();

            var vendorUser = await _context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(c => c.Email == userEmail);

            var count =await agencyCases.CountAsync(i => i.VendorId == vendorUser.VendorId &&
            i.SubStatus == submitted2Supervisor);
            return count;
        }
        private async Task<int> GetAgencyAllocateCount(string userEmail)
        {
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var agencyCases = GetAgencyClaims().Where(i => i.VendorId == vendorUser.VendorId);


            var count = await agencyCases
                    .CountAsync(i => i.SubStatus == allocated ||
                    i.SubStatus == requestedAssessor);

            return count;
        }
        private async Task<int> GetAssessorAssess(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count =await cases.CountAsync(i => i.ClientCompanyId == companyUser.ClientCompanyId &&
            i.SubStatus == submitted2Assessor ||
            i.SubStatus == reply2Assessor
             );

            return count;
        }
        private async Task<int> GetAgencyyCompleted(string userEmail)
        {
            var agencyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var applicationDbContext = GetAgencyClaims().Where(c =>
                c.CustomerDetail != null && c.VendorId == agencyUser.VendorId);
            if (agencyUser.IsVendorAdmin)
            {
                var claimsSubmitted = 0;
                foreach (var item in applicationDbContext)
                {
                    if (item.Status == finished &&
                        item.SubStatus == approved ||
                        item.SubStatus == rejectd
                        )
                    {
                        claimsSubmitted += 1;
                    }
                }
                return claimsSubmitted;
            }
            else
            {

                var claimsSubmitted = 0;
                foreach (var item in applicationDbContext)
                {
                    if (item.SubmittingSupervisordEmail == userEmail && item.Status == finished &&
                        (item.SubStatus == approved ||
                        item.SubStatus == rejectd)
                        )
                    {
                        claimsSubmitted += 1;
                    }
                }
                return claimsSubmitted;
            }

        }
        private async Task<int> GetCompanyManagerApproved(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);


            var count =await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.Status == finished &&
                c.SubStatus == approved
                );

            return count;
        }

        private async Task<int> GetCompanyCompleted(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count =await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubmittedAssessordEmail == userEmail &&
                c.Status == finished &&
                (c.SubStatus == approved)
                );

            return count;
        }
        private async Task<int> GetAssessorReject(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);


            var count =await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubStatus == rejectd && c.Status == finished && c.SubmittedAssessordEmail == userEmail
                );

            return count;
        }
        private async Task<int> GetManagerReject(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count =await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubStatus == rejectd && c.Status == finished);

            return count;
        }

        private async Task<int> GetAssessorReview(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var count =await cases.CountAsync(a => a.ClientCompanyId == companyUser.ClientCompanyId &&
            a.SubStatus == requestedAssessor && a.RequestedAssessordEmail == userEmail);
            return count;
        }

        private async Task<int> GetCreatorActive(string userEmail, InsuranceType insuranceType)
        {
            var subStatus = new[]
           {
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_IN_PROGRESS,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
            };
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count =await cases.CountAsync(a => !a.Deleted && a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                            a.ClientCompanyId == companyUser.ClientCompanyId && a.CreatedUser == userEmail &&
                            !subStatus
                            .Contains(a.SubStatus));

            return count;
        }
        private async Task<int> GetManagerActive(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count =await cases.CountAsync(a => a.ClientCompanyId == companyUser.ClientCompanyId &&
                    a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                    (a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR));

            return count;
        }

        private async Task<int> GetCreatorAssignAuto(string userEmail, InsuranceType insuranceType)
        {
            var claims = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count =await claims.CountAsync(a => !a.Deleted &&
                a.ClientCompanyId == companyUser.ClientCompanyId &&
                     a.CreatedUser == companyUser.Email &&
                         (
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER
                    ));
            return count;
        }

        private IQueryable<InvestigationTask> GetCases()
        {
            var applicationDbContext = _context.Investigations
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.InvestigationServiceType)
               .Include(c => c.ClientCompany)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CostCentre)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.State)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.District)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.Vendor)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderBy(o => o.Created);
        }

        private IQueryable<InvestigationTask> GetAgencyClaims()
        {
            var applicationDbContext = _context.Investigations
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderBy(o => o.Created);
        }
    }
}