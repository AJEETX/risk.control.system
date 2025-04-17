using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IClaimPolicyService
    {
        ClaimsInvestigation AddClaimPolicy(string userEmail, long? lineOfBusinessId);

        Task<ClaimTransactionModel> GetClaimDetail(string id);
        Task<ClaimTransactionModel> GetClaimSummary(string userEmail, string id);
    }

    public class ClaimPolicyService : IClaimPolicyService
    {
        private const string CLAIMS = "claims";
        private const string UNDERWRITING = "underwriting";
        private readonly ApplicationDbContext _context;
        private readonly INumberSequenceService numberService;

        public ClaimPolicyService(ApplicationDbContext context, INumberSequenceService numberService)
        {
            this._context = context;
            this.numberService = numberService;
        }

        public ClaimsInvestigation AddClaimPolicy(string userEmail, long? lineOfBusinessId)
        {
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var contractNumber = numberService.GetNumberSequence("PX");
            var model = new ClaimsInvestigation
            {
                PolicyDetail = new PolicyDetail
                {
                    CaseEnablerId = _context.CaseEnabler.FirstOrDefault().CaseEnablerId,
                    CauseOfLoss = "LOST IN ACCIDENT",
                    ClaimType = ClaimType.DEATH,
                    ContractIssueDate = DateTime.Now.AddDays(-10),
                    CostCentreId = _context.CostCentre.FirstOrDefault().CostCentreId,
                    DateOfIncident = DateTime.Now.AddDays(-3),
                    InvestigationServiceTypeId = _context.InvestigationServiceType.FirstOrDefault(i=>i.LineOfBusinessId == lineOfBusinessId).InvestigationServiceTypeId,
                    Comments = "SOMETHING FISHY",
                    SumAssuredValue = new Random().Next(10000, 99999),
                    ContractNumber = contractNumber
                },
                InvestigationCaseSubStatusId = createdStatus.InvestigationCaseSubStatusId,
                UserEmailActioned = userEmail,
                UserEmailActionedTo = userEmail,
            };
            return model;
        }

        public async Task<ClaimTransactionModel> GetClaimDetail(string id)
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
                .Include(c => c.ClientCompany)
                .Include(c => c.AgencyReport)
                    .ThenInclude(c => c.EnquiryRequest)
              .Include(c => c.AgencyReport.AgentIdReport)
              .Include(c => c.AgencyReport.DigitalIdReport)
              .Include(c => c.AgencyReport.PanIdReport)
              .Include(c => c.AgencyReport.PassportIdReport)
              .Include(c => c.AgencyReport.AudioReport)
              .Include(c => c.AgencyReport.VideoReport)
              .Include(c => c.AgencyReport.ReportQuestionaire)
                .Include(c => c.PolicyDetail)
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                  .Include(c=>c.Vendor)

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
                .Include(c =>c.ClaimNotes)
                .Include(c =>c.ClaimMessages)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);
            
            var claimsLineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIMS).LineOfBusinessId;

            var isClaim = claim.PolicyDetail.LineOfBusinessId == claimsLineOfBusinessId;

            if (isClaim)
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date and time of death ?";
            }
            else
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
            }
            claim.CompanyWithdrawlComment = string.Empty;
            var model = new ClaimTransactionModel
            {
                ClaimsInvestigation = claim,
                Log = caseLogs,
                Location = claim.BeneficiaryDetail,
                NotWithdrawable = claim.NotWithdrawable,
                TimeTaken = GetElapsedTime(caseLogs)
            };
            return model;
        }

        public async Task<ClaimTransactionModel> GetClaimSummary(string userEmail, string id)
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u=>u.Email == userEmail);

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
                .Include(c => c.ClientCompany)
                .Include(c => c.AgencyReport)
                    .ThenInclude(c => c.EnquiryRequest)
              .Include(c => c.AgencyReport.DigitalIdReport)
              .Include(c => c.AgencyReport.PanIdReport)
              .Include(c => c.AgencyReport.PassportIdReport)
              .Include(c => c.AgencyReport.ReportQuestionaire)
                .Include(c => c.PolicyDetail)
                .Include(c => c.ClientCompany)
                  .Include(c=>c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
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
                .Include(c=>c.ClaimNotes)
                .Include(c=>c.ClaimMessages)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);
            var submittedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var location = claim.BeneficiaryDetail;
            var claimsLineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIMS).LineOfBusinessId;

            var isClaim = claim.PolicyDetail.LineOfBusinessId == claimsLineOfBusinessId;

            if (isClaim)
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date and time of death ?";
            }
            else
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
            }
            if (caseLogs.Any(l => l.UserEmailActioned == companyUser.Email || l.InvestigationCaseSubStatusId == submittedStatus.InvestigationCaseSubStatusId))
            {
                return new ClaimTransactionModel
                {
                    ClaimsInvestigation = claim,
                    Log = caseLogs,
                    Location = location,
                    TimeTaken = GetElapsedTime(caseLogs)
                };
            }
            return null!;
        }

        private static string GetElapsedTime(List<InvestigationTransaction> caseLogs)
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
    }
}