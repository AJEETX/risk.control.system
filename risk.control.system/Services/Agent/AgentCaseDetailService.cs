using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Agent
{
    public interface IAgentCaseDetailService
    {
        Task<CaseAgencyModel> GetInvestigate(string userEmail, long selectedcase, bool uploaded = false);

        Task<CaseTransactionModel> GetInvestigatedForAgent(string currentUserEmail, long id);

        Task<InvestigationTask> GetCaseById(long id);

        Task<InvestigationTask> GetCaseByIdForMedia(long id);

        Task<InvestigationTask> GetCaseByIdForQuestions(long id);

        Task<InvestigationTask> GetNotesOfCase(long id);

        Task<InvestigationTask> GetCaseDetailForAgentDetail(long id);
    }

    internal class AgentCaseDetailService : IAgentCaseDetailService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AgentCaseDetailService> _logger;

        public AgentCaseDetailService(ApplicationDbContext context, ILogger<AgentCaseDetailService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CaseAgencyModel> GetInvestigate(string userEmail, long selectedCaseId, bool uploaded = false)
        {
            _logger.LogInformation("Fetching investigation case {CaseId} for user {UserEmail}", selectedCaseId, userEmail);

            var caseTask = await _context.Investigations
                .Include(c => c.ClientCompany)
                    .ThenInclude(c => c.Country)
                .Include(c => c.PolicyDetail)
                    .ThenInclude(p => p.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                    .ThenInclude(p => p.CostCentre)
                .Include(c => c.PolicyDetail)
                    .ThenInclude(p => p.CaseEnabler)
                .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(b => b.PinCode)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(b => b.District)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(b => b.State)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(b => b.Country)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(b => b.BeneficiaryRelation)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .FirstOrDefaultAsync(c => c.Id == selectedCaseId);

            if (caseTask == null)
            {
                _logger.LogWarning("Investigation case {CaseId} not found", selectedCaseId);
                return null;
            }

            // Mask sensitive phone numbers
            caseTask.CustomerDetail.PhoneNumber = MaskPhoneNumber(caseTask.CustomerDetail?.PhoneNumber);
            caseTask.BeneficiaryDetail.PhoneNumber = MaskPhoneNumber(caseTask.BeneficiaryDetail?.PhoneNumber);
            _logger.LogInformation("Masked phone numbers for case {CaseId}", selectedCaseId);

            // Assign agent email
            caseTask.InvestigationReport.AgentEmail = userEmail;

            // Load report templates
            var templates = await _context.ReportTemplates.AsNoTracking()
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.AgentIdReport)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.FaceIds)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.MediaReports)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.DocumentIds)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.Questions)
                .FirstOrDefaultAsync(r => r.Id == caseTask.ReportTemplateId);

            if (templates == null)
            {
                _logger.LogWarning("Report template {TemplateId} not found for case {CaseId}", caseTask.ReportTemplateId, selectedCaseId);
            }
            else
            {
                caseTask.InvestigationReport.ReportTemplate = templates;
                _logger.LogInformation("Loaded report template {TemplateId} for case {CaseId}", templates.Id, selectedCaseId);
            }

            _context.Investigations.Update(caseTask);
            var rowsAffected = await _context.SaveChangesAsync();
            _logger.LogInformation("{RowsAffected} rows updated for case {CaseId}", rowsAffected, selectedCaseId);

            var model = new CaseAgencyModel
            {
                InvestigationReport = caseTask.InvestigationReport,
                Beneficiary = caseTask.BeneficiaryDetail,
                CaseTask = caseTask,
                Currency = CustomExtensions.GetCultureByCountry(caseTask.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol
            };

            _logger.LogInformation("Returning investigation model for case {CaseId}", selectedCaseId);
            return model;
        }

        public async Task<CaseTransactionModel> GetInvestigatedForAgent(string currentUserEmail, long id)
        {
            _logger.LogInformation("Fetching investigation case {CaseId} for agent {UserEmail}", id, currentUserEmail);

            // Fetch case with related entities
            var caseTask = await _context.Investigations.AsNoTracking()
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .Include(c => c.InvestigationTimeline)
                .Include(c => c.PolicyDetail)
                    .ThenInclude(p => p.CaseEnabler)
                .Include(c => c.PolicyDetail)
                    .ThenInclude(p => p.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                    .ThenInclude(p => p.CostCentre)
                .Include(c => c.ClientCompany)
                .Include(c => c.Vendor)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(b => b.PinCode)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(b => b.District)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(b => b.State)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(b => b.Country)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(b => b.BeneficiaryRelation)
                .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (caseTask == null)
            {
                _logger.LogWarning("Investigation case {CaseId} not found for agent {UserEmail}", id, currentUserEmail);
                return null;
            }

            _logger.LogInformation("Investigation case {CaseId} found. Fetching related data...", id);

            // Fetch company
            var company = await _context.ClientCompany.AsNoTracking().Include(c => c.Country).FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);
            if (company == null)
            {
                _logger.LogWarning("ClientCompany {CompanyId} not found for case {CaseId}", caseTask.ClientCompanyId, id);
            }
            // Fetch report templates
            var templates = await _context.ReportTemplates.AsNoTracking()
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

            if (templates != null)
            {
                caseTask.InvestigationReport.ReportTemplate = templates;
                _logger.LogInformation("Loaded report template {TemplateId} for case {CaseId}", templates.Id, id);
            }
            else
            {
                _logger.LogWarning("Report template {TemplateId} not found for case {CaseId}", caseTask.ReportTemplateId, id);
            }

            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Beneficiary = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                Withdrawable = caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                Currency = CustomExtensions.GetCultureByCountry(company.Country.Code.ToUpper()).NumberFormat.CurrencySymbol
            };

            _logger.LogInformation("Returning CaseTransactionModel for case {CaseId}", id);
            return model;
        }

        public async Task<InvestigationTask> GetCaseById(long id)
        {
            var _case = await _context.Investigations
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationReport)
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CaseNotes)
                .FirstOrDefaultAsync(c => c.Id == id);
            return _case;
        }

        public async Task<InvestigationTask> GetCaseByIdForMedia(long id)
        {
            var claim = await _context.Investigations
                 .Include(c => c.PolicyDetail)
                 .Include(c => c.CustomerDetail)
                 .Include(c => c.BeneficiaryDetail)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationReport)
                .FirstOrDefaultAsync(c => c.Id == id);
            return claim;
        }

        public async Task<InvestigationTask> GetCaseByIdForQuestions(long id)
        {
            var claim = await _context.Investigations
                    .Include(c => c.InvestigationReport)
                    .ThenInclude(c => c.ReportTemplate)
                    .ThenInclude(c => c.LocationReport)
                    .FirstOrDefaultAsync(c => c.Id == id);
            return claim;
        }

        public async Task<InvestigationTask> GetCaseDetailForAgentDetail(long id)
        {
            var caseDetail = await _context.Investigations.AsNoTracking()
               .Include(c => c.PolicyDetail)
               .Include(c => c.BeneficiaryDetail)
                .Include(c => c.CustomerDetail)
                .Where(c => !c.Deleted)
                .FirstOrDefaultAsync(c => c.Id == id);
            return caseDetail;
        }

        public async Task<InvestigationTask> GetNotesOfCase(long id)
        {
            var caseInvestigation = await _context.Investigations.AsNoTracking()
               .Include(c => c.CaseNotes)
                .Where(c => !c.Deleted)
                .FirstOrDefaultAsync(c => c.Id == id);
            return caseInvestigation;
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time == TimeSpan.Zero) return "-";

            return $"{(time.Days > 0 ? $"{time.Days}d " : "")}" +
                   $"{(time.Hours > 0 ? $"{time.Hours}h " : "")}" +
                   $"{(time.Minutes > 0 ? $"{time.Minutes}m " : "")}" +
                   $"{(time.Seconds > 0 ? $"{time.Seconds}s" : "less than a sec")}";
        }

        // Helper to check PDF download eligibility
        private async Task<bool> CanDownloadPdf(string userEmail, long reportId)
        {
            var tracker = await _context.PdfDownloadTracker.AsNoTracking()
                .FirstOrDefaultAsync(t => t.ReportId == reportId && t.UserEmail == userEmail);

            return tracker == null || tracker.DownloadCount <= 3;
        }

        private static string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length <= 4)
                return phoneNumber;

            return new string('*', phoneNumber.Length - 4) + phoneNumber[^4..];
        }
    }
}