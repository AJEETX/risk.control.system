using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IVendorInvestigationService
    {
        Task<List<ClaimsInvestigationAgencyResponse>> GetNewCases(string userEmail);
        Task<List<ClaimsInvestigationResponse>> GetOpenCases(string userEmail);
        Task<List<ClaimsInvestigationAgencyResponse>> GetReport(string userEmail);
        Task<List<ClaimsInvestigationAgencyResponse>> GetCompleted(string userEmail, string userClaim);
        Task<CaseTransactionModel> GetClaimDetails(string currentUserEmail, long id);
        Task<CaseInvestigationVendorAgentModel> SelectVendorAgent(string userEmail, long selectedcase);
        Task<InvestigationTask> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, long claimsInvestigationId);
        Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, long claimsInvestigationId, string remarks);
        Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long id);
    }
    internal class VendorInvestigationService : IVendorInvestigationService
    {
        private readonly ApplicationDbContext context;
        private readonly IWeatherInfoService weatherInfoService;
        private readonly ILogger<VendorInvestigationService> logger;
        private readonly IWebHostEnvironment env;
        private readonly ITimelineService timelineService;
        private readonly ICustomApiClient customApiCLient;

        public VendorInvestigationService(ApplicationDbContext context,
            IWeatherInfoService weatherInfoService,
            ILogger<VendorInvestigationService> logger,
            IWebHostEnvironment env,
            ITimelineService timelineService,
            ICustomApiClient customApiCLient)
        {
            this.context = context;
            this.weatherInfoService = weatherInfoService;
            this.logger = logger;
            this.env = env;
            this.timelineService = timelineService;
            this.customApiCLient = customApiCLient;
        }

        public async Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long id)
        {
            var claim = await context.Investigations
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

            var companyUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = claim.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();
            var endTIme = claim.Status == CONSTANTS.CASE_STATUS.FINISHED ? claim.ProcessedByAssessorTime.GetValueOrDefault() : DateTime.Now;
            var timeTaken = endTIme - claim.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = await context.VendorInvoice.FirstOrDefaultAsync(i => i.InvestigationReportId == claim.InvestigationReportId);
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
                  .FirstOrDefaultAsync(q => q.Id == claim.ReportTemplateId);

            claim.InvestigationReport.ReportTemplate = templates;

            var tracker = await context.PdfDownloadTracker
                          .FirstOrDefaultAsync(t => t.ReportId == id && t.UserEmail == currentUserEmail);
            bool canDownload = true;
            if (tracker != null)
            {
                canDownload = tracker.DownloadCount <= 3;
            }
            var maskedCustomerContact = new string('*', claim.CustomerDetail.PhoneNumber.ToString().Length - 4) + claim.CustomerDetail.PhoneNumber.ToString().Substring(claim.CustomerDetail.PhoneNumber.ToString().Length - 4);
            claim.CustomerDetail.PhoneNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + claim.BeneficiaryDetail.PhoneNumber.ToString().Substring(claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
            claim.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;

            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = claim,
                CaseIsValidToAssign = claim.IsValidCaseData(),
                Location = claim.BeneficiaryDetail,
                Assigned = claim.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser != null ? companyUser.ClientCompany.AutoAllocation : false,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                CanDownload = canDownload,
                Withdrawable = (claim.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }
        public async Task<CaseTransactionModel> GetClaimDetails(string currentUserEmail, long id)
        {
            var claim = await context.Investigations
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

            var lastHistory = claim.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var maskedCustomerContact = new string('*', claim.CustomerDetail.PhoneNumber.ToString().Length - 4) + claim.CustomerDetail.PhoneNumber.ToString().Substring(claim.CustomerDetail.PhoneNumber.ToString().Length - 4);
            claim.CustomerDetail.PhoneNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + claim.BeneficiaryDetail.PhoneNumber.ToString().Substring(claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
            claim.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;

            var timeTaken = DateTime.Now - lastHistory.StatusChangedAt;
            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = claim,
                CaseIsValidToAssign = claim.IsValidCaseData(),
                Location = claim.BeneficiaryDetail,
                Assigned = claim.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                TimeTaken = timeTaken.ToString(@"hh\:mm\:ss") ?? "-",
                Withdrawable = (claim.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }

        public async Task<CaseInvestigationVendorAgentModel> SelectVendorAgent(string userEmail, long selectedcase)
        {
            var claimsAllocate2Agent = GetCases().Include(c => c.CaseNotes).FirstOrDefault(v => v.Id == selectedcase);

            var beneficiaryDetail = await context.BeneficiaryDetail
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.BeneficiaryDetailId == claimsAllocate2Agent.BeneficiaryDetail.BeneficiaryDetailId);

            var maskedCustomerContact = new string('*', claimsAllocate2Agent.CustomerDetail.PhoneNumber.ToString().Length - 4) + claimsAllocate2Agent.CustomerDetail.PhoneNumber.ToString().Substring(claimsAllocate2Agent.CustomerDetail.PhoneNumber.ToString().Length - 4);
            claimsAllocate2Agent.CustomerDetail.PhoneNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', beneficiaryDetail.PhoneNumber.ToString().Length - 4) + beneficiaryDetail.PhoneNumber.ToString().Substring(beneficiaryDetail.PhoneNumber.ToString().Length - 4);
            claimsAllocate2Agent.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            beneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;

            var model = new CaseInvestigationVendorAgentModel
            {
                CaseLocation = beneficiaryDetail,
                ClaimsInvestigation = claimsAllocate2Agent,
            };
            return model;
        }

        public async Task<InvestigationTask> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, long claimsInvestigationId)
        {
            try
            {
                var assignedToAgent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
                var claim = await context.Investigations
                    .Include(c => c.InvestigationReport)
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .Include(c => c.BeneficiaryDetail)
                    .FirstOrDefaultAsync(c => c.Id == claimsInvestigationId);

                var agentUser = await context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(u => u.Email == vendorAgentEmail);

                string drivingDistance, drivingDuration, drivingMap;
                float distanceInMeters;
                int durationInSeconds;
                string LocationLatitude = string.Empty;
                string LocationLongitude = string.Empty;
                if (claim.PolicyDetail?.InsuranceType == InsuranceType.UNDERWRITING)
                {
                    LocationLatitude = claim.CustomerDetail?.Latitude;
                    LocationLongitude = claim.CustomerDetail?.Longitude;
                }
                else
                {
                    LocationLatitude = claim.BeneficiaryDetail?.Latitude;
                    LocationLongitude = claim.BeneficiaryDetail?.Longitude;
                }
                (drivingDistance, distanceInMeters, drivingDuration, durationInSeconds, drivingMap) = await customApiCLient.GetMap(
                  double.Parse(agentUser.AddressLatitude),
                  double.Parse(agentUser.AddressLongitude),
                   double.Parse(LocationLatitude),
                    double.Parse(LocationLongitude));
                claim.AllocatingSupervisordEmail = currentUser;
                claim.CaseOwner = vendorAgentEmail;
                claim.TaskedAgentEmail = vendorAgentEmail;
                claim.IsNewAssignedToAgency = true;
                claim.Updated = DateTime.Now;
                claim.UpdatedBy = currentUser;
                claim.SubStatus = assignedToAgent;
                claim.SelectedAgentDrivingDistance = drivingDistance;
                claim.SelectedAgentDrivingDuration = drivingDuration;
                claim.SelectedAgentDrivingDistanceInMetres = distanceInMeters;
                claim.SelectedAgentDrivingDurationInSeconds = durationInSeconds;
                claim.SelectedAgentDrivingMap = string.Format(drivingMap, "400", "400");
                claim.TaskToAgentTime = DateTime.Now;

                context.Investigations.Update(claim);
                var rows = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(claim.Id, currentUser);
                return claim;

            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        public async Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, long claimsInvestigationId, string remarks)
        {
            try
            {
                var agent = await context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(a => a.Email.Trim().ToLower() == userEmail.ToLower());

                var submitted2Supervisor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR;

                var claim = GetCases().Include(c => c.InvestigationReport)
                    .FirstOrDefault(c => c.Id == claimsInvestigationId);

                claim.Updated = DateTime.Now;
                claim.UpdatedBy = agent.Email;
                claim.SubStatus = submitted2Supervisor;
                claim.SubmittedToSupervisorTime = DateTime.Now;
                claim.CaseOwner = agent.Vendor.Email;
                var claimReport = claim.InvestigationReport;

                claimReport.AgentRemarks = remarks;
                claimReport.AgentRemarksUpdated = DateTime.Now;
                claimReport.AgentEmail = userEmail;

                context.Investigations.Update(claim);

                var rows = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(claim.Id, userEmail);

                return (agent.Vendor, claim.PolicyDetail.ContractNumber);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        private IQueryable<InvestigationTask> GetCases()
        {
            var claim = context.Investigations
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
            return claim;
        }

        public async Task<List<ClaimsInvestigationAgencyResponse>> GetNewCases(string userEmail)
        {
            var vendorUser = await context.ApplicationUser
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            // Filter claims early and minimize loading
            var claims = await context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Where(a => a.VendorId == vendorUser.VendorId &&
                            (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR ||
                             a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)).ToListAsync();
            // Process each claim and update as necessary

            foreach (var claim in claims)
            {

                if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING && claim.CustomerDetail != null)
                {
                    // Fetch weather data for HEALTH claims
                    claim.CustomerDetail.AddressLocationInfo = await weatherInfoService.GetWeatherAsync(claim.CustomerDetail.Latitude, claim.CustomerDetail.Longitude);
                }
                else if (claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM && claim.BeneficiaryDetail != null)
                {
                    // Fetch weather data for DEATH claims
                    claim.BeneficiaryDetail.AddressLocationInfo = await weatherInfoService.GetWeatherAsync(claim.BeneficiaryDetail.Latitude, claim.BeneficiaryDetail.Longitude);
                }
            }

            var response = claims.Select(a => new ClaimsInvestigationAgencyResponse
            {
                Id = a.Id,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                Company = a.ClientCompany.Name,
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.ClientCompany.DocumentUrl)))),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                AssignedToAgency = a.AssignedToAgency,
                Document = a.PolicyDetail.DocumentPath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) : Applicationsettings.NO_POLICY_IMAGE,
                Customer =
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail)))))
                ,
                Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                Policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                Status = a.Status,
                ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetSupervisorNewTimePending(a),
                PolicyNum = GetPolicyNumForAgency(a, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR),
                BeneficiaryPhoto = a.BeneficiaryDetail.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" :
                    a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.Value).TotalSeconds,
                IsNewAssigned = a.IsNewAssignedToAgency,
                IsQueryCase = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR,
                PersonMapAddressUrl = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ?
                string.Format(a.CustomerDetail.CustomerLocationMap, "400", "400") : string.Format(a.BeneficiaryDetail.BeneficiaryLocationMap, "400", "400"),
                AddressLocationInfo = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.AddressLocationInfo : a.BeneficiaryDetail.AddressLocationInfo
            }).ToList();
            // Mark claims as viewed
            var idsToMarkViewed = response.Where(x => x.IsNewAssigned.GetValueOrDefault()).Select(x => x.Id).ToList();
            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNewAssignedToAgency = false;

                await context.SaveChangesAsync(null, false); // mark as viewed
            }

            return response;
        }
        public async Task<List<ClaimsInvestigationResponse>> GetOpenCases(string userEmail)
        {
            var vendorUser = await context.ApplicationUser
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);
            List<InvestigationTask> claims = null;
            if (vendorUser.Role.ToString() == AppRoles.SUPERVISOR.ToString())
            {
                claims = await context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Where(a => a.Status == CONSTANTS.CASE_STATUS.INPROGRESS)
                .Where(a => a.VendorId == vendorUser.VendorId)
                .Where(a => (a.AllocatingSupervisordEmail == userEmail &&
                            a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT) ||
                            ((a.SubmittingSupervisordEmail == userEmail) &&
                            (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                             a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR))).ToListAsync();
            }
            else
            {
                claims = await context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Where(a => a.Status == CONSTANTS.CASE_STATUS.INPROGRESS)
                .Where(a => a.VendorId == vendorUser.VendorId &&
                            (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ||
                            a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                             a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)).ToListAsync();
            }

            var response = claims?.Select(a => new ClaimsInvestigationResponse
            {
                Id = a.Id,
                AssignedToAgency = a.IsNewSubmittedToAgent,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                Agent = GetOwnerEmail(a),
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwner(a))),
                CaseWithPerson = IsCaseWithAgent(a),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Company = a.ClientCompany.Name,
                Document = a.PolicyDetail.DocumentPath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) : Applicationsettings.NO_POLICY_IMAGE,
                Customer =
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail))))),
                Name = a.CustomerDetail.Name,
                Policy = $"<span class='badge badge-light'>{a.PolicyDetail.InsuranceType.GetEnumDisplayName()}</span>",
                Status = a.Status,
                ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetSupervisorOpenTimePending(a),
                PolicyNum = a.PolicyDetail.ContractNumber,
                BeneficiaryPhoto = a.BeneficiaryDetail.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ? "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = GetTimeElapsed(a),
                PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration
            })?.ToList();

            var idsToMarkViewed = response.Where(x => x.IsNewAssigned.GetValueOrDefault()).Select(x => x.Id).ToList();
            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    if (entity.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
                    {
                        entity.IsNewSubmittedToAgent = false;
                    }
                    else if (entity.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
                    {
                        entity.IsNewSubmittedToCompany = false;
                    }

                await context.SaveChangesAsync(null, false); // mark as viewed
            }

            return response;
        }

        public async Task<List<ClaimsInvestigationAgencyResponse>> GetCompleted(string userEmail, string userClaim)
        {
            var agencyUser = await context.ApplicationUser
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            var finishedStatus = CONSTANTS.CASE_STATUS.FINISHED;
            var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
            var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

            var claims = context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Where(a => a.VendorId == agencyUser.VendorId && a.Status == finishedStatus && (a.SubStatus == approvedStatus || a.SubStatus == rejectedStatus));

            if (agencyUser.Role.ToString() == AppRoles.SUPERVISOR.ToString())
            {
                claims = claims
                    .Where(a => a.SubmittedAssessordEmail == userEmail);
            }
            var responseData = await claims.ToListAsync();
            var response = responseData
                .Select(a => new ClaimsInvestigationAgencyResponse
                {
                    Id = a.Id,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(agencyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.ClientCompany.DocumentUrl)))),
                    Document = a.PolicyDetail.DocumentPath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) : Applicationsettings.NO_POLICY_IMAGE,
                    Customer =
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail))))),
                    Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                    Policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                    Status = a.Status,
                    ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = GetSupervisorCompletedTimePending(a),
                    PolicyNum = a.PolicyDetail.ContractNumber,
                    BeneficiaryPhoto = a.BeneficiaryDetail.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                                    "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>" :
                                    a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime.Value).TotalSeconds,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration,
                    CanDownload = CanDownload(a.Id, userClaim)
                })
                .ToList();
            return response;
        }
        public async Task<List<ClaimsInvestigationAgencyResponse>> GetReport(string userEmail)
        {

            // Fetch the vendor user along with the related Vendor and Country info in one query
            var vendorUser = await context.ApplicationUser
                .Include(v => v.Country)
                .Include(u => u.Vendor)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            // Filter the claims based on the vendor ID and required status
            var cases = context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Where(a => a.VendorId == vendorUser.VendorId &&
                            a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var responseData = await cases.ToListAsync();
            var response = responseData.Select(a =>
                new ClaimsInvestigationAgencyResponse
                {
                    Id = a.Id,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.ClientCompany.DocumentUrl)))),
                    Document = a.PolicyDetail.DocumentPath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) : Applicationsettings.NO_POLICY_IMAGE,
                    Customer =
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail))))),
                    Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                    Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Status = a.SubStatus,
                    ServiceType = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    RawStatus = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = GetSupervisorReportTimePending(a),
                    PolicyNum = a.PolicyDetail.ContractNumber,
                    BeneficiaryPhoto = a.BeneficiaryDetail.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                            "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" :
                            a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.SubmittedToSupervisorTime.Value).TotalSeconds,
                    IsNewAssigned = a.IsNewSubmittedToAgency,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                }).ToList();
            var idsToMarkViewed = response.Where(x => x.IsNewAssigned.GetValueOrDefault()).Select(x => x.Id).ToList();
            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNewSubmittedToAgency = false;

                await context.SaveChangesAsync(null, false); // mark as viewed
            }
            return response;
        }
        private static string GetSupervisorNewTimePending(InvestigationTask a)
        {
            DateTime timeToCompare = a.AllocatedToAgencyTime.Value;

            var requested2agency = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
            //1. All new case
            if (a.SubStatus == requested2agency)
            {
                timeToCompare = a.EnquiredByAssessorTime.GetValueOrDefault();
            }

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        private static string GetPolicyNumForAgency(InvestigationTask a, string enquiryStatus, string allocatedStatus)
        {
            var claim = a;
            if (claim is not null)
            {
                var isRequested = a.SubStatus == enquiryStatus;
                if (isRequested)
                {
                    return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"ENQUIRY\"></i>");
                }

            }
            return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style-none\"></i>");
        }

        private double GetTimeElapsed(InvestigationTask a)
        {

            var timeElapsed = DateTime.Now.Subtract(a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ? a.TaskToAgentTime.Value :
                                                     a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ? a.SubmittedToAssessorTime.Value :
                                                     a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ?
                                                     a.EnquiryReplyByAssessorTime.Value : a.Created).TotalSeconds;
            return timeElapsed;
        }
        private string GetSupervisorOpenTimePending(InvestigationTask a)
        {
            DateTime timeToCompare = a.TaskToAgentTime.Value;
            if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                timeToCompare = a.TaskToAgentTime.GetValueOrDefault();

            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
            {
                timeToCompare = a.SubmittedToAssessorTime.GetValueOrDefault();
            }

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        private static bool IsCaseWithAgent(InvestigationTask a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var allocated2agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;

            return (a.SubStatus == allocated2agent);

        }
        private string GetOwnerEmail(InvestigationTask a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var allocated2agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;

            if (a.SubStatus == allocated2agent)
            {
                ownerEmail = a.TaskedAgentEmail;
                var agencyUser = context.ApplicationUser.FirstOrDefault(u => u.Email == ownerEmail);
                if (agencyUser != null && !string.IsNullOrWhiteSpace(agencyUser.Email))
                {
                    return agencyUser?.Email;
                }
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR || a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                var companyUser = context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == a.ClientCompanyId);
                if (companyUser != null && !string.IsNullOrWhiteSpace(companyUser.Email))
                {
                    return companyUser.Email;
                }
            }
            return "noDataimage";
        }
        private byte[] GetOwner(InvestigationTask a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var allocated2agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
            var noDataImagefilePath = Path.Combine(env.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);

            if (a.SubStatus == allocated2agent)
            {
                ownerEmail = a.TaskedAgentEmail;
                var agencyUser = context.ApplicationUser.FirstOrDefault(u => u.Email == ownerEmail);
                if (agencyUser != null && !string.IsNullOrWhiteSpace(agencyUser?.ProfilePictureUrl))
                {
                    var agencyUserImagePath = Path.Combine(env.ContentRootPath, agencyUser.ProfilePictureUrl);
                    if (System.IO.File.Exists(agencyUserImagePath))
                    {
                        return System.IO.File.ReadAllBytes(agencyUserImagePath);
                    }
                }
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR || a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                var companyUser = context.ClientCompany.FirstOrDefault(u => u.ClientCompanyId == a.ClientCompanyId);
                if (companyUser != null && !string.IsNullOrWhiteSpace(companyUser?.DocumentUrl))
                {
                    var companyUserImagePath = Path.Combine(env.ContentRootPath, companyUser.DocumentUrl);
                    if (System.IO.File.Exists(companyUserImagePath))
                    {
                        return System.IO.File.ReadAllBytes(companyUserImagePath);
                    }
                }
            }
            return noDataimage;
        }
        private static string GetSupervisorReportTimePending(InvestigationTask a)
        {
            DateTime timeToCompare = a.SubmittedToSupervisorTime.Value;

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        private bool CanDownload(long id, string userEmail)
        {
            var tracker = context.PdfDownloadTracker
                          .FirstOrDefault(t => t.ReportId == id && t.UserEmail == userEmail);
            bool canDownload = true;
            if (tracker != null && tracker.DownloadCount > 3)
            {
                canDownload = false;
            }
            return canDownload;
        }
        private static string GetSupervisorCompletedTimePending(InvestigationTask a)
        {
            DateTime timeToCompare = a.ProcessedByAssessorTime.Value;

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
    }
}
