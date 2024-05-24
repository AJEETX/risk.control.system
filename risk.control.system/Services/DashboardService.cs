using System.Linq;
using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Linq;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Services
{
    public interface IDashboardService
    {
        Dictionary<string, int> CalculateAgencyCaseStatus(string userEmail);

        Dictionary<string, int> CalculateAgentCaseStatus(string userEmail);

        Dictionary<string, int> CalculateWeeklyCaseStatus(string userEmail);

        Dictionary<string, int> CalculateMonthlyCaseStatus(string userEmail);

        Dictionary<string, int> CalculateCaseChart(string userEmail);

        TatResult CalculateTimespan(string userEmail);

        DashboardData GetClaimsCount(string userEmail, string role);
        DashboardData GetCreatorCount(string userEmail, string role);
        DashboardData GetAssessorCount(string userEmail, string role);
        DashboardData GetCompanyAdminCount(string userEmail, string role);
        DashboardData GetManagerCount(string userEmail, string role);
        DashboardData GetSupervisorCount(string userEmail, string role);
        DashboardData GetAgentCount(string userEmail, string role);
        DashboardData GetSuperAdminCount(string userEmail, string role);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public DashboardData GetAgentCount(string userEmail, string role)
        {
            var vendorUser = _context.VendorApplicationUser.Include(u=>u.Vendor).FirstOrDefault(c => c.Email == userEmail);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submitted2Supervisor = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var taskCount = _context.ClaimsInvestigation.Count(c => c.VendorId == vendorUser.VendorId &&
            c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId &&
            c.UserEmailActionedTo == userEmail && c.UserRoleActionedTo == $"{AppRoles.AGENT.GetEnumDisplayName()} ({vendorUser.Vendor.Email})");

            var userAttendedClaims = _context.InvestigationTransaction.Where(t => (t.UserEmailActioned == vendorUser.Email &&
                            t.InvestigationCaseSubStatusId == submitted2Supervisor.InvestigationCaseSubStatusId))?.Select(c => c.ClaimsInvestigationId).Distinct();

            var claims = GetAgencyClaims();
            int completedCount = 0;

            var count = claims.Count(c => userAttendedClaims.Contains(c.ClaimsInvestigationId) && c.UserEmailActionedTo != vendorUser.Email);

            var data = new DashboardData();
            data.FirstBlockName = "Tasks";
            data.FirstBlockCount = taskCount;
            data.FirstBlockUrl = "/Agent/Index";

            data.SecondBlockName = "Submitted";
            data.SecondBlockCount = count;
            data.SecondBlockUrl = "/Agent/Submitted";

            return data;
        }
        public DashboardData GetSuperAdminCount(string userEmail, string role)
        {
            var allCompaniesCount = _context.ClientCompany.Count();
            var allAgenciesCount = _context.Vendor.Count();
            var AllUsersCount = _context.ApplicationUser.Count();
            //var availableAgenciesCount = GetAvailableAgencies(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "Companies";
            data.FirstBlockCount = allCompaniesCount;
            data.FirstBlockUrl = "/ClientCompany/Companies";

            data.SecondBlockName = "All Agencies";
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
            data.FirstBlockName = "Allocate(new)";
            data.FirstBlockCount = claimsAllocate;
            data.FirstBlockUrl = "/SuperVisor/Allocate";

            data.SecondBlockName = "Submit(report)";
            data.SecondBlockCount = claimsVerified;
            data.SecondBlockUrl = "/SuperVisor/ClaimReport";

            data.ThirdBlockName = "Active";
            data.ThirdBlockCount = claimsActiveCount;
            data.ThirdBlockUrl = "/SuperVisor/Open";

            data.LastBlockName = "Completed";
            data.LastBlockCount = claimsCompleted;
            data.LastBlockUrl = "/SuperVisor/Completed";

            return data;
        }
        public DashboardData GetCreatorCount(string userEmail, string role)
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            //var claimsIncomplete = GetCreatorIncomplete(userEmail);
            //var claimsAssignAuto = GetCreatorAssignAuto(userEmail);
            //var claimsAssign = GetCreatorAssignManual(userEmail);
            //var claimsAssignReAssign = GetCreatorReAssign(userEmail);
            //var claimsAssignReAssignAuto = GetCreatorReAssignAuto(userEmail);
            var claimsActive = GetCreatorActive(userEmail);
            //var claimsCompleted = GetCompanyCompleted(userEmail);

            var data = new DashboardData
            {
                AutoAllocation = company.AutoAllocation,
                BulkUpload = company.BulkUpload
            };


            if (company.AutoAllocation)
            {
                data.FirstBlockName = "Assign(auto)";
                data.FirstBlockCount = GetCreatorAssignAuto(userEmail);
                data.FirstBlockUrl = "/ClaimsInvestigation/Draft";
            }

            data.SecondBlockName = "Assign(manual)";
                data.SecondBlockUrl = "/ClaimsInvestigation/ReAssignerAuto";
                data.SecondBlockCount = GetCreatorReAssignAuto(userEmail);
            //else
            //{
            //    data.FirstBlockName = "Assign";
            //    data.FirstBlockCount = GetCreatorAssignManual(userEmail);
            //    data.FirstBlockUrl = "/ClaimsInvestigation/Assigner";

            //data.SecondBlockName = "ReAssign ";
            //    data.SecondBlockUrl = "/ClaimsInvestigation/ReAssigner";
            //    data.SecondBlockCount = GetCreatorReAssign(userEmail);

            //}

            var filesUploadCount = _context.FilesOnFileSystem.Count(f => f.CompanyId == company.ClientCompanyId && f.UploadedBy == companyUser.Email);
            data.BulkUploadBlockName = "Upload Log";
            data.BulkUploadBlockUrl = "/Uploads/Uploads";
            data.BulkUploadBlockCount = filesUploadCount;

            data.ThirdBlockName = "Active";
            data.ThirdBlockCount = claimsActive;
            data.ThirdBlockUrl = "/ClaimsInvestigation/Active";

            return data;
        }

        public DashboardData GetManagerCount(string userEmail, string role)
        {

            var claimsAssessor = GetManagerAssess(userEmail);
            //var claimsReview = GetManagerReview(userEmail);
            var claimsReject = GetManagerReject(userEmail);
            var claimsCompleted = GetCompanyManagerApproved(userEmail);
            var actives = GetManagerActive(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "Assess(new)";
            data.FirstBlockCount = claimsAssessor;
            data.FirstBlockUrl = "/ClaimsInvestigation/Manager";

            //data.SecondBlockName = "Review";
            //data.SecondBlockCount = claimsReview.Count;
            //data.SecondBlockUrl = "/ClaimsInvestigation/ManagerReview";

            data.SecondBBlockName = "Active";
            data.SecondBBlockUrl = "/ClaimsInvestigation/ManagerActive";
            data.SecondBBlockCount = actives;


            data.ThirdBlockName = "Approved";
            data.ThirdBlockCount = claimsCompleted;
            data.ThirdBlockUrl = "/Report/ManagerIndex";

            data.LastBlockName = "Rejected";
            data.LastBlockCount = claimsReject;
            data.LastBlockUrl = "/Report/ManagerRejected";

            return data;
        }
        public DashboardData GetAssessorCount(string userEmail, string role)
        {

            var claimsAssessor = GetAssessorAssess(userEmail);
            var claimsReview = GetAssessorReview(userEmail);
            var claimsReject= GetAssessorReject(userEmail);
            var claimsCompleted = GetCompanyCompleted(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "Assess(report)";
            data.FirstBlockCount = claimsAssessor;
            data.FirstBlockUrl = "/ClaimsInvestigation/Assessor";

            data.SecondBlockName = "Review";
            data.SecondBlockCount = claimsReview;
            data.SecondBlockUrl = "/ClaimsInvestigation/Review";

            data.ThirdBlockName = "Approved";
            data.ThirdBlockCount = claimsCompleted;
            data.ThirdBlockUrl = "/Report";

            data.LastBlockName = "Rejected";
            data.LastBlockCount = claimsReject;
            data.LastBlockUrl = "/Report/Rejected";

            return data;
        }

        public DashboardData GetCompanyAdminCount(string userEmail, string role)
        {
            var companyUsersCount = GetCompanyUsers(userEmail);
            var allAgenciesCount = GetAllAgencies(userEmail);
            var empanelledAgenciesCount = GetEmpanelledAgencies(userEmail);
            var availableAgenciesCount = GetAvailableAgencies(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "All Users";
            data.FirstBlockCount = companyUsersCount;
            data.FirstBlockUrl = "/Company/Users";

            data.SecondBlockName = "All Agencies";
            data.SecondBlockCount = allAgenciesCount;
            data.SecondBlockUrl = "/Vendors/Agencies";

            data.ThirdBlockName = "Empanelled Agencies";
            data.ThirdBlockCount = empanelledAgenciesCount;
            data.ThirdBlockUrl = "/Company/EmpanelledVendors";

            data.LastBlockName = "Available Agencies";
            data.LastBlockCount = availableAgenciesCount;
            data.LastBlockUrl = "/Company/AvailableVendors";

            return data;
        }
        private int GetAllAgencies(string userEmail)
        {
            var agencyCount = _context.Vendor.Count();
            return agencyCount;
        }
        private int GetAvailableAgencies(string userEmail)
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var availableVendors = _context.Vendor.Include(a=>a.VendorInvestigationServiceTypes)
                           .Count(v =>
                           !v.Clients.Any(c => c.ClientCompanyId == companyUser.ClientCompanyId) &&
                           (v.VendorInvestigationServiceTypes != null) && v.VendorInvestigationServiceTypes.Count > 0 &&  !v.Deleted && v.Status == VendorStatus.ACTIVE);
            return availableVendors;
        }
        private int GetEmpanelledAgencies(string userEmail)
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var empAgencies = _context.ClientCompany.Include(c=>c.EmpanelledVendors).FirstOrDefault(c=>c.ClientCompanyId == companyUser.ClientCompanyId);
            var count = empAgencies.EmpanelledVendors.Count(v=>v.Status == VendorStatus.ACTIVE);
            return count;
        }
        private int GetCompanyUsers(string userEmail)
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);

            var allCompanyUserCount = _context.ClientCompanyApplicationUser.Count(u => u.ClientCompanyId == companyUser.ClientCompanyId);

            return allCompanyUserCount;
        }
        private int GetSuperVisorActiveCount(string userEmail)
        {

            var openSubstatusesForSupervisor = _context.InvestigationCaseSubStatus.Where(i =>
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
            ).Select(s => s.InvestigationCaseSubStatusId).ToList();

            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submittedToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var replyStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR);

            IQueryable<ClaimsInvestigation> applicationDbContext = GetAgencyClaims();
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            if(vendorUser.IsVendorAdmin)
            {
                return applicationDbContext.Count(a => a.VendorId == vendorUser.VendorId &&
            openSubstatusesForSupervisor.Contains(a.InvestigationCaseSubStatusId) &&
                 (a.InvestigationCaseSubStatus == assignedToAgentStatus ||
                a.InvestigationCaseSubStatus == replyStatus ||
                 a.InvestigationCaseSubStatus == submittedToAssesssorStatus));
            }
            var count = applicationDbContext.Count(a => a.VendorId == vendorUser.VendorId &&
            openSubstatusesForSupervisor.Contains(a.InvestigationCaseSubStatusId) &&
                a.UserEmailActioned == vendorUser.Email && 
                (a.InvestigationCaseSubStatus == assignedToAgentStatus ||
                a.InvestigationCaseSubStatus == replyStatus ||
                 a.InvestigationCaseSubStatus == submittedToAssesssorStatus));

          
            return count;
        }

        private int GetAgencyVerifiedCount(string userEmail)
        {
            var submittedToVendorSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            IQueryable<ClaimsInvestigation> applicationDbContext = GetAgencyClaims();

            var vendorUser = _context.VendorApplicationUser.Include(u=>u.Vendor).FirstOrDefault(c => c.Email == userEmail);

            var count = applicationDbContext.Count(i => i.VendorId == vendorUser.VendorId &&
            i.UserEmailActionedTo == string.Empty &&
            i.UserRoleActionedTo == $"{vendorUser.Vendor.Email}" &&
            i.InvestigationCaseSubStatusId == submittedToVendorSupervisorStatus.InvestigationCaseSubStatusId);
            return count;
        }
        private int GetAgencyAllocateCount(string userEmail)
        {
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var requestedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            IQueryable<ClaimsInvestigation> applicationDbContext = GetAgencyClaims().Where(i => i.VendorId == vendorUser.VendorId);


            var count = applicationDbContext
                    .Count(i => i.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                    i.InvestigationCaseSubStatusId == requestedStatus.InvestigationCaseSubStatusId);
            
            return count;
        }
        private int GetAssessorAssess(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var replyByAgency = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var count = applicationDbContext.Count(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
            i.UserEmailActionedTo == string.Empty &&
             i.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}" &&
            i.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId ||
            i.InvestigationCaseSubStatusId == replyByAgency.InvestigationCaseSubStatusId
             );
            
            return count;
        }
        private int GetManagerAssess(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var count = applicationDbContext.Count(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
            i.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId);
            
            return count;
        }
        private int GetAgencyyCompleted(string userEmail)
        {
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var agencyUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            IQueryable<ClaimsInvestigation> applicationDbContext = GetAgencyClaims().Where(c =>
                c.CustomerDetail != null && c.VendorId == agencyUser.VendorId);
            if (agencyUser.IsVendorAdmin)
            {
                var claimsSubmitted = 0;
                foreach (var item in applicationDbContext)
                {
                    if (item.InvestigationCaseStatus.Name == CONSTANTS.CASE_STATUS.FINISHED &&
                        item.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ||
                        item.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR
                        )
                    {
                        claimsSubmitted += 1;
                    }
                }
                return claimsSubmitted;
            }
            else
            {
                var userAttendedClaims = _context.InvestigationTransaction.Where(t => (t.UserEmailActioned == agencyUser.Email &&
            t.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId))?.Select(c => c.ClaimsInvestigationId);

                var claimsSubmitted = 0;
                foreach (var item in applicationDbContext)
                {
                    if (item.InvestigationCaseStatus.Name == CONSTANTS.CASE_STATUS.FINISHED &&
                        item.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ||
                        item.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR
                        )
                    {
                        if (userAttendedClaims.Contains(item.ClaimsInvestigationId))
                        {
                            claimsSubmitted += 1;
                        }
                    }
                }
                return claimsSubmitted;
            }
            
        }
        private int GetCompanyManagerApproved(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            var count = applicationDbContext.Count(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                c.InvestigationCaseStatusId == finishStatus.InvestigationCaseStatusId &&
                c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId
                );
            
            return count;
        }

        private int GetCompanyCompleted(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims()
                .Where(c => c.CustomerDetail != null && c.AgencyReport != null);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            applicationDbContext = applicationDbContext.Where(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId && 
                c.InvestigationCaseStatusId == finishStatus.InvestigationCaseStatusId)
                || c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId
                );
            var count = 0;
            if (companyUser.UserRole == CompanyRole.CREATOR)
            {
                applicationDbContext = applicationDbContext.Where(c => c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId 
                || c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId);
            }
            else
            {
                applicationDbContext = applicationDbContext.Where(c => c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId);
            }

            foreach (var claim in applicationDbContext)
            {
                var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase &&
                c.UserEmailActioned == companyUser.Email && c.UserEmailActionedTo == companyUser.Email)?.ToList();

                int? reviewLogCount = 0;
                if (userHasReviewClaimLogs != null && userHasReviewClaimLogs.Count > 0)
                {
                    reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o => o.HopCount).First().HopCount;
                }

                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId &&
                c.HopCount >= reviewLogCount &&
                c.UserEmailActioned == companyUser.Email);
                if (userHasClaimLog)
                {
                    count += 1;
                }
            }
            return count;
        }
        private int GetAssessorReject(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims()
                .Where(c =>
                c.CustomerDetail != null && c.AgencyReport != null);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            applicationDbContext = applicationDbContext.Where(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId && 
                c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId
                );
            var count = 0;
            
            foreach (var claim in applicationDbContext)
            {
                var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase &&
                c.UserEmailActioned == companyUser.Email && c.UserEmailActionedTo == companyUser.Email)?.ToList();

                int? reviewLogCount = 0;
                if (userHasReviewClaimLogs != null && userHasReviewClaimLogs.Count > 0)
                {
                    reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o => o.HopCount).First().HopCount;
                }

                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId &&
                c.HopCount >= reviewLogCount &&
                c.UserEmailActioned == companyUser.Email);
                if (userHasClaimLog)
                {
                    count += 1;
                }
            }
            return count;
        }
        private int GetManagerReject(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            var count = applicationDbContext.Count(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId && c.InvestigationCaseStatusId == finishStatus.InvestigationCaseStatusId);
            
            return count;
        }

        private int GetAssessorReview(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();

            var requestedByAssessor = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);

            var count = 0;
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            applicationDbContext = applicationDbContext.Where(a => a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
            openStatusesIds.Contains(a.InvestigationCaseStatusId));

            foreach (var claim in applicationDbContext)
            {
                var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase &&
                c.UserEmailActioned == companyUser.Email)?.ToList();

                int? reviewLogCount = 0;
                if (userHasReviewClaimLogs != null && userHasReviewClaimLogs.Count > 0)
                {
                        reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o=>o.HopCount).First().HopCount;
                }
                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && 
                c.UserEmailActioned == companyUser.Email && c.HopCount >= reviewLogCount);

                if (claim.IsReviewCase && userHasClaimLog || claim.InvestigationCaseSubStatusId == requestedByAssessor.InvestigationCaseSubStatusId)
                {
                    count +=1;
                }
            }
            return count;
        }
        private int GetManagerReview(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var count = applicationDbContext.Count(a => a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
            openStatusesIds.Contains(a.InvestigationCaseStatusId) && a.IsReviewCase);

            
            return count;
        }

        private int GetCreatorActive(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            
            var count = 0;
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

            var withdrawnByCompanyStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);
            var declinedByAgencyStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);


            var companyUser = _context.ClientCompanyApplicationUser.Include(u=>u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);

            applicationDbContext = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
            a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId 
            && a.InvestigationCaseSubStatusId != createdStatus.InvestigationCaseSubStatusId
            && a.InvestigationCaseSubStatusId != withdrawnByCompanyStatus.InvestigationCaseSubStatusId
            && a.InvestigationCaseSubStatusId != declinedByAgencyStatus.InvestigationCaseSubStatusId
            && a.InvestigationCaseSubStatusId != assigned2AssignerStatus.InvestigationCaseSubStatusId
            );
            foreach (var claim in applicationDbContext)
            {
                var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase && 
                c.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")?.ToList();

                int? reviewLogCount = 0;
                if(userHasReviewClaimLogs !=null && userHasReviewClaimLogs.Count >0)
                {
                    reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o=>o.HopCount).First().HopCount;
                }
                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && 
                c.UserEmailActioned == companyUser.Email && c.HopCount >= reviewLogCount);
                if (userHasClaimLog)
                {
                    count +=1;
                }
            }
            return count;
        }
        private int GetManagerActive(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var submitted2AssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                      i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);

            var count = applicationDbContext.Count(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
            a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
            a.InvestigationCaseSubStatusId != createdStatus.InvestigationCaseSubStatusId  &&
            a.InvestigationCaseSubStatusId != submitted2AssessorStatus.InvestigationCaseSubStatusId  && 
            a.InvestigationCaseSubStatusId != assigned2AssignerStatus.InvestigationCaseSubStatusId
            );
            
            return count;
        }
        private int GetCreatorIncomplete(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var count = 0;
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            applicationDbContext = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) && a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);
            foreach (var claim in applicationDbContext)
            {
                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.UserEmailActioned == companyUser.Email);
                if (userHasClaimLog && !claim.AssignedToAgency && !claim.IsReady2Assign && claim.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId)
                {
                    count += 1;
                }
            }
            return count;
        }
        private int GetCreatorReAssignAuto(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();


            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);

            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

            var reAssignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                      i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);
            var withdrawnByCompany = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);
            var companyUser = _context.ClientCompanyApplicationUser.Include(u=>u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);

            // SHOWING DIFFERRENT PAGES AS PER ROLES
            var count = applicationDbContext.Count(a => a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (a.InvestigationCaseSubStatusId == withdrawnByAgency.InvestigationCaseSubStatusId &&
                        a.UserEmailActionedTo == string.Empty &&
                        a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")
                 ||
                 (a.InvestigationCaseSubStatusId == withdrawnByCompany.InvestigationCaseSubStatusId &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.UserEmailActioned == companyUser.Email &&
                        a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")
                 ||
                  (a.UserEmailActioned == companyUser.Email &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId)
                        ||
                        (!companyUser.ClientCompany.AutoAllocation && a.UserEmailActioned == companyUser.Email &&
                         a.UserEmailActionedTo == companyUser.Email &&
                         a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId) ||
                (a.IsReviewCase && a.InvestigationCaseSubStatusId == reAssignedStatus.InvestigationCaseSubStatusId &&
                a.UserEmailActionedTo == string.Empty &&
                a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}"));

            return count;
        }

        private int GetCreatorReAssign(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
               i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

            var reAssignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                      i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);
            var withdrawnByCompany = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);

            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);

            // SHOWING DIFFERRENT PAGES AS PER ROLES
            var count = applicationDbContext.Count(a => a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (a.InvestigationCaseSubStatusId == withdrawnByAgency.InvestigationCaseSubStatusId &&
                        a.UserEmailActionedTo == string.Empty &&
                        a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")
                 ||
                 (a.InvestigationCaseSubStatusId == withdrawnByCompany.InvestigationCaseSubStatusId &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.UserEmailActioned == companyUser.Email &&
                        a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")
                 ||
                 (a.UserEmailActioned == companyUser.Email &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId) 
                        ||
                (a.IsReviewCase && a.InvestigationCaseSubStatusId == reAssignedStatus.InvestigationCaseSubStatusId &&
                a.UserEmailActionedTo == string.Empty &&
                a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}"));

            return count;
        }

        private int GetCreatorAssignManual(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var count = applicationDbContext.Count(a => a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (
                    (a.UserEmailActioned == companyUser.Email &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId)
                ));

            return count;
        }
        private int GetCreatorAssignAuto(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var count = applicationDbContext.Count(a => a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (
                    (a.UserEmailActioned == companyUser.Email &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId)
                ));

            return count;
        }
        private IQueryable<ClaimsInvestigation> GetClaims()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.ClientCompany)
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
               .ThenInclude(c => c.District)
               .Include(c => c.InvestigationCaseStatus)
               .Include(c => c.InvestigationCaseSubStatus)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.InvestigationServiceType)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.LineOfBusiness)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.State)
               .Include(c => c.Vendor)
               .Include(c => c.PreviousClaimReports)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderBy(o => o.Created);
        }
        private IQueryable<ClaimsInvestigation> GetAgencyClaims()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.ClientCompany)
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
               .ThenInclude(c => c.District)
               .Include(c => c.InvestigationCaseStatus)
               .Include(c => c.InvestigationCaseSubStatus)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.InvestigationServiceType)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.LineOfBusiness)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.State)
               .Include(c => c.Vendor)
               .Include(c => c.PreviousClaimReports)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderBy(o => o.Created);
        }
        public DashboardData GetClaimsCount(string userEmail, string role)
        {
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED))?.ToList();

            var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submittededToSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var submittededToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var reAssigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);

            var assessorApprovedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();

            var companyUser = _context.ClientCompanyApplicationUser
                .Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = _context.VendorApplicationUser
                .Include(v => v.Vendor).FirstOrDefault(c => c.Email == userEmail);

            var data = new DashboardData();

            if (companyUser != null)
            {
                var pendinClaims = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Where(c => c.CurrentClaimOwner == userEmail && openStatusesIds.Contains(c.InvestigationCaseStatusId) && !c.Deleted &&
                    c.PolicyDetail.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId).ToList();

                var approvedClaims = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Where(c => c.InvestigationCaseSubStatusId == assessorApprovedStatus.InvestigationCaseSubStatusId &&
                    c.PolicyDetail.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();

                var rejectedClaims = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Where(c => c.IsReviewCase && openStatusesIds.Contains(c.InvestigationCaseStatusId) &&
                    c.PolicyDetail.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();

                var activeCount = 0;

                if (role.Contains(AppRoles.COMPANY_ADMIN.ToString()) || role.Contains(AppRoles.CREATOR.ToString()))
                {
                    var creatorActiveClaims = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Where(c => openStatusesIds.Contains(c.InvestigationCaseStatusId) && !c.Deleted &&
                    c.PolicyDetail.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();
                    activeCount = creatorActiveClaims.Count;
                }

                //if (role.Contains(AppRoles.Assigner.ToString()) && !role.Contains(AppRoles.CREATOR.ToString()))
                //{
                //    var creatorActiveClaims = _context.ClaimsInvestigation
                //    .Include(c => c.PolicyDetail)
                //    .Where(c => openStatusesIds.Contains(c.InvestigationCaseStatusId) &&
                //    c.InvestigationCaseSubStatusId == assignedToAssignerStatus.InvestigationCaseSubStatusId &&
                //    c.PolicyDetail.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();
                //    activeCount = creatorActiveClaims.Count;
                //}
                if (role.Contains(AppRoles.ASSESSOR.ToString()))
                {
                    var creatorActiveClaims = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Where(c => openStatusesIds.Contains(c.InvestigationCaseStatusId) && !c.Deleted &&
                    c.PolicyDetail.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId &&
                    c.InvestigationCaseSubStatusId == submittededToAssesssorStatus.InvestigationCaseSubStatusId
                    )?.ToList();
                    activeCount = creatorActiveClaims.Count;
                }

                data.FirstBlockName = "Active Claims";
                data.FirstBlockCount = activeCount;

                data.SecondBlockName = "Pending Claims";
                data.SecondBlockCount = pendinClaims.Count;

                data.ThirdBlockName = "Approved Claims";
                data.ThirdBlockCount = approvedClaims.Count;

                data.LastBlockName = "Review Claims";
                data.LastBlockCount = rejectedClaims.Count;
            }
            else if (vendorUser != null)
            {
                var activeClaims = _context.ClaimsInvestigation.Include(c => c.BeneficiaryDetail) 
                    .Where(c => openStatusesIds.Contains(c.InvestigationCaseStatusId) && !c.Deleted)?.ToList();
                var agencyActiveClaims = activeClaims.Where(c =>
                (c.VendorId == vendorUser.VendorId) &&
                (c.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId ||
                c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                c.InvestigationCaseSubStatusId == submittededToSupervisorStatus.InvestigationCaseSubStatusId))?.ToList();

                data.FirstBlockName = "Draft Claims";
                data.FirstBlockCount = agencyActiveClaims.Count;

                var pendinClaims = _context.ClaimsInvestigation
                     .Where(c => c.CurrentClaimOwner == userEmail && openStatusesIds.Contains(c.InvestigationCaseStatusId)).ToList();

                data.SecondBlockName = "Pending Claims";
                data.SecondBlockCount = pendinClaims.Count;

                var agentActiveClaims = _context.ClaimsInvestigation.Include(c => c.VendorId == vendorUser.VendorId &&
                c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId && !c.Deleted)?.ToList();

                data.ThirdBlockName = "Allocated Claims";
                data.ThirdBlockCount = agentActiveClaims.Count;

                var submitClaims = _context.ClaimsInvestigation.Include(c => c.VendorId == vendorUser.VendorId &&
                    c.InvestigationCaseSubStatusId == submittededToAssesssorStatus.InvestigationCaseSubStatusId && !c.Deleted)?.ToList();
                data.LastBlockName = "Submitted Claims";
                data.LastBlockCount = submitClaims.Count;
            }

            return data;
        }

        public Dictionary<string, int> CalculateAgencyCaseStatus(string userEmail) 
        {
            var vendorCaseCount = new Dictionary<string, int>();

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            List<Vendor> existingVendors = _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .ToList();

            if (companyUser == null)
            {
                return vendorCaseCount;
            }

            var claimsCases = _context.ClaimsInvestigation
               .Include(c => c.Vendors)
               .Include(c => c.BeneficiaryDetail);

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            int countOfCases = 0;
            foreach (var claimsCase in claimsCases)
            {
                if (claimsCase.BeneficiaryDetail?.BeneficiaryDetailId > 0)
                {
                    if (claimsCase.VendorId.HasValue)
                    {
                        if (claimsCase.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                claimsCase.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                claimsCase.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId
                                )
                        {
                            if (!vendorCaseCount.TryGetValue(claimsCase.VendorId.Value.ToString(), out countOfCases))
                            {
                                vendorCaseCount.Add(claimsCase.VendorId.Value.ToString(), 1);
                            }
                            else
                            {
                                int currentCount = vendorCaseCount[claimsCase.VendorId.Value.ToString()];
                                ++currentCount;
                                vendorCaseCount[claimsCase.VendorId.Value.ToString()] = currentCount;
                            }
                        }
                    }
                }
            }

            Dictionary<string, int> vendorWithCaseCounts = new();

            foreach (var existingVendor in existingVendors)
            {
                foreach (var vendorCase in vendorCaseCount)
                {
                    if (vendorCase.Key == existingVendor.VendorId.ToString())
                    {
                        vendorWithCaseCounts.Add(existingVendor.Name, vendorCase.Value);
                    }
                }
            }
            return vendorWithCaseCounts;
        }

        public Dictionary<string, int> CalculateAgentCaseStatus(string userEmail)
        {
            var vendorCaseCount = new Dictionary<string, int>();

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var existingVendor = _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .FirstOrDefault(v => v.VendorId == vendorUser.VendorId);

            var claimsCases = _context.ClaimsInvestigation
               .Include(c => c.Vendor)
               .Include(c => c.BeneficiaryDetail).Where(c => c.VendorId == vendorUser.VendorId);

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            int countOfCases = 0;

            var agentCaseCount = new Dictionary<string, int>();

            var vendorUsers = _context.VendorApplicationUser.Where(u =>
            u.VendorId == existingVendor.VendorId && !u.IsVendorAdmin);

            foreach (var vendorNonAdminUser in vendorUsers)
            {
                vendorCaseCount.Add(vendorNonAdminUser.Email, 0);

                foreach (var claimsCase in claimsCases)
                {
                    if (claimsCase.BeneficiaryDetail?.BeneficiaryDetailId > 0)
                    {
                        if (claimsCase.VendorId.HasValue && claimsCase.UserEmailActionedTo?.Trim()?.ToLower() == vendorNonAdminUser.Email.Trim().ToLower())
                        {
                            if (claimsCase.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    claimsCase.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId
                                    )
                            {
                                vendorCaseCount[vendorNonAdminUser.Email] += 1;
                            }
                            else
                            {
                                if (!vendorCaseCount.TryGetValue(vendorNonAdminUser.Email, out countOfCases))
                                {
                                    vendorCaseCount.Add(vendorNonAdminUser.Email, 1);
                                }
                                else
                                {
                                    int currentCount = vendorCaseCount[vendorNonAdminUser.Email];
                                    ++currentCount;
                                    vendorCaseCount[vendorNonAdminUser.Email] = currentCount;
                                }
                            }
                        }
                    }
                }
            }

            return vendorCaseCount;
        }

        public Dictionary<string, int> CalculateCaseChart(string userEmail)
        {
            Dictionary<string, int> dictMonthlySum = new Dictionary<string, int>();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var startDate = new DateTime(DateTime.Now.Year, 1, 1);
            var months = Enumerable.Range(0, 11)
                                   .Select(startDate.AddMonths)
                       .Select(m => m)
                       .ToList();
            var txn = _context.InvestigationTransaction;
            if (companyUser != null)
            {
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation)
                    .ThenInclude(i => i.PolicyDetail)
                    .Where(d =>
                        (companyUser.IsClientAdmin ? true : d.UpdatedBy == userEmail) &&
                     d.ClaimsInvestigation.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var monthName in months)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };

                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();
                        if (userSubStatuses.Contains(caseCurrentStatus.InvestigationCaseSubStatusId) && caseCurrentStatus.Created > monthName.Date && caseCurrentStatus.Created <= monthName.AddMonths(1))
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictMonthlySum.Add(monthName.ToString("MMM"), casesWithSameStatus.Count);
                }
            }
            else if (vendorUser != null)
            {
                var subStatuses = _context.InvestigationCaseSubStatus.Where(s =>
                s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR ||
                s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ||
                s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR
                );

                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation)
                    .ThenInclude(i => i.BeneficiaryDetail)
                    .Where(d =>
                        (vendorUser.IsVendorAdmin ? true : d.UpdatedBy == userEmail) &&
                     d.ClaimsInvestigation.VendorId == vendorUser.VendorId);

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var monthName in months)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };

                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();
                        if (userSubStatuses.Contains(caseCurrentStatus.InvestigationCaseSubStatusId) && caseCurrentStatus.Created > monthName.Date && caseCurrentStatus.Created <= monthName.AddMonths(1))
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictMonthlySum.Add(monthName.ToString("MMM"), casesWithSameStatus.Count);
                }
            }
            return dictMonthlySum;
        }

        public Dictionary<string, int> CalculateMonthlyCaseStatus(string userEmail)
        {
            var statuses = _context.InvestigationCaseStatus;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            Dictionary<string, int> dictWeeklyCases = new Dictionary<string, int>();
            if (companyUser != null)
            {
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation).Where(d =>
                        (companyUser.IsClientAdmin ? true : d.UpdatedBy == userEmail) &&
                       d.ClaimsInvestigation.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                       d.Created > DateTime.Now.AddMonths(-7));
                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var subStatus in filteredCases)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null && caseCurrentStatus.InvestigationCaseSubStatusId == subStatus.InvestigationCaseSubStatusId)
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictWeeklyCases.Add(subStatus.Name, casesWithSameStatus.Count);
                }
            }
            else if (vendorUser != null)
            {
                var subStatuses = _context.InvestigationCaseSubStatus.Where(s =>
                    s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR ||
                    s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                    s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ||
                    s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR
                    );
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation)
                    .ThenInclude(i => i.BeneficiaryDetail)
                    .Where(d =>
                        (vendorUser.IsVendorAdmin ? true : d.UpdatedBy == userEmail) &&
                     d.ClaimsInvestigation.VendorId == vendorUser.VendorId &&
                       d.Created > DateTime.Now.AddMonths(-7));
                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var subStatus in filteredCases)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null && caseCurrentStatus.InvestigationCaseSubStatusId == subStatus.InvestigationCaseSubStatusId)
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictWeeklyCases.Add(subStatus.Name, casesWithSameStatus.Count);
                }
            }
            return dictWeeklyCases;
        }

        public TatResult CalculateTimespan(string userEmail)
        {
            var dictWeeklyCases = new Dictionary<string, List<int>>();
            var result = new List<TatDetail>();
            int totalStatusChanged = 0;
            var statuses = _context.InvestigationCaseStatus;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            if (companyUser != null)
            {
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation)
                    .ThenInclude(i => i.PolicyDetail)
                    .Where(d =>
                    d.ClaimsInvestigation.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                    (companyUser.IsClientAdmin || d.UpdatedBy == userEmail) &&
                    d.Created > DateTime.Now.AddDays(-28));

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var userCaseStatuses = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var caseLogs = tdetail.GroupBy(g => g.InvestigationCaseSubStatusId)?.ToList();

                var workDays = new List<int> { 1, 2, 3, 4, 5 };
                var casesWithSameStatus = new List<InvestigationTransaction> { };
                foreach (var userCaseStatus in userCaseStatuses)
                {
                    List<int> caseListByStatus = new List<int>();

                    var caseLogsByStatus = caseLogs.Where(
                        c => c.Key == userCaseStatus.InvestigationCaseSubStatusId);

                    foreach (var caseWithSameStatus in caseLogsByStatus)
                    {
                        var casesWithCurrentStatus = caseWithSameStatus
                                                      .Where(c => c.InvestigationCaseSubStatusId == userCaseStatus.InvestigationCaseSubStatusId);
                        for (int i = 0; i < workDays.Count; i++)
                        {
                            var caseWithCurrentWorkDay = casesWithCurrentStatus.Where(c => c.Time2Update >= i && c.Time2Update < i + 1);

                            caseListByStatus.Add(caseWithCurrentWorkDay.Count());
                            if (caseWithCurrentWorkDay.Count() > 0)
                            {
                                totalStatusChanged++;
                            }
                        }
                    }

                    dictWeeklyCases.Add(userCaseStatus.Name, caseListByStatus);
                    result.Add(new TatDetail { Name = userCaseStatus.Name, Data = caseListByStatus });
                }
            }
            else if (vendorUser != null)
            {
                var subStatuses = _context.InvestigationCaseSubStatus.Where(s =>
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR ||
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ||
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR
                   );
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation)
                    .ThenInclude(i => i.BeneficiaryDetail)
                    .Where(d =>
                     d.ClaimsInvestigation.VendorId == vendorUser.VendorId &&
                    (vendorUser.IsVendorAdmin || d.UpdatedBy == userEmail) &&
                    d.Created > DateTime.Now.AddDays(-28));

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var userCaseStatuses = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var caseLogs = tdetail.GroupBy(g => g.InvestigationCaseSubStatusId)?.ToList();

                var workDays = new List<int> { 1, 2, 3, 4, 5 };
                var casesWithSameStatus = new List<InvestigationTransaction> { };
                foreach (var userCaseStatus in userCaseStatuses)
                {
                    List<int> caseListByStatus = new List<int>();

                    var caseLogsByStatus = caseLogs.Where(
                        c => c.Key == userCaseStatus.InvestigationCaseSubStatusId);

                    foreach (var caseWithSameStatus in caseLogsByStatus)
                    {
                        var casesWithCurrentStatus = caseWithSameStatus
                                                      .Where(c => c.InvestigationCaseSubStatusId == userCaseStatus.InvestigationCaseSubStatusId);
                        for (int i = 0; i < workDays.Count; i++)
                        {
                            var caseWithCurrentWorkDay = casesWithCurrentStatus.Where(c => c.Time2Update >= i && c.Time2Update < i + 1);

                            caseListByStatus.Add(caseWithCurrentWorkDay.Count());
                            if (caseWithCurrentWorkDay.Count() > 0)
                            {
                                totalStatusChanged++;
                            }
                        }
                    }

                    dictWeeklyCases.Add(userCaseStatus.Name, caseListByStatus);
                    result.Add(new TatDetail { Name = userCaseStatus.Name, Data = caseListByStatus });
                }
            }

            return new TatResult { Count = totalStatusChanged, TatDetails = result };
        }

        public Dictionary<string, int> CalculateWeeklyCaseStatus(string userEmail)
        {
            Dictionary<string, int> dictWeeklyCases = new Dictionary<string, int>();

            var tdetailDays = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation)
                    .ThenInclude(i => i.PolicyDetail)
                    .Include(i => i.ClaimsInvestigation)
             .ThenInclude(i => i.BeneficiaryDetail)
             .Where(d =>
             d.Created > DateTime.Now.AddDays(-28));

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            if (companyUser != null)
            {
                var statuses = _context.InvestigationCaseStatus;
                var tdetail = tdetailDays.Where(d =>
                    (companyUser.IsClientAdmin || d.UpdatedBy == userEmail) &&
                    d.ClaimsInvestigation.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var subStatus in filteredCases)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null && caseCurrentStatus.InvestigationCaseSubStatusId == subStatus.InvestigationCaseSubStatusId)
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }
                    dictWeeklyCases.Add(subStatus.Name, casesWithSameStatus.Count);
                }
            }
            else if (vendorUser != null)
            {
                var subStatuses = _context.InvestigationCaseSubStatus.Where(s =>
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR ||
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ||
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR
                   );
                var statuses = _context.InvestigationCaseStatus;
                var tdetail = tdetailDays.Where(d =>
                    (vendorUser.IsVendorAdmin || d.UpdatedBy == userEmail) &&
                    d.ClaimsInvestigation.VendorId == vendorUser.VendorId);

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var subStatus in filteredCases)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null && caseCurrentStatus.InvestigationCaseSubStatusId == subStatus.InvestigationCaseSubStatusId)
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }
                    dictWeeklyCases.Add(subStatus.Name, casesWithSameStatus.Count);
                }
            }
            return dictWeeklyCases;
        }

    }

    public class TatDetail
    {
        public string Name { get; set; }
        public List<int> Data { get; set; }
    }

    public class TatResult
    {
        public List<TatDetail> TatDetails { get; set; }
        public int Count { get; set; }
    }
}