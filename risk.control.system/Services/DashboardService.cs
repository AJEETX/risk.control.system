using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Linq;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

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
        DashboardData GetSupervisorCount(string userEmail, string role);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public DashboardData GetSupervisorCount(string userEmail, string role)
        {

            var claimsAllocate = GetAgencyAllocateCount(userEmail);
            var claimsVerified = GetAgencyVerifiedCount(userEmail);
            var claimsActive = GetSuperVisorActiveCount(userEmail);

            var claimsCompleted = GetAgencyyCompleted(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "Allocate";
            data.FirstBlockCount = claimsAllocate.Count;
            data.FirstBlockUrl = "/ClaimsVendor/Allocate";

            data.SecondBlockName = "Verify(report)";
            data.SecondBlockCount = claimsVerified.Count;
            data.SecondBlockUrl = "/ClaimsVendor/ClaimReport";

            data.ThirdBlockName = "Active";
            data.ThirdBlockCount = claimsActive.Count;
            data.ThirdBlockUrl = "/ClaimsVendor/Open";

            data.LastBlockName = "Completed";
            data.LastBlockCount = claimsCompleted.Count;
            data.LastBlockUrl = "/ClaimsVendor/Completed";

            return data;
        }
        public DashboardData GetCreatorCount(string userEmail, string role)
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var claimsIncomplete = GetCreatorIncomplete(userEmail);
            var claimsAssignAuto = GetCreatorAssignAuto(userEmail);
            var claimsAssignManual = GetCreatorAssignManual(userEmail);
            var claimsActive = GetCreatorActive(userEmail);
            //var claimsCompleted = GetCompanyCompleted(userEmail);

            var data = new DashboardData
            {
                AutoAllocation = company.AutoAllocation,
                BulkUpload = company.BulkUpload
            };
            data.FirstBlockName = "Draft";
            data.FirstBlockCount = claimsIncomplete.Count;
            data.FirstBlockUrl = "/ClaimsInvestigation/Incomplete";

            if (company.AutoAllocation)
            {
                data.SecondBlockName = "Assign(auto)";
                data.SecondBlockUrl = "/ClaimsInvestigation/Draft";
                data.SecondBlockCount = claimsAssignAuto.Count;

                data.SecondBBlockName = "Re / Assign";
                data.SecondBBlockUrl = "/ClaimsInvestigation/Assigner";
                data.SecondBBlockCount = claimsAssignManual.Count;

                if(company.BulkUpload)
                {
                    var files = _context.FilesOnFileSystem.Where(f => f.CompanyId == company.ClientCompanyId).ToList();
                    data.BulkUploadBlockName = "Upload Count";
                    data.BulkUploadBlockUrl = "Report";
                    data.BulkUploadBlockCount = files.Count;
                }
            }
            else
            {
                data.SecondBlockName = "Re / Assign";
                data.SecondBlockUrl = "/ClaimsInvestigation/Assigner";
                data.SecondBlockCount = claimsAssignManual.Count;
            }

            data.ThirdBlockName = "Active";
            data.ThirdBlockCount = claimsActive.Count;
            data.ThirdBlockUrl = "/ClaimsInvestigation/Active";

            //data.LastBlockName = "Completed";
            //data.LastBlockCount = claimsCompleted.Count;
            //data.LastBlockUrl = "/Report";

            return data;
        }

        public DashboardData GetAssessorCount(string userEmail, string role)
        {

            var claimsAssessor = GetAssessorAssess(userEmail);
            var claimsReview = GetAssessorReview(userEmail);
            var claimsReject= GetAssessorReject(userEmail);
            var claimsCompleted = GetCompanyCompleted(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "Assess";
            data.FirstBlockCount = claimsAssessor.Count;
            data.FirstBlockUrl = "/ClaimsInvestigation/Assessor";

            data.SecondBlockName = "Review";
            data.SecondBlockCount = claimsReview.Count;
            data.SecondBlockUrl = "/ClaimsInvestigation/Review";

            data.ThirdBlockName = "Approved";
            data.ThirdBlockCount = claimsCompleted.Count;
            data.ThirdBlockUrl = "/Report";

            data.LastBlockName = "Rejected";
            data.LastBlockCount = claimsReject.Count;
            data.LastBlockUrl = "/Report/Rejected";

            return data;
        }
        private List<ClaimsInvestigation> GetSuperVisorActiveCount(string userEmail)
        {

            var openSubstatusesForSupervisor = _context.InvestigationCaseSubStatus.Where(i =>
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
            ).Select(s => s.InvestigationCaseSubStatusId).ToList();

            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submittedToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            IQueryable<ClaimsInvestigation> applicationDbContext = GetAgencyClaims();
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            applicationDbContext = applicationDbContext.Where(i => i.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));
            applicationDbContext = applicationDbContext.Where(a => openSubstatusesForSupervisor.Contains(a.InvestigationCaseSubStatusId) &&
                (a.UserEmailActioned == vendorUser.Email && a.InvestigationCaseSubStatus == assignedToAgentStatus) ||
                (a.UserEmailActioned == vendorUser.Email && a.InvestigationCaseSubStatus == submittedToAssesssorStatus)
                );

            var claimsAllocated = new List<ClaimsInvestigation>();

            var finalQuery = applicationDbContext.ToList();

            return finalQuery;
        }

        private List<ClaimsInvestigation> GetAgencyVerifiedCount(string userEmail)
        {
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submittedToVendorSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            IQueryable<ClaimsInvestigation> applicationDbContext = GetAgencyClaims();

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            applicationDbContext = applicationDbContext.Where(i => i.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));

            var claimsSubmitted = new List<ClaimsInvestigation>();
            foreach (var item in applicationDbContext)
            {
                item.CaseLocations = item.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId
                    && c.InvestigationCaseSubStatusId == submittedToVendorSupervisorStatus.InvestigationCaseSubStatusId
                    && !c.IsReviewCaseLocation
                    )?.ToList();
                if (item.CaseLocations.Any())
                {
                    claimsSubmitted.Add(item);
                }
            }
            return claimsSubmitted;
        }
        private List<ClaimsInvestigation> GetAgencyAllocateCount(string userEmail)
        {
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            IQueryable<ClaimsInvestigation> applicationDbContext = GetAgencyClaims();

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            applicationDbContext = applicationDbContext
                    .Include(a => a.PolicyDetail)
                    .ThenInclude(a => a.LineOfBusiness)
                    .Where(i => i.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));
            var claims = new List<ClaimsInvestigation>();
            applicationDbContext = applicationDbContext.Where(a =>
                a.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId);
            foreach (var item in applicationDbContext)
            {
                item.CaseLocations = item.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId)?.ToList();
                if (item.CaseLocations.Any())
                {
                    claims.Add(item);
                }
            }
            return claims;
        }
        private List<ClaimsInvestigation> GetAssessorAssess(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);

            var claimsSubmitted = new List<ClaimsInvestigation>();
            applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId != null));

            foreach (var item in applicationDbContext)
            {
                item.CaseLocations = item.CaseLocations.Where(c => c.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId)?.ToList();
                if (item.CaseLocations.Any())
                {
                    claimsSubmitted.Add(item);
                }
            }
            return claimsSubmitted;
        }
        private List<ClaimsInvestigation> GetAgencyyCompleted(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetAgencyClaims().Where(c =>
                c.CustomerDetail != null && c.CaseLocations.Count > 0 &&
                c.CaseLocations.All(c => c.ClaimReport != null));
            var agencyUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var userAttendedClaims = _context.InvestigationTransaction.Where(t => (t.UserEmailActioned == agencyUser.Email && 
            t.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId))?.Select(c => c.ClaimsInvestigationId);

            var claimsSubmitted = new List<ClaimsInvestigation>();
            foreach (var item in applicationDbContext)
            {
                if ((item.InvestigationCaseStatus.Name == CONSTANTS.CASE_STATUS.FINISHED &&
                    item.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR) ||
                    (item.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR))
                {
                    if (userAttendedClaims.Contains(item.ClaimsInvestigationId))
                    {
                        claimsSubmitted.Add(item);
                    }
                }
            }
            return claimsSubmitted;
        }
        private List<ClaimsInvestigation> GetCompanyCompleted(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims().Where(c =>
                c.CustomerDetail != null && c.CaseLocations.Count > 0 &&
                c.CaseLocations.All(c => c.ClaimReport != null));
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            var claims = applicationDbContext.Where(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (c.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR && c.InvestigationCaseStatusId == finishStatus.InvestigationCaseStatusId)
                || c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId
                )?.ToList();
            var claimsSubmitted = new List<ClaimsInvestigation>();
            if (companyUser.UserRole == CompanyRole.Creator)
            {
                claims = claims.Where(c => c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId || c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId)?.ToList();
            }
            else
            {
                claims = claims.Where(c => c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId)?.ToList();
            }

            foreach (var claim in claims)
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
                    claimsSubmitted.Add(claim);
                }
            }
            return claimsSubmitted;
        }
        private List<ClaimsInvestigation> GetAssessorReject(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims().Where(c =>
                c.CustomerDetail != null && c.CaseLocations.Count > 0 &&
                c.CaseLocations.All(c => c.ClaimReport != null));
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            var claims = applicationDbContext.Where(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId && 
                c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId
                )?.ToList();
            var claimsSubmitted = new List<ClaimsInvestigation>();
            
            foreach (var claim in claims)
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
                    claimsSubmitted.Add(claim);
                }
            }
            return claimsSubmitted;
        }
        private List<ClaimsInvestigation> GetAssessorReview(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
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
            var claimsSubmitted = new List<ClaimsInvestigation>();
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            applicationDbContext = applicationDbContext.Where(a => a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
            openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
            a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId != null)
            );

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

                if (claim.IsReviewCase && userHasClaimLog)
                {
                    claimsSubmitted.Add(claim);
                }
            }
            return claimsSubmitted;
        }
        private List<ClaimsInvestigation> GetCreatorActive(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                         i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                     i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);
            var reAssignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                         i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var approvedStatus = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var claimsSubmitted = new List<ClaimsInvestigation>();
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            var companyUser = _context.ClientCompanyApplicationUser.Include(u=>u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);

            var claims = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
            a.InvestigationCaseStatusId != _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR).InvestigationCaseSubStatusId &&
            a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);
            foreach (var claim in claims)
            {
                var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase && 
                c.UserRoleActionedTo == $"{AppRoles.Creator.GetEnumDisplayName()} ( {companyUser.ClientCompany.Email})")?.ToList();

                int? reviewLogCount = 0;
                if(userHasReviewClaimLogs !=null && userHasReviewClaimLogs.Count >0)
                {
                    reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o=>o.HopCount).First().HopCount;
                }
                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && 
                c.UserEmailActioned == companyUser.Email && c.HopCount >= reviewLogCount);
                if (userHasClaimLog && claim.InvestigationCaseSubStatusId != createdStatus.InvestigationCaseSubStatusId &&
                    claim.InvestigationCaseSubStatusId != withdrawnByAgency.InvestigationCaseSubStatusId
                    &&
                    claim.InvestigationCaseSubStatusId != reAssignedToAssignerStatus.InvestigationCaseSubStatusId
                    &&
                    claim.InvestigationCaseSubStatusId != approvedStatus.InvestigationCaseSubStatusId
                    )
                {
                    claimsSubmitted.Add(claim);
                }
            }
            return claimsSubmitted;
        }
        private List<ClaimsInvestigation> GetCreatorIncomplete(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var claimsSubmitted = new List<ClaimsInvestigation>();
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            applicationDbContext = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) && a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);
            foreach (var claim in applicationDbContext)
            {
                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.UserEmailActioned == companyUser.Email);
                if (userHasClaimLog && !claim.AssignedToAgency && !claim.IsReady2Assign && claim.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId)
                {
                    claimsSubmitted.Add(claim);
                }
            }
            return claimsSubmitted;
        }
        private List<ClaimsInvestigation> GetCreatorAssignManual(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var reAssignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                      i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);

            var companyUser = _context.ClientCompanyApplicationUser.Include(u=>u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);

            // SHOWING DIFFERRENT PAGES AS PER ROLES
            applicationDbContext = applicationDbContext.Where(a => a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (
                    a.IsReady2Assign && !a.AssignedToAgency && ( a.UserEmailActioned == companyUser.Email &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId
                        || a.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId)
                ) ||
                 (a.InvestigationCaseSubStatusId == withdrawnByAgency.InvestigationCaseSubStatusId &&
                        a.UserEmailActionedTo == string.Empty &&
                        a.UserRoleActionedTo == $"{AppRoles.Creator.GetEnumDisplayName()} ({companyUser.ClientCompany.Email})")
                 ||
                (a.IsReviewCase && a.InvestigationCaseSubStatusId == reAssignedStatus.InvestigationCaseSubStatusId && 
                a.UserEmailActionedTo == string.Empty &&
                a.UserRoleActionedTo == $"{AppRoles.Creator.GetEnumDisplayName()} ( {companyUser.ClientCompany.Email})")
                );

            var claimsAssigned = new List<ClaimsInvestigation>();

            foreach (var item in applicationDbContext)
            {
                claimsAssigned.Add(item);
            }
            return claimsAssigned;
        }
        private List<ClaimsInvestigation> GetCreatorAssignAuto(string userEmail)
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var reAssignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            applicationDbContext = applicationDbContext.Where(a => a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (
                    a.IsReady2Assign && !a.AssignedToAgency && (a.UserEmailActioned == companyUser.Email &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId)
                ));

            var claimsAssigned = new List<ClaimsInvestigation>();

            foreach (var item in applicationDbContext)
            {
                claimsAssigned.Add(item);
            }
            return claimsAssigned;
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
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.InvestigationCaseSubStatus)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.CaseLocations)
                .ThenInclude(c => c.District)
                .Include(c => c.CaseLocations)
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
               .Include(c => c.CaseLocations)
               .ThenInclude(l => l.PreviousClaimReports)
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
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.InvestigationCaseSubStatus)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.CaseLocations)
                .ThenInclude(c => c.District)
                .Include(c => c.CaseLocations)
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
               .Include(c => c.CaseLocations)
               .ThenInclude(l => l.PreviousClaimReports)
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

                if (role.Contains(AppRoles.CompanyAdmin.ToString()) || role.Contains(AppRoles.Creator.ToString()))
                {
                    var creatorActiveClaims = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Where(c => openStatusesIds.Contains(c.InvestigationCaseStatusId) && !c.Deleted &&
                    c.PolicyDetail.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();
                    activeCount = creatorActiveClaims.Count;
                }

                //if (role.Contains(AppRoles.Assigner.ToString()) && !role.Contains(AppRoles.Creator.ToString()))
                //{
                //    var creatorActiveClaims = _context.ClaimsInvestigation
                //    .Include(c => c.PolicyDetail)
                //    .Where(c => openStatusesIds.Contains(c.InvestigationCaseStatusId) &&
                //    c.InvestigationCaseSubStatusId == assignedToAssignerStatus.InvestigationCaseSubStatusId &&
                //    c.PolicyDetail.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId)?.ToList();
                //    activeCount = creatorActiveClaims.Count;
                //}
                if (role.Contains(AppRoles.Assessor.ToString()))
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
                var activeClaims = _context.ClaimsInvestigation.Include(c => c.CaseLocations) 
                    .Where(c => openStatusesIds.Contains(c.InvestigationCaseStatusId) && !c.Deleted)?.ToList();
                var agencyActiveClaims = activeClaims.Where(c =>
                (c.CaseLocations?.Count() > 0 && c.CaseLocations.Any(l => l.VendorId == vendorUser.VendorId)) &&
                (c.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId ||
                c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                c.InvestigationCaseSubStatusId == submittededToSupervisorStatus.InvestigationCaseSubStatusId))?.ToList();

                data.FirstBlockName = "Draft Claims";
                data.FirstBlockCount = agencyActiveClaims.Count;

                var pendinClaims = _context.ClaimsInvestigation
                     .Where(c => c.CurrentClaimOwner == userEmail && openStatusesIds.Contains(c.InvestigationCaseStatusId)).ToList();

                data.SecondBlockName = "Pending Claims";
                data.SecondBlockCount = pendinClaims.Count;

                var agentActiveClaims = _context.ClaimsInvestigation.Include(c => c.CaseLocations).Where(c =>
                (c.CaseLocations.Count() > 0 && c.CaseLocations.Any(l => l.VendorId == vendorUser.VendorId)) &&
                c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId && !c.Deleted)?.ToList();

                data.ThirdBlockName = "Allocated Claims";
                data.ThirdBlockCount = agentActiveClaims.Count;

                var submitClaims = _context.ClaimsInvestigation.Include(c => c.CaseLocations).Where(c =>
                (c.CaseLocations.Count() > 0 && c.CaseLocations.Any(l => l.VendorId == vendorUser.VendorId)) &&
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
               .Include(c => c.CaseLocations);

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            int countOfCases = 0;
            foreach (var claimsCase in claimsCases)
            {
                if (claimsCase.CaseLocations.Count > 0)
                {
                    foreach (var CaseLocation in claimsCase.CaseLocations)
                    {
                        if (CaseLocation.VendorId.HasValue)
                        {
                            if (CaseLocation.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    CaseLocation.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                    CaseLocation.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId
                                    )
                            {
                                if (!vendorCaseCount.TryGetValue(CaseLocation.VendorId.Value.ToString(), out countOfCases))
                                {
                                    vendorCaseCount.Add(CaseLocation.VendorId.Value.ToString(), 1);
                                }
                                else
                                {
                                    int currentCount = vendorCaseCount[CaseLocation.VendorId.Value.ToString()];
                                    ++currentCount;
                                    vendorCaseCount[CaseLocation.VendorId.Value.ToString()] = currentCount;
                                }
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
               .Include(c => c.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId));

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
                    if (claimsCase.CaseLocations.Count > 0)
                    {
                        foreach (var CaseLocation in claimsCase.CaseLocations)
                        {
                            if (CaseLocation.VendorId.HasValue && CaseLocation.AssignedAgentUserEmail.Trim().ToLower() == vendorNonAdminUser.Email.Trim().ToLower())
                            {
                                if (CaseLocation.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                        CaseLocation.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId
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
                    .ThenInclude(i => i.CaseLocations)
                    .Where(d =>
                        (vendorUser.IsVendorAdmin ? true : d.UpdatedBy == userEmail) &&
                     d.ClaimsInvestigation.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));

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
                    .ThenInclude(i => i.CaseLocations)
                    .Where(d =>
                        (vendorUser.IsVendorAdmin ? true : d.UpdatedBy == userEmail) &&
                     d.ClaimsInvestigation.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId) &&
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
                    .ThenInclude(i => i.CaseLocations)
                    .Where(d =>
                     d.ClaimsInvestigation.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId) &&
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
             .ThenInclude(i => i.CaseLocations)
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
                    d.ClaimsInvestigation.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));

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