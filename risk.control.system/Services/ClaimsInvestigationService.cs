using AspNetCoreHero.ToastNotification.Notyf;

using Hangfire;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Company;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Services
{
    public interface IClaimsInvestigationService
    {
        //Task<bool> SubmitNotes(string userEmail, string claimId, string notes);
        Task<bool> SubmitNotes(string userEmail, long claimId, string notes);
    }

    public class ClaimsInvestigationService : IClaimsInvestigationService
    {
        private const string CLAIMS = "claims";
        private const string UNDERWRITING = "underwriting";
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpContextAccessor accessor;
        private readonly IPdfReportService reportService;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IProgressService progressService;
        private readonly ICustomApiCLient customApiCLient;

        public ClaimsInvestigationService(ApplicationDbContext context,
            IHttpContextAccessor accessor,
            IPdfReportService reportService,
            IBackgroundJobClient backgroundJobClient,
            IProgressService progressService,
            ICustomApiCLient customApiCLient,
            IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            this.accessor = accessor;
            this.reportService = reportService;
            this.backgroundJobClient = backgroundJobClient;
            this.progressService = progressService;
            this.customApiCLient = customApiCLient;
            this.webHostEnvironment = webHostEnvironment;
        }
        //[AutomaticRetry(Attempts = 0)]
        //public async Task<List<string>> BackgroundUploadAutoAllocation(List<string> claimIds, string userEmail, string url = "")
        //{
        //    var autoAllocatedCases = await DoAutoAllocation(claimIds, userEmail, url); // Run all tasks in parallel

        //    var notAutoAllocated = claimIds.Except(autoAllocatedCases)?.ToList();

        //    if (claimIds.Count > autoAllocatedCases.Count)
        //    {
        //        await AssignToAssigner(userEmail, notAutoAllocated, url);

        //    }
        //    var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAssignmentToAssigner(userEmail, autoAllocatedCases, notAutoAllocated, url));

        //    return (autoAllocatedCases);
        //}
        //[AutomaticRetry(Attempts = 0)]
        //public async Task BackgroundAutoAllocation(List<string> claimIds, string userEmail, string url = "")
        //{
        //    var autoAllocatedCases = await DoAutoAllocation(claimIds, userEmail, url); // Run all tasks in parallel

        //    var notAutoAllocated = claimIds.Except(autoAllocatedCases)?.ToList();

        //    if (claimIds.Count > autoAllocatedCases.Count)
        //    {
        //        await AssignToAssigner(userEmail, notAutoAllocated, url);

        //    }
        //    var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAssignmentToAssigner(userEmail, autoAllocatedCases, notAutoAllocated, url));
        //}
        //async Task<List<string>> DoAutoAllocation(List<string> claims, string userEmail, string url = "")
        //{
        //    var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
        //    var uploadedRecordsCount = 0;

        //    var company = _context.ClientCompany
        //            .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
        //            .ThenInclude(e => e.VendorInvestigationServiceTypes)
        //            .ThenInclude(v => v.District)
        //            .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
        //    var claimTasks = claims.Select(async claim =>
        //    {
        //        int progress = (int)(((uploadedRecordsCount + 1) / (double)claims.Count) * 100);
        //        progressService.UpdateAssignmentProgress(claim, progress);

        //        // 1. Fetch Claim Details & Pincode in Parallel
        //        var claimsInvestigation = await _context.ClaimsInvestigation
        //            .AsNoTracking()
        //            .Include(c => c.PolicyDetail)
        //            .ThenInclude(c => c.LineOfBusiness)
        //            .Include(c => c.CustomerDetail)
        //            .ThenInclude(c => c.PinCode)
        //            .Include(c => c.BeneficiaryDetail)
        //                .ThenInclude(c => c.PinCode)
        //            .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == claim);

        //        if (claimsInvestigation == null || !claimsInvestigation.IsValidCaseData()) return null; // Handle missing claim

        //        string pinCode2Verify = claimsInvestigation.PolicyDetail?.LineOfBusiness.Name.ToLower() == UNDERWRITING
        //            ? claimsInvestigation.CustomerDetail?.PinCode?.Code
        //            : claimsInvestigation.BeneficiaryDetail?.PinCode?.Code;

        //        var pincodeDistrictState = await _context.PinCode
        //            .AsNoTracking()
        //            .Include(d => d.District)
        //            .Include(s => s.State)
        //            .FirstOrDefaultAsync(p => p.Code == pinCode2Verify);

        //        // 2. Find Vendors Using LINQ
        //        var distinctVendorIds = company.EmpanelledVendors
        //            .Where(vendor => vendor.VendorInvestigationServiceTypes.Any(serviceType =>
        //                serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
        //                serviceType.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId &&
        //                (serviceType.StateId == pincodeDistrictState.StateId &&
        //                 (serviceType.DistrictId == null || serviceType.DistrictId == pincodeDistrictState.DistrictId))
        //            ))
        //            .Select(v => v.VendorId) // Select only VendorId
        //            .Distinct() // Ensure uniqueness
        //            .ToList();

        //        if (!distinctVendorIds.Any()) return null; // No vendors found, skip this claim

        //        // 3. Get Vendor Load & Allocate
        //        var vendorsWithCaseLoad = GetAgencyIdsLoad(distinctVendorIds)
        //            .OrderBy(o => o.CaseCount)
        //            .ToList();

        //        var selectedVendorId = vendorsWithCaseLoad.FirstOrDefault();
        //        if (selectedVendorId == null) return null; // No vendors available

        //        var (policy, status) = await AllocateToVendor(userEmail, claimsInvestigation.ClaimsInvestigationId, selectedVendorId.VendorId);

        //        if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
        //        {
        //            return null;
        //        }
        //        var jobId = backgroundJobClient.Enqueue(() =>
        //            mailboxService.NotifyClaimAllocationToVendor(userEmail, policy, claimsInvestigation.ClaimsInvestigationId, selectedVendorId.VendorId, url));

        //        return claim; // Return allocated claim
        //    });

        //    var results = await Task.WhenAll(claimTasks); // Run all tasks in parallel
        //    return results.Where(r => r != null).ToList(); // Remove nulls and return allocated claims
        //}
        //public async Task<string> ProcessAutoSingleAllocation(string claim, string userEmail, string url = "")
        //{
        //    var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);

        //    var company = _context.ClientCompany
        //            .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
        //            .ThenInclude(e => e.VendorInvestigationServiceTypes)
        //            .ThenInclude(v => v.District)
        //            .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

        //    // 1. Fetch Claim Details & Pincode in Parallel
        //    var claimsInvestigation = await _context.ClaimsInvestigation
        //        .AsNoTracking()
        //        .Include(c => c.PolicyDetail)
        //            .ThenInclude(c => c.LineOfBusiness)
        //        .Include(c => c.CustomerDetail)
        //            .ThenInclude(c => c.PinCode)
        //        .Include(c => c.BeneficiaryDetail)
        //            .ThenInclude(c => c.PinCode)
        //        .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == claim);

        //    string pinCode2Verify = claimsInvestigation.PolicyDetail?.LineOfBusiness.Name.ToLower() == UNDERWRITING
        //        ? claimsInvestigation.CustomerDetail?.PinCode?.Code
        //        : claimsInvestigation.BeneficiaryDetail?.PinCode?.Code;

        //    var pincodeDistrictState = await _context.PinCode
        //        .AsNoTracking()
        //        .Include(d => d.District)
        //        .Include(s => s.State)
        //        .FirstOrDefaultAsync(p => p.Code == pinCode2Verify);

        //    // 2. Find Vendors Using LINQ
        //    var distinctVendorIds = company.EmpanelledVendors
        //        .Where(vendor => vendor.VendorInvestigationServiceTypes.Any(serviceType =>
        //            serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
        //            serviceType.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId &&
        //            (serviceType.StateId == pincodeDistrictState.StateId &&
        //             (serviceType.DistrictId == null || serviceType.DistrictId == pincodeDistrictState.DistrictId))
        //        ))
        //        .Select(v => v.VendorId) // Select only VendorId
        //        .Distinct() // Ensure uniqueness
        //        .ToList();

        //    if (!distinctVendorIds.Any()) return null; // No vendors found, skip this claim

        //    // 3. Get Vendor Load & Allocate
        //    var vendorsWithCaseLoad = GetAgencyIdsLoad(distinctVendorIds)
        //        .OrderBy(o => o.CaseCount)
        //        .ToList();

        //    var selectedVendorId = vendorsWithCaseLoad.FirstOrDefault();
        //    if (selectedVendorId == null) return null; // No vendors available

        //    var (policy, status) = await AllocateToVendor(userEmail, claimsInvestigation.ClaimsInvestigationId, selectedVendorId.VendorId);

        //    if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
        //    {
        //        await AssignToAssigner(userEmail, new List<string> { claim });
        //        await mailboxService.NotifyClaimAssignmentToAssigner(userEmail, new List<string> { claim }, url);
        //        return null;
        //    }

        //    // 4. Send Notification
        //    var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAllocationToVendor(userEmail, policy, claimsInvestigation.ClaimsInvestigationId, selectedVendorId.VendorId, url));

        //    return claimsInvestigation.PolicyDetail.ContractNumber; // Return allocated claim
        //}
        //public List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors)
        //{
        //    // Get relevant status IDs in one query
        //    var relevantStatuses = _context.InvestigationCaseSubStatus
        //        .Where(i => new[]
        //        {
        //            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
        //            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
        //            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
        //            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
        //        }.Contains(i.Name.ToUpper()))
        //        .Select(i => i.InvestigationCaseSubStatusId)
        //        .ToHashSet(); // Improves lookup performance

        //    // Fetch cases that match the criteria
        //    var vendorCaseCount = _context.ClaimsInvestigation
        //        .Where(c => !c.Deleted &&
        //                    c.VendorId.HasValue &&
        //                    c.AssignedToAgency &&
        //                    relevantStatuses.Contains(c.InvestigationCaseSubStatusId))
        //        .GroupBy(c => c.VendorId.Value)
        //        .ToDictionary(g => g.Key, g => g.Count());

        //    // Create the list of VendorIdWithCases
        //    return existingVendors
        //        .Select(vendorId => new VendorIdWithCases
        //        {
        //            VendorId = vendorId,
        //            CaseCount = vendorCaseCount.GetValueOrDefault(vendorId, 0)
        //        })
        //        .ToList();
        //}

        //public List<VendorCaseModel> GetAgencyLoad(List<Vendor> existingVendors)
        //{
        //    var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
        //    var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
        //    var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
        //    var requestedByAssessor = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);

        //    var claimsCases = _context.ClaimsInvestigation
        //        .Where(c =>
        //        !c.Deleted &&
        //        c.VendorId.HasValue &&
        //        c.AssignedToAgency &&
        //        (c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
        //                            c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
        //                            c.InvestigationCaseSubStatusId == requestedByAssessor.InvestigationCaseSubStatusId ||
        //                            c.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId)
        //        );

        //    var vendorCaseCount = claimsCases
        //        .Where(c => c.VendorId.HasValue)
        //        .GroupBy(c => c.VendorId.Value)
        //        .ToDictionary(g => g.Key, g => g.Count());

        //    var vendorWithCaseCounts = existingVendors
        //        .Select(vendor => new VendorCaseModel
        //        {
        //            Vendor = vendor,
        //            CaseCount = vendorCaseCount.GetValueOrDefault(vendor.VendorId, 0)
        //        })
        //        .ToList();

        //    return vendorWithCaseCounts;

        //}

        //public async Task AssignToAssigner(string userEmail, List<string> claims, string url = "")
        //{
        //    if (claims is not null && claims.Count > 0)
        //    {
        //        var cases2Assign = _context.ClaimsInvestigation
        //            .Where(v => claims.Contains(v.ClaimsInvestigationId));

        //        var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
        //        var companyUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == currentUser.ClientCompanyId);
        //        var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));
        //        var assigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

        //        foreach (var claimsInvestigation in cases2Assign)
        //        {
        //            claimsInvestigation.Updated = DateTime.Now;
        //            claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
        //            claimsInvestigation.CurrentUserEmail = userEmail;
        //            claimsInvestigation.UserEmailActioned = currentUser.Email;
        //            claimsInvestigation.UserRoleActionedTo = $"{currentUser.ClientCompany.Email}";
        //            claimsInvestigation.CurrentClaimOwner = currentUser.Email;
        //            claimsInvestigation.AssignedToAgency = false;
        //            claimsInvestigation.STATUS = claimsInvestigation.IsValidCaseData() ? ALLOCATION_STATUS.READY : ALLOCATION_STATUS.PENDING;
        //            claimsInvestigation.IsReady2Assign = claimsInvestigation.IsValidCaseData() ? true : false;
        //            claimsInvestigation.CREATEDBY = CREATEDBY.MANUAL;
        //            claimsInvestigation.AutoAllocated = false;
        //            claimsInvestigation.ActiveView = 0;
        //            claimsInvestigation.ManualNew = 0;
        //            claimsInvestigation.AllocateView = 0;
        //            claimsInvestigation.AutoNew = 0;
        //            claimsInvestigation.VendorId = null;
        //            claimsInvestigation.Vendor = null;
        //            claimsInvestigation.AgencyDeclineComment = string.Empty;
        //            claimsInvestigation.CompanyWithdrawlComment = string.Empty;
        //            claimsInvestigation.AgencyWithdrawComment = string.Empty;
        //            claimsInvestigation.InvestigationCaseSubStatusId = assigned.InvestigationCaseSubStatusId;


        //            var lastLog = _context.InvestigationTransaction
        //                .Where(i =>
        //                    i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
        //                    .OrderByDescending(o => o.Created)?.FirstOrDefault();

        //            var lastLogHop = _context.InvestigationTransaction
        //                .Where(i => i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
        //                .AsNoTracking().Max(s => s.HopCount);

        //            var log = new InvestigationTransaction
        //            {
        //                HopCount = lastLogHop + 1,
        //                UserEmailActioned = userEmail,
        //                UserRoleActionedTo = $"{currentUser.ClientCompany.Email}",
        //                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
        //                CurrentClaimOwner = currentUser.Email,
        //                ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
        //                InvestigationCaseStatusId = lastLog.InvestigationCaseStatusId,
        //                InvestigationCaseSubStatusId = assigned.InvestigationCaseSubStatusId,
        //                UpdatedBy = currentUser.Email,
        //                Updated = DateTime.Now
        //            };
        //            _context.InvestigationTransaction.Add(log);
        //        }
        //        _context.UpdateRange(cases2Assign);
        //        await _context.SaveChangesAsync();
        //    }
        //}

        //public async Task<(ClientCompany, long)> WithdrawCaseByCompany(string userEmail, ClaimTransactionModel model, string claimId)
        //{
        //    try
        //    {
        //        var currentUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
        //        var claimsInvestigation = _context.ClaimsInvestigation
        //            .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
        //        var vendorId = claimsInvestigation.VendorId;
        //        var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

        //        var withdrawnByCompany = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                   i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);

        //        claimsInvestigation.Updated = DateTime.Now;
        //        claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
        //        claimsInvestigation.CurrentUserEmail = userEmail;
        //        claimsInvestigation.STATUS = ALLOCATION_STATUS.READY;
        //        claimsInvestigation.AssignedToAgency = false;
        //        claimsInvestigation.CurrentClaimOwner = userEmail;
        //        claimsInvestigation.UserEmailActioned = userEmail;
        //        claimsInvestigation.UserEmailActionedTo = userEmail;
        //        claimsInvestigation.UserRoleActionedTo = $"{company.Email}";
        //        claimsInvestigation.CompanyWithdrawlComment = $"WITHDRAWN: {currentUser.Email} :{model.ClaimsInvestigation.CompanyWithdrawlComment}";
        //        claimsInvestigation.ActiveView = 0;
        //        claimsInvestigation.ManualNew = 0;
        //        claimsInvestigation.AllocateView = 0;
        //        claimsInvestigation.AutoNew = 0;
        //        claimsInvestigation.VendorId = null;
        //        claimsInvestigation.Vendor = null;

        //        claimsInvestigation.InvestigationCaseSubStatusId = withdrawnByCompany.InvestigationCaseSubStatusId;

        //        var lastLog = _context.InvestigationTransaction
        //            .Where(i =>
        //                i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
        //                .OrderByDescending(o => o.Created)?.FirstOrDefault();

        //        var lastLogHop = _context.InvestigationTransaction
        //            .Where(i => i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
        //            .AsNoTracking().Max(s => s.HopCount);

        //        var log = new InvestigationTransaction
        //        {
        //            HopCount = lastLogHop + 1,
        //            UserEmailActioned = userEmail,
        //            UserEmailActionedTo = userEmail,
        //            UserRoleActionedTo = $"{company.Email}",
        //            Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
        //            CurrentClaimOwner = userEmail,
        //            ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
        //            InvestigationCaseStatusId = lastLog.InvestigationCaseStatusId,
        //            InvestigationCaseSubStatusId = withdrawnByCompany.InvestigationCaseSubStatusId,
        //            UpdatedBy = currentUser.Email,
        //            Updated = DateTime.Now
        //        };
        //        _context.InvestigationTransaction.Add(log);
        //        _context.ClaimsInvestigation.Update(claimsInvestigation);

        //        var rows = await _context.SaveChangesAsync();
        //        return (company, vendorId.GetValueOrDefault());
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}

        //public async Task<Vendor> WithdrawCaseFromAgent(string userEmail, ClaimTransactionModel model, string claimId)
        //{
        //    try
        //    {
        //        var currentUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(u => u.Email == userEmail);
        //        var claimsInvestigation = _context.ClaimsInvestigation
        //            .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

        //        var allocatedToAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                   i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);

        //        claimsInvestigation.Updated = DateTime.Now;
        //        claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
        //        claimsInvestigation.CurrentUserEmail = userEmail;
        //        claimsInvestigation.STATUS = ALLOCATION_STATUS.READY;
        //        claimsInvestigation.AssignedToAgency = false;
        //        claimsInvestigation.CurrentClaimOwner = currentUser.Email;
        //        claimsInvestigation.UserEmailActioned = userEmail;
        //        claimsInvestigation.UserEmailActionedTo = string.Empty;
        //        claimsInvestigation.AgencyWithdrawComment = $"WITHDRAWN: {currentUser.Email}";
        //        claimsInvestigation.UserEmailActioned = userEmail;
        //        claimsInvestigation.UserEmailActionedTo = string.Empty;
        //        claimsInvestigation.UserRoleActionedTo = currentUser.Vendor.Email;
        //        claimsInvestigation.AllocateView = 0;
        //        claimsInvestigation.InvestigationCaseSubStatusId = allocatedToAgency.InvestigationCaseSubStatusId;
        //        var lastLog = _context.InvestigationTransaction
        //            .Where(i =>
        //                i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
        //                .OrderByDescending(o => o.Created)?.FirstOrDefault();

        //        var lastLogHop = _context.InvestigationTransaction
        //            .Where(i => i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
        //            .AsNoTracking().Max(s => s.HopCount);

        //        var log = new InvestigationTransaction
        //        {
        //            HopCount = lastLogHop + 1,
        //            UserEmailActioned = userEmail,
        //            UserEmailActionedTo = string.Empty,
        //            UserRoleActionedTo = $"{currentUser.Vendor.Email}",
        //            Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
        //            CurrentClaimOwner = currentUser.Email,
        //            ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
        //            InvestigationCaseStatusId = lastLog.InvestigationCaseStatusId,
        //            InvestigationCaseSubStatusId = allocatedToAgency.InvestigationCaseSubStatusId,
        //            UpdatedBy = currentUser.Email,
        //            Updated = DateTime.Now
        //        };
        //        _context.InvestigationTransaction.Add(log);
        //        _context.ClaimsInvestigation.Update(claimsInvestigation);

        //        var rows = await _context.SaveChangesAsync();
        //        return currentUser.Vendor;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}

        //public async Task<Vendor> WithdrawCase(string userEmail, ClaimTransactionModel model, string claimId)
        //{
        //    try
        //    {
        //        var currentUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(u => u.Email == userEmail);
        //        var claimsInvestigation = _context.ClaimsInvestigation
        //            .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
        //        var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

        //        var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                   i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);

        //        claimsInvestigation.Updated = DateTime.Now;
        //        claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
        //        claimsInvestigation.CurrentUserEmail = userEmail;
        //        claimsInvestigation.STATUS = ALLOCATION_STATUS.READY;
        //        claimsInvestigation.AssignedToAgency = false;
        //        claimsInvestigation.CurrentClaimOwner = currentUser.Email;
        //        claimsInvestigation.UserEmailActioned = userEmail;
        //        claimsInvestigation.UserEmailActionedTo = string.Empty;
        //        claimsInvestigation.AgencyDeclineComment = $"DECLINED: {currentUser.Email} :{model.ClaimsInvestigation.AgencyDeclineComment}";
        //        claimsInvestigation.ActiveView = 0;
        //        claimsInvestigation.AllocateView = 0;
        //        claimsInvestigation.AutoNew = 0;
        //        claimsInvestigation.VendorId = null;
        //        claimsInvestigation.UserRoleActionedTo = $"{company.Email}";
        //        claimsInvestigation.InvestigationCaseSubStatusId = withdrawnByAgency.InvestigationCaseSubStatusId;
        //        var lastLog = _context.InvestigationTransaction
        //            .Where(i =>
        //                i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
        //                .OrderByDescending(o => o.Created)?.FirstOrDefault();

        //        var lastLogHop = _context.InvestigationTransaction
        //            .Where(i => i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
        //            .AsNoTracking().Max(s => s.HopCount);

        //        var log = new InvestigationTransaction
        //        {
        //            HopCount = lastLogHop + 1,
        //            UserEmailActioned = userEmail,
        //            UserEmailActionedTo = string.Empty,
        //            UserRoleActionedTo = $"{company.Email}",
        //            Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
        //            CurrentClaimOwner = currentUser.Email,
        //            ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
        //            InvestigationCaseStatusId = lastLog.InvestigationCaseStatusId,
        //            InvestigationCaseSubStatusId = withdrawnByAgency.InvestigationCaseSubStatusId,
        //            UpdatedBy = currentUser.Email,
        //            Updated = DateTime.Now
        //        };
        //        _context.InvestigationTransaction.Add(log);
        //        _context.ClaimsInvestigation.Update(claimsInvestigation);

        //        var rows = await _context.SaveChangesAsync();
        //        return currentUser.Vendor;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}

        //public async Task<(string, string)> AllocateToVendor(string userEmail, string claimsInvestigationId, long vendorId, bool autoAllocated = true)
        //{
        //    try
        //    {
        //        // Fetch vendor & user details
        //        var vendor = await _context.Vendor.FindAsync(vendorId);
        //        var currentUser = await _context.ClientCompanyApplicationUser
        //            .Include(c => c.ClientCompany)
        //            .FirstOrDefaultAsync(u => u.Email == userEmail);

        //        if (vendor == null || currentUser == null) return (string.Empty, string.Empty); // Handle missing data
        //        var inProgressStatus = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INPROGRESS));

        //        var subStatuses = await _context.InvestigationCaseSubStatus
        //            .Where(i => new[]
        //            {
        //                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR
        //            }.Contains(i.Name))
        //            .ToDictionaryAsync(i => i.Name, i => i.InvestigationCaseSubStatusId);

        //        if (!subStatuses.TryGetValue(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR, out var allocatedToVendorId))
        //        {
        //            return (string.Empty, string.Empty); // Handle missing status/substatus
        //        }

        //        // Fetch case
        //        var claimsCase = await _context.ClaimsInvestigation
        //            .Include(c => c.PolicyDetail)
        //            .FirstOrDefaultAsync(v => v.ClaimsInvestigationId == claimsInvestigationId);

        //        if (claimsCase == null) return (string.Empty, string.Empty); // Handle missing case

        //        // Update case details
        //        claimsCase.STATUS = ALLOCATION_STATUS.COMPLETED;
        //        claimsCase.AssignedToAgency = true;
        //        claimsCase.Updated = DateTime.Now;
        //        claimsCase.UpdatedBy = $"{currentUser.FirstName} {currentUser.LastName} ({currentUser.Email})";
        //        claimsCase.CurrentUserEmail = userEmail;
        //        claimsCase.EnablePassport = currentUser.ClientCompany.EnablePassport;
        //        claimsCase.AiEnabled = currentUser.ClientCompany.AiEnabled;
        //        claimsCase.EnableMedia = currentUser.ClientCompany.EnableMedia;
        //        claimsCase.InvestigationCaseSubStatusId = allocatedToVendorId;
        //        claimsCase.UserEmailActioned = userEmail;
        //        claimsCase.AgencyWithdrawComment = string.Empty;
        //        claimsCase.AgencyDeclineComment = string.Empty;
        //        claimsCase.CompanyWithdrawlComment = string.Empty;
        //        claimsCase.UserEmailActionedTo = string.Empty;
        //        claimsCase.UserRoleActionedTo = vendor.Email;
        //        claimsCase.VendorId = vendorId;
        //        claimsCase.AllocateView = 0;
        //        claimsCase.AutoAllocated = autoAllocated;
        //        claimsCase.AllocatedToAgencyTime = DateTime.Now;
        //        claimsCase.CreatorSla = currentUser.ClientCompany.CreatorSla;
        //        claimsCase.AssessorSla = currentUser.ClientCompany.AssessorSla;
        //        claimsCase.SupervisorSla = currentUser.ClientCompany.SupervisorSla;
        //        claimsCase.AgentSla = currentUser.ClientCompany.AgentSla;
        //        claimsCase.UpdateAgentReport = currentUser.ClientCompany.UpdateAgentReport;
        //        claimsCase.UpdateAgentAnswer = currentUser.ClientCompany.UpdateAgentAnswer;
        //        claimsCase.InvestigationCaseStatus = inProgressStatus;
        //        claimsCase.InvestigationCaseSubStatusId = subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR];
        //        claimsCase.Vendors.Add(vendor); // Ensures relationship update
        //        _context.ClaimsInvestigation.Update(claimsCase);

        //        // Get last transaction log
        //        var lastLog = await _context.InvestigationTransaction
        //            .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
        //            .OrderByDescending(o => o.Created)
        //            .FirstOrDefaultAsync();

        //        var lastLogHop = await _context.InvestigationTransaction
        //            .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
        //            .AsNoTracking()
        //            .MaxAsync(s => (int?)s.HopCount) ?? 0;

        //        // Calculate time elapsed
        //        string timeElapsed = GetTimeElaspedFromLog(lastLog);

        //        // Create new transaction log
        //        var log = new InvestigationTransaction
        //        {
        //            UserEmailActioned = userEmail,
        //            UserRoleActionedTo = vendor.Email,
        //            UserEmailActionedTo = string.Empty,
        //            HopCount = lastLogHop + 1,
        //            ClaimsInvestigationId = claimsInvestigationId,
        //            CurrentClaimOwner = claimsCase.CurrentClaimOwner,
        //            Time2Update = lastLog != null ? (DateTime.Now - lastLog.Created).Days : 0,
        //            InvestigationCaseStatusId = lastLog.InvestigationCaseStatusId,
        //            InvestigationCaseSubStatusId = allocatedToVendorId,
        //            UpdatedBy = currentUser.Email,
        //            Updated = DateTime.Now,
        //            TimeElapsed = timeElapsed
        //        };

        //        _context.InvestigationTransaction.Add(log);

        //        // Save changes
        //        await _context.SaveChangesAsync();

        //        return (claimsCase.PolicyDetail.ContractNumber, claimsCase.InvestigationCaseSubStatus.Name);

        //    }

        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}


        //public async Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, string claimsInvestigationId, string remarks, string? answer1, string? answer2, string? answer3, string? answer4)
        //{
        //    try
        //    {
        //        var agent = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(a => a.Email.Trim().ToLower() == userEmail.ToLower());

        //        var submitted2Supervisor = _context.InvestigationCaseSubStatus
        //            .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

        //        var claim = _context.ClaimsInvestigation
        //            .Include(c => c.PolicyDetail)
        //            .ThenInclude(c => c.LineOfBusiness)
        //            .Include(c => c.InvestigationReport)
        //            .ThenInclude(c => c.ReportQuestionaire)
        //            .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

        //        claim.AgencyDeclineComment = string.Empty;
        //        claim.AgencyWithdrawComment = string.Empty;
        //        claim.CompanyWithdrawlComment = string.Empty;
        //        claim.VerifyView = 0;
        //        claim.InvestigateView = 0;
        //        claim.UserEmailActioned = userEmail;
        //        claim.UserEmailActionedTo = string.Empty;
        //        claim.UserRoleActionedTo = $"{agent.Vendor.Email}";
        //        claim.Updated = DateTime.Now;
        //        claim.UpdatedBy = agent.FirstName + " " + agent.LastName + "(" + agent.Email + ")";
        //        claim.CurrentUserEmail = userEmail;
        //        claim.CurrentClaimOwner = userEmail;
        //        claim.InvestigationCaseSubStatusId = submitted2Supervisor.InvestigationCaseSubStatusId;
        //        claim.SubmittedToSupervisorTime = DateTime.Now;
        //        var claimReport = claim.InvestigationReport;

        //        claimReport.ReportQuestionaire.Answer1 = answer1;
        //        claimReport.ReportQuestionaire.Answer2 = answer2;
        //        claimReport.ReportQuestionaire.Answer3 = answer3;
        //        claimReport.ReportQuestionaire.Answer4 = answer4;
        //        claimReport.AgentRemarks = remarks;
        //        claimReport.AgentRemarksUpdated = DateTime.Now;
        //        claimReport.AgentEmail = userEmail;

        //        var lastLog = _context.InvestigationTransaction.Where(i =>
        //           i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

        //        var lastLogHop = _context.InvestigationTransaction
        //                                   .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
        //                                   .AsNoTracking().Max(s => s.HopCount);

        //        var log = new InvestigationTransaction
        //        {
        //            ClaimsInvestigationId = claimsInvestigationId,
        //            UserEmailActioned = agent.Email,
        //            UserRoleActionedTo = $"{agent.Vendor.Email}",
        //            HopCount = lastLogHop + 1,
        //            CurrentClaimOwner = userEmail,
        //            Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
        //            InvestigationCaseStatusId = lastLog.InvestigationCaseStatusId,
        //            InvestigationCaseSubStatusId = submitted2Supervisor.InvestigationCaseSubStatusId,
        //            UpdatedBy = agent.Email,
        //            Updated = DateTime.Now,
        //            TimeElapsed = GetTimeElaspedFromLog(lastLog)
        //        };
        //        _context.InvestigationTransaction.Add(log);

        //        _context.ClaimsInvestigation.Update(claim);

        //        var rows = await _context.SaveChangesAsync();
        //        return (agent.Vendor, claim.PolicyDetail.ContractNumber);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}


        //public async Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, string claimsInvestigationId, AssessorRemarkType reportUpdateStatus, string reportAiSummary)
        //{
        //    if (reportUpdateStatus == AssessorRemarkType.OK)
        //    {
        //        return await ApproveCaseReport(userEmail, assessorRemarks, claimsInvestigationId, reportUpdateStatus, reportAiSummary);
        //    }
        //    else if (reportUpdateStatus == AssessorRemarkType.REJECT)
        //    {
        //        //PUT th case back in review list :: Assign back to Agent
        //        return await RejectCaseReport(userEmail, assessorRemarks, claimsInvestigationId, reportUpdateStatus, reportAiSummary);
        //    }
        //    else
        //    {
        //        //PUT th case back in review list :: Assign back to Agent
        //        return await ReAssignToCreator(userEmail, claimsInvestigationId, assessorRemarks, reportUpdateStatus, reportAiSummary);
        //    }
        //}

        //private async Task<(ClientCompany, string)> RejectCaseReport(string userEmail, string assessorRemarks, string claimsInvestigationId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        //{
        //    var rejected = _context.InvestigationCaseSubStatus
        //        .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
        //    var finished = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

        //    try
        //    {
        //        var claim = _context.ClaimsInvestigation
        //        .Include(c => c.ClientCompany)
        //        .Include(c => c.PolicyDetail)
        //        .Include(r => r.InvestigationReport)
        //        .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

        //        claim.InvestigationReport.AiSummary = reportAiSummary;
        //        claim.InvestigationReport.AssessorRemarkType = assessorRemarkType;
        //        claim.InvestigationReport.AssessorRemarks = assessorRemarks;
        //        claim.InvestigationReport.AssessorRemarksUpdated = DateTime.Now;
        //        claim.InvestigationReport.AssessorEmail = userEmail;

        //        claim.InvestigationCaseStatusId = finished.InvestigationCaseStatusId;
        //        claim.InvestigationCaseSubStatusId = rejected.InvestigationCaseSubStatusId;
        //        claim.Updated = DateTime.Now;
        //        claim.UserEmailActioned = userEmail;
        //        claim.UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})";
        //        claim.UserEmailActionedTo = userEmail;
        //        claim.ProcessedByAssessorTime = DateTime.Now;
        //        _context.ClaimsInvestigation.Update(claim);

        //        var finalHop = _context.InvestigationTransaction
        //                           .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
        //                            .AsNoTracking().Max(s => s.HopCount);
        //        var lastLog = _context.InvestigationTransaction.Where(i =>
        //      i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

        //        var finalLog = new InvestigationTransaction
        //        {
        //            HopCount = finalHop + 1,
        //            UserEmailActioned = userEmail,
        //            UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})",
        //            ClaimsInvestigationId = claimsInvestigationId,
        //            CurrentClaimOwner = claim.CurrentClaimOwner,
        //            Created = DateTime.Now,
        //            Time2Update = DateTime.Now.Subtract(claim.Created).Days,
        //            InvestigationCaseStatusId = finished.InvestigationCaseStatusId,
        //            InvestigationCaseSubStatusId = rejected.InvestigationCaseSubStatusId,
        //            UpdatedBy = userEmail,
        //            Updated = DateTime.Now,
        //            TimeElapsed = GetTimeElaspedFromLog(lastLog)
        //        };

        //        _context.InvestigationTransaction.Add(finalLog);

        //        var saveCount = await _context.SaveChangesAsync();

        //        //backgroundJobClient.Enqueue(() => reportService.Run(userEmail, claimsInvestigationId));

        //        var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
        //        return saveCount > 0 ? (currentUser.ClientCompany, claim.PolicyDetail.ContractNumber) : (null!, string.Empty);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //    }
        //    return (null!, string.Empty);
        //}

        //private async Task<(ClientCompany, string)> ApproveCaseReport(string userEmail, string assessorRemarks, string claimsInvestigationId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        //{

        //    try
        //    {
        //        var approved = _context.InvestigationCaseSubStatus
        //        .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
        //        var finished = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

        //        var claim = _context.ClaimsInvestigation
        //        .Include(c => c.ClientCompany)
        //        .Include(c => c.PolicyDetail)
        //        .Include(r => r.InvestigationReport)
        //        .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

        //        claim.InvestigationReport.AiSummary = reportAiSummary;
        //        claim.InvestigationReport.AssessorRemarkType = assessorRemarkType;
        //        claim.InvestigationReport.AssessorRemarks = assessorRemarks;
        //        claim.InvestigationReport.AssessorRemarksUpdated = DateTime.Now;
        //        claim.InvestigationReport.AssessorEmail = userEmail;

        //        claim.InvestigationCaseStatusId = finished.InvestigationCaseStatusId;
        //        claim.InvestigationCaseSubStatusId = approved.InvestigationCaseSubStatusId;
        //        claim.Updated = DateTime.Now;
        //        claim.UserEmailActioned = userEmail;
        //        claim.UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})";
        //        claim.UserEmailActionedTo = userEmail;
        //        claim.ProcessedByAssessorTime = DateTime.Now;
        //        _context.ClaimsInvestigation.Update(claim);

        //        var finalHop = _context.InvestigationTransaction
        //                           .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
        //                            .AsNoTracking().Max(s => s.HopCount);
        //        var lastLog = _context.InvestigationTransaction.Where(i =>
        //                     i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

        //        var finalLog = new InvestigationTransaction
        //        {
        //            HopCount = finalHop + 1,
        //            UserEmailActioned = userEmail,
        //            UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})",
        //            ClaimsInvestigationId = claimsInvestigationId,
        //            CurrentClaimOwner = claim.CurrentClaimOwner,
        //            Created = DateTime.Now,
        //            Time2Update = DateTime.Now.Subtract(claim.Created).Days,
        //            InvestigationCaseStatusId = finished.InvestigationCaseStatusId,
        //            InvestigationCaseSubStatusId = approved.InvestigationCaseSubStatusId,
        //            UpdatedBy = userEmail,
        //            Updated = DateTime.Now,
        //            TimeElapsed = GetTimeElaspedFromLog(lastLog)
        //        };

        //        _context.InvestigationTransaction.Add(finalLog);

        //        var saveCount = await _context.SaveChangesAsync();

        //        var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

        //        //backgroundJobClient.Enqueue(() => reportService.Run(userEmail, claimsInvestigationId));

        //        return saveCount > 0 ? (currentUser.ClientCompany, claim.PolicyDetail.ContractNumber) : (null!, string.Empty);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //    }
        //    return (null!, string.Empty);
        //}

        //private async Task<(ClientCompany, string)> ReAssignToCreator(string userEmail, string claimsInvestigationId, string assessorRemarks, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        //{
        //    try
        //    {


        //        var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

        //        var claimsCaseToReassign = _context.ClaimsInvestigation
        //            .Include(c => c.InvestigationReport)
        //            .Include(c => c.InvestigationReport.DigitalIdReport)
        //            .Include(c => c.InvestigationReport.PanIdReport)
        //            .Include(c => c.InvestigationReport.ReportQuestionaire)
        //            .Include(c => c.PolicyDetail)
        //            .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);


        //        claimsCaseToReassign.InvestigationReport.AiSummary = reportAiSummary;
        //        claimsCaseToReassign.InvestigationReport.AssessorRemarkType = assessorRemarkType;
        //        claimsCaseToReassign.InvestigationReport.AssessorRemarks = assessorRemarks;
        //        claimsCaseToReassign.InvestigationReport.AssessorRemarksUpdated = DateTime.Now;
        //        claimsCaseToReassign.ReviewByAssessorTime = DateTime.Now;
        //        claimsCaseToReassign.InvestigationReport.AssessorEmail = userEmail;
        //        var reAssigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
        //                i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);

        //        var newReport = new InvestigationReport
        //        {
        //            ReportQuestionaire = new ReportQuestionaire(),
        //            PanIdReport = new DocumentIdReport(),
        //            PassportIdReport = new DocumentIdReport(),
        //            DigitalIdReport = new DigitalIdReport()
        //        };
        //        claimsCaseToReassign.InvestigationReport.DigitalIdReport = new DigitalIdReport();
        //        claimsCaseToReassign.InvestigationReport.PanIdReport = new DocumentIdReport();
        //        claimsCaseToReassign.InvestigationReport.PassportIdReport = new DocumentIdReport();
        //        claimsCaseToReassign.InvestigationReport.ReportQuestionaire = new ReportQuestionaire();

        //        claimsCaseToReassign.AssignedToAgency = false;
        //        claimsCaseToReassign.ReviewCount += 1;
        //        claimsCaseToReassign.UserEmailActioned = userEmail;
        //        claimsCaseToReassign.UserEmailActionedTo = string.Empty;
        //        claimsCaseToReassign.UserRoleActionedTo = $"{currentUser.ClientCompany.Email}";
        //        claimsCaseToReassign.Updated = DateTime.Now;
        //        claimsCaseToReassign.UpdatedBy = userEmail;
        //        claimsCaseToReassign.VendorId = null;
        //        claimsCaseToReassign.CurrentUserEmail = userEmail;
        //        claimsCaseToReassign.IsReviewCase = true;
        //        claimsCaseToReassign.AssessView = 0;
        //        claimsCaseToReassign.ActiveView = 0;
        //        claimsCaseToReassign.AllocateView = 0;
        //        claimsCaseToReassign.VerifyView = 0;
        //        claimsCaseToReassign.AssessView = 0;
        //        claimsCaseToReassign.ManualNew = 0;
        //        claimsCaseToReassign.CurrentClaimOwner = currentUser.Email;
        //        claimsCaseToReassign.InvestigationCaseSubStatusId = reAssigned.InvestigationCaseSubStatusId;
        //        claimsCaseToReassign.ProcessedByAssessorTime = DateTime.Now;
        //        _context.ClaimsInvestigation.Update(claimsCaseToReassign);
        //        var lastLog = _context.InvestigationTransaction.Where(i =>
        //                        i.ClaimsInvestigationId == claimsCaseToReassign.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

        //        var lastLogHop = _context.InvestigationTransaction
        //                                   .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
        //            .AsNoTracking().Max(s => s.HopCount);

        //        var log = new InvestigationTransaction
        //        {
        //            IsReviewCase = true,
        //            HopCount = lastLogHop + 1,
        //            UserEmailActioned = userEmail,
        //            UserRoleActionedTo = $"{currentUser.ClientCompany.Email}",
        //            ClaimsInvestigationId = claimsCaseToReassign.ClaimsInvestigationId,
        //            Created = DateTime.Now,
        //            Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
        //            InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
        //            InvestigationCaseSubStatusId = reAssigned.InvestigationCaseSubStatusId,
        //            UpdatedBy = userEmail,
        //            CurrentClaimOwner = currentUser.Email,
        //            Updated = DateTime.Now,
        //            TimeElapsed = GetTimeElaspedFromLog(lastLog)
        //        };
        //        _context.InvestigationTransaction.Add(log);

        //        return await _context.SaveChangesAsync() > 0 ? (currentUser.ClientCompany, claimsCaseToReassign.PolicyDetail.ContractNumber) : (null!, string.Empty);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}
        ////public async Task<bool> SubmitNotes(string userEmail, string claimId, string notes)
        //{
        //    var claim = _context.ClaimsInvestigation
        //       .Include(c => c.ClaimNotes)
        //       .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
        //    claim.ClaimNotes.Add(new ClaimNote
        //    {
        //        Comment = notes,
        //        Sender = userEmail,
        //        Created = DateTime.Now,
        //        Updated = DateTime.Now,
        //        UpdatedBy = userEmail
        //    });
        //    _context.ClaimsInvestigation.Update(claim);
        //    return await _context.SaveChangesAsync() > 0;
        //}
        public async Task<bool> SubmitNotes(string userEmail, long claimId, string notes)
        {
            var claim = _context.Investigations
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
            _context.Investigations.Update(claim);
            return await _context.SaveChangesAsync() > 0;
        }


    }
}