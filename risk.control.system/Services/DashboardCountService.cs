using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IDashboardCountService
    {
        DashboardData GetCreatorCount(string userEmail, string role);
        DashboardData GetAssessorCount(string userEmail, string role);
        DashboardData GetCompanyAdminCount(string userEmail, string role);
        DashboardData GetManagerCount(string userEmail, string role);
        DashboardData GetSupervisorCount(string userEmail, string role);
        DashboardData GetAgentCount(string userEmail, string role);
        DashboardData GetSuperAdminCount(string userEmail, string role);
    }

    public class DashboardCountService : IDashboardCountService
    {
        private readonly ApplicationDbContext _context;

        private const string uploaded = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED;
        private const string created = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR;
        private const string assigned = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;
        private const string reAssigned = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER;
        private const string withdrawnByAgency = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY;
        private const string withdrawnByCompany = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY;
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

        public DashboardData GetAgentCount(string userEmail, string role)
        {
            var vendorUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(c => c.Email == userEmail);
            var taskCount = GetCases().Count(c => c.VendorId == vendorUser.VendorId &&
            c.SubStatus == assigned2Agent &&
            c.TaskedAgentEmail == userEmail);

            var agentSubmittedCount = GetCases().Distinct().Count(t => t.TaskedAgentEmail == userEmail && t.SubStatus != assigned2Agent);

            var data = new DashboardData();
            data.FirstBlockName = "Tasks";
            data.FirstBlockCount = taskCount;
            data.FirstBlockUrl = "/Agent/Index";

            data.SecondBlockName = "Submitted";
            data.SecondBlockCount = agentSubmittedCount;
            data.SecondBlockUrl = "/Agent/Submitted";

            return data;
        }
        public DashboardData GetSuperAdminCount(string userEmail, string role)
        {
            var allCompaniesCount = _context.ClientCompany.Count(c => !c.Deleted);
            var allAgenciesCount = _context.Vendor.Count(v => !v.Deleted);
            var AllUsersCount = _context.ApplicationUser.Count(u => !u.Deleted);
            //var availableAgenciesCount = GetAvailableAgencies(userEmail);

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

            //data.LastBlockName = "Available Agencies";
            //data.LastBlockCount = availableAgenciesCount;
            //data.LastBlockUrl = "/Company/AvailableVendors";

            return data;
        }

        public DashboardData GetSupervisorCount(string userEmail, string role)
        {

            var claimsAllocate = GetAgencyAllocateCount(userEmail);
            var claimsVerified = GetAgencyVerifiedCount(userEmail);
            var claimsActiveCount = GetSuperVisorActiveCount(userEmail);

            var claimsCompleted = GetAgencyyCompleted(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "Allocate/Enquiry";
            data.FirstBlockCount = claimsAllocate;
            data.FirstBlockUrl = "/VendorInvestigation/Allocate";

            data.SecondBlockName = "Submit(report)";
            data.SecondBlockCount = claimsVerified;
            data.SecondBlockUrl = "/VendorInvestigation/ClaimReport";

            data.ThirdBlockName = "Active";
            data.ThirdBlockCount = claimsActiveCount;
            data.ThirdBlockUrl = "/VendorInvestigation/Open";

            data.LastBlockName = "Completed";
            data.LastBlockCount = claimsCompleted;
            data.LastBlockUrl = "/VendorInvestigation/Completed";

            return data;
        }
        public DashboardData GetCreatorCount(string userEmail, string role)
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var data = new DashboardData
            {
                AutoAllocation = company.AutoAllocation,
                BulkUpload = company.BulkUpload
            };


            data.FirstBlockName = "ADD/ASSIGN";
            //data.FirstBlockCount = GetCreatorAssignAuto(userEmail);
            data.FirstBlockUrl = "/Investigation/New";

            var claimCount = GetCreatorAssignAuto(userEmail, InsuranceType.CLAIM);
            var underWritingCount = GetCreatorAssignAuto(userEmail, InsuranceType.UNDERWRITING);
            data.FirstBlockCount = claimCount;
            data.SecondBlockCount = underWritingCount;

            var filesUploadCount = _context.FilesOnFileSystem.Count(f => f.CompanyId == company.ClientCompanyId && !f.Deleted && f.UploadedBy == companyUser.Email && !f.DirectAssign);
            var filesUploadAssignCount = _context.FilesOnFileSystem.Count(f => f.CompanyId == company.ClientCompanyId && !f.Deleted && f.UploadedBy == companyUser.Email && f.DirectAssign);
            data.BulkUploadBlockName = "UPLOAD  ";

            data.BulkUploadBlockUrl = "/CaseUpload/Uploads";

            data.BulkUploadBlockCount = filesUploadCount;
            data.BulkUploadAssignCount = filesUploadAssignCount;

            data.ThirdBlockName = "ACTIVE";
            var claimsActive = GetCreatorActive(userEmail, InsuranceType.CLAIM);
            var underWritingActive = GetCreatorActive(userEmail, InsuranceType.UNDERWRITING);
            data.ThirdBlockCount = claimsActive;
            data.LastBlockCount = underWritingActive;
            data.ThirdBlockUrl = "/CaseActive/Active";


            return data;
        }

        public DashboardData GetManagerCount(string userEmail, string role)
        {

            //var claimsAssessor = GetManagerAssess(userEmail, InsuranceType.CLAIM);
            //var underwritingAssessor = GetManagerAssess(userEmail, InsuranceType.UNDERWRITING);
            //var claimsReview = GetManagerReview(userEmail);

            var claimsReject = GetManagerReject(userEmail, InsuranceType.CLAIM);
            var undewrwritingReject = GetManagerReject(userEmail, InsuranceType.UNDERWRITING);

            var claimsCompleted = GetCompanyManagerApproved(userEmail, InsuranceType.CLAIM);
            var underwritingCompleted = GetCompanyManagerApproved(userEmail, InsuranceType.UNDERWRITING);

            var activeClaims = GetManagerActive(userEmail, InsuranceType.CLAIM);
            var activeUnderwritings = GetManagerActive(userEmail, InsuranceType.UNDERWRITING);

            var empanelledAgenciesCount = GetEmpanelledAgencies(userEmail);
            var availableAgenciesCount = GetAvailableAgencies(userEmail);

            var data = new DashboardData();
            //data.FirstBlockName = "Assess (new)";
            //data.FirstBlockCount = claimsAssessor;
            //data.FirstBlockUrl = "/Manager/Assessor";

            ////data.SecondBlockName = "Review";
            //data.UnderwritingCount = underwritingAssessor;
            //data.SecondBlockUrl = "/ClaimsInvestigation/ManagerReview";

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
        public DashboardData GetAssessorCount(string userEmail, string role)
        {

            var claimsAssessor = GetAssessorAssess(userEmail, InsuranceType.CLAIM);
            var underwritingAssessor = GetAssessorAssess(userEmail, InsuranceType.UNDERWRITING);

            var claimsReview = GetAssessorReview(userEmail, InsuranceType.CLAIM);
            var underwritingReview = GetAssessorReview(userEmail, InsuranceType.UNDERWRITING);


            var claimsReject = GetAssessorReject(userEmail, InsuranceType.CLAIM);
            var underwritingReject = GetAssessorReject(userEmail, InsuranceType.UNDERWRITING);

            var claimsCompleted = GetCompanyCompleted(userEmail, InsuranceType.CLAIM);
            var underwritingCompleted = GetCompanyCompleted(userEmail, InsuranceType.UNDERWRITING);

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

        public DashboardData GetCompanyAdminCount(string userEmail, string role)
        {
            var companyUsersCount = GetCompanyUsers(userEmail);
            //var allAgenciesCount = GetAllAgencies(userEmail);
            //var empanelledAgenciesCount = GetEmpanelledAgencies(userEmail);
            //var availableAgenciesCount = GetAvailableAgencies(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "All Users";
            data.FirstBlockCount = companyUsersCount;
            data.FirstBlockUrl = "/Company/Users";

            //data.SecondBlockName = "Agencies";
            //data.SecondBlockCount = allAgenciesCount;
            //data.SecondBlockUrl = "/Vendors/Agencies";

            //data.ThirdBlockName = "Empanelled Agencies";
            //data.ThirdBlockCount = empanelledAgenciesCount;
            //data.ThirdBlockUrl = "/Company/EmpanelledVendors";

            //data.LastBlockName = "Available Agencies";
            //data.LastBlockCount = availableAgenciesCount;
            //data.LastBlockUrl = "/Company/AvailableVendors";

            return data;
        }
        private int GetAvailableAgencies(string userEmail)
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var company = _context.ClientCompany
               .Include(c => c.EmpanelledVendors)
               .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var availableVendors = _context.Vendor
                .Count(v => !company.EmpanelledVendors.Contains(v) && v.CountryId == companyUser.CountryId && !v.Deleted);
            return availableVendors;
        }
        private int GetEmpanelledAgencies(string userEmail)
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var empAgencies = _context.ClientCompany.Include(c => c.EmpanelledVendors).FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var count = empAgencies.EmpanelledVendors.Count(v => v.Status == VendorStatus.ACTIVE && !v.Deleted);
            return count;
        }
        private int GetCompanyUsers(string userEmail)
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);

            var allCompanyUserCount = _context.ClientCompanyApplicationUser.Count(u => u.ClientCompanyId == companyUser.ClientCompanyId && !u.Deleted && u.Email != userEmail);

            return allCompanyUserCount;
        }
        private int GetSuperVisorActiveCount(string userEmail)
        {
            var claims = GetAgencyClaims();
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            if (vendorUser.IsVendorAdmin)
            {
                return claims.Count(a => a.VendorId == vendorUser.VendorId &&
            a.Status != finished &&
                 (a.SubStatus == assigned2Agent ||
                a.SubStatus == submitted2Assessor ||
                 a.SubStatus == reply2Assessor));
            }
            var count = claims.Count(a => a.VendorId == vendorUser.VendorId &&
            a.Status != finished &&
                 ((a.SubStatus == assigned2Agent && a.AllocatingSupervisordEmail == userEmail) ||
                (a.SubStatus == submitted2Assessor && a.SubmittingSupervisordEmail == userEmail) ||
                 (a.SubStatus == reply2Assessor && a.SubmittingSupervisordEmail == userEmail)));
            return count;
        }

        private int GetAgencyVerifiedCount(string userEmail)
        {
            var applicationDbContext = GetAgencyClaims();

            var vendorUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(c => c.Email == userEmail);

            var count = applicationDbContext.Count(i => i.VendorId == vendorUser.VendorId &&
            i.SubStatus == submitted2Supervisor);
            return count;
        }
        private int GetAgencyAllocateCount(string userEmail)
        {
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var applicationDbContext = GetAgencyClaims().Where(i => i.VendorId == vendorUser.VendorId);


            var count = applicationDbContext
                    .Count(i => i.SubStatus == allocated ||
                    i.SubStatus == requestedAssessor);

            return count;
        }
        private int GetAssessorAssess(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var count = cases.Count(i => i.ClientCompanyId == companyUser.ClientCompanyId &&
            i.SubStatus == submitted2Assessor ||
            i.SubStatus == reply2Assessor
             );

            return count;
        }
        private int GetManagerAssess(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var count = cases.Count(i => i.ClientCompanyId == companyUser.ClientCompanyId &&
            (i.SubStatus == submitted2Assessor ||
            i.SubStatus == reply2Assessor)
            );

            return count;
        }
        private int GetAgencyyCompleted(string userEmail)
        {
            var agencyUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
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
        private int GetCompanyManagerApproved(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);


            var count = cases.Count(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.Status == finished &&
                c.SubStatus == approved
                );

            return count;
        }

        private int GetCompanyCompleted(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var count = cases.Count(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubmittedAssessordEmail == userEmail &&
                c.Status == finished &&
                (c.SubStatus == approved)
                );

            return count;
        }
        private int GetAssessorReject(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);


            var count = cases.Count(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubStatus == rejectd && c.Status == finished && c.SubmittedAssessordEmail == userEmail
                );

            return count;
        }
        private int GetManagerReject(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var count = cases.Count(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubStatus == rejectd && c.Status == finished);

            return count;
        }

        private int GetAssessorReview(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var count = cases.Count(a => a.ClientCompanyId == companyUser.ClientCompanyId &&
            a.SubStatus == requestedAssessor && a.RequestedAssessordEmail == userEmail);
            return count;
        }

        private int GetCreatorActive(string userEmail, InsuranceType insuranceType)
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
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var count = cases.Count(a => !a.Deleted && a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                            a.ClientCompanyId == companyUser.ClientCompanyId && a.CreatedUser == userEmail &&
                            !subStatus
                            .Contains(a.SubStatus));

            return count;
        }
        private int GetManagerActive(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var count = cases.Count(a => a.ClientCompanyId == companyUser.ClientCompanyId &&
                    a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                    (a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR));

            return count;
        }

        private int GetCreatorAssignAuto(string userEmail, InsuranceType insuranceType)
        {
            var claims = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var count = claims.Count(a => !a.Deleted &&
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
        //public DashboardData GetClaimsCount(string userEmail, string role)
        //{
        //    var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED))?.ToList();

        //    var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
        //    var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
        //    var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

        //    var submittededToSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

        //    var submittededToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

        //    var reAssigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);

        //    var assessorApprovedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
        //        i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
        //    var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();

        //    var companyUser = _context.ClientCompanyApplicationUser
        //        .Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == userEmail);
        //    var vendorUser = _context.VendorApplicationUser
        //        .Include(v => v.Vendor).FirstOrDefault(c => c.Email == userEmail);

        //    var data = new DashboardData();

        //    if (companyUser != null)
        //    {
        //        var pendinClaims = _context.ClaimsInvestigation
        //            .Include(c => c.PolicyDetail)
        //            .Where(c => c.CurrentClaimOwner == userEmail && openStatusesIds.Contains(c.InvestigationCaseStatusId) && !c.Deleted &&
        //            c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId).ToList();

        //        var approvedClaims = _context.ClaimsInvestigation
        //            .Include(c => c.PolicyDetail)
        //            .Where(c => c.InvestigationCaseSubStatusId == assessorApprovedStatus.InvestigationCaseSubStatusId &&
        //            c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();

        //        var rejectedClaims = _context.ClaimsInvestigation
        //            .Include(c => c.PolicyDetail)
        //            .Where(c => c.IsReviewCase && openStatusesIds.Contains(c.InvestigationCaseStatusId) &&
        //            c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();

        //        var activeCount = 0;

        //        if (role.Contains(AppRoles.COMPANY_ADMIN.ToString()) || role.Contains(AppRoles.CREATOR.ToString()))
        //        {
        //            var creatorActiveClaims = _context.ClaimsInvestigation
        //            .Include(c => c.PolicyDetail)
        //            .Where(c => openStatusesIds.Contains(c.InvestigationCaseStatusId) && !c.Deleted &&
        //            c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();
        //            activeCount = creatorActiveClaims.Count;
        //        }

        //        if (role.Contains(AppRoles.ASSESSOR.ToString()))
        //        {
        //            var creatorActiveClaims = _context.ClaimsInvestigation
        //            .Include(c => c.PolicyDetail)
        //            .Where(c => openStatusesIds.Contains(c.InvestigationCaseStatusId) && !c.Deleted &&
        //            c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId &&
        //            c.InvestigationCaseSubStatusId == submittededToAssesssorStatus.InvestigationCaseSubStatusId
        //            )?.ToList();
        //            activeCount = creatorActiveClaims.Count;
        //        }

        //        data.FirstBlockName = "Active Claims";
        //        data.FirstBlockCount = activeCount;

        //        data.SecondBlockName = "Pending Claims";
        //        data.SecondBlockCount = pendinClaims.Count;

        //        data.ThirdBlockName = "Approved Claims";
        //        data.ThirdBlockCount = approvedClaims.Count;

        //        data.LastBlockName = "Review Claims";
        //        data.LastBlockCount = rejectedClaims.Count;
        //    }
        //    else if (vendorUser != null)
        //    {
        //        var activeClaims = _context.ClaimsInvestigation.Include(c => c.BeneficiaryDetail)
        //            .Where(c => openStatusesIds.Contains(c.InvestigationCaseStatusId) && !c.Deleted)?.ToList();
        //        var agencyActiveClaims = activeClaims.Where(c =>
        //        (c.VendorId == vendorUser.VendorId) &&
        //        (c.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId ||
        //        c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
        //        c.InvestigationCaseSubStatusId == submittededToSupervisorStatus.InvestigationCaseSubStatusId))?.ToList();

        //        data.FirstBlockName = "Draft Claims";
        //        data.FirstBlockCount = agencyActiveClaims.Count;

        //        var pendinClaims = _context.ClaimsInvestigation
        //             .Where(c => c.CurrentClaimOwner == userEmail && openStatusesIds.Contains(c.InvestigationCaseStatusId)).ToList();

        //        data.SecondBlockName = "Pending Claims";
        //        data.SecondBlockCount = pendinClaims.Count;

        //        var agentActiveClaims = _context.ClaimsInvestigation.Include(c => c.VendorId == vendorUser.VendorId &&
        //        c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId && !c.Deleted)?.ToList();

        //        data.ThirdBlockName = "Allocated Claims";
        //        data.ThirdBlockCount = agentActiveClaims.Count;

        //        var submitClaims = _context.ClaimsInvestigation.Include(c => c.VendorId == vendorUser.VendorId &&
        //            c.InvestigationCaseSubStatusId == submittededToAssesssorStatus.InvestigationCaseSubStatusId && !c.Deleted)?.ToList();
        //        data.LastBlockName = "Submitted Claims";
        //        data.LastBlockCount = submitClaims.Count;
        //    }

        //    return data;
        //}

    }
}