using System.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Linq;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IClaimsInvestigationService
    {
        List<ClaimsInvestigation> GetAll();

        Task Create(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument, IFormFile? customerDocument);

        Task AssignToAssigner(string userEmail, List<string> claimsInvestigations);

        Task AllocateToVendor(string userEmail, string claimsInvestigationId, string vendorId, long caseLocationId);

        Task AssignToVendorAgent(string userEmail, string vendorId, string claimsInvestigationId);

        Task SubmitToVendorSupervisor(string userEmail, long caseLocationId, string claimsInvestigationId, string remarks);

        Task<bool> Process(string userEmail, string supervisorRemarks, long caseLocationId, string claimsInvestigationId, SupervisorRemarkType remarks);

        Task<bool> ProcessCaseReport(string userEmail, string assessorRemarks, long caseLocationId, string claimsInvestigationId, AssessorRemarkType assessorRemarkType);
    }

    public class ClaimsInvestigationService : IClaimsInvestigationService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;

        public ClaimsInvestigationService(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            this._context = context;
            this.roleManager = roleManager;
            this.userManager = userManager;
        }

        private async Task<VendorApplicationUser> GetSupervisor(string vendorId)
        {
            var vendorNonAdminUsers = _context.VendorApplicationUser.Where(u =>
            u.VendorId == vendorId && !u.IsVendorAdmin);

            var supervisor = roleManager.Roles.FirstOrDefault(r =>
                r.Name.Contains(AppRoles.Supervisor.ToString()));

            foreach (var vendorNonAdminUser in vendorNonAdminUsers)
            {
                if (await userManager.IsInRoleAsync(vendorNonAdminUser, supervisor?.Name))
                {
                    return vendorNonAdminUser;
                }
            }
            return null;
        }

        public async Task AllocateToVendor(string userEmail, string claimsInvestigationId, string vendorId, long caseLocationId)
        {
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorId);

            var supervisor = await GetSupervisor(vendorId);

            if (vendor != null)
            {
                var claimsCaseLocation = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.Vendor)
                .Include(c => c.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.State)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

                claimsCaseLocation.Vendor = vendor;
                claimsCaseLocation.VendorId = vendorId;
                claimsCaseLocation.AssignedAgentUserEmail = supervisor.Email;
                claimsCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR).InvestigationCaseSubStatusId;
                _context.CaseLocation.Update(claimsCaseLocation);

                var claimsCaseToAllocateToVendor = _context.ClaimsInvestigation.FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);
                claimsCaseToAllocateToVendor.Updated = DateTime.UtcNow;
                claimsCaseToAllocateToVendor.UpdatedBy = userEmail;
                claimsCaseToAllocateToVendor.CurrentUserEmail = userEmail;
                claimsCaseToAllocateToVendor.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR).InvestigationCaseSubStatusId;
                var existinCaseLocation = claimsCaseToAllocateToVendor.CaseLocations.FirstOrDefault(c => c.CaseLocationId == caseLocationId);
                existinCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR).InvestigationCaseSubStatusId;
                claimsCaseToAllocateToVendor.Vendors.Add(vendor);
                _context.ClaimsInvestigation.Update(claimsCaseToAllocateToVendor);
                var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claimsCaseToAllocateToVendor.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

                var log = new InvestigationTransaction
                {
                    ClaimsInvestigationId = claimsCaseToAllocateToVendor.ClaimsInvestigationId,
                    Created = DateTime.UtcNow,
                    Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                    InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR).InvestigationCaseSubStatusId,
                    UpdatedBy = userEmail
                };
                _context.InvestigationTransaction.Add(log);

                await _context.SaveChangesAsync();
            }
        }

        public async Task AssignToAssigner(string userEmail, List<string> claims)
        {
            if (claims is not null && claims.Count > 0)
            {
                var cases2Assign = _context.ClaimsInvestigation
                    .Include(c => c.CaseLocations)
                    .Where(v => claims.Contains(v.ClaimsInvestigationId));
                foreach (var claimsInvestigation in cases2Assign)
                {
                    claimsInvestigation.Updated = DateTime.UtcNow;
                    claimsInvestigation.UpdatedBy = userEmail;
                    claimsInvestigation.CurrentUserEmail = userEmail;
                    claimsInvestigation.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
                    claimsInvestigation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId;
                    foreach (var caseLocation in claimsInvestigation.CaseLocations)
                    {
                        caseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId;
                    }

                    var lastLog = _context.InvestigationTransaction.Where(i =>
                    i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

                    var log = new InvestigationTransaction
                    {
                        ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                        Created = DateTime.UtcNow,
                        Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                        InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                        InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId,
                        UpdatedBy = userEmail
                    };
                    _context.InvestigationTransaction.Add(log);
                }
                _context.UpdateRange(cases2Assign);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AssignToVendorAgent(string userEmail, string vendorId, string claimsInvestigationId)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Where(c => c.ClaimsInvestigationId == claimsInvestigationId).FirstOrDefault();
            if (claim != null)
            {
                var claimsCaseLocation = _context.CaseLocation
                    .Include(c => c.ClaimsInvestigation)
                    .Include(c => c.InvestigationCaseSubStatus)
                    .Include(c => c.Vendor)
                    .Include(c => c.PinCode)
                    .Include(c => c.District)
                    .Include(c => c.State)
                    .Include(c => c.State)
                    .FirstOrDefault(c => c.VendorId == vendorId && c.ClaimsInvestigationId == claimsInvestigationId);
                claimsCaseLocation.AssignedAgentUserEmail = userEmail;
                claimsCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).InvestigationCaseSubStatusId;
                _context.CaseLocation.Update(claimsCaseLocation);

                claim.Updated = DateTime.UtcNow;
                claim.UpdatedBy = userEmail;
                claim.CurrentUserEmail = userEmail;
                claim.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
                claim.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).InvestigationCaseSubStatusId;

                var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claim.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

                var log = new InvestigationTransaction
                {
                    ClaimsInvestigationId = claim.ClaimsInvestigationId,
                    Created = DateTime.UtcNow,
                    Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                    InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).InvestigationCaseSubStatusId,
                    UpdatedBy = userEmail
                };
                _context.InvestigationTransaction.Add(log);
            }
            _context.ClaimsInvestigation.Update(claim);
            await _context.SaveChangesAsync();
        }

        public async Task Create(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument, IFormFile? customerDocument)
        {
            if (claimsInvestigation is not null)
            {
                try
                {
                    claimsInvestigation.Updated = DateTime.UtcNow;
                    claimsInvestigation.UpdatedBy = userEmail;
                    claimsInvestigation.CurrentUserEmail = userEmail;
                    claimsInvestigation.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId;
                    claimsInvestigation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId;
                    if (customerDocument is not null)
                    {
                        var messageDocumentFileName = Path.GetFileNameWithoutExtension(customerDocument.FileName);
                        var extension = Path.GetExtension(customerDocument.FileName);
                        claimsInvestigation.ProfileImage = customerDocument;
                        using var dataStream = new MemoryStream();
                        await claimsInvestigation.ProfileImage.CopyToAsync(dataStream);
                        claimsInvestigation.ProfilePicture = dataStream.ToArray();
                    }

                    if (claimDocument is not null)
                    {
                        var messageDocumentFileName = Path.GetFileNameWithoutExtension(claimDocument.FileName);
                        var extension = Path.GetExtension(claimDocument.FileName);
                        claimsInvestigation.Document = claimDocument;
                        using var dataStream = new MemoryStream();
                        await claimsInvestigation.Document.CopyToAsync(dataStream);
                        claimsInvestigation.DocumentImage = dataStream.ToArray();
                    }

                    _context.ClaimsInvestigation.Add(claimsInvestigation);
                    var log = new InvestigationTransaction
                    {
                        ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                        Created = DateTime.UtcNow,
                        Time2Update = 0,
                        InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId,
                        InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId,
                        UpdatedBy = userEmail
                    };

                    _context.InvestigationTransaction.Add(log);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public List<ClaimsInvestigation> GetAll()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ProcessCaseReport(string userEmail, string assessorRemarks, long caseLocationId, string claimsInvestigationId, AssessorRemarkType reportUpdateStatus)
        {
            var claim = _context.ClaimsInvestigation
                 .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            if (reportUpdateStatus == AssessorRemarkType.OK)
            {
                return await ApproveCaseReport(userEmail, assessorRemarks, caseLocationId, claimsInvestigationId, reportUpdateStatus);
            }
            else
            {
                //PUT th case back in review list :: Assign back to Agent
                await ReAssignToAssigner(userEmail, claimsInvestigationId, caseLocationId, assessorRemarks, reportUpdateStatus);

                return false;
            }
        }

        private async Task ReAssignToAssigner(string userEmail, string claimsInvestigationId, long caseLocationId, string assessorRemarks, AssessorRemarkType assessorRemarkType)
        {
            var claimsCaseLocation = _context.CaseLocation
                .Include(c => c.ClaimReport)
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.Vendor)
                .Include(c => c.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.State)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            var report = _context.ClaimReport.FirstOrDefault(c => c.ClaimReportId == claimsCaseLocation.ClaimReport.ClaimReportId);
            report.AssessorRemarkType = assessorRemarkType;
            report.AssessorRemarks = assessorRemarks;

            _context.ClaimReport.Update(report);
            claimsCaseLocation.ClaimReport = report;
            claimsCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId;
            claimsCaseLocation.IsReviewCaseLocation = true;
            _context.CaseLocation.Update(claimsCaseLocation);

            var claimsCaseToReassign = _context.ClaimsInvestigation.FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);
            claimsCaseToReassign.Updated = DateTime.UtcNow;
            claimsCaseToReassign.UpdatedBy = userEmail;
            claimsCaseToReassign.CurrentUserEmail = userEmail;
            claimsCaseToReassign.IsReviewCase = true;
            claimsCaseToReassign.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId;

            _context.ClaimsInvestigation.Update(claimsCaseToReassign);
            var lastLog = _context.InvestigationTransaction.Where(i =>
                            i.ClaimsInvestigationId == claimsCaseToReassign.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var log = new InvestigationTransaction
            {
                ClaimsInvestigationId = claimsCaseToReassign.ClaimsInvestigationId,
                Created = DateTime.UtcNow,
                Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId,
                UpdatedBy = userEmail
            };
            _context.InvestigationTransaction.Add(log);

            await _context.SaveChangesAsync();
        }

        private async Task<bool> ApproveCaseReport(string userEmail, string assessorRemarks, long caseLocationId, string claimsInvestigationId, AssessorRemarkType assessorRemarkType)
        {
            var caseLocation = _context.CaseLocation
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            var report = _context.ClaimReport.FirstOrDefault(c => c.ClaimReportId == caseLocation.ClaimReport.ClaimReportId);
            report.AssessorRemarkType = assessorRemarkType;
            report.AssessorRemarks = assessorRemarks;

            _context.ClaimReport.Update(report);
            caseLocation.ClaimReport = report;
            caseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR).InvestigationCaseSubStatusId;
            caseLocation.Updated = DateTime.UtcNow;
            caseLocation.UpdatedBy = userEmail;
            caseLocation.AssignedAgentUserEmail = userEmail;
            _context.CaseLocation.Update(caseLocation);

            var lastLog = _context.InvestigationTransaction.Where(i =>
                 i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var log = new InvestigationTransaction
            {
                ClaimsInvestigationId = claimsInvestigationId,
                Created = DateTime.UtcNow,
                Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR).InvestigationCaseSubStatusId,
                UpdatedBy = userEmail
            };
            _context.InvestigationTransaction.Add(log);

            try
            {
                await _context.SaveChangesAsync();
                var claim = _context.ClaimsInvestigation
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

                if (claim != null && claim.CaseLocations.All(c => c.InvestigationCaseSubStatusId == _context.InvestigationCaseSubStatus
                    .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR).InvestigationCaseSubStatusId))
                {
                    claim.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED).InvestigationCaseStatusId;
                    _context.ClaimsInvestigation.Update(claim);

                    var finalLog = new InvestigationTransaction
                    {
                        ClaimsInvestigationId = claimsInvestigationId,
                        Created = DateTime.UtcNow,
                        Time2Update = DateTime.UtcNow.Subtract(claim.Created).Days,
                        InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED).InvestigationCaseStatusId,
                        InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR).InvestigationCaseSubStatusId,
                        UpdatedBy = userEmail
                    };

                    _context.InvestigationTransaction.Add(finalLog);

                    return await _context.SaveChangesAsync() > 0 ? true : false;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return false;
        }

        public async Task<bool> Process(string userEmail, string supervisorRemarks, long caseLocationId, string claimsInvestigationId, SupervisorRemarkType reportUpdateStatus)
        {
            if (reportUpdateStatus == SupervisorRemarkType.OK)
            {
                return await ApproveAgentReport(userEmail, claimsInvestigationId, caseLocationId, supervisorRemarks, reportUpdateStatus);
            }
            else
            {
                //PUT th case back in review list :: Assign back to Agent
                await ReAllocateToVendor(userEmail, claimsInvestigationId, caseLocationId, supervisorRemarks, reportUpdateStatus);

                return false;
            }
        }

        private async Task ReAllocateToVendor(string userEmail, string claimsInvestigationId, long caseLocationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus)
        {
            var claimsCaseLocation = _context.CaseLocation
            .Include(c => c.ClaimReport)
            .Include(c => c.ClaimsInvestigation)
            .Include(c => c.InvestigationCaseSubStatus)
            .Include(c => c.Vendor)
            .Include(c => c.PinCode)
            .Include(c => c.District)
            .Include(c => c.State)
            .Include(c => c.State)
            .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            var report = _context.ClaimReport.FirstOrDefault(c => c.ClaimReportId == claimsCaseLocation.ClaimReport.ClaimReportId);
            report.SupervisorRemarkType = reportUpdateStatus;
            report.SupervisorRemarks = supervisorRemarks;

            _context.ClaimReport.Update(report);
            claimsCaseLocation.ClaimReport = report;
            claimsCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR).InvestigationCaseSubStatusId;
            claimsCaseLocation.IsReviewCaseLocation = true;
            _context.CaseLocation.Update(claimsCaseLocation);

            var claimsCaseToAllocateToVendor = _context.ClaimsInvestigation.FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);
            claimsCaseToAllocateToVendor.Updated = DateTime.UtcNow;
            claimsCaseToAllocateToVendor.UpdatedBy = userEmail;
            claimsCaseToAllocateToVendor.CurrentUserEmail = userEmail;
            claimsCaseToAllocateToVendor.IsReviewCase = true;
            claimsCaseToAllocateToVendor.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR).InvestigationCaseSubStatusId;
            var existinCaseLocation = claimsCaseToAllocateToVendor.CaseLocations.FirstOrDefault(c => c.CaseLocationId == caseLocationId);
            existinCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR).InvestigationCaseSubStatusId;
            _context.ClaimsInvestigation.Update(claimsCaseToAllocateToVendor);

            var lastLog = _context.InvestigationTransaction.Where(i =>
                 i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var log = new InvestigationTransaction
            {
                ClaimsInvestigationId = claimsInvestigationId,
                Created = DateTime.UtcNow,
                Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR).InvestigationCaseSubStatusId,
                UpdatedBy = userEmail
            };
            _context.InvestigationTransaction.Add(log);

            await _context.SaveChangesAsync();
        }

        private async Task<bool> ApproveAgentReport(string userEmail, string claimsInvestigationId, long caseLocationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus)
        {
            var caseLocation = _context.CaseLocation
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);
            var report = _context.ClaimReport.FirstOrDefault(c => c.ClaimReportId == caseLocation.ClaimReport.ClaimReportId);
            report.SupervisorRemarkType = reportUpdateStatus;
            report.SupervisorRemarks = supervisorRemarks;

            _context.ClaimReport.Update(report);
            caseLocation.ClaimReport = report;
            caseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR).InvestigationCaseSubStatusId;
            caseLocation.Updated = DateTime.UtcNow;
            caseLocation.UpdatedBy = userEmail;
            caseLocation.AssignedAgentUserEmail = userEmail;
            _context.CaseLocation.Update(caseLocation);

            var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var log = new InvestigationTransaction
            {
                ClaimsInvestigationId = claimsInvestigationId,
                Created = DateTime.UtcNow,
                Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR).InvestigationCaseSubStatusId,
                UpdatedBy = userEmail
            };
            _context.InvestigationTransaction.Add(log);

            try
            {
                return await _context.SaveChangesAsync() > 0 ? true : false;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task SubmitToVendorSupervisor(string userEmail, long caseLocationId, string claimsInvestigationId, string remarks)
        {
            var claim = _context.ClaimsInvestigation
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            var caseLocation = _context.CaseLocation
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            var report = new ClaimReport
            {
                AgentRemarks = remarks,
                CaseLocationId = caseLocationId,
            };

            _context.ClaimReport.Add(report);
            caseLocation.ClaimReport = report;
            caseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR).InvestigationCaseSubStatusId;
            caseLocation.Updated = DateTime.UtcNow;
            caseLocation.UpdatedBy = userEmail;
            caseLocation.AssignedAgentUserEmail = userEmail;
            caseLocation.IsReviewCaseLocation = false;
            _context.CaseLocation.Update(caseLocation);

            var lastLog = _context.InvestigationTransaction.Where(i =>
               i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var log = new InvestigationTransaction
            {
                ClaimsInvestigationId = claimsInvestigationId,
                Created = DateTime.UtcNow,
                Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR).InvestigationCaseSubStatusId,
                UpdatedBy = userEmail
            };
            _context.InvestigationTransaction.Add(log);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}