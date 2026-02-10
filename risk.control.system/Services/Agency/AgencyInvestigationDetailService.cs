using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agency
{
    public interface IAgencyInvestigationDetailService
    {
        Task<CaseTransactionModel> GetClaimDetails(string currentUserEmail, long caseId);

        Task<CaseInvestigationVendorAgentModel> SelectVendorAgent(string userEmail, long selectedcase);

        Task<InvestigationTask> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, long caseId);

        Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, long caseId, string remarks);

        Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long caseId);
    }

    internal class AgencyInvestigationDetailService : IAgencyInvestigationDetailService
    {
        private readonly ILogger<AgencyInvestigationDetailService> logger;
        private readonly ApplicationDbContext context;
        private readonly ICustomApiClient customApiCLient;
        private readonly ITimelineService timelineService;

        public AgencyInvestigationDetailService(
            ILogger<AgencyInvestigationDetailService> logger,
            ApplicationDbContext context,
            ICustomApiClient customApiCLient,
            ITimelineService timelineService)
        {
            this.logger = logger;
            this.context = context;
            this.customApiCLient = customApiCLient;
            this.timelineService = timelineService;
        }

        public async Task<CaseTransactionModel> GetClaimDetails(string currentUserEmail, long caseId)
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
                .FirstOrDefaultAsync(m => m.Id == caseId);

            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var maskedCustomerContact = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
            caseTask.CustomerDetail.PhoneNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
            caseTask.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;

            var timeTaken = DateTime.UtcNow - lastHistory.StatusChangedAt;
            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Location = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                TimeTaken = timeTaken.ToString(@"hh\:mm\:ss") ?? "-",
                Withdrawable = (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }

        public async Task<CaseInvestigationVendorAgentModel> SelectVendorAgent(string userEmail, long selectedcase)
        {
            var caseAllocate2Agent = await GetCases().Include(c => c.CaseNotes).FirstOrDefaultAsync(v => v.Id == selectedcase);

            var beneficiaryDetail = await context.BeneficiaryDetail
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.BeneficiaryDetailId == caseAllocate2Agent.BeneficiaryDetail.BeneficiaryDetailId);

            var maskedCustomerContact = new string('*', caseAllocate2Agent.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseAllocate2Agent.CustomerDetail.PhoneNumber.ToString().Substring(caseAllocate2Agent.CustomerDetail.PhoneNumber.ToString().Length - 4);
            caseAllocate2Agent.CustomerDetail.PhoneNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', beneficiaryDetail.PhoneNumber.ToString().Length - 4) + beneficiaryDetail.PhoneNumber.ToString().Substring(beneficiaryDetail.PhoneNumber.ToString().Length - 4);
            caseAllocate2Agent.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            beneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;

            var model = new CaseInvestigationVendorAgentModel
            {
                CaseLocation = beneficiaryDetail,
                ClaimsInvestigation = caseAllocate2Agent,
            };
            return model;
        }

        public async Task<InvestigationTask> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, long caseId)
        {
            try
            {
                var caseTask = await context.Investigations
                    .Include(c => c.InvestigationReport)
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .Include(c => c.BeneficiaryDetail)
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                var agentUser = await context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(u => u.Email == vendorAgentEmail);

                string drivingDistance, drivingDuration, drivingMap;
                float distanceInMeters;
                int durationInSeconds;
                string LocationLatitude = string.Empty;
                string LocationLongitude = string.Empty;
                if (caseTask.PolicyDetail?.InsuranceType == InsuranceType.UNDERWRITING)
                {
                    LocationLatitude = caseTask.CustomerDetail?.Latitude;
                    LocationLongitude = caseTask.CustomerDetail?.Longitude;
                }
                else
                {
                    LocationLatitude = caseTask.BeneficiaryDetail?.Latitude;
                    LocationLongitude = caseTask.BeneficiaryDetail?.Longitude;
                }
                (drivingDistance, distanceInMeters, drivingDuration, durationInSeconds, drivingMap) = await customApiCLient.GetMap(
                  double.Parse(agentUser.AddressLatitude),
                  double.Parse(agentUser.AddressLongitude),
                   double.Parse(LocationLatitude),
                    double.Parse(LocationLongitude));
                caseTask.AllocatingSupervisordEmail = currentUser;
                caseTask.CaseOwner = vendorAgentEmail;
                caseTask.TaskedAgentEmail = vendorAgentEmail;
                caseTask.IsNewAssignedToAgency = true;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.UpdatedBy = currentUser;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
                caseTask.SelectedAgentDrivingDistance = drivingDistance;
                caseTask.SelectedAgentDrivingDuration = drivingDuration;
                caseTask.SelectedAgentDrivingDistanceInMetres = distanceInMeters;
                caseTask.SelectedAgentDrivingDurationInSeconds = durationInSeconds;
                caseTask.SelectedAgentDrivingMap = string.Format(drivingMap, "400", "400");
                caseTask.TaskToAgentTime = DateTime.UtcNow;

                context.Investigations.Update(caseTask);
                var rows = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, currentUser);
                return caseTask;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        public async Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, long caseId, string remarks)
        {
            try
            {
                var agent = await context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(a => a.Email.Trim().ToLower() == userEmail.Trim().ToLower());

                var submitted2Supervisor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR;

                var caseTask = await GetCases().Include(c => c.InvestigationReport)
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                caseTask.Updated = DateTime.UtcNow;
                caseTask.UpdatedBy = agent.Email;
                caseTask.SubStatus = submitted2Supervisor;
                caseTask.SubmittedToSupervisorTime = DateTime.UtcNow;
                caseTask.CaseOwner = agent.Vendor.Email;
                var claimReport = caseTask.InvestigationReport;

                claimReport.AgentRemarks = remarks;
                claimReport.AgentRemarksUpdated = DateTime.UtcNow;
                claimReport.AgentEmail = userEmail;

                context.Investigations.Update(caseTask);

                var rows = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                return (agent.Vendor, caseTask.PolicyDetail.ContractNumber);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        public async Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long caseId)
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
                .FirstOrDefaultAsync(m => m.Id == caseId);

            var companyUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();
            var endTIme = caseTask.Status == CONSTANTS.CASE_STATUS.FINISHED ? caseTask.ProcessedByAssessorTime.GetValueOrDefault() : DateTime.UtcNow;
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
                          .FirstOrDefaultAsync(t => t.ReportId == caseId && t.UserEmail == currentUserEmail);
            bool canDownload = true;
            if (tracker != null)
            {
                canDownload = tracker.DownloadCount <= 3;
            }
            var maskedCustomerContact = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
            caseTask.CustomerDetail.PhoneNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
            caseTask.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;

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

        private IQueryable<InvestigationTask> GetCases()
        {
            var caseTasks = context.Investigations
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
                .ThenInclude(c => c.PinCode);
            return caseTasks;
        }
    }
}