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

        Task Create(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument);

        Task AssignToAssigner(string userEmail, List<string> claimsInvestigations);

        Task AllocateToVendor(string userEmail, string claimsInvestigationId, string vendorId, long caseLocationId);

        Task AssignToVendorAgent(string userEmail, string vendorId, string claimsInvestigationId);

        Task SubmitToVendorSupervisor(string userEmail, long caseLocationId, string claimsInvestigationId, string remarks);

        Task SubmitToCompany(string userEmail, string supervisorRemarks, long caseLocationId, string claimsInvestigationId, string remarks);

        Task ProcessCaseReport(string userEmail, string supervisorRemarks, long caseLocationId, string claimsInvestigationId, string remarks);
    }

    public class ClaimsInvestigationService : IClaimsInvestigationService
    {
        private readonly ApplicationDbContext _context;

        public ClaimsInvestigationService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task AllocateToVendor(string userEmail, string claimsInvestigationId, string vendorId, long caseLocationId)
        {
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorId);

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
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId);

                claimsCaseLocation.Vendor = vendor;
                claimsCaseLocation.VendorId = vendorId;
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

                await _context.SaveChangesAsync();
            }
        }

        public async Task AssignToAssigner(string userEmail, List<string> claims)
        {
            if (claims is not null && claims.Count > 0)
            {
                var cases2Assign = _context.ClaimsInvestigation.Include(c => c.CaseLocations).Where(v => claims.Contains(v.ClaimsInvestigationId));
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
                //claim.CaseLocations = claim.CaseLocations.Where(
                //    c => c.VendorId == vendorId
                //    && c.InvestigationCaseSubStatusId == _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).InvestigationCaseSubStatusId)?.ToList();
            }
            _context.ClaimsInvestigation.Update(claim);
            await _context.SaveChangesAsync();
        }

        public async Task Create(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument)
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

        public async Task ProcessCaseReport(string userEmail, string assessorRemarks, long caseLocationId, string claimsInvestigationId, string assessorRemarkType)
        {
            var caseLocation = _context.CaseLocation
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId);

            var report = _context.ClaimReport.FirstOrDefault(c => c.ClaimReportId == caseLocation.ClaimReport.ClaimReportId);
            report.AssessorRemarkType = Enum.Parse<AssessorRemarkType>(assessorRemarkType);
            report.AssessorRemarks = assessorRemarks;

            _context.ClaimReport.Update(report);
            caseLocation.ClaimReport = report;
            caseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR).InvestigationCaseSubStatusId;
            caseLocation.Updated = DateTime.UtcNow;
            caseLocation.UpdatedBy = userEmail;
            caseLocation.AssignedAgentUserEmail = userEmail;
            _context.CaseLocation.Update(caseLocation);
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
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task SubmitToCompany(string userEmail, string supervisorRemarks, long caseLocationId, string claimsInvestigationId, string supervisorRemarkType)
        {
            var claim = _context.ClaimsInvestigation
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            var caseLocation = _context.CaseLocation
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId);

            var report = _context.ClaimReport.FirstOrDefault(c => c.ClaimReportId == caseLocation.ClaimReport.ClaimReportId);
            report.SupervisorRemarkType = Enum.Parse<SupervisorRemarkType>(supervisorRemarkType);
            report.SupervisorRemarks = supervisorRemarks;

            _context.ClaimReport.Update(report);
            caseLocation.ClaimReport = report;
            caseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR).InvestigationCaseSubStatusId;
            caseLocation.Updated = DateTime.UtcNow;
            caseLocation.UpdatedBy = userEmail;
            caseLocation.AssignedAgentUserEmail = userEmail;
            _context.CaseLocation.Update(caseLocation);
            try
            {
                await _context.SaveChangesAsync();
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
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId);

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
            _context.CaseLocation.Update(caseLocation);
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