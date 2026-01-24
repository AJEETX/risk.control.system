using Hangfire;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IProcessCaseService
    {
        Task<int> UpdateCaseAllocationStatus(string userEmail, List<long> caseIds);
        Task BackgroundAutoAllocation(List<long> claims, string userEmail, string url = "");
        Task<List<long>> BackgroundUploadAutoAllocation(List<long> caseIds, string userEmail, string url = "");

        Task<string> ProcessAutoSingleAllocation(long caseId, string userEmail, string url = "");
        Task<(string, string)> AllocateToVendor(string userEmail, long caseId, long vendorId, bool autoAllocated = true);
        Task<Vendor> WithdrawCase(string userEmail, CaseTransactionModel model, long caseId);
        Task<(ClientCompany, long)> WithdrawCaseByCompany(string userEmail, CaseTransactionModel model, long caseId);
        Task<Vendor> WithdrawCaseFromAgent(string userEmail, CaseTransactionModel model, long caseId);

        Task<InvestigationTask> SubmitQueryReplyToCompany(string userEmail, long caseId, EnquiryRequest request, List<EnquiryRequest> requests, IFormFile document);
        Task<InvestigationTask> ProcessAgentReport(string userEmail, string supervisorRemarks, long caseId, SupervisorRemarkType reportUpdateStatus, IFormFile? document = null, string editRemarks = "");

        Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType reportUpdateStatus, string reportAiSummary);
        Task<InvestigationTask> SubmitQueryToAgency(string userEmail, long caseId, EnquiryRequest request, List<EnquiryRequest> requests, IFormFile document);
        List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors);
        Task<bool> SubmitNotes(string userEmail, long caseId, string notes);
    }
    internal class ProcessCaseService : IProcessCaseService
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<ProcessCaseService> logger;
        private readonly IPdfGenerativeService pdfGenerativeService;
        private readonly IMailService mailboxService;
        private readonly ITimelineService timelineService;
        private readonly IBackgroundJobClient backgroundJobClient;

        public ProcessCaseService(ApplicationDbContext context,
            ILogger<ProcessCaseService> logger,
            IPdfGenerativeService pdfGenerativeService,
            IMailService mailboxService,
            ITimelineService timelineService,
            IBackgroundJobClient backgroundJobClient)
        {
            this.context = context;
            this.logger = logger;
            this.pdfGenerativeService = pdfGenerativeService;
            this.mailboxService = mailboxService;
            this.timelineService = timelineService;
            this.backgroundJobClient = backgroundJobClient;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task<List<long>> BackgroundUploadAutoAllocation(List<long> caseIds, string userEmail, string url = "")
        {
            var autoAllocatedCases = await DoAutoAllocation(caseIds, userEmail, url); // Run all tasks in parallel

            var notAutoAllocated = caseIds.Except(autoAllocatedCases)?.ToList();

            if (caseIds.Count > autoAllocatedCases.Count)
            {
                await AssignToAssigner(userEmail, notAutoAllocated, url);

            }
            var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAssignmentToAssigner(userEmail, autoAllocatedCases, notAutoAllocated, url));

            return (autoAllocatedCases);
        }
        public async Task<int> UpdateCaseAllocationStatus(string userEmail, List<long> caseIds)
        {
            try
            {
                if (caseIds == null || !caseIds.Any())
                    return 0; // No cases to update

                // Fetch all matching cases in one query
                var cases = await context.Investigations
                    .Where(v => caseIds.Contains(v.Id))
                    .ToListAsync();

                if (!cases.Any())
                    return 0; // No matching cases found

                // Update the status only for cases that are not already PENDING
                foreach (var claimsCase in cases)
                {
                    claimsCase.UpdatedBy = userEmail;
                    claimsCase.Updated = DateTime.Now;
                }

                context.Investigations.UpdateRange(cases);
                return await context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }
        [AutomaticRetry(Attempts = 0)]
        public async Task BackgroundAutoAllocation(List<long> caseIds, string userEmail, string url = "")
        {
            var autoAllocatedCases = await DoAutoAllocation(caseIds, userEmail, url); // Run all tasks in parallel

            var notAutoAllocated = caseIds.Except(autoAllocatedCases)?.ToList();

            if (caseIds.Count > autoAllocatedCases.Count)
            {
                await AssignToAssigner(userEmail, notAutoAllocated, url);

            }
            var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAssignmentToAssigner(userEmail, autoAllocatedCases, notAutoAllocated, url));
        }
        async Task<List<long>> DoAutoAllocation(List<long> claims, string userEmail, string url = "")
        {
            var companyUser = await context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);

            var company = await context.ClientCompany
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var claimTasks = claims.Select(async claim =>
            {
                // 1. Fetch Claim Details & Pincode in Parallel
                var caseTask = await context.Investigations
                    .AsNoTracking()
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                        .ThenInclude(c => c.PinCode)
                    .FirstOrDefaultAsync(c => c.Id == claim);

                if (caseTask == null || !caseTask.IsValidCaseData()) return 0; // Handle missing claim

                var pinCode2Verify = caseTask.PolicyDetail?.InsuranceType == InsuranceType.UNDERWRITING
                    ? caseTask.CustomerDetail?.PinCode?.Code
                    : caseTask.BeneficiaryDetail?.PinCode?.Code;

                var pincodeDistrictState = await context.PinCode
                    .AsNoTracking()
                    .Include(d => d.District)
                    .Include(s => s.State)
                    .FirstOrDefaultAsync(p => p.Code == pinCode2Verify);

                // 2. Find Vendors Using LINQ
                var distinctVendorIds = company.EmpanelledVendors
                    .Where(vendor => vendor.VendorInvestigationServiceTypes.Any(serviceType =>
                        serviceType.InvestigationServiceTypeId == caseTask.PolicyDetail.InvestigationServiceTypeId &&
                        serviceType.InsuranceType == caseTask.PolicyDetail.InsuranceType &&
                        (serviceType.StateId == pincodeDistrictState.StateId &&
                         (serviceType.SelectedDistrictIds?.Contains(-1) == true
                         || serviceType.SelectedDistrictIds.Contains(pincodeDistrictState.DistrictId.Value)))
                    ))
                    .Select(v => v.VendorId) // Select only VendorId
                    .Distinct() // Ensure uniqueness
                    .ToList();

                if (!distinctVendorIds.Any()) return 0; // No vendors found, skip this claim

                // 3. Get Vendor Load & Allocate
                var vendorsWithCaseLoad = GetAgencyIdsLoad(distinctVendorIds)
                    .OrderBy(o => o.CaseCount)
                    .ToList();

                var selectedVendorId = vendorsWithCaseLoad.FirstOrDefault();
                if (selectedVendorId == null) return 0; // No vendors available

                var (policy, status) = await AllocateToVendor(userEmail, caseTask.Id, selectedVendorId.VendorId);

                if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    return 0;
                }
                var jobId = backgroundJobClient.Enqueue(() =>
                    mailboxService.NotifyCaseAllocationToVendor(userEmail, policy, caseTask.Id, selectedVendorId.VendorId, url));

                return claim; // Return allocated claim
            });

            var results = await Task.WhenAll(claimTasks); // Run all tasks in parallel
            return results.Where(r => r != null && r != 0).ToList(); // Remove nulls and return allocated claims
        }
        public async Task<string> ProcessAutoSingleAllocation(long caseId, string userEmail, string url = "")
        {
            var companyUser = await context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);

            var company = await context.ClientCompany
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            // 1. Fetch Claim Details & Pincode in Parallel
            var caseTask = await context.Investigations
                .AsNoTracking()
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.PinCode)
            .FirstOrDefaultAsync(c => c.Id == caseId);

            var pinCode2Verify = caseTask.PolicyDetail?.InsuranceType == InsuranceType.UNDERWRITING
                ? caseTask.CustomerDetail?.PinCode?.Code
                : caseTask.BeneficiaryDetail?.PinCode?.Code;

            var pincodeDistrictState = await context.PinCode
                .AsNoTracking()
                .Include(d => d.District)
                .Include(s => s.State)
                .FirstOrDefaultAsync(p => p.Code == pinCode2Verify);

            // 2. Find Vendors Using LINQ
            var distinctVendorIds = company.EmpanelledVendors
                .Where(vendor => vendor.VendorInvestigationServiceTypes.Any(serviceType =>
                    serviceType.InvestigationServiceTypeId == caseTask.PolicyDetail.InvestigationServiceTypeId &&
                    serviceType.InsuranceType == caseTask.PolicyDetail.InsuranceType &&
                    (serviceType.StateId == pincodeDistrictState.StateId &&
                      (serviceType.SelectedDistrictIds?.Contains(-1) == true
                         || serviceType.SelectedDistrictIds.Contains(pincodeDistrictState.DistrictId.Value)))
                    ))
                .Select(v => v.VendorId) // Select only VendorId
                .Distinct() // Ensure uniqueness
                .ToList();

            if (!distinctVendorIds.Any()) return null; // No vendors found, skip this claim

            // 3. Get Vendor Load & Allocate
            var vendorsWithCaseLoad = GetAgencyIdsLoad(distinctVendorIds)
                .OrderBy(o => o.CaseCount)
                .ToList();

            var selectedVendorId = vendorsWithCaseLoad.FirstOrDefault();
            if (selectedVendorId == null) return null; // No vendors available

            var (policy, status) = await AllocateToVendor(userEmail, caseTask.Id, selectedVendorId.VendorId);

            if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
            {
                await AssignToAssigner(userEmail, new List<long> { caseId });
                await mailboxService.NotifyCaseAssignmentToAssigner(userEmail, new List<long> { caseId }, url);
                return null;
            }

            // 4. Send Notification
            var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAllocationToVendor(userEmail, policy, caseTask.Id, selectedVendorId.VendorId, url));

            return caseTask.PolicyDetail.ContractNumber; // Return allocated claim
        }
        public async Task AssignToAssigner(string userEmail, List<long> claims, string url = "")
        {
            if (claims is null || claims.Count == 0)
            {
                return;
            }
            var cases2Assign = context.Investigations
                .Include(c => c.InvestigationTimeline)
                   .Where(v => claims.Contains(v.Id));
            var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
            var assigned = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;

            foreach (var case2Assign in cases2Assign)
            {
                case2Assign.CaseOwner = currentUser.Email;
                case2Assign.IsNew = true;
                case2Assign.Updated = DateTime.Now;
                case2Assign.UpdatedBy = currentUser.Email;
                case2Assign.AssignedToAgency = false;
                case2Assign.IsReady2Assign = case2Assign.IsValidCaseData() ? true : false;
                case2Assign.VendorId = null;
                case2Assign.SubStatus = assigned;
            }
            context.Investigations.UpdateRange(cases2Assign);
            await context.SaveChangesAsync(null, false);

            var autoAllocatedTasks = cases2Assign.ToList().Select(u => timelineService.UpdateTaskStatus(u.Id, userEmail));

            await Task.WhenAll(autoAllocatedTasks);
        }
        public async Task<(string, string)> AllocateToVendor(string userEmail, long caseId, long vendorId, bool autoAllocated = true)
        {
            try
            {
                // Fetch vendor & user details
                var currentUser = await context.ApplicationUser
                    .Include(c => c.ClientCompany)
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                // Fetch case
                var caseTask = await context.Investigations
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.ReportTemplate)
                    .ThenInclude(c => c.LocationReport)
                    .ThenInclude(c => c.FaceIds)
                    .Include(c => c.ReportTemplate)
                    .ThenInclude(c => c.LocationReport)
                    .ThenInclude(c => c.DocumentIds)
                    .Include(c => c.ReportTemplate)
                    .ThenInclude(c => c.LocationReport)
                    .ThenInclude(c => c.Questions)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var vendor = await context.Vendor.FindAsync(vendorId);

                // Update case details
                caseTask.IsAutoAllocated = autoAllocated;
                caseTask.IsNew = true;
                caseTask.IsNewAssignedToAgency = true;
                caseTask.AssignedToAgency = true;
                caseTask.Updated = DateTime.Now;
                caseTask.AllocatedToAgencyTime = DateTime.Now;
                caseTask.UpdatedBy = currentUser.Email;
                caseTask.AiEnabled = currentUser.ClientCompany.AiEnabled;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;
                caseTask.Status = CONSTANTS.CASE_STATUS.INPROGRESS;
                caseTask.VendorId = vendorId;
                caseTask.CaseOwner = vendor.Email;
                caseTask.CreatorSla = currentUser.ClientCompany.CreatorSla;
                caseTask.AssessorSla = currentUser.ClientCompany.AssessorSla;
                caseTask.SupervisorSla = currentUser.ClientCompany.SupervisorSla;
                caseTask.AgentSla = currentUser.ClientCompany.AgentSla;
                caseTask.UpdateAgentAnswer = currentUser.ClientCompany.UpdateAgentAnswer;

                //REPORT TEMPLATE
                var investigationReport = new InvestigationReport
                {
                    ReportTemplateId = caseTask.ReportTemplateId,
                    ReportTemplate = caseTask.ReportTemplate, // Optional
                };

                // Save InvestigationReport
                context.InvestigationReport.Add(investigationReport);
                await context.SaveChangesAsync(null, false);

                // Link the InvestigationReport back to the InvestigationTask
                caseTask.InvestigationReportId = investigationReport.Id;
                caseTask.InvestigationReport = investigationReport;


                context.Investigations.Update(caseTask);
                // Save changes
                await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, currentUser.Email);

                return (caseTask.PolicyDetail.ContractNumber, caseTask.SubStatus);

            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }
        public async Task<(ClientCompany, long)> WithdrawCaseByCompany(string userEmail, CaseTransactionModel model, long caseId)
        {
            try
            {
                var currentUser = await context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
                var caseTask = await context.Investigations
                    .FirstOrDefaultAsync(c => c.Id == caseId);
                var vendorId = caseTask.VendorId;
                var company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);

                caseTask.IsNew = true;
                caseTask.Updated = DateTime.Now;
                caseTask.UpdatedBy = currentUser.Email;
                caseTask.AssignedToAgency = false;
                caseTask.CaseOwner = company.Email;
                caseTask.VendorId = null;
                caseTask.Vendor = null;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY;
                context.Investigations.Update(caseTask);
                var rows = await context.SaveChangesAsync(null, false) > 0;

                await timelineService.UpdateTaskStatus(caseTask.Id, currentUser.Email);

                return rows ? (company, vendorId.GetValueOrDefault()) : (null, 0);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        public async Task<Vendor> WithdrawCaseFromAgent(string userEmail, CaseTransactionModel model, long caseId)
        {
            try
            {
                var currentUser = await context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(u => u.Email == userEmail);
                var caseTask = await context.Investigations
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                caseTask.IsNewAssignedToAgency = true;
                caseTask.CaseOwner = currentUser.Vendor.Email;
                caseTask.TaskedAgentEmail = null;
                caseTask.Updated = DateTime.Now;
                caseTask.UpdatedBy = currentUser.Email;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;
                context.Investigations.Update(caseTask);
                var rows = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, currentUser.Email);

                return currentUser.Vendor;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        public async Task<Vendor> WithdrawCase(string userEmail, CaseTransactionModel model, long caseId)
        {
            try
            {
                var currentUser = await context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(u => u.Email == userEmail);
                var caseTask = await context.Investigations
                    .FirstOrDefaultAsync(c => c.Id == caseId);
                var company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);
                caseTask.CaseOwner = company.Email;
                caseTask.IsAutoAllocated = false;
                caseTask.IsNew = true;
                caseTask.IsNewAssignedToAgency = true;
                caseTask.IsNewSubmittedToAgent = true;
                caseTask.Updated = DateTime.Now;
                caseTask.UpdatedBy = currentUser.Email;
                caseTask.AssignedToAgency = false;
                caseTask.VendorId = null;
                caseTask.Vendor = null;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY;
                context.Investigations.Update(caseTask);
                var rows = await context.SaveChangesAsync(null, false);
                await timelineService.UpdateTaskStatus(caseTask.Id, currentUser.Email);
                return currentUser.Vendor;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
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

        public async Task<InvestigationTask> ProcessAgentReport(string userEmail, string supervisorRemarks, long caseId, SupervisorRemarkType reportUpdateStatus, IFormFile? document = null, string editRemarks = "")
        {
            if (reportUpdateStatus == SupervisorRemarkType.OK)
            {
                return await ApproveAgentReport(userEmail, caseId, supervisorRemarks, reportUpdateStatus, document, editRemarks);
            }
            else
            {
                //PUT th case back in review list :: Assign back to Agent
                return await ReAllocateToVendorAgent(userEmail, caseId, supervisorRemarks, reportUpdateStatus);
            }
        }

        private async Task<InvestigationTask> ApproveAgentReport(string userEmail, long claimsInvestigationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus, IFormFile? document = null, string editRemarks = "")
        {
            try
            {
                var caseTask = await context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.InvestigationReport)
                .Include(c => c.Vendor)
                .Include(c => c.ClientCompany)
                .FirstOrDefaultAsync(c => c.Id == claimsInvestigationId);

                var submitted2Assessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR;
                caseTask.SubmittingSupervisordEmail = userEmail;
                caseTask.SubmittedToAssessorTime = DateTime.Now;
                caseTask.AssignedToAgency = false;
                caseTask.Updated = DateTime.Now;
                caseTask.UpdatedBy = userEmail;
                caseTask.SubStatus = submitted2Assessor;
                caseTask.CaseOwner = caseTask.ClientCompany.Email;
                caseTask.SubmittedToAssessorTime = DateTime.Now;
                var report = caseTask.InvestigationReport;
                var edited = report.AgentRemarks.Trim() != editRemarks.Trim();
                if (edited)
                {
                    report.AgentRemarksEdit = editRemarks;
                    report.AgentRemarksEditUpdated = DateTime.Now;
                }

                report.SupervisorRemarkType = reportUpdateStatus;
                report.SupervisorRemarks = supervisorRemarks;
                report.SupervisorRemarksUpdated = DateTime.Now;
                report.SupervisorEmail = userEmail;

                if (document is not null)
                {
                    using var dataStream = new MemoryStream();
                    document.CopyTo(dataStream);
                    report.SupervisorAttachment = dataStream.ToArray();
                    report.SupervisorFileName = Path.GetFileName(document.FileName);
                    report.SupervisorFileExtension = Path.GetExtension(document.FileName);
                    report.SupervisorFileType = document.ContentType;
                }

                context.Investigations.Update(caseTask);
                var rowsAffected = await context.SaveChangesAsync(null, false) > 0;

                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                return rowsAffected ? caseTask : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        private async Task<InvestigationTask> ReAllocateToVendorAgent(string userEmail, long caseId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus)
        {
            try
            {

                var agencyUser = await context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(s => s.Email == userEmail);

                var assignedToAgentSubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
                var caseToAllocateToVendor = await context.Investigations
                    .Include(c => c.InvestigationReport)
                    .Include(c => c.PolicyDetail)
                    .Include(p => p.ClientCompany)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                //var report = claimsCaseToAllocateToVendor.InvestigationReport;
                //report.SupervisorRemarkType = reportUpdateStatus;
                //report.SupervisorRemarks = supervisorRemarks;
                caseToAllocateToVendor.CaseOwner = agencyUser.Email;
                caseToAllocateToVendor.TaskedAgentEmail = agencyUser.Email;
                caseToAllocateToVendor.Updated = DateTime.Now;
                caseToAllocateToVendor.UpdatedBy = userEmail;
                caseToAllocateToVendor.SubStatus = assignedToAgentSubStatus;
                caseToAllocateToVendor.TaskToAgentTime = DateTime.Now;
                context.Investigations.Update(caseToAllocateToVendor);

                var rowsAffected = await context.SaveChangesAsync(null, false) > 0;

                await timelineService.UpdateTaskStatus(caseToAllocateToVendor.Id, userEmail);
                return rowsAffected ? caseToAllocateToVendor : null;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        public async Task<InvestigationTask> SubmitQueryReplyToCompany(string userEmail, long caseId, EnquiryRequest request, List<EnquiryRequest> requests, IFormFile document)
        {
            try
            {
                var caseTask = await context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(p => p.ClientCompany)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.EnquiryRequest)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.EnquiryRequests)
                .Include(c => c.Vendor)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                var replyByAgency = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR;
                caseTask.CaseOwner = caseTask.ClientCompany.Email;
                caseTask.SubStatus = replyByAgency;
                caseTask.UpdatedBy = userEmail;
                caseTask.AssignedToAgency = false;
                caseTask.EnquiryReplyByAssessorTime = DateTime.Now;
                caseTask.SubmittedToAssessorTime = DateTime.Now;
                var enquiryRequest = caseTask.InvestigationReport.EnquiryRequest;
                enquiryRequest.DescriptiveAnswer = request.DescriptiveAnswer;

                enquiryRequest.Updated = DateTime.Now;
                enquiryRequest.UpdatedBy = userEmail;

                if (document != null)
                {
                    using var ms = new MemoryStream();
                    document.CopyTo(ms);
                    enquiryRequest.AnswerImageAttachment = ms.ToArray();
                    enquiryRequest.AnswerImageFileName = Path.GetFileName(document.FileName);
                    enquiryRequest.AnswerImageFileExtension = Path.GetExtension(document.FileName);
                    enquiryRequest.AnswerImageFileType = document.ContentType;
                }

                context.QueryRequest.Update(enquiryRequest);
                foreach (var enquiry in requests)
                {
                    var dbEnquiry = caseTask.InvestigationReport.EnquiryRequests
                        .FirstOrDefault(e => e.QueryRequestId == enquiry.QueryRequestId);

                    if (dbEnquiry != null)
                    {
                        dbEnquiry.AnswerSelected = enquiry.AnswerSelected;
                        dbEnquiry.Updated = DateTime.Now;
                        dbEnquiry.UpdatedBy = userEmail;
                    }
                }

                context.Investigations.Update(caseTask);
                var rowsUpdated = await context.SaveChangesAsync(null, false) > 0;
                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                return rowsUpdated ? caseTask : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        public async Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType reportUpdateStatus, string reportAiSummary)
        {
            if (reportUpdateStatus == AssessorRemarkType.OK)
            {
                return await ApproveCaseReport(userEmail, assessorRemarks, caseId, reportUpdateStatus, reportAiSummary);
            }
            else if (reportUpdateStatus == AssessorRemarkType.REJECT)
            {
                //PUT th case back in review list :: Assign back to Agent
                return await RejectCaseReport(userEmail, assessorRemarks, caseId, reportUpdateStatus, reportAiSummary);
            }
            else
            {
                return (null!, string.Empty);
            }
        }

        private async Task<(ClientCompany, string)> RejectCaseReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            var rejected = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
            var finished = CONSTANTS.CASE_STATUS.FINISHED;

            try
            {
                var caseTask = await context.Investigations
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.InvestigationReport)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                caseTask.InvestigationReport.AiSummary = reportAiSummary;
                caseTask.InvestigationReport.AssessorRemarkType = assessorRemarkType;
                caseTask.InvestigationReport.AssessorRemarks = assessorRemarks;
                caseTask.InvestigationReport.AssessorRemarksUpdated = DateTime.Now;
                caseTask.InvestigationReport.AssessorEmail = userEmail;

                caseTask.Status = finished;
                caseTask.SubStatus = rejected;
                caseTask.Updated = DateTime.Now;
                caseTask.UpdatedBy = userEmail;
                caseTask.ProcessedByAssessorTime = DateTime.Now;
                caseTask.SubmittedAssessordEmail = userEmail;
                caseTask.CaseOwner = caseTask.ClientCompany.Email;
                context.Investigations.Update(caseTask);

                var saveCount = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                backgroundJobClient.Enqueue(() => pdfGenerativeService.Generate(caseId, userEmail));

                var currentUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
                return saveCount > 0 ? (currentUser.ClientCompany, caseTask.PolicyDetail.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        private async Task<(ClientCompany, string)> ApproveCaseReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            try
            {
                var approved = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                var finished = CONSTANTS.CASE_STATUS.FINISHED;

                var caseTask = await context.Investigations
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.InvestigationReport)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                caseTask.InvestigationReport.AiSummary = reportAiSummary;
                caseTask.InvestigationReport.AssessorRemarkType = assessorRemarkType;
                caseTask.InvestigationReport.AssessorRemarks = assessorRemarks;
                caseTask.InvestigationReport.AssessorRemarksUpdated = DateTime.Now;
                caseTask.InvestigationReport.AssessorEmail = userEmail;

                caseTask.Status = finished;
                caseTask.SubStatus = approved;
                caseTask.Updated = DateTime.Now;
                caseTask.UpdatedBy = userEmail;
                caseTask.CaseOwner = caseTask.ClientCompany.Email;
                caseTask.ProcessedByAssessorTime = DateTime.Now;
                caseTask.SubmittedAssessordEmail = userEmail;
                context.Investigations.Update(caseTask);

                var saveCount = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                backgroundJobClient.Enqueue(() => pdfGenerativeService.Generate(caseId, userEmail));

                return saveCount > 0 ? (caseTask.ClientCompany, caseTask.PolicyDetail.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        public async Task<InvestigationTask> SubmitQueryToAgency(string userEmail, long caseId, EnquiryRequest request, List<EnquiryRequest> requests, IFormFile document)
        {
            try
            {
                var caseTask = await context.Investigations
                .Include(c => c.Vendor)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.EnquiryRequest)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.EnquiryRequests)
                .Include(c => c.Vendor)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                var requestedByAssessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;

                caseTask.SubStatus = requestedByAssessor;
                caseTask.UpdatedBy = userEmail;
                caseTask.CaseOwner = caseTask.Vendor.Email;
                caseTask.RequestedAssessordEmail = userEmail;
                caseTask.AssignedToAgency = true;
                caseTask.IsQueryCase = true;
                if (document != null)
                {
                    using var ms = new MemoryStream();
                    document.CopyTo(ms);
                    request.QuestionImageAttachment = ms.ToArray();
                    request.QuestionImageFileName = Path.GetFileName(document.FileName);
                    request.QuestionImageFileExtension = Path.GetExtension(document.FileName);
                    request.QuestionImageFileType = document.ContentType;
                }
                caseTask.InvestigationReport.EnquiryRequest = request;
                caseTask.InvestigationReport.EnquiryRequests = requests;
                caseTask.InvestigationReport.Updated = DateTime.Now;
                caseTask.InvestigationReport.UpdatedBy = userEmail;
                caseTask.InvestigationReport.EnquiryRequest.Updated = DateTime.Now;
                caseTask.InvestigationReport.EnquiryRequest.UpdatedBy = userEmail;
                caseTask.EnquiredByAssessorTime = DateTime.Now;
                context.QueryRequest.Update(request);
                context.Investigations.Update(caseTask);

                var saved = await context.SaveChangesAsync(null, false) > 0;

                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                return saved ? caseTask : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        public async Task<bool> SubmitNotes(string userEmail, long caseId, string notes)
        {
            var caseTask = await context.Investigations
               .Include(c => c.CaseNotes)
               .FirstOrDefaultAsync(c => c.Id == caseId);
            caseTask.CaseNotes.Add(new CaseNote
            {
                Comment = notes,
                SenderEmail = userEmail,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                UpdatedBy = userEmail
            });
            context.Investigations.Update(caseTask);
            return await context.SaveChangesAsync(null, false) > 0;
        }
    }
}
