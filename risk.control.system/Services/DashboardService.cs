﻿using System.Linq;
using System.Security.Claims;

using Amazon.Runtime.Internal.Transform;

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
        Dictionary<string, int> CalculateAgencyClaimStatus(string userEmail);
        Dictionary<string, int> CalculateAgencyUnderwritingStatus(string userEmail);

        Dictionary<string, int> CalculateAgentCaseStatus(string userEmail);

        Dictionary<string, (int count1, int count2)> CalculateWeeklyCaseStatus(string userEmail);

        Dictionary<string, (int count1, int count2)> CalculateMonthlyCaseStatus(string userEmail);

        Dictionary<string, (int count1, int count2)> CalculateCaseChart(string userEmail);

        Dictionary<string, int> CalculateWeeklyCaseStatusPieClaims(string userEmail);

        Dictionary<string, int> CalculateWeeklyCaseStatusPieUnderwritings(string userEmail);

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
        private const string UNDERWRITING = "underwriting";
        private const string CLAIMS = "claims";
        private readonly ApplicationDbContext _context;
        private long claimLineOfBusinessId;
        private long underwritingLineOfBusinessId;

        public DashboardService(ApplicationDbContext context)
        {
            this._context = context;
            claimLineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIMS).LineOfBusinessId;
            underwritingLineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;
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
            var allCompaniesCount = _context.ClientCompany.Count(c=>!c.Deleted);
            var allAgenciesCount = _context.Vendor.Count(v=>!v.Deleted);
            var AllUsersCount = _context.ApplicationUser.Count(u=>!u.Deleted);
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
            var data = new DashboardData
            {
                AutoAllocation = company.AutoAllocation,
                BulkUpload = company.BulkUpload
            };


            data.FirstBlockName = "ADD/ASSIGN";
            //data.FirstBlockCount = GetCreatorAssignAuto(userEmail);
            data.FirstBlockUrl = "/CreatorAuto/New";

            var claimCount = GetCreatorAssignAuto(userEmail, claimLineOfBusinessId);
            var underWritingCount = GetCreatorAssignAuto(userEmail, underwritingLineOfBusinessId);
            data.FirstBlockCount = claimCount;
            data.SecondBlockCount = underWritingCount;
            
            var filesUploadCount = _context.FilesOnFileSystem.Count(f => f.CompanyId == company.ClientCompanyId && !f.Deleted && f.UploadedBy == companyUser.Email && !f.DirectAssign);
            var filesUploadAssignCount = _context.FilesOnFileSystem.Count(f => f.CompanyId == company.ClientCompanyId && !f.Deleted && f.UploadedBy == companyUser.Email && f.DirectAssign);
            data.BulkUploadBlockName = "UPLOAD  ";
            data.BulkUploadBlockUrl = "/ClaimsLog/Uploads";
            data.BulkUploadBlockCount = filesUploadCount;
            data.BulkUploadAssignCount = filesUploadAssignCount;

            data.ThirdBlockName = "ACTIVE";
            var claimsActive = GetCreatorActive(userEmail, claimLineOfBusinessId);
            var underWritingActive = GetCreatorActive(userEmail, underwritingLineOfBusinessId);
            data.ThirdBlockCount = claimsActive;
            data.LastBlockCount = underWritingActive;
            data.ThirdBlockUrl = "/ClaimsActive/Active";

            return data;
        }

        public DashboardData GetManagerCount(string userEmail, string role)
        {

            var claimsAssessor = GetManagerAssess(userEmail, claimLineOfBusinessId);
            var underwritingAssessor = GetManagerAssess(userEmail, underwritingLineOfBusinessId);
            //var claimsReview = GetManagerReview(userEmail);
            
            var claimsReject = GetManagerReject(userEmail, claimLineOfBusinessId);
            var undewrwritingReject = GetManagerReject(userEmail, underwritingLineOfBusinessId);
            
            var claimsCompleted = GetCompanyManagerApproved(userEmail, claimLineOfBusinessId);
            var underwritingCompleted = GetCompanyManagerApproved(userEmail, underwritingLineOfBusinessId);
            
            var activeClaims = GetManagerActive(userEmail, claimLineOfBusinessId);
            var activeUnderwritings = GetManagerActive(userEmail,underwritingLineOfBusinessId);
            
            var empanelledAgenciesCount = GetEmpanelledAgencies(userEmail);
            var availableAgenciesCount = GetAvailableAgencies(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "Assess (new)";
            data.FirstBlockCount = claimsAssessor;
            data.FirstBlockUrl = "/Manager/Assessor";

            //data.SecondBlockName = "Review";
            data.UnderwritingCount = underwritingAssessor;
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

            var claimsAssessor = GetAssessorAssess(userEmail, claimLineOfBusinessId);
            var underwritingAssessor = GetAssessorAssess(userEmail, underwritingLineOfBusinessId);

            var claimsReview = GetAssessorReview(userEmail, claimLineOfBusinessId);
            var underwritingReview = GetAssessorReview(userEmail, underwritingLineOfBusinessId);


            var claimsReject= GetAssessorReject(userEmail, claimLineOfBusinessId);
            var underwritingReject= GetAssessorReject(userEmail, underwritingLineOfBusinessId);

            var claimsCompleted = GetCompanyCompleted(userEmail, claimLineOfBusinessId);
            var underwritingCompleted = GetCompanyCompleted(userEmail, underwritingLineOfBusinessId);

            var data = new DashboardData();
            data.FirstBlockName = "Assess (report)";
            data.FirstBlockCount = claimsAssessor;
            data.UnderwritingCount = underwritingAssessor;
            data.FirstBlockUrl = "/Assessor/Assessor";

            data.SecondBlockName = "Review";
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
        private int GetAllAgencies(string userEmail)
        {
            var agencyCount = _context.Vendor.Count(a=>!a.Deleted);
            return agencyCount;
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
            var empAgencies = _context.ClientCompany.Include(c=>c.EmpanelledVendors).FirstOrDefault(c=>c.ClientCompanyId == companyUser.ClientCompanyId);
            var count = empAgencies.EmpanelledVendors.Count(v=>v.Status == VendorStatus.ACTIVE && !v.Deleted);
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
        private int GetAssessorAssess(string userEmail, long lineOfBusinessId = 0)
        {
            IQueryable<ClaimsInvestigation> cases = lineOfBusinessId > 0 ? GetClaims().Where(c => c.PolicyDetail.LineOfBusinessId == lineOfBusinessId) : GetClaims();
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var replyByAgency = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var count = cases.Count(i => i.ClientCompanyId == companyUser.ClientCompanyId &&
            i.UserEmailActionedTo == string.Empty &&
             i.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}" &&
            i.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId ||
            i.InvestigationCaseSubStatusId == replyByAgency.InvestigationCaseSubStatusId
             );
            
            return count;
        }
        private int GetManagerAssess(string userEmail, long lineOfBusinessId = 0)
        {
            IQueryable<ClaimsInvestigation> cases = lineOfBusinessId > 0 ? GetClaims().Where(c=>c.PolicyDetail.LineOfBusinessId == lineOfBusinessId) : GetClaims();
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var replyToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var count = cases.Count(i => i.ClientCompanyId == companyUser.ClientCompanyId &&
            (i.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId ||
            i.InvestigationCaseSubStatusId == replyToAssessorStatus.InvestigationCaseSubStatusId)
            );
            
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
        private int GetCompanyManagerApproved(string userEmail, long lineOfBusinessId = 0)
        {
            IQueryable<ClaimsInvestigation> cases = lineOfBusinessId > 0 ? GetClaims().Where(c => c.PolicyDetail.LineOfBusinessId == lineOfBusinessId) : GetClaims();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            var count = cases.Count(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.InvestigationCaseStatusId == finishStatus.InvestigationCaseStatusId &&
                c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId
                );
            
            return count;
        }

        private int GetCompanyCompleted(string userEmail, long lineOfBusinessId = 0)
        {
            IQueryable<ClaimsInvestigation> cases = lineOfBusinessId > 0 ? GetClaims().Where(c => c.PolicyDetail.LineOfBusinessId == lineOfBusinessId) : GetClaims();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            cases = cases.Where(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                (c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId && 
                c.InvestigationCaseStatusId == finishStatus.InvestigationCaseStatusId)
                || c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId
                );
            var count = 0;
            if (companyUser.UserRole == CompanyRole.CREATOR)
            {
                cases = cases.Where(c => c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId 
                || c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId);
            }
            else
            {
                cases = cases.Where(c => c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId);
            }

            foreach (var claim in cases)
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
        private int GetAssessorReject(string userEmail, long lineOfBusinessId = 0)
        {
            IQueryable<ClaimsInvestigation> cases = lineOfBusinessId > 0 ? GetClaims().Where(c => c.PolicyDetail.LineOfBusinessId == lineOfBusinessId) : GetClaims();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            cases = cases.Where(c => c.ClientCompanyId == companyUser.ClientCompanyId && 
                c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId
                );
            var count = 0;
            
            foreach (var claim in cases)
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
        private int GetManagerReject(string userEmail, long lineOfBusinessId = 0)
        {
            IQueryable<ClaimsInvestigation> cases = lineOfBusinessId > 0 ? GetClaims().Where(c => c.PolicyDetail.LineOfBusinessId == lineOfBusinessId) : GetClaims();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            var count = cases.Count(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId && c.InvestigationCaseStatusId == finishStatus.InvestigationCaseStatusId);
            
            return count;
        }

        private int GetAssessorReview(string userEmail, long lineOfBusinessId = 0)
        {
            IQueryable<ClaimsInvestigation> cases = lineOfBusinessId > 0 ? GetClaims().Where(c => c.PolicyDetail.LineOfBusinessId == lineOfBusinessId) : GetClaims();
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();

            var requestedByAssessor = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);

            var count = 0;
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            cases = cases.Where(a => a.ClientCompanyId == companyUser.ClientCompanyId &&
            openStatusesIds.Contains(a.InvestigationCaseStatusId));

            foreach (var claim in cases)
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

        private int GetCreatorActive(string userEmail, long lineOfBusinessId = 0)
        {
            IQueryable<ClaimsInvestigation> cases = lineOfBusinessId > 0 ? GetClaims().Where(c=>c.PolicyDetail.LineOfBusinessId == lineOfBusinessId) : GetClaims();
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

            cases = cases.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
            a.ClientCompanyId == companyUser.ClientCompanyId 
            && a.InvestigationCaseSubStatusId != createdStatus.InvestigationCaseSubStatusId
            && a.InvestigationCaseSubStatusId != withdrawnByCompanyStatus.InvestigationCaseSubStatusId
            && a.InvestigationCaseSubStatusId != declinedByAgencyStatus.InvestigationCaseSubStatusId
            && a.InvestigationCaseSubStatusId != assigned2AssignerStatus.InvestigationCaseSubStatusId
            );
            foreach (var claim in cases)
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
        private int GetManagerActive(string userEmail, long lineOfBusinessId = 0)
        {
            IQueryable<ClaimsInvestigation> claims = lineOfBusinessId > 0 ? GetClaims().Where(c => c.PolicyDetail.LineOfBusinessId == lineOfBusinessId) : GetClaims();
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var submitted2AssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                      i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var replyToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR);

            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);

            var count = claims.Count(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
            a.ClientCompanyId == companyUser.ClientCompanyId &&
            a.InvestigationCaseSubStatusId != createdStatus.InvestigationCaseSubStatusId  &&
            a.InvestigationCaseSubStatusId != submitted2AssessorStatus.InvestigationCaseSubStatusId  && 
            a.InvestigationCaseSubStatusId != replyToAssessorStatus.InvestigationCaseSubStatusId  && 
            a.InvestigationCaseSubStatusId != assigned2AssignerStatus.InvestigationCaseSubStatusId
            );
            
            return count;
        }

        private int GetCreatorAssignAuto(string userEmail, long lineOfBusinessId = 0)
        {
            IQueryable<ClaimsInvestigation> claims = lineOfBusinessId > 0 ? GetClaims().Where(c=>c.PolicyDetail.LineOfBusinessId == lineOfBusinessId) : GetClaims();

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var reAssignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);

            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                      i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);
            var withdrawnByCompany = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            
            var count= claims.Count(a =>
                a.ClientCompanyId == companyUser.ClientCompanyId &&
                     (a.UserEmailActioned == companyUser.Email &&
                         a.UserEmailActionedTo == companyUser.Email &&
                         a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId)
                         ||
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

                 );
            return count;
        }

        private IQueryable<ClaimsInvestigation> GetClaims()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
               .Include(c => c.PolicyDetail)
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
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderBy(o => o.Created);
        }
        private IQueryable<ClaimsInvestigation> GetAgencyClaims()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
               .Include(c => c.PolicyDetail)
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
                    c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId).ToList();

                var approvedClaims = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Where(c => c.InvestigationCaseSubStatusId == assessorApprovedStatus.InvestigationCaseSubStatusId &&
                    c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();

                var rejectedClaims = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Where(c => c.IsReviewCase && openStatusesIds.Contains(c.InvestigationCaseStatusId) &&
                    c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();

                var activeCount = 0;

                if (role.Contains(AppRoles.COMPANY_ADMIN.ToString()) || role.Contains(AppRoles.CREATOR.ToString()))
                {
                    var creatorActiveClaims = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Where(c => openStatusesIds.Contains(c.InvestigationCaseStatusId) && !c.Deleted &&
                    c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();
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
                    c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId &&
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

        public Dictionary<string, int> CalculateAgencyClaimStatus(string userEmail) 
        {
            return CalculateAgencyCaseStatus(userEmail, claimLineOfBusinessId);
        }

        public Dictionary<string, int> CalculateAgencyUnderwritingStatus(string userEmail)
        {
            return CalculateAgencyCaseStatus(userEmail, underwritingLineOfBusinessId);
        }
        Dictionary<string, int> CalculateAgencyCaseStatus(string userEmail, long lineOfBusinessId)
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
                .ToList();

            if (companyUser == null)
            {
                return vendorCaseCount;
            }

            var claimsCases = _context.ClaimsInvestigation
               .Include(c => c.Vendors)
               .Include(c => c.PolicyDetail)
               .Include(c => c.BeneficiaryDetail).Where(c=>c.PolicyDetail.LineOfBusinessId == lineOfBusinessId && !c.Deleted);

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var enquiryStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);
            int countOfCases = 0;
            foreach (var claimsCase in claimsCases)
            {
                if (claimsCase.BeneficiaryDetail?.BeneficiaryDetailId > 0)
                {
                    if (claimsCase.VendorId.HasValue && (claimsCase.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                claimsCase.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                claimsCase.InvestigationCaseSubStatusId == enquiryStatus.InvestigationCaseSubStatusId ||
                                claimsCase.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId)
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

        public Dictionary<string, (int count1, int count2)> CalculateCaseChart(string userEmail)
        {
            Dictionary<string, (int count1, int count2)> dictMonthlySum = new Dictionary<string, (int count1, int count2)>();
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
                     d.ClaimsInvestigation.ClientCompanyId == companyUser.ClientCompanyId);

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var monthName in months)
                {
                    var claimsWithSameStatus = new List<InvestigationTransaction> { };
                    var underwritingWithSameStatus = new List<InvestigationTransaction> { };

                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();
                        if (userSubStatuses.Contains(caseCurrentStatus.InvestigationCaseSubStatusId) && 
                            caseCurrentStatus.Created > monthName.Date && 
                            caseCurrentStatus.Created <= monthName.AddMonths(1))
                        {

                            if (caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == claimLineOfBusinessId && !caseCurrentStatus.ClaimsInvestigation.Deleted)
                            {
                                claimsWithSameStatus.Add(caseCurrentStatus);
                            }
                            else if (caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == underwritingLineOfBusinessId && !caseCurrentStatus.ClaimsInvestigation.Deleted)
                            {
                                underwritingWithSameStatus.Add(caseCurrentStatus);
                            }
                        }
                    }

                    dictMonthlySum.Add(monthName.ToString("MMM"), (claimsWithSameStatus.Count, underwritingWithSameStatus.Count));
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
                    var claimsWithSameStatus = new List<InvestigationTransaction> { };
                    var underwritingWithSameStatus = new List<InvestigationTransaction> { };

                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();
                        if (userSubStatuses.Contains(caseCurrentStatus.InvestigationCaseSubStatusId) && caseCurrentStatus.Created > monthName.Date && caseCurrentStatus.Created <= monthName.AddMonths(1))
                        {
                            if (caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == claimLineOfBusinessId)
                            {
                                claimsWithSameStatus.Add(caseCurrentStatus);
                            }
                            else if (caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == underwritingLineOfBusinessId)
                            {
                                underwritingWithSameStatus.Add(caseCurrentStatus);
                            }
                        }
                    }

                    dictMonthlySum.Add(monthName.ToString("MMM"), (claimsWithSameStatus.Count, underwritingWithSameStatus.Count));
                }
            }
            return dictMonthlySum;
        }

        public Dictionary<string, (int count1, int count2)> CalculateMonthlyCaseStatus(string userEmail)
        {
            var statuses = _context.InvestigationCaseStatus;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            Dictionary<string, (int count1, int count2)> dictWeeklyCases = new Dictionary<string, (int count1, int count2)>();
            if (companyUser != null)
            {
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation).Where(d =>
                        (companyUser.IsClientAdmin ? true : d.UpdatedBy == userEmail) &&
                       d.ClaimsInvestigation.ClientCompanyId == companyUser.ClientCompanyId &&
                       d.Created > DateTime.Now.AddMonths(-7));
                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var subStatus in filteredCases)
                {
                    var claimsWithSameStatus = new List<InvestigationTransaction> { };
                    var underwritingWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == claimLineOfBusinessId && !caseCurrentStatus.ClaimsInvestigation.Deleted)
                        {
                            claimsWithSameStatus.Add(caseCurrentStatus);
                        }
                        else if (caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == underwritingLineOfBusinessId && !caseCurrentStatus.ClaimsInvestigation.Deleted)
                        {
                            underwritingWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictWeeklyCases.Add(subStatus.Name, (claimsWithSameStatus.Count, underwritingWithSameStatus.Count));
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
                    var claimsWithSameStatus = new List<InvestigationTransaction> { };
                    var underwritingWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == claimLineOfBusinessId && !caseCurrentStatus.ClaimsInvestigation.Deleted)
                        {
                            claimsWithSameStatus.Add(caseCurrentStatus);
                        }
                        else if (caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == underwritingLineOfBusinessId && !caseCurrentStatus.ClaimsInvestigation.Deleted)
                        {
                            underwritingWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictWeeklyCases.Add(subStatus.Name, (claimsWithSameStatus.Count, underwritingWithSameStatus.Count));
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
                    d.ClaimsInvestigation.ClientCompanyId == companyUser.ClientCompanyId &&
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

        public Dictionary<string, (int count1, int count2)> CalculateWeeklyCaseStatus(string userEmail)
        {
            Dictionary<string, (int count1, int count2)> dictWeeklyCases = new Dictionary<string, (int, int)>();

            var tdetailDays = _context.InvestigationTransaction
                 .Include(i => i.ClaimsInvestigation)
                    .ThenInclude(i => i.InvestigationCaseSubStatus)
                    .Include(i => i.ClaimsInvestigation)
                    .ThenInclude(i => i.PolicyDetail)
                     .Where(d => d.Created > DateTime.Now.AddDays(-28));

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            if (companyUser != null)
            {
                var statuses = _context.InvestigationCaseStatus;
                var tdetail = tdetailDays.Where(d =>
                    (companyUser.IsClientAdmin || d.UpdatedBy == userEmail) &&
                    d.ClaimsInvestigation.ClientCompanyId == companyUser.ClientCompanyId);

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var subStatus in filteredCases)
                {
                    var claimsWithSameStatus = new List<InvestigationTransaction> { };
                    var underwritingWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null && caseCurrentStatus.InvestigationCaseSubStatusId == subStatus.InvestigationCaseSubStatusId && !caseCurrentStatus.ClaimsInvestigation.Deleted)
                        {
                            if(caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == claimLineOfBusinessId)
                            {
                                claimsWithSameStatus.Add(caseCurrentStatus);
                            }
                            else if (caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == underwritingLineOfBusinessId)
                            {
                                underwritingWithSameStatus.Add(caseCurrentStatus);
                            }
                        }
                    }
                    dictWeeklyCases.Add(subStatus.Name, (claimsWithSameStatus.Count,underwritingWithSameStatus.Count));
                }
            }
            else if (vendorUser != null)
            {
                var subStatuses = _context.InvestigationCaseSubStatus.Where(s =>
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR ||
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ||
                   s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR ||
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
                    var claimsWithSameStatus = new List<InvestigationTransaction> { };
                    var underwritingWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == claimLineOfBusinessId && !caseCurrentStatus.ClaimsInvestigation.Deleted)
                        {
                            claimsWithSameStatus.Add(caseCurrentStatus);
                        }
                        else if (caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == underwritingLineOfBusinessId && !caseCurrentStatus.ClaimsInvestigation.Deleted)
                        {
                            underwritingWithSameStatus.Add(caseCurrentStatus);
                        }
                    }
                    dictWeeklyCases.Add(subStatus.Name, (claimsWithSameStatus.Count, underwritingWithSameStatus.Count));
                }
            }
            return dictWeeklyCases;
        }

        public Dictionary<string, int> CalculateWeeklyCaseStatusPieClaims(string userEmail)
        {
            return CalculateWeeklyCaseStatusPie(userEmail, claimLineOfBusinessId);
        }
        public Dictionary<string, int> CalculateWeeklyCaseStatusPieUnderwritings(string userEmail)
        {
            return CalculateWeeklyCaseStatusPie(userEmail, underwritingLineOfBusinessId);
        }
        private Dictionary<string, int> CalculateWeeklyCaseStatusPie(string userEmail, long lineOfBusinessId)
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
                    d.ClaimsInvestigation.ClientCompanyId == companyUser.ClientCompanyId);

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

                        if (caseCurrentStatus != null && 
                            caseCurrentStatus.InvestigationCaseSubStatusId == subStatus.InvestigationCaseSubStatusId && 
                            !caseCurrentStatus.ClaimsInvestigation.Deleted && 
                            caseCurrentStatus.ClaimsInvestigation.PolicyDetail.LineOfBusinessId == lineOfBusinessId)
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