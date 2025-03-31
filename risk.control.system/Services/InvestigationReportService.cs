using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IInvestigationReportService
    {
        ClaimsInvestigationVendorsModel GetInvestigateReport(string currentUserEmail, string selectedcase);

        Task<ClaimTransactionModel> SubmittedDetail(string selectedcase, string currentUserEmail);

        Task<ClaimTransactionModel> GetClaimDetails(string currentUserEmail, string id);

        Task<ClaimsInvestigation> GetAssignDetails(string id);
        EnquiryRequest GetQueryReport(string currentUserEmail, string id);

        PreviousClaimReport GetPreviousReport(long id);
    }

    public class InvestigationReportService : IInvestigationReportService
    {
        private const string CLAIMS = "claims";
        private const string UNDERWRITING = "underwriting";
        private readonly ApplicationDbContext _context;

        public InvestigationReportService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task<ClaimTransactionModel> SubmittedDetail(string selectedcase, string currentUserEmail)
        {
            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.BeneficiaryDetail)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == selectedcase)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claim = await _context.ClaimsInvestigation
              .Include(c => c.ClientCompany)
              .Include(c => c.AgencyReport)
                    .ThenInclude(c => c.EnquiryRequest)
              .Include(c => c.PreviousClaimReports)
              .Include(c => c.AgencyReport.AgentIdReport)
              .Include(c => c.AgencyReport.DigitalIdReport)
              .Include(c => c.AgencyReport.PanIdReport)
              .Include(c => c.AgencyReport.PassportIdReport)
              .Include(c => c.AgencyReport.VideoReport)
              .Include(c => c.AgencyReport.AudioReport)
              .Include(c => c.AgencyReport.ReportQuestionaire)
              .Include(c => c.PolicyDetail)
              .Include(c => c.ClientCompany)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CaseEnabler)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CostCentre)
             .Include(c=>c.Vendor)
              .Include(c => c.BeneficiaryDetail)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.BeneficiaryDetail)
              .ThenInclude(c => c.BeneficiaryRelation)
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
              .Include(c => c.ClaimNotes)
              .Include(c => c.ClaimMessages)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);
            var isAgencyUser = _context.VendorApplicationUser.Any(u => u.Email == currentUserEmail);

            var location = await _context.BeneficiaryDetail
                .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == selectedcase);
            if (isAgencyUser)
            {
                var customerContactMasked = new string('*', claim.CustomerDetail.ContactNumber.Length - 4) + claim.CustomerDetail.ContactNumber.Substring(claim.CustomerDetail.ContactNumber.Length - 4);
                claim.CustomerDetail.ContactNumber = customerContactMasked;

                var beneficairyContactMasked = new string('*', claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4) + claim.BeneficiaryDetail.ContactNumber.ToString().Substring(claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4);

                claim.BeneficiaryDetail.ContactNumber = beneficairyContactMasked;

                location.ContactNumber = beneficairyContactMasked;
            }
            
            var invoice = _context.VendorInvoice.FirstOrDefault(i => i.AgencyReportId == claim.AgencyReport.AgencyReportId);
            var claimsLineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIMS).LineOfBusinessId;

            var isClaim = claim.PolicyDetail.LineOfBusinessId == claimsLineOfBusinessId;

            if (isClaim)
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Medical report question ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Detailed Diagnosis of death ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of Doctor met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with Doctor ?";
            }
            else
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
            }
            var model = new ClaimTransactionModel
            {
                ClaimsInvestigation = claim,
                Log = caseLogs,
                Location = location,
                VendorInvoice = invoice,
                TimeTaken = GetElapsedTime(caseLogs)
            };
            return model;
        }

        private string GetElapsedTime(List<InvestigationTransaction> caseLogs)
        {
            var orderedLogs = caseLogs.OrderBy(l => l.Created);

            var startTime = orderedLogs.FirstOrDefault();
            var completedTime = orderedLogs.LastOrDefault();
            var elaspedTime = completedTime.Created.Subtract(startTime.Created).Days;
            if (completedTime.Created.Subtract(startTime.Created).Days >= 1)
            {
                return elaspedTime + " day(s)";
            }
            if (completedTime.Created.Subtract(startTime.Created).TotalHours < 24 && completedTime.Created.Subtract(startTime.Created).TotalHours >= 1)
            {
                return completedTime.Created.Subtract(startTime.Created).Hours + " hour(s)";
            }
            if (completedTime.Created.Subtract(startTime.Created).Minutes < 60 && completedTime.Created.Subtract(startTime.Created).Minutes >= 1)
            {
                return completedTime.Created.Subtract(startTime.Created).Minutes + " min(s)";
            }
            return completedTime.Created.Subtract(startTime.Created).Seconds + " sec";
        }

        public async Task<ClaimsInvestigation> GetAssignDetails(string id)
        {
            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.AgencyReport)
                    .ThenInclude(c => c.EnquiryRequest)
              .Include(c => c.PreviousClaimReports)
              .Include(c => c.AgencyReport.DigitalIdReport)
              .Include(c => c.AgencyReport.PanIdReport)
              .Include(c => c.AgencyReport.PassportIdReport)
              .Include(c => c.AgencyReport.ReportQuestionaire)
                .Include(c => c.PolicyDetail)
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c=> c.Vendor)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
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
                .Include(c => c.ClaimNotes)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);
            return claimsInvestigation;
        }

        public async Task<ClaimTransactionModel> GetClaimDetails(string currentUserEmail, string id)
        {
            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.BeneficiaryDetail)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claim = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.ClientCompany)
                .Include(c => c.AgencyReport)
                    .ThenInclude(c => c.EnquiryRequest)
                  .Include(c => c.PreviousClaimReports)
                  .Include(c => c.AgencyReport.DigitalIdReport)
                  .Include(c => c.AgencyReport.PanIdReport)
                  .Include(c => c.AgencyReport.PassportIdReport)
                  .Include(c => c.AgencyReport.AudioReport)
                  .Include(c => c.AgencyReport.VideoReport)
                  .Include(c => c.AgencyReport.ReportQuestionaire)
                .Include(c=> c.Vendor)
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
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c=>c.ClaimNotes)
                .Include(c=>c.ClaimMessages)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = claim.BeneficiaryDetail;
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var companyUser = _context.ClientCompanyApplicationUser.Include(u=>u.ClientCompany).FirstOrDefault(u=>u.Email == currentUserEmail);
            
            var claimsLineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIMS).LineOfBusinessId;

            var isClaim = claim.PolicyDetail.LineOfBusinessId == claimsLineOfBusinessId;

            if (isClaim)
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Medical report question ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Detailed Diagnosis of death ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of Doctor met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with Doctor ?";
            }
            else
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
            }
            var model = new ClaimTransactionModel
            {
                ClaimsInvestigation = claim,
                Log = caseLogs,
                Location = location,
                Assigned = claim.InvestigationCaseSubStatus == assignedStatus,
                AutoAllocation = companyUser.ClientCompany.AutoAllocation
            };

            return model;
        }

        public ClaimsInvestigationVendorsModel GetInvestigateReport(string currentUserEmail, string selectedcase)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.PreviousClaimReports)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.EnquiryRequest)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.EnquiryRequests)
                .Include(c => c.AgencyReport.AgentIdReport)
                .Include(c => c.AgencyReport.DigitalIdReport)
                .Include(c => c.AgencyReport.PanIdReport)
                .Include(c => c.AgencyReport.PassportIdReport)
                .Include(c => c.AgencyReport.AudioReport)
                .Include(c => c.AgencyReport.VideoReport)
                .Include(c => c.AgencyReport.ReportQuestionaire)
                .Include(c => c.ClaimMessages)
                .Include(c => c.PolicyDetail)
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c=> c.Vendor)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
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
                .Include(c => c.ClaimNotes)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var claimCase = _context.BeneficiaryDetail
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            var companyUser = _context.ClientCompanyApplicationUser.Include(u=>u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);

            if (claim.IsReviewCase)
            {
                claim.AgencyReport.AssessorRemarks = null;
            }
            ClaimsInvestigationVendorsModel model = null;
            var claimsLineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIMS).LineOfBusinessId;

            var isClaim = claim.PolicyDetail.LineOfBusinessId == claimsLineOfBusinessId;

            if (claim.AgencyReport == null)
            {
                claim.AgencyReport = new AgencyReport();
            }

            if (isClaim)
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Medical report question ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Detailed Diagnosis of death ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of Doctor met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with Doctor ?";
            }
            else
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
            }
            return (new ClaimsInvestigationVendorsModel { AgencyReport = claim.AgencyReport, Location = claimCase, ClaimsInvestigation = claim, 
                TrialVersion = companyUser?.ClientCompany?.LicenseType == Standard.Licensing.LicenseType.Trial });
        }

        public PreviousClaimReport GetPreviousReport(long id)
        {
            var report = _context.PreviousClaimReport
                .Include(r=>r.Vendor)
                .Include(r=>r.ClaimsInvestigation)
                .Include(r=>r.DigitalIdReport)
                .Include(r=>r.PanIdReport)
                .Include(r=>r.AudioReport)
                .Include(r=>r.VideoReport)
                .Include(r=>r.PassportIdReport)
                .Include(r=>r.ReportQuestionaire)
                .FirstOrDefault(r => r.PreviousClaimReportId == id);
            return report;
        }

        public EnquiryRequest GetQueryReport(string currentUserEmail, string id)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c=>c.AgencyReport)
                .ThenInclude(a=>a.EnquiryRequest)
                .FirstOrDefault(c=>c.ClaimsInvestigationId == id);
            var request = new EnquiryRequest();
            claim.AgencyReport.EnquiryRequest = request;
            _context.ClaimsInvestigation.Update(claim);
            _context.SaveChanges();
            return request;
        }
    }
}