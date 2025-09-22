using Hangfire;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IProcessCaseService
    {
        Task<int> UpdateCaseAllocationStatus(string userEmail, List<long> claimsInvestigations);
        Task BackgroundAutoAllocation(List<long> claims, string userEmail, string url = "");
        Task<List<long>> BackgroundUploadAutoAllocation(List<long> claimIds, string userEmail, string url = "");

        Task<string> ProcessAutoSingleAllocation(long claim, string userEmail, string url = "");
        Task<(string, string)> AllocateToVendor(string userEmail, long claimsInvestigationId, long vendorId, bool autoAllocated = true);
        Task<Vendor> WithdrawCase(string userEmail, CaseTransactionModel model, long claimId);
        Task<(ClientCompany, long)> WithdrawCaseByCompany(string userEmail, CaseTransactionModel model, long claimId);
        Task<Vendor> WithdrawCaseFromAgent(string userEmail, CaseTransactionModel model, long claimId);

        Task<InvestigationTask> SubmitQueryReplyToCompany(string userEmail, long claimId, EnquiryRequest request, IFormFile messageDocument, List<string> flexRadioDefault);
        Task<InvestigationTask> ProcessAgentReport(string userEmail, string supervisorRemarks, long claimsInvestigationId, SupervisorRemarkType reportUpdateStatus, IFormFile? claimDocument = null, string editRemarks = "");

        Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, long claimsInvestigationId, AssessorRemarkType reportUpdateStatus, string reportAiSummary);
        Task<InvestigationTask> SubmitQueryToAgency(string userEmail, long claimId, EnquiryRequest request, List<EnquiryRequest> requests, IFormFile messageDocument);
        List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors);
        Task<bool> SubmitNotes(string userEmail, long claimId, string notes);
    }
    public class ProcessCaseService : IProcessCaseService
    {
        private readonly ApplicationDbContext context;
        private readonly IPdfGenerativeService pdfGenerativeService;
        private readonly IMailService mailboxService;
        private readonly ITimelineService timelineService;
        private readonly IBackgroundJobClient backgroundJobClient;

        public ProcessCaseService(ApplicationDbContext context,
            IPdfGenerativeService pdfGenerativeService,
            IMailService mailboxService,
            ITimelineService timelineService,
            IBackgroundJobClient backgroundJobClient)
        {
            this.context = context;
            this.pdfGenerativeService = pdfGenerativeService;
            this.mailboxService = mailboxService;
            this.timelineService = timelineService;
            this.backgroundJobClient = backgroundJobClient;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task<List<long>> BackgroundUploadAutoAllocation(List<long> claimIds, string userEmail, string url = "")
        {
            var autoAllocatedCases = await DoAutoAllocation(claimIds, userEmail, url); // Run all tasks in parallel

            var notAutoAllocated = claimIds.Except(autoAllocatedCases)?.ToList();

            if (claimIds.Count > autoAllocatedCases.Count)
            {
                await AssignToAssigner(userEmail, notAutoAllocated, url);

            }
            var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAssignmentToAssigner(userEmail, autoAllocatedCases, notAutoAllocated, url));

            return (autoAllocatedCases);
        }
        public async Task<int> UpdateCaseAllocationStatus(string userEmail, List<long> claimsInvestigations)
        {
            try
            {
                if (claimsInvestigations == null || !claimsInvestigations.Any())
                    return 0; // No cases to update

                // Fetch all matching cases in one query
                var cases = await context.Investigations
                    .Where(v => claimsInvestigations.Contains(v.Id))
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
                // Log the error properly instead of just rethrowing
                Console.WriteLine("Error updating case allocation status", ex);
                throw;
            }
        }
        [AutomaticRetry(Attempts = 0)]
        public async Task BackgroundAutoAllocation(List<long> claimIds, string userEmail, string url = "")
        {
            var autoAllocatedCases = await DoAutoAllocation(claimIds, userEmail, url); // Run all tasks in parallel

            var notAutoAllocated = claimIds.Except(autoAllocatedCases)?.ToList();

            if (claimIds.Count > autoAllocatedCases.Count)
            {
                await AssignToAssigner(userEmail, notAutoAllocated, url);

            }
            var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAssignmentToAssigner(userEmail, autoAllocatedCases, notAutoAllocated, url));
        }
        async Task<List<long>> DoAutoAllocation(List<long> claims, string userEmail, string url = "")
        {
            var companyUser = context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);

            var company = context.ClientCompany
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var claimTasks = claims.Select(async claim =>
            {
                // 1. Fetch Claim Details & Pincode in Parallel
                var claimsInvestigation = await context.Investigations
                    .AsNoTracking()
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                        .ThenInclude(c => c.PinCode)
                    .FirstOrDefaultAsync(c => c.Id == claim);

                if (claimsInvestigation == null || !claimsInvestigation.IsValidCaseData()) return 0; // Handle missing claim

                string pinCode2Verify = claimsInvestigation.PolicyDetail?.InsuranceType == InsuranceType.UNDERWRITING
                    ? claimsInvestigation.CustomerDetail?.PinCode?.Code
                    : claimsInvestigation.BeneficiaryDetail?.PinCode?.Code;

                var pincodeDistrictState = await context.PinCode
                    .AsNoTracking()
                    .Include(d => d.District)
                    .Include(s => s.State)
                    .FirstOrDefaultAsync(p => p.Code == pinCode2Verify);

                // 2. Find Vendors Using LINQ
                var distinctVendorIds = company.EmpanelledVendors
                    .Where(vendor => vendor.VendorInvestigationServiceTypes.Any(serviceType =>
                        serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
                        serviceType.InsuranceType == claimsInvestigation.PolicyDetail.InsuranceType &&
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

                var (policy, status) = await AllocateToVendor(userEmail, claimsInvestigation.Id, selectedVendorId.VendorId);

                if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    return 0;
                }
                var jobId = backgroundJobClient.Enqueue(() =>
                    mailboxService.NotifyClaimAllocationToVendor(userEmail, policy, claimsInvestigation.Id, selectedVendorId.VendorId, url));

                return claim; // Return allocated claim
            });

            var results = await Task.WhenAll(claimTasks); // Run all tasks in parallel
            return results.Where(r => r != null && r != 0).ToList(); // Remove nulls and return allocated claims
        }
        public async Task<string> ProcessAutoSingleAllocation(long claim, string userEmail, string url = "")
        {
            var companyUser = context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);

            var company = context.ClientCompany
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            // 1. Fetch Claim Details & Pincode in Parallel
            var claimsInvestigation = await context.Investigations
                .AsNoTracking()
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.PinCode)
            .FirstOrDefaultAsync(c => c.Id == claim);

            string pinCode2Verify = claimsInvestigation.PolicyDetail?.InsuranceType == InsuranceType.UNDERWRITING
                ? claimsInvestigation.CustomerDetail?.PinCode?.Code
                : claimsInvestigation.BeneficiaryDetail?.PinCode?.Code;

            var pincodeDistrictState = await context.PinCode
                .AsNoTracking()
                .Include(d => d.District)
                .Include(s => s.State)
                .FirstOrDefaultAsync(p => p.Code == pinCode2Verify);

            // 2. Find Vendors Using LINQ
            var distinctVendorIds = company.EmpanelledVendors
                .Where(vendor => vendor.VendorInvestigationServiceTypes.Any(serviceType =>
                    serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
                    serviceType.InsuranceType == claimsInvestigation.PolicyDetail.InsuranceType &&
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

            var (policy, status) = await AllocateToVendor(userEmail, claimsInvestigation.Id, selectedVendorId.VendorId);

            if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
            {
                await AssignToAssigner(userEmail, new List<long> { claim });
                await mailboxService.NotifyClaimAssignmentToAssigner(userEmail, new List<long> { claim }, url);
                return null;
            }

            // 4. Send Notification
            var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAllocationToVendor(userEmail, policy, claimsInvestigation.Id, selectedVendorId.VendorId, url));

            return claimsInvestigation.PolicyDetail.ContractNumber; // Return allocated claim
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
            var currentUser = context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var assigned = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;

            foreach (var claimsInvestigation in cases2Assign)
            {
                claimsInvestigation.CaseOwner = currentUser.Email;
                claimsInvestigation.IsNew = true;
                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UpdatedBy = currentUser.Email;
                claimsInvestigation.AssignedToAgency = false;
                claimsInvestigation.IsReady2Assign = claimsInvestigation.IsValidCaseData() ? true : false;
                claimsInvestigation.VendorId = null;
                claimsInvestigation.SubStatus = assigned;
            }
            context.Investigations.UpdateRange(cases2Assign);
            await context.SaveChangesAsync(null, false);

            var autoAllocatedTasks = cases2Assign.ToList().Select(u => timelineService.UpdateTaskStatus(u.Id, userEmail));

            await Task.WhenAll(autoAllocatedTasks);
        }
        public async Task<(string, string)> AllocateToVendor(string userEmail, long claimsInvestigationId, long vendorId, bool autoAllocated = true)
        {
            try
            {
                // Fetch vendor & user details
                var currentUser = await context.ClientCompanyApplicationUser
                    .Include(c => c.ClientCompany)
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                // Fetch case
                var claimsCase = await context.Investigations
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.ReportTemplate)
                    .ThenInclude(c => c.LocationTemplate)
                    .ThenInclude(c => c.FaceIds)
                    .Include(c => c.ReportTemplate)
                    .ThenInclude(c => c.LocationTemplate)
                    .ThenInclude(c => c.DocumentIds)
                    .Include(c => c.ReportTemplate)
                    .ThenInclude(c => c.LocationTemplate)
                    .ThenInclude(c => c.Questions)
                    .FirstOrDefaultAsync(v => v.Id == claimsInvestigationId);

                var vendor = await context.Vendor.FindAsync(vendorId);

                // Update case details
                claimsCase.IsAutoAllocated = autoAllocated;
                claimsCase.IsNew = true;
                claimsCase.IsNewAssignedToAgency = true;
                claimsCase.AssignedToAgency = true;
                claimsCase.Updated = DateTime.Now;
                claimsCase.AllocatedToAgencyTime = DateTime.Now;
                claimsCase.UpdatedBy = currentUser.Email;
                claimsCase.AiEnabled = currentUser.ClientCompany.AiEnabled;
                claimsCase.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;
                claimsCase.Status = CONSTANTS.CASE_STATUS.INPROGRESS;
                claimsCase.VendorId = vendorId;
                claimsCase.CaseOwner = vendor.Email;
                claimsCase.CreatorSla = currentUser.ClientCompany.CreatorSla;
                claimsCase.AssessorSla = currentUser.ClientCompany.AssessorSla;
                claimsCase.SupervisorSla = currentUser.ClientCompany.SupervisorSla;
                claimsCase.AgentSla = currentUser.ClientCompany.AgentSla;
                claimsCase.UpdateAgentAnswer = currentUser.ClientCompany.UpdateAgentAnswer;

                //REPORT TEMPLATE
                var investigationReport = new InvestigationReport
                {
                    ReportTemplateId = claimsCase.ReportTemplateId,
                    ReportTemplate = claimsCase.ReportTemplate, // Optional
                };

                // Save InvestigationReport
                context.InvestigationReport.Add(investigationReport);
                await context.SaveChangesAsync(null, false);

                // Link the InvestigationReport back to the InvestigationTask
                claimsCase.InvestigationReportId = investigationReport.Id;
                claimsCase.InvestigationReport = investigationReport;


                context.Investigations.Update(claimsCase);
                // Save changes
                await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(claimsCase.Id, currentUser.Email);

                return (claimsCase.PolicyDetail.ContractNumber, claimsCase.SubStatus);

            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        public async Task<(ClientCompany, long)> WithdrawCaseByCompany(string userEmail, CaseTransactionModel model, long claimId)
        {
            try
            {
                var currentUser = context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
                var claimsInvestigation = context.Investigations
                    .FirstOrDefault(c => c.Id == claimId);
                var vendorId = claimsInvestigation.VendorId;
                var company = context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                claimsInvestigation.IsNew = true;
                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UpdatedBy = currentUser.Email;
                claimsInvestigation.AssignedToAgency = false;
                claimsInvestigation.CaseOwner = company.Email;
                claimsInvestigation.VendorId = null;
                claimsInvestigation.Vendor = null;
                claimsInvestigation.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY;
                context.Investigations.Update(claimsInvestigation);
                var rows = await context.SaveChangesAsync(null, false) > 0;

                await timelineService.UpdateTaskStatus(claimsInvestigation.Id, currentUser.Email);

                return rows ? (company, vendorId.GetValueOrDefault()) : (null, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<Vendor> WithdrawCaseFromAgent(string userEmail, CaseTransactionModel model, long claimId)
        {
            try
            {
                var currentUser = context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(u => u.Email == userEmail);
                var claimsInvestigation = context.Investigations
                    .FirstOrDefault(c => c.Id == claimId);

                claimsInvestigation.IsNewAssignedToAgency = true;
                claimsInvestigation.CaseOwner = currentUser.Vendor.Email;
                claimsInvestigation.TaskedAgentEmail = null;
                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UpdatedBy = currentUser.Email;
                claimsInvestigation.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;
                context.Investigations.Update(claimsInvestigation);
                var rows = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(claimsInvestigation.Id, currentUser.Email);

                return currentUser.Vendor;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<Vendor> WithdrawCase(string userEmail, CaseTransactionModel model, long claimId)
        {
            try
            {
                var currentUser = context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(u => u.Email == userEmail);
                var claimsInvestigation = context.Investigations
                    .FirstOrDefault(c => c.Id == claimId);
                var company = context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);
                claimsInvestigation.CaseOwner = company.Email;
                claimsInvestigation.IsAutoAllocated = false;
                claimsInvestigation.IsNew = true;
                claimsInvestigation.IsNewAssignedToAgency = true;
                claimsInvestigation.IsNewSubmittedToAgent = true;
                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UpdatedBy = currentUser.Email;
                claimsInvestigation.AssignedToAgency = false;
                claimsInvestigation.VendorId = null;
                claimsInvestigation.Vendor = null;
                claimsInvestigation.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY;
                context.Investigations.Update(claimsInvestigation);
                var rows = await context.SaveChangesAsync(null, false);
                await timelineService.UpdateTaskStatus(claimsInvestigation.Id, currentUser.Email);
                return currentUser.Vendor;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
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

        public async Task<InvestigationTask> ProcessAgentReport(string userEmail, string supervisorRemarks, long claimsInvestigationId, SupervisorRemarkType reportUpdateStatus, IFormFile? claimDocument = null, string editRemarks = "")
        {
            if (reportUpdateStatus == SupervisorRemarkType.OK)
            {
                return await ApproveAgentReport(userEmail, claimsInvestigationId, supervisorRemarks, reportUpdateStatus, claimDocument, editRemarks);
            }
            else
            {
                //PUT th case back in review list :: Assign back to Agent
                return await ReAllocateToVendorAgent(userEmail, claimsInvestigationId, supervisorRemarks, reportUpdateStatus);
            }
        }

        private async Task<InvestigationTask> ApproveAgentReport(string userEmail, long claimsInvestigationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus, IFormFile? claimDocument = null, string editRemarks = "")
        {
            try
            {
                var claim = context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.InvestigationReport)
                .Include(c => c.Vendor)
                .Include(c => c.ClientCompany)
                .FirstOrDefault(c => c.Id == claimsInvestigationId);

                var submitted2Assessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR;
                claim.SubmittingSupervisordEmail = userEmail;
                claim.SubmittedToAssessorTime = DateTime.Now;
                claim.AssignedToAgency = false;
                claim.Updated = DateTime.Now;
                claim.UpdatedBy = userEmail;
                claim.SubStatus = submitted2Assessor;
                claim.CaseOwner = claim.ClientCompany.Email;
                claim.SubmittedToAssessorTime = DateTime.Now;
                var report = claim.InvestigationReport;
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

                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    report.SupervisorAttachment = dataStream.ToArray();
                    report.SupervisorFileName = Path.GetFileName(claimDocument.FileName);
                    report.SupervisorFileExtension = Path.GetExtension(claimDocument.FileName);
                    report.SupervisorFileType = claimDocument.ContentType;
                }

                context.Investigations.Update(claim);
                var rowsAffected = await context.SaveChangesAsync(null, false) > 0;

                await timelineService.UpdateTaskStatus(claim.Id, userEmail);

                return rowsAffected ? claim : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private async Task<InvestigationTask> ReAllocateToVendorAgent(string userEmail, long claimsInvestigationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus)
        {
            try
            {

                var agencyUser = context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(s => s.Email == userEmail);

                var assignedToAgentSubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
                var claimsCaseToAllocateToVendor = context.Investigations
                    .Include(c => c.InvestigationReport)
                    .Include(c => c.PolicyDetail)
                    .Include(p => p.ClientCompany)
                    .FirstOrDefault(v => v.Id == claimsInvestigationId);

                //var report = claimsCaseToAllocateToVendor.InvestigationReport;
                //report.SupervisorRemarkType = reportUpdateStatus;
                //report.SupervisorRemarks = supervisorRemarks;
                claimsCaseToAllocateToVendor.CaseOwner = agencyUser.Email;
                claimsCaseToAllocateToVendor.TaskedAgentEmail = agencyUser.Email;
                claimsCaseToAllocateToVendor.Updated = DateTime.Now;
                claimsCaseToAllocateToVendor.UpdatedBy = userEmail;
                claimsCaseToAllocateToVendor.SubStatus = assignedToAgentSubStatus;
                claimsCaseToAllocateToVendor.TaskToAgentTime = DateTime.Now;
                context.Investigations.Update(claimsCaseToAllocateToVendor);

                var rowsAffected = await context.SaveChangesAsync(null, false) > 0;

                await timelineService.UpdateTaskStatus(claimsCaseToAllocateToVendor.Id, userEmail);
                return rowsAffected ? claimsCaseToAllocateToVendor : null;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<InvestigationTask> SubmitQueryReplyToCompany(string userEmail, long claimId, EnquiryRequest request, IFormFile messageDocument, List<string> flexRadioDefault)
        {
            try
            {
                var claim = context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(p => p.ClientCompany)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.EnquiryRequest)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.EnquiryRequests)
                .Include(c => c.Vendor)
                .FirstOrDefault(c => c.Id == claimId);

                var replyByAgency = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR;
                claim.CaseOwner = claim.ClientCompany.Email;
                claim.SubStatus = replyByAgency;
                claim.UpdatedBy = userEmail;
                claim.AssignedToAgency = false;
                claim.EnquiryReplyByAssessorTime = DateTime.Now;
                claim.SubmittedToAssessorTime = DateTime.Now;
                var enquiryRequest = claim.InvestigationReport.EnquiryRequest;
                enquiryRequest.Answer = request.Answer;
                if (flexRadioDefault[0] == "a")
                {
                    enquiryRequest.AnswerSelected = enquiryRequest.AnswerA;
                }
                else if (flexRadioDefault[0] == "b")
                {
                    enquiryRequest.AnswerSelected = enquiryRequest.AnswerB;
                }
                else if (flexRadioDefault[0] == "c")
                {
                    enquiryRequest.AnswerSelected = enquiryRequest.AnswerC;
                }

                else if (flexRadioDefault[0] == "d")
                {
                    enquiryRequest.AnswerSelected = enquiryRequest.AnswerD;
                }

                enquiryRequest.Updated = DateTime.Now;
                enquiryRequest.UpdatedBy = userEmail;

                if (messageDocument != null)
                {
                    using var ms = new MemoryStream();
                    messageDocument.CopyTo(ms);
                    enquiryRequest.AnswerImageAttachment = ms.ToArray();
                    enquiryRequest.AnswerImageFileName = Path.GetFileName(messageDocument.FileName);
                    enquiryRequest.AnswerImageFileExtension = Path.GetExtension(messageDocument.FileName);
                    enquiryRequest.AnswerImageFileType = messageDocument.ContentType;
                }

                claim.InvestigationReport.EnquiryRequests.Add(enquiryRequest);

                context.QueryRequest.Update(enquiryRequest);
                claim.InvestigationReport.EnquiryRequests.Add(enquiryRequest);
                context.Investigations.Update(claim);
                var rowsUpdated = await context.SaveChangesAsync(null, false) > 0;
                await timelineService.UpdateTaskStatus(claim.Id, userEmail);

                return rowsUpdated ? claim : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, long claimsInvestigationId, AssessorRemarkType reportUpdateStatus, string reportAiSummary)
        {
            if (reportUpdateStatus == AssessorRemarkType.OK)
            {
                return await ApproveCaseReport(userEmail, assessorRemarks, claimsInvestigationId, reportUpdateStatus, reportAiSummary);
            }
            else if (reportUpdateStatus == AssessorRemarkType.REJECT)
            {
                //PUT th case back in review list :: Assign back to Agent
                return await RejectCaseReport(userEmail, assessorRemarks, claimsInvestigationId, reportUpdateStatus, reportAiSummary);
            }
            else
            {
                return (null!, string.Empty);
            }
        }

        private async Task<(ClientCompany, string)> RejectCaseReport(string userEmail, string assessorRemarks, long claimsInvestigationId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            var rejected = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
            var finished = CONSTANTS.CASE_STATUS.FINISHED;

            try
            {
                var claim = context.Investigations
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.InvestigationReport)
                .FirstOrDefault(c => c.Id == claimsInvestigationId);

                claim.InvestigationReport.AiSummary = reportAiSummary;
                claim.InvestigationReport.AssessorRemarkType = assessorRemarkType;
                claim.InvestigationReport.AssessorRemarks = assessorRemarks;
                claim.InvestigationReport.AssessorRemarksUpdated = DateTime.Now;
                claim.InvestigationReport.AssessorEmail = userEmail;

                claim.Status = finished;
                claim.SubStatus = rejected;
                claim.Updated = DateTime.Now;
                claim.UpdatedBy = userEmail;
                claim.ProcessedByAssessorTime = DateTime.Now;
                claim.SubmittedAssessordEmail = userEmail;
                claim.CaseOwner = claim.ClientCompany.Email;
                context.Investigations.Update(claim);

                var saveCount = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(claim.Id, userEmail);

                backgroundJobClient.Enqueue(() => pdfGenerativeService.Generate(claimsInvestigationId, userEmail));

                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                return saveCount > 0 ? (currentUser.ClientCompany, claim.PolicyDetail.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return (null!, string.Empty);
        }

        private async Task<(ClientCompany, string)> ApproveCaseReport(string userEmail, string assessorRemarks, long claimsInvestigationId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {

            try
            {
                var approved = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                var finished = CONSTANTS.CASE_STATUS.FINISHED;

                var claim = context.Investigations
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.InvestigationReport)
                .FirstOrDefault(c => c.Id == claimsInvestigationId);

                claim.InvestigationReport.AiSummary = reportAiSummary;
                claim.InvestigationReport.AssessorRemarkType = assessorRemarkType;
                claim.InvestigationReport.AssessorRemarks = assessorRemarks;
                claim.InvestigationReport.AssessorRemarksUpdated = DateTime.Now;
                claim.InvestigationReport.AssessorEmail = userEmail;

                claim.Status = finished;
                claim.SubStatus = approved;
                claim.Updated = DateTime.Now;
                claim.UpdatedBy = userEmail;
                claim.CaseOwner = claim.ClientCompany.Email;
                claim.ProcessedByAssessorTime = DateTime.Now;
                claim.SubmittedAssessordEmail = userEmail;
                context.Investigations.Update(claim);

                var saveCount = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(claim.Id, userEmail);

                backgroundJobClient.Enqueue(() => pdfGenerativeService.Generate(claimsInvestigationId, userEmail));

                return saveCount > 0 ? (claim.ClientCompany, claim.PolicyDetail.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return (null!, string.Empty);
        }

        public async Task<InvestigationTask> SubmitQueryToAgency(string userEmail, long claimId, EnquiryRequest request, List<EnquiryRequest> requests, IFormFile messageDocument)
        {

            try
            {
                var claim = context.Investigations
                .Include(c => c.Vendor)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.EnquiryRequest)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.EnquiryRequests)
                .Include(c => c.Vendor)
                .FirstOrDefault(c => c.Id == claimId);

                var requestedByAssessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;

                claim.SubStatus = requestedByAssessor;
                claim.UpdatedBy = userEmail;
                claim.CaseOwner = claim.Vendor.Email;
                claim.RequestedAssessordEmail = userEmail;
                claim.AssignedToAgency = true;
                claim.IsQueryCase = true;
                if (messageDocument != null)
                {
                    using var ms = new MemoryStream();
                    messageDocument.CopyTo(ms);
                    request.QuestionImageAttachment = ms.ToArray();
                    request.QuestionImageFileName = Path.GetFileName(messageDocument.FileName);
                    request.QuestionImageFileExtension = Path.GetExtension(messageDocument.FileName);
                    request.QuestionImageFileType = messageDocument.ContentType;
                }
                claim.InvestigationReport.EnquiryRequest = request;
                claim.InvestigationReport.EnquiryRequests = requests;
                claim.InvestigationReport.Updated = DateTime.Now;
                claim.InvestigationReport.UpdatedBy = userEmail;
                claim.InvestigationReport.EnquiryRequest.Updated = DateTime.Now;
                claim.InvestigationReport.EnquiryRequest.UpdatedBy = userEmail;
                claim.EnquiredByAssessorTime = DateTime.Now;
                context.QueryRequest.Update(request);
                context.Investigations.Update(claim);

                var saved = await context.SaveChangesAsync(null, false) > 0;

                await timelineService.UpdateTaskStatus(claim.Id, userEmail);

                return saved ? claim : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> SubmitNotes(string userEmail, long claimId, string notes)
        {
            var claim = context.Investigations
               .Include(c => c.CaseNotes)
               .FirstOrDefault(c => c.Id == claimId);
            claim.CaseNotes.Add(new CaseNote
            {
                Comment = notes,
                Sender = userEmail,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                UpdatedBy = userEmail
            });
            context.Investigations.Update(claim);
            return await context.SaveChangesAsync(null, false) > 0;
        }
    }
}
