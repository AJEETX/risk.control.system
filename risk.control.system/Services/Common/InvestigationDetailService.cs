using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Common
{
    public interface IInvestigationDetailService
    {
        Task<CaseTransactionModel> GetCaseDetails(string currentUserEmail, long id);

        List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors);

        Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long id);

        Task<CaseTransactionModel> GetClaimPdfReport(string currentUserEmail, long id);
    }

    internal class InvestigationDetailService : IInvestigationDetailService
    {
        private readonly ApplicationDbContext context;

        public InvestigationDetailService(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<CaseTransactionModel> GetCaseDetails(string currentUserEmail, long id)
        {
            var caseTask = await context.Investigations
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.PolicyDetail)
                .Include(c => c.InvestigationTimeline)
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
            if (caseTask.CustomerDetail != null)
            {
                var maskedCustomerContact = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
                caseTask.CustomerDetail.PhoneNumber = maskedCustomerContact;
            }
            if (caseTask.BeneficiaryDetail != null)
            {
                var maskedBeneficiaryContact = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
                caseTask.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            }
            var companyUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.Now - caseTask.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";
            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Location = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser.ClientCompany.AutoAllocation,
                TimeTaken = totalTimeTaken,
                Withdrawable = (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }

        public async Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long id)
        {
            var caseTask = await context.Investigations
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .Include(c => c.InvestigationTimeline)
                .Include(c => c.InvestigationReport.EnquiryRequest)
                .Include(c => c.InvestigationReport.EnquiryRequests)
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

            if (caseTask.CustomerDetail != null)
            {
                var maskedCustomerContact = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
                caseTask.CustomerDetail.PhoneNumber = maskedCustomerContact;
            }
            if (caseTask.BeneficiaryDetail != null)
            {
                var maskedBeneficiaryContact = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
                caseTask.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            }
            var companyUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();
            var endTIme = caseTask.Status == CONSTANTS.CASE_STATUS.FINISHED ? caseTask.ProcessedByAssessorTime.GetValueOrDefault() : DateTime.Now;
            var timeTaken = endTIme - caseTask.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = await context.VendorInvoice.FirstOrDefaultAsync(i => i.InvestigationReportId == caseTask.InvestigationReportId);
            var templates = await context.ReportTemplates
               .Include(r => r.LocationReport)
                  .ThenInclude(l => l.AgentIdReport)
              .Include(r => r.LocationReport)
               .ThenInclude(l => l.MediaReports)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.FaceIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.DocumentIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.Questions)
                  .FirstOrDefaultAsync(q => q.Id == caseTask.ReportTemplateId);

            caseTask.InvestigationReport.ReportTemplate = templates;

            var tracker = await context.PdfDownloadTracker
                          .FirstOrDefaultAsync(t => t.ReportId == id && t.UserEmail == currentUserEmail);
            bool canDownload = true;
            if (tracker != null)
            {
                canDownload = tracker.DownloadCount <= 3;
            }

            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Location = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser != null ? companyUser.ClientCompany.AutoAllocation : false,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                CanDownload = canDownload,
                Withdrawable = (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }

        public async Task<CaseTransactionModel> GetClaimPdfReport(string currentUserEmail, long id)
        {
            var caseTask = await context.Investigations
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .Include(c => c.InvestigationTimeline)
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
            var maskedCustomerContact = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
            caseTask.CustomerDetail.PhoneNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
            caseTask.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            var companyUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.Now - caseTask.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = await context.VendorInvoice.FirstOrDefaultAsync(i => i.InvestigationReportId == caseTask.InvestigationReportId);
            var templates = await context.ReportTemplates
               .Include(r => r.LocationReport)
                  .ThenInclude(l => l.AgentIdReport)
              .Include(r => r.LocationReport)
               .ThenInclude(l => l.MediaReports)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.FaceIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.DocumentIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.Questions)
                  .FirstOrDefaultAsync(q => q.Id == caseTask.ReportTemplateId);

            caseTask.InvestigationReport.ReportTemplate = templates;

            var tracker = await context.PdfDownloadTracker
                          .FirstOrDefaultAsync(t => t.ReportId == id && t.UserEmail == currentUserEmail);
            bool canDownload = true;
            if (tracker != null)
            {
                canDownload = tracker.DownloadCount <= 3;
                tracker.DownloadCount++;
                tracker.LastDownloaded = DateTime.UtcNow;
                context.PdfDownloadTracker.Update(tracker);
            }
            else
            {
                tracker = new PdfDownloadTracker
                {
                    ReportId = id,
                    UserEmail = currentUserEmail,
                    DownloadCount = 1,
                    LastDownloaded = DateTime.UtcNow
                };
                context.PdfDownloadTracker.Add(tracker);
            }
            context.SaveChanges();
            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Location = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser != null ? companyUser.ClientCompany.AutoAllocation : false,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                CanDownload = canDownload,
                Withdrawable = (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }

        public List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors)
        {
            // Get relevant status IDs in one query
            var relevantStatuses = new[]
                {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
                }; // Improves lookup performance

            // Fetch cases that match the criteria
            var vendorCaseCount = context.Investigations
                .Where(c => !c.Deleted &&
                            c.VendorId.HasValue &&
                            c.AssignedToAgency &&
                            relevantStatuses.Contains(c.SubStatus))
                .GroupBy(c => c.VendorId.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // Create the list of VendorIdWithCases
            return existingVendors
                .Select(vendorId => new VendorIdWithCases
                {
                    VendorId = vendorId,
                    CaseCount = vendorCaseCount.GetValueOrDefault(vendorId, 0)
                })
                .ToList();
        }
    }
}