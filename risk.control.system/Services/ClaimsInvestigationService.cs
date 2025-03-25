using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using System.Data;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Services
{
    public interface IClaimsInvestigationService
    {
        Task AssignToAssigner(string userEmail, List<string> claimsInvestigations);

        Task<ClaimsInvestigation> AllocateToVendor(string userEmail, string claimsInvestigationId, long vendorId, bool AutoAllocated = true);

        Task<ClaimsInvestigation> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, string claimsInvestigationId, string drivingMap, string drivingDistance, string drivingDuration, string distanceInMeters, string durationInSeconds);

        Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, string claimsInvestigationId, string remarks, string? answer1, string? answer2, string? answer3, string? answer4);

        Task<ClaimsInvestigation> ProcessAgentReport(string userEmail, string supervisorRemarks, string claimsInvestigationId, SupervisorRemarkType remarks, IFormFile? claimDocument = null, string editRemarks = "");

        Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, string claimsInvestigationId, AssessorRemarkType assessorRemarkType, string reportAiSummary);

        List<VendorCaseModel> GetAgencyLoad(List<Vendor> existingVendors);

        Task<Vendor> WithdrawCase(string userEmail, ClaimTransactionModel model, string claimId);

        Task<List<string>> ProcessAutoAllocation(List<string> claims, ClientCompany company, string userEmail);
        Task<ClientCompany> WithdrawCaseByCompany(string userEmail, ClaimTransactionModel model, string claimId);
        Task<bool> SubmitNotes(string userEmail, string claimId, string notes);

        Task<ClaimsInvestigation> SubmitQueryToAgency(string userEmail, string claimId, EnquiryRequest request, IFormFile messageDocument);
        Task<ClaimsInvestigation> SubmitQueryReplyToCompany(string userEmail, string claimId, EnquiryRequest request, IFormFile messageDocument, List<string> flexRadioDefault);
    }

    public class ClaimsInvestigationService : IClaimsInvestigationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMailboxService mailboxService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ClaimsInvestigationService(ApplicationDbContext context,
            IMailboxService mailboxService, 
            IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            this.mailboxService = mailboxService;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task<List<string>> ProcessAutoAllocation(List<string> claims, ClientCompany company, string userEmail)
        {
            var autoAllocatedClaims = new List<string>();
            foreach (var claim in claims)
            {
                string pinCode2Verify = string.Empty;
                
                //1. GET THE PINCODE FOR EACH CLAIM
                var claimsInvestigation = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.PinCode)
                    .First(c => c.ClaimsInvestigationId == claim);

                if (claimsInvestigation.PolicyDetail?.ClaimType == ClaimType.HEALTH)
                {
                    pinCode2Verify = claimsInvestigation.CustomerDetail?.PinCode?.Code;
                }
                else
                {
                    pinCode2Verify = claimsInvestigation.BeneficiaryDetail.PinCode?.Code;
                }
                var pincodeDistrictState = _context.PinCode.Include(d => d.District).Include(s => s.State).FirstOrDefault(p => p.Code == pinCode2Verify);
                var vendorsInPincode = new List<Vendor>();

                //2. GET THE VENDORID FOR EACH CASE BASED ON PINCODE
                foreach (var empanelledVendor in company.EmpanelledVendors)
                {
                    foreach (var serviceType in empanelledVendor.VendorInvestigationServiceTypes)
                    {
                        if (serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
                                serviceType.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId)
                        {
                            if (serviceType.StateId == pincodeDistrictState.StateId && serviceType.DistrictId == null)
                            {
                                vendorsInPincode.Add(empanelledVendor);
                                continue;
                            }
                            if (serviceType.StateId == pincodeDistrictState.StateId && serviceType.DistrictId == pincodeDistrictState.DistrictId)
                            {
                                vendorsInPincode.Add(empanelledVendor);
                                continue;
                            }
                        }
                        var added = vendorsInPincode.Any(v => v.VendorId == empanelledVendor.VendorId);
                        if (added)
                        {
                            continue;
                        }
                    }
                }

                var distinctVendors = vendorsInPincode.Distinct()?.ToList();

                //3. CALL SERVICE WITH VENDOR_ID
                if (vendorsInPincode is not null && vendorsInPincode.Count > 0)
                {
                    var vendorsWithCaseLoad = GetAgencyLoad(distinctVendors).OrderBy(o => o.CaseCount)?.ToList();

                    if (vendorsWithCaseLoad is not null && vendorsWithCaseLoad.Count > 0)
                    {
                        var selectedVendor = vendorsWithCaseLoad.FirstOrDefault();

                        var policy = await AllocateToVendor(userEmail, claimsInvestigation.ClaimsInvestigationId, selectedVendor.Vendor.VendorId);

                        autoAllocatedClaims.Add(claim);

                        await mailboxService.NotifyClaimAllocationToVendor(userEmail, policy.PolicyDetail.ContractNumber, claimsInvestigation.ClaimsInvestigationId, selectedVendor.Vendor.VendorId);
                    }
                }
            }
            return autoAllocatedClaims;
        }

        public List<VendorCaseModel> GetAgencyLoad(List<Vendor> existingVendors)
        {
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var requestedByAssessor = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);

            var claimsCases = _context.ClaimsInvestigation
                .Include(c => c.BeneficiaryDetail)
                .Include(c => c.Vendors)
                .Where(c =>
                !c.Deleted &&
                c.VendorId.HasValue &&
                (c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == requestedByAssessor.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId)
                );

            var vendorCaseCount = new Dictionary<long, int>();

            int countOfCases = 0;
            foreach (var claimsCase in claimsCases)
            {
                if (claimsCase.BeneficiaryDetail.BeneficiaryDetailId > 0)
                {
                    if (claimsCase.VendorId.HasValue)
                    {
                        if (claimsCase.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                claimsCase.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                claimsCase.InvestigationCaseSubStatusId == requestedByAssessor.InvestigationCaseSubStatusId ||
                                claimsCase.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId
                                )
                        {
                            if (!vendorCaseCount.TryGetValue(claimsCase.VendorId.Value, out countOfCases))
                            {
                                vendorCaseCount.Add(claimsCase.VendorId.Value, 1);
                            }
                            else
                            {
                                int currentCount = vendorCaseCount[claimsCase.VendorId.Value];
                                ++currentCount;
                                vendorCaseCount[claimsCase.VendorId.Value] = currentCount;
                            }
                        }
                    }
                }
            }

            List<VendorCaseModel> vendorWithCaseCounts = new();

            foreach (var existingVendor in existingVendors)
            {
                var vendorCase = vendorCaseCount.FirstOrDefault(v => v.Key == existingVendor.VendorId);
                if (vendorCase.Key == existingVendor.VendorId)
                {
                    vendorWithCaseCounts.Add(new VendorCaseModel
                    {
                        CaseCount = vendorCase.Value,
                        Vendor = existingVendor,
                    });
                }
                else
                {
                    vendorWithCaseCounts.Add(new VendorCaseModel
                    {
                        CaseCount = 0,
                        Vendor = existingVendor,
                    });
                }
            }
            return vendorWithCaseCounts;
        }

        public async Task AssignToAssigner(string userEmail, List<string> claims)
        {
            if (claims is not null && claims.Count > 0)
            {
                var cases2Assign = _context.ClaimsInvestigation
                    .Where(v => claims.Contains(v.ClaimsInvestigationId));

                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                var companyUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == currentUser.ClientCompanyId);
                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));
                var assigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
                var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
                foreach (var claimsInvestigation in cases2Assign)
                {
                    claimsInvestigation.Updated = DateTime.Now;
                    claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
                    claimsInvestigation.CurrentUserEmail = userEmail;
                    claimsInvestigation.UserEmailActioned = currentUser.Email;
                    claimsInvestigation.UserRoleActionedTo = $"{currentUser.ClientCompany.Email}";
                    claimsInvestigation.CurrentClaimOwner = currentUser.Email;
                    claimsInvestigation.AssignedToAgency = false;
                    claimsInvestigation.IsReady2Assign = true;
                    claimsInvestigation.CREATEDBY = CREATEDBY.MANUAL;
                    claimsInvestigation.AutoAllocated = false;
                    claimsInvestigation.InvestigationCaseSubStatusId = assigned.InvestigationCaseSubStatusId;


                    var lastLog = _context.InvestigationTransaction
                        .Where(i =>
                            i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                            .OrderByDescending(o => o.Created)?.FirstOrDefault();

                    var lastLogHop = _context.InvestigationTransaction
                        .Where(i => i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                        .AsNoTracking().Max(s => s.HopCount);

                    var log = new InvestigationTransaction
                    {
                        HopCount = lastLogHop + 1,
                        UserEmailActioned = userEmail,
                        UserRoleActionedTo = $"{currentUser.ClientCompany.Email}",
                        Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                        CurrentClaimOwner = currentUser.Email,
                        ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                        InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                        InvestigationCaseSubStatusId = assigned.InvestigationCaseSubStatusId,
                        UpdatedBy = currentUser.Email,
                        Updated = DateTime.Now
                    };
                    _context.InvestigationTransaction.Add(log);
                }
                _context.UpdateRange(cases2Assign);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<ClientCompany> WithdrawCaseByCompany(string userEmail, ClaimTransactionModel model, string claimId)
        {
            var currentUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var claimsInvestigation = _context.ClaimsInvestigation
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var withdrawnByCompany = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);

            claimsInvestigation.Updated = DateTime.Now;
            claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
            claimsInvestigation.CurrentUserEmail = userEmail;
            claimsInvestigation.AssignedToAgency = false;
            claimsInvestigation.CurrentClaimOwner = userEmail;
            claimsInvestigation.UserEmailActioned = userEmail;
            claimsInvestigation.UserEmailActionedTo = userEmail;
            claimsInvestigation.UserRoleActionedTo = $"{company.Email}";
            claimsInvestigation.CompanyWithdrawlComment = $"WITHDRAWN: {currentUser.Email} :{model.ClaimsInvestigation.CompanyWithdrawlComment}";
            claimsInvestigation.ActiveView = 0;
            claimsInvestigation.ManualNew = 0;
            claimsInvestigation.AllocateView = 0;
            claimsInvestigation.AutoNew = 0;
            claimsInvestigation.VendorId = null;
            claimsInvestigation.Vendor = null;

            claimsInvestigation.InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseSubStatusId = withdrawnByCompany.InvestigationCaseSubStatusId;


            var lastLog = _context.InvestigationTransaction
                .Where(i =>
                    i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                    .OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                .Where(i => i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                UserEmailActioned = userEmail,
                UserEmailActionedTo = userEmail,
                UserRoleActionedTo = $"{company.Email}",
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                CurrentClaimOwner = userEmail,
                ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = withdrawnByCompany.InvestigationCaseSubStatusId,
                UpdatedBy = currentUser.Email,
                Updated = DateTime.Now
            };
            _context.InvestigationTransaction.Add(log);
            _context.ClaimsInvestigation.Update(claimsInvestigation);
            try
            {
                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return company;
        }

        public async Task<Vendor> WithdrawCase(string userEmail, ClaimTransactionModel model, string claimId)
        {
            var currentUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(u => u.Email == userEmail);
            var claimsInvestigation = _context.ClaimsInvestigation
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var assigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);

            claimsInvestigation.Updated = DateTime.Now;
            claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
            claimsInvestigation.CurrentUserEmail = userEmail;
            claimsInvestigation.AssignedToAgency = false;
            claimsInvestigation.CurrentClaimOwner = currentUser.Email;
            claimsInvestigation.UserEmailActioned = userEmail;
            claimsInvestigation.UserEmailActionedTo = string.Empty;
            claimsInvestigation.AgencyDeclineComment = $"DECLINED: {currentUser.Email} :{model.ClaimsInvestigation.AgencyDeclineComment}";
            claimsInvestigation.ActiveView = 0;
            claimsInvestigation.AllocateView = 0;
            claimsInvestigation.AutoNew = 0;
            claimsInvestigation.VendorId = null;
            claimsInvestigation.UserRoleActionedTo = $"{company.Email}";
            claimsInvestigation.InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseSubStatusId = withdrawnByAgency.InvestigationCaseSubStatusId;
            var lastLog = _context.InvestigationTransaction
                .Where(i =>
                    i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                    .OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                .Where(i => i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                UserEmailActioned = userEmail,
                UserEmailActionedTo = string.Empty,
                UserRoleActionedTo = $"{company.Email}",
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                CurrentClaimOwner = currentUser.Email,
                ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = withdrawnByAgency.InvestigationCaseSubStatusId,
                UpdatedBy = currentUser.Email,
                Updated = DateTime.Now
            };
            _context.InvestigationTransaction.Add(log);
            _context.ClaimsInvestigation.Update(claimsInvestigation);
            try
            {
                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return currentUser.Vendor;
        }

        public async Task<ClaimsInvestigation> AllocateToVendor(string userEmail, string claimsInvestigationId, long vendorId, bool AutoAllocated = true)
        {
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorId);
            var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var allocatedToVendor = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            if (vendor != null)
            {
                var claimsCaseToAllocateToVendor = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);
                claimsCaseToAllocateToVendor.AssignedToAgency = true;
                claimsCaseToAllocateToVendor.Updated = DateTime.Now;
                claimsCaseToAllocateToVendor.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + " (" + currentUser.Email + ")";
                claimsCaseToAllocateToVendor.CurrentUserEmail = userEmail;

                claimsCaseToAllocateToVendor.EnablePassport = currentUser.ClientCompany.EnablePassport;
                claimsCaseToAllocateToVendor.AiEnabled = currentUser.ClientCompany.AiEnabled;
                claimsCaseToAllocateToVendor.EnableMedia = currentUser.ClientCompany.EnableMedia;

                claimsCaseToAllocateToVendor.InvestigationCaseSubStatusId = allocatedToVendor.InvestigationCaseSubStatusId;
                claimsCaseToAllocateToVendor.UserEmailActioned = userEmail;
                claimsCaseToAllocateToVendor.UserEmailActionedTo = string.Empty;
                claimsCaseToAllocateToVendor.UserRoleActionedTo = $"{vendor.Email}";
                claimsCaseToAllocateToVendor.Vendors.Add(vendor);
                claimsCaseToAllocateToVendor.VendorId = vendorId;
                claimsCaseToAllocateToVendor.AllocateView = 0;
                claimsCaseToAllocateToVendor.AutoAllocated = AutoAllocated;
                claimsCaseToAllocateToVendor.AllocatedToAgencyTime = DateTime.Now;
                claimsCaseToAllocateToVendor.CreatorSla = currentUser.ClientCompany.CreatorSla;
                claimsCaseToAllocateToVendor.AssessorSla = currentUser.ClientCompany.AssessorSla;
                claimsCaseToAllocateToVendor.SupervisorSla = currentUser.ClientCompany.SupervisorSla;
                claimsCaseToAllocateToVendor.AgentSla = currentUser.ClientCompany.AgentSla;
                claimsCaseToAllocateToVendor.UpdateAgentReport = currentUser.ClientCompany.UpdateAgentReport;
                claimsCaseToAllocateToVendor.UpdateAgentAnswer = currentUser.ClientCompany.UpdateAgentAnswer;
                _context.ClaimsInvestigation.Update(claimsCaseToAllocateToVendor);
                var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claimsCaseToAllocateToVendor.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

                var lastLogHop = _context.InvestigationTransaction
                        .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                        .AsNoTracking().Max(s => s.HopCount);
                string timeElapsed = GetTimeElaspedFromLog(lastLog);

                var log = new InvestigationTransaction
                {
                    UserEmailActioned = userEmail,
                    UserRoleActionedTo = $"{vendor.Email}",
                    UserEmailActionedTo = "",
                    HopCount = lastLogHop + 1,
                    ClaimsInvestigationId = claimsCaseToAllocateToVendor.ClaimsInvestigationId,
                    CurrentClaimOwner = claimsCaseToAllocateToVendor.CurrentClaimOwner,
                    Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                    InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = allocatedToVendor.InvestigationCaseSubStatusId,
                    UpdatedBy = currentUser.Email,
                    Updated = DateTime.Now,
                    TimeElapsed = timeElapsed
                };
                _context.InvestigationTransaction.Add(log);

                await _context.SaveChangesAsync();

                return claimsCaseToAllocateToVendor;
            }
            return null;
        }

        public async Task<ClaimsInvestigation> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, string claimsInvestigationId, string drivingMap, string drivingDistance, 
            string drivingDuration, string distanceInMeters, string durationInSeconds)
        {
            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var assignedToAgent = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var claim = _context.ClaimsInvestigation
                .Include(c=>c.PolicyDetail)
                .Where(c => c.ClaimsInvestigationId == claimsInvestigationId).FirstOrDefault();
            if (claim != null)
            {
                var agentUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(u => u.Email == vendorAgentEmail);
                claim.UserEmailActioned = currentUser;
                claim.UserEmailActionedTo = agentUser.Email;
                claim.UserRoleActionedTo = $"{AppRoles.AGENT.GetEnumDisplayName()} ({agentUser.Vendor.Email})";
                claim.Updated = DateTime.Now;
                claim.UpdatedBy = currentUser;
                claim.CurrentUserEmail = currentUser;
                claim.InvestigateView = 0;
                claim.NotWithdrawable = true;
                claim.NotDeclinable = true;
                claim.CurrentClaimOwner = agentUser.Email;
                claim.InvestigationCaseSubStatusId = assignedToAgent.InvestigationCaseSubStatusId;
                claim.SelectedAgentDrivingDistance = drivingDistance;
                claim.SelectedAgentDrivingDuration = drivingDuration;
                claim.SelectedAgentDrivingDistanceInMetres = float.Parse(distanceInMeters);
                claim.SelectedAgentDrivingDurationInSeconds = int.Parse(durationInSeconds);
                claim.SelectedAgentDrivingMap = drivingMap;
                claim.TaskToAgentTime = DateTime.Now;
                var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claim.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

                var lastLogHop = _context.InvestigationTransaction
                                        .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
                                        .AsNoTracking().Max(s => s.HopCount);
                string timeElapsed = GetTimeElaspedFromLog(lastLog);

                var log = new InvestigationTransaction
                {
                    UserEmailActioned = currentUser,
                    UserEmailActionedTo = agentUser.Email,
                    UserRoleActionedTo = $"{AppRoles.AGENT.GetEnumDisplayName()} ({agentUser.Vendor.Email})",
                    HopCount = lastLogHop + 1,
                    ClaimsInvestigationId = claim.ClaimsInvestigationId,
                    CurrentClaimOwner = claim.CurrentClaimOwner,
                    Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                    InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = assignedToAgent.InvestigationCaseSubStatusId,
                    UpdatedBy = currentUser,
                    Updated = DateTime.Now,
                    TimeElapsed = timeElapsed
                };
                _context.InvestigationTransaction.Add(log);
            }
            _context.ClaimsInvestigation.Update(claim);
            var rows = await _context.SaveChangesAsync();
            return claim;
        }

        public async Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, string claimsInvestigationId, string remarks, string? answer1, string? answer2, string? answer3, string? answer4)
        {
            var agent = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(a => a.Email.Trim().ToLower() == userEmail.ToLower());
            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                                   i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var submitted2Supervisor = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.ReportQuestionaire)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            claim.VerifyView = 0;
            claim.InvestigateView = 0;
            claim.UserEmailActioned = userEmail;
            claim.UserEmailActionedTo = string.Empty;
            claim.UserRoleActionedTo = $"{agent.Vendor.Email}";
            claim.Updated = DateTime.Now;
            claim.UpdatedBy = agent.FirstName + " " + agent.LastName + "(" + agent.Email + ")";
            claim.CurrentUserEmail = userEmail;
            claim.CurrentClaimOwner = userEmail;
            claim.InvestigationCaseSubStatusId = submitted2Supervisor.InvestigationCaseSubStatusId;
            claim.SubmittedToSupervisorTime = DateTime.Now;
            var claimReport = claim.AgencyReport;

            claimReport.ReportQuestionaire.Answer1 = answer1;

            Income? income = HtmlHelperExtensions.GetEnumFromDisplayName<Income>(answer2);
            claimReport.ReportQuestionaire.Answer2 = income.ToString();
            claimReport.ReportQuestionaire.Answer3 = answer3;
            claimReport.ReportQuestionaire.Answer4 = answer4;
            claimReport.AgentRemarks = remarks;
            claimReport.AgentRemarksUpdated = DateTime.Now;
            claimReport.AgentEmail = userEmail;

            var lastLog = _context.InvestigationTransaction.Where(i =>
               i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
                                       .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                ClaimsInvestigationId = claimsInvestigationId,
                UserEmailActioned = agent.Email,
                UserRoleActionedTo = $"{agent.Vendor.Email}",
                HopCount = lastLogHop + 1,
                CurrentClaimOwner = userEmail,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = submitted2Supervisor.InvestigationCaseSubStatusId,
                UpdatedBy = agent.Email,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog)
            };
            _context.InvestigationTransaction.Add(log);

            _context.ClaimsInvestigation.Update(claim);

            try
            {
                var rows = await _context.SaveChangesAsync();
                return (agent.Vendor, claim.PolicyDetail.ContractNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<ClaimsInvestigation> ProcessAgentReport(string userEmail, string supervisorRemarks, string claimsInvestigationId, SupervisorRemarkType reportUpdateStatus, IFormFile? claimDocument = null, string editRemarks = "")
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

        public async Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, string claimsInvestigationId, AssessorRemarkType reportUpdateStatus, string reportAiSummary)
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
                //PUT th case back in review list :: Assign back to Agent
                return await ReAssignToCreator(userEmail, claimsInvestigationId, assessorRemarks, reportUpdateStatus, reportAiSummary);
            }
        }

        private async Task<(ClientCompany, string)> RejectCaseReport(string userEmail, string assessorRemarks, string claimsInvestigationId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            var rejected = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            var finished = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            try
            {
                var claim = _context.ClaimsInvestigation
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.District)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.State)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.Country)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c=>c.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.District)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.State)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.Country)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.ClientCompany)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
                .Include(r => r.AgencyReport)
                .ThenInclude(r => r.DigitalIdReport)
                .Include(r => r.AgencyReport)
                .ThenInclude(r => r.PanIdReport)
                .Include(r => r.Vendor)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

                claim.AgencyReport.AiSummary = reportAiSummary;
                claim.AgencyReport.AssessorRemarkType = assessorRemarkType;
                claim.AgencyReport.AssessorRemarks = assessorRemarks;
                claim.AgencyReport.AssessorRemarksUpdated = DateTime.Now;
                claim.AgencyReport.AssessorEmail = userEmail;

                claim.InvestigationCaseStatusId = finished.InvestigationCaseStatusId;
                claim.InvestigationCaseSubStatusId = rejected.InvestigationCaseSubStatusId;
                claim.Updated = DateTime.Now;
                claim.UserEmailActioned = userEmail;
                claim.UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})";
                claim.UserEmailActionedTo = userEmail;
                claim.ProcessedByAssessorTime = DateTime.Now;
                _context.ClaimsInvestigation.Update(claim);

                var finalHop = _context.InvestigationTransaction
                                   .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                                    .AsNoTracking().Max(s => s.HopCount);
                var lastLog = _context.InvestigationTransaction.Where(i =>
              i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

                var finalLog = new InvestigationTransaction
                {
                    HopCount = finalHop + 1,
                    UserEmailActioned = userEmail,
                    UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})",
                    ClaimsInvestigationId = claimsInvestigationId,
                    CurrentClaimOwner = claim.CurrentClaimOwner,
                    Created = DateTime.Now,
                    Time2Update = DateTime.Now.Subtract(claim.Created).Days,
                    InvestigationCaseStatusId = finished.InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = rejected.InvestigationCaseSubStatusId,
                    UpdatedBy = userEmail,
                    Updated = DateTime.Now,
                    TimeElapsed = GetTimeElaspedFromLog(lastLog)
                };

                _context.InvestigationTransaction.Add(finalLog);

                //create invoice

                var vendor = _context.Vendor.Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == claim.VendorId);
                var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                var investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault(s => s.InvestigationServiceTypeId == claim.PolicyDetail.InvestigationServiceTypeId);

                //THIS SHOULD NOT HAPPEN IN PROD : demo purpose
                if (investigationServiced == null)
                {
                    investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault();
                }
                //END
                var investigatService = _context.InvestigationServiceType.FirstOrDefault(i => i.InvestigationServiceTypeId == claim.PolicyDetail.InvestigationServiceTypeId);

                var invoice = new VendorInvoice
                {
                    ClientCompanyId = currentUser.ClientCompany.ClientCompanyId,
                    GrandTotal = investigationServiced.Price + investigationServiced.Price * 10,
                    NoteToRecipient = "Auto generated Invoice",
                    Updated = DateTime.Now,
                    Vendor = vendor,
                    ClientCompany = currentUser.ClientCompany,
                    UpdatedBy = userEmail,
                    VendorId = vendor.VendorId,
                    AgencyReportId = claim.AgencyReport?.AgencyReportId,
                    SubTotal = investigationServiced.Price,
                    TaxAmount = investigationServiced.Price * 10,
                    InvestigationServiceType = investigatService,
                    ClaimId = claimsInvestigationId
                };

                _context.VendorInvoice.Add(invoice);
                
                var saveCount = await _context.SaveChangesAsync();

                Task.Run(() => DoTask(claim, claimsInvestigationId));

                return saveCount > 0 ? (currentUser.ClientCompany, claim.PolicyDetail.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return (null!, string.Empty);
        }
        
        private async Task<(ClientCompany, string)> ApproveCaseReport(string userEmail, string assessorRemarks, string claimsInvestigationId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            var approved = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var finished = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            try
            {
                var claim = _context.ClaimsInvestigation
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.District)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.State)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.Country)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.District)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.State)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.Country)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.ClientCompany)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
                .Include(r => r.AgencyReport)
                .ThenInclude(r => r.DigitalIdReport)
                .Include(r => r.AgencyReport)
                .ThenInclude(r => r.PanIdReport)
                .Include(r => r.Vendor)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
                .Include(r => r.AgencyReport)
                .Include(r => r.Vendor)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

                claim.AgencyReport.AiSummary = reportAiSummary;
                claim.AgencyReport.AssessorRemarkType = assessorRemarkType;
                claim.AgencyReport.AssessorRemarks = assessorRemarks;
                claim.AgencyReport.AssessorRemarksUpdated = DateTime.Now;
                claim.AgencyReport.AssessorEmail = userEmail;

                claim.InvestigationCaseStatusId = finished.InvestigationCaseStatusId;
                claim.InvestigationCaseSubStatusId = approved.InvestigationCaseSubStatusId;
                claim.Updated = DateTime.Now;
                claim.UserEmailActioned = userEmail;
                claim.UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})";
                claim.UserEmailActionedTo = userEmail;
                claim.ProcessedByAssessorTime = DateTime.Now;
                _context.ClaimsInvestigation.Update(claim);

                var finalHop = _context.InvestigationTransaction
                                   .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                                    .AsNoTracking().Max(s => s.HopCount);
                var lastLog = _context.InvestigationTransaction.Where(i =>
                             i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

                var finalLog = new InvestigationTransaction
                {
                    HopCount = finalHop + 1,
                    UserEmailActioned = userEmail,
                    UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})",
                    ClaimsInvestigationId = claimsInvestigationId,
                    CurrentClaimOwner = claim.CurrentClaimOwner,
                    Created = DateTime.Now,
                    Time2Update = DateTime.Now.Subtract(claim.Created).Days,
                    InvestigationCaseStatusId = finished.InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = approved.InvestigationCaseSubStatusId,
                    UpdatedBy = userEmail,
                    Updated = DateTime.Now,
                    TimeElapsed = GetTimeElaspedFromLog(lastLog)
                };

                _context.InvestigationTransaction.Add(finalLog);

                //create invoice

                var vendor = _context.Vendor.Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == claim.VendorId);
                var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                var investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault(s => s.InvestigationServiceTypeId == claim.PolicyDetail.InvestigationServiceTypeId);

                //THIS SHOULD NOT HAPPEN IN PROD : demo purpose
                if (investigationServiced == null)
                {
                    investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault();
                }
                //END
                var investigatService = _context.InvestigationServiceType.FirstOrDefault(i => i.InvestigationServiceTypeId == claim.PolicyDetail.InvestigationServiceTypeId);

                var invoice = new VendorInvoice
                {
                    ClientCompanyId = currentUser.ClientCompany.ClientCompanyId,
                    GrandTotal = investigationServiced.Price + investigationServiced.Price * 10,
                    NoteToRecipient = "Auto generated Invoice",
                    Updated = DateTime.Now,
                    Vendor = vendor,
                    ClientCompany = currentUser.ClientCompany,
                    UpdatedBy = userEmail,
                    VendorId = vendor.VendorId,
                    AgencyReportId = claim.AgencyReport?.AgencyReportId,
                    SubTotal = investigationServiced.Price,
                    TaxAmount = investigationServiced.Price * 10,
                    InvestigationServiceType = investigatService,
                    ClaimId = claimsInvestigationId
                };

                _context.VendorInvoice.Add(invoice);

                var saveCount = await _context.SaveChangesAsync();

                Task.Run(() => DoTask(claim, claimsInvestigationId));

                return saveCount > 0 ? (currentUser.ClientCompany, claim.PolicyDetail.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return (null!, string.Empty);
        }
        private async Task<int> DoTask(ClaimsInvestigation claim, string claimsInvestigationId)
        {
            //create and save report

            string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var filename = "report" + claimsInvestigationId + ".pdf";

            var filePath = Path.Combine(webHostEnvironment.WebRootPath, "report", filename);

            (await PdfReportRunner.Run(webHostEnvironment.WebRootPath, claim)).Build(filePath);

            claim.AgencyReport.PdfReportFilePath = filePath;

            var saveCount = await _context.SaveChangesAsync();
            return saveCount;
        }
        private async Task<(ClientCompany, string)> ReAssignToCreator(string userEmail, string claimsInvestigationId, string assessorRemarks, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

            var claimsCaseToReassign = _context.ClaimsInvestigation
                .Include(c => c.PreviousClaimReports)
                .Include(c => c.AgencyReport)
                .Include(c => c.AgencyReport.DigitalIdReport)
                .Include(c => c.AgencyReport.PanIdReport)
                .Include(c => c.AgencyReport.ReportQuestionaire)
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);


            claimsCaseToReassign.AgencyReport.AiSummary = reportAiSummary;
            claimsCaseToReassign.AgencyReport.AssessorRemarkType = assessorRemarkType;
            claimsCaseToReassign.AgencyReport.AssessorRemarks = assessorRemarks;
            claimsCaseToReassign.AgencyReport.AssessorRemarksUpdated = DateTime.Now;
            claimsCaseToReassign.ReviewByAssessorTime = DateTime.Now;
            claimsCaseToReassign.AgencyReport.AssessorEmail = userEmail;
            var reAssigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var saveReport = new PreviousClaimReport
            {
                ClaimsInvestigationId = claimsInvestigationId,
                AgentEmail = claimsCaseToReassign.AgencyReport.AgentEmail,
                DigitalIdReport = claimsCaseToReassign.AgencyReport.DigitalIdReport,
                PanIdReport = claimsCaseToReassign.AgencyReport.PanIdReport,
                AudioReport = claimsCaseToReassign.AgencyReport.AudioReport,
                VideoReport = claimsCaseToReassign.AgencyReport.VideoReport,
                PassportIdReport = claimsCaseToReassign.AgencyReport.PassportIdReport,
                AgentRemarks = claimsCaseToReassign.AgencyReport.AgentRemarks,
                AgentRemarksUpdated = claimsCaseToReassign.AgencyReport.AssessorRemarksUpdated,
                AssessorEmail = claimsCaseToReassign.AgencyReport.AssessorEmail,
                AssessorRemarks = claimsCaseToReassign.AgencyReport.AssessorRemarks,
                AssessorRemarkType = claimsCaseToReassign.AgencyReport.AssessorRemarkType,
                AssessorRemarksUpdated = claimsCaseToReassign.AgencyReport.AssessorRemarksUpdated,
                ReportQuestionaire = claimsCaseToReassign.AgencyReport.ReportQuestionaire,
                SupervisorEmail = claimsCaseToReassign.AgencyReport.SupervisorEmail,
                SupervisorRemarks = claimsCaseToReassign.AgencyReport.SupervisorRemarks,
                SupervisorRemarksUpdated = claimsCaseToReassign.AgencyReport.SupervisorRemarksUpdated,
                SupervisorRemarkType = claimsCaseToReassign.AgencyReport.SupervisorRemarkType,
                Updated = DateTime.Now,
                UpdatedBy = userEmail,
            };
            var currentSavedReport = _context.PreviousClaimReport.Add(saveReport);

            var newReport = new AgencyReport
            {
                ReportQuestionaire = new ReportQuestionaire(),
                PanIdReport = new DocumentIdReport(),
                PassportIdReport = new DocumentIdReport(),
                DigitalIdReport = new DigitalIdReport()
            };
            claimsCaseToReassign.PreviousClaimReports.Add(saveReport);
            claimsCaseToReassign.AgencyReport.DigitalIdReport = new DigitalIdReport();
            claimsCaseToReassign.AgencyReport.PanIdReport = new DocumentIdReport();
            claimsCaseToReassign.AgencyReport.PassportIdReport = new DocumentIdReport();
            claimsCaseToReassign.AgencyReport.ReportQuestionaire = new ReportQuestionaire();

            claimsCaseToReassign.AssignedToAgency = false;
            claimsCaseToReassign.ReviewCount += 1;
            claimsCaseToReassign.UserEmailActioned = userEmail;
            claimsCaseToReassign.UserEmailActionedTo = string.Empty;
            claimsCaseToReassign.UserRoleActionedTo = $"{currentUser.ClientCompany.Email}";
            claimsCaseToReassign.Updated = DateTime.Now;
            claimsCaseToReassign.UpdatedBy = userEmail;
            claimsCaseToReassign.VendorId = null;
            claimsCaseToReassign.CurrentUserEmail = userEmail;
            claimsCaseToReassign.IsReviewCase = true;
            claimsCaseToReassign.AssessView = 0;
            claimsCaseToReassign.ActiveView = 0;
            claimsCaseToReassign.AllocateView = 0;
            claimsCaseToReassign.VerifyView = 0;
            claimsCaseToReassign.AssessView = 0;
            claimsCaseToReassign.ManualNew = 0;
            claimsCaseToReassign.CurrentClaimOwner = currentUser.Email;
            claimsCaseToReassign.InvestigationCaseSubStatusId = reAssigned.InvestigationCaseSubStatusId;
            claimsCaseToReassign.ProcessedByAssessorTime = DateTime.Now;
            _context.ClaimsInvestigation.Update(claimsCaseToReassign);
            var lastLog = _context.InvestigationTransaction.Where(i =>
                            i.ClaimsInvestigationId == claimsCaseToReassign.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                IsReviewCase = true,
                HopCount = lastLogHop + 1,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{currentUser.ClientCompany.Email}",
                ClaimsInvestigationId = claimsCaseToReassign.ClaimsInvestigationId,
                Created = DateTime.Now,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = reAssigned.InvestigationCaseSubStatusId,
                UpdatedBy = userEmail,
                CurrentClaimOwner = currentUser.Email,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog)
            };
            _context.InvestigationTransaction.Add(log);

            return await _context.SaveChangesAsync() > 0 ? (currentUser.ClientCompany, claimsCaseToReassign.PolicyDetail.ContractNumber) : (null!, string.Empty);
        }

        private async Task<ClaimsInvestigation> ApproveAgentReport(string userEmail, string claimsInvestigationId,  string supervisorRemarks, SupervisorRemarkType reportUpdateStatus, IFormFile? claimDocument = null, string editRemarks = "")
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(c => c.AgencyReport)
                .Include(c => c.Vendor)
                .Include(c => c.ClientCompany)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            var submitted2Assessor = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();
            claim.AssignedToAgency = false;
            claim.IsReviewCase = false;
            claim.UserEmailActioned = userEmail;
            claim.UserEmailActionedTo = string.Empty;
            claim.UserRoleActionedTo = $"{claim.ClientCompany.Email}";
            claim.UserEmailActionedTo = string.Empty;
            claim.Updated = DateTime.Now;
            claim.UpdatedBy = userEmail;
            claim.NotDeclinable = true;
            claim.CurrentUserEmail = userEmail;
            claim.CurrentClaimOwner = userEmail;
            claim.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
            claim.InvestigationCaseSubStatusId = submitted2Assessor.InvestigationCaseSubStatusId;
            claim.SubmittedToAssessorTime = DateTime.Now;
            var report = claim.AgencyReport;
            var edited = report.AgentRemarks.Trim() != editRemarks.Trim();
            if(edited)
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

            report.Vendor = claim.Vendor;
            _context.AgencyReport.Update(report);
            _context.ClaimsInvestigation.Update(claim);

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                ClaimsInvestigationId = claimsInvestigationId,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{claim.ClientCompany.Email}",
                CurrentClaimOwner = userEmail,
                Created = DateTime.Now,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = submitted2Assessor.InvestigationCaseSubStatusId,
                UpdatedBy = userEmail,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog),
                AgentAnswerEdited = edited
            };
            _context.InvestigationTransaction.Add(log);
            _context.ClaimsInvestigation.Update(claim);
            try
            {
                return await _context.SaveChangesAsync() > 0 ? claim : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private async Task<ClaimsInvestigation> ReAllocateToVendorAgent(string userEmail, string claimsInvestigationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus)
        {
            var agencyUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(s => s.Email == userEmail);


            var claimsCaseToAllocateToVendor = _context.ClaimsInvestigation
                .Include(c => c.AgencyReport)
                .Include(c => c.PolicyDetail)
                .Include(p => p.ClientCompany)
                .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);

            var report = claimsCaseToAllocateToVendor.AgencyReport;
            report.SupervisorRemarkType = reportUpdateStatus;
            report.SupervisorRemarks = supervisorRemarks;

            claimsCaseToAllocateToVendor.UserEmailActioned = userEmail;
            claimsCaseToAllocateToVendor.UserRoleActionedTo = $"{AppRoles.AGENT.GetEnumDisplayName()} ({agencyUser.Vendor.Email})";
            claimsCaseToAllocateToVendor.Updated = DateTime.Now;
            claimsCaseToAllocateToVendor.UpdatedBy = userEmail;
            claimsCaseToAllocateToVendor.CurrentUserEmail = userEmail;
            claimsCaseToAllocateToVendor.IsReviewCase = true;
            claimsCaseToAllocateToVendor.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).InvestigationCaseSubStatusId;
            claimsCaseToAllocateToVendor.TaskToAgentTime = DateTime.Now;
            _context.ClaimsInvestigation.Update(claimsCaseToAllocateToVendor);

            var lastLog = _context.InvestigationTransaction.Where(i =>
                 i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                                        .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                 .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{claimsCaseToAllocateToVendor.ClientCompany.Email}",
                ClaimsInvestigationId = claimsInvestigationId,
                Created = DateTime.Now,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).InvestigationCaseSubStatusId,
                UpdatedBy = userEmail,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog)
            };
            _context.InvestigationTransaction.Add(log);

            return await _context.SaveChangesAsync() > 0 ? claimsCaseToAllocateToVendor : null;
        }

        public async Task<ClaimsInvestigation> SubmitQueryToAgency(string userEmail, string claimId, EnquiryRequest request, IFormFile messageDocument)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.AgencyReport)
                .Include(c => c.Vendor)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

            var requestedByAssessor = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);
            var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claim.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            claim.InvestigationCaseSubStatusId = requestedByAssessor.InvestigationCaseSubStatusId;
            claim.UpdatedBy = userEmail;
            claim.UserEmailActioned = userEmail;
            claim.AssignedToAgency = true;
            claim.IsQueryCase = true;
            claim.UserRoleActionedTo = $"{claim.Vendor.Email}";

            if (messageDocument != null)
            {
                using var ms = new MemoryStream();
                messageDocument.CopyTo(ms);
                request.QuestionImageAttachment = ms.ToArray();
                request.QuestionImageFileName = Path.GetFileName(messageDocument.FileName);
                request.QuestionImageFileExtension = Path.GetExtension(messageDocument.FileName);
                request.QuestionImageFileType = messageDocument.ContentType;
            }
            claim.AgencyReport.EnquiryRequest = request;
            claim.AgencyReport.Updated = DateTime.Now;
            claim.AgencyReport.UpdatedBy = userEmail;
            claim.AgencyReport.EnquiryRequest.Updated = DateTime.Now;
            claim.AgencyReport.EnquiryRequest.UpdatedBy = userEmail;
            claim.EnquiredByAssessorTime = DateTime.Now;
            _context.QueryRequest.Update(request);
            _context.ClaimsInvestigation.Update(claim);

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                ClaimsInvestigationId = claim.ClaimsInvestigationId,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{claim.Vendor.Email}",
                CurrentClaimOwner = userEmail,
                Created = DateTime.Now,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = requestedByAssessor.InvestigationCaseSubStatusId,
                UpdatedBy = userEmail,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog)
            };
            _context.InvestigationTransaction.Add(log);

            try
            {
                return await _context.SaveChangesAsync() > 0 ? claim : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<ClaimsInvestigation> SubmitQueryReplyToCompany(string userEmail, string claimId, EnquiryRequest request, IFormFile messageDocument, List<string> flexRadioDefault)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(p => p.ClientCompany)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.EnquiryRequest)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.EnquiryRequests)
                .Include(c => c.Vendor)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

            var replyByAgency = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR);
            var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claim.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            claim.InvestigationCaseSubStatusId = replyByAgency.InvestigationCaseSubStatusId;
            claim.UpdatedBy = userEmail;
            claim.UserEmailActioned = userEmail;
            claim.AssignedToAgency = false;
            claim.AssessView = 0;
            claim.UserRoleActionedTo = $"{claim.ClientCompany.Email}";
            claim.EnquiryReplyByAssessorTime = DateTime.Now;
            claim.SubmittedToAssessorTime = DateTime.Now;
            var enquiryRequest = claim.AgencyReport.EnquiryRequest;
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

            claim.AgencyReport.EnquiryRequests.Add(enquiryRequest);

            _context.QueryRequest.Update(enquiryRequest);
            claim.AgencyReport.EnquiryRequests.Add(enquiryRequest);
            _context.ClaimsInvestigation.Update(claim);

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                ClaimsInvestigationId = claim.ClaimsInvestigationId,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{claim.Vendor.Email}",
                CurrentClaimOwner = userEmail,
                Created = DateTime.Now,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = replyByAgency.InvestigationCaseSubStatusId,
                UpdatedBy = userEmail,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog)
            };
            _context.InvestigationTransaction.Add(log);

            try
            {
                return await _context.SaveChangesAsync() > 0 ? claim : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> SubmitNotes(string userEmail, string claimId, string notes)
        {
            var claim = _context.ClaimsInvestigation
               .Include(c => c.ClaimNotes)
               .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            claim.ClaimNotes.Add(new ClaimNote
            {
                Comment = notes,
                Sender = userEmail,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                UpdatedBy = userEmail
            });
            _context.ClaimsInvestigation.Update(claim);
            return await _context.SaveChangesAsync() > 0;
        }

        private string GetTimeElaspedFromLog(InvestigationTransaction lastLog)
        {
            string timeElapsed = string.Empty;
            if (DateTime.Now.Subtract(lastLog.Created).Days >= 1)
            {
                timeElapsed = DateTime.Now.Subtract(lastLog.Created).Days.ToString() + " days";
            }
            else if (DateTime.Now.Subtract(lastLog.Created).Hours >= 1)
            {
                timeElapsed = DateTime.Now.Subtract(lastLog.Created).Hours.ToString() + " hours";
            }
            else if (DateTime.Now.Subtract(lastLog.Created).Minutes >= 1)
            {
                timeElapsed = DateTime.Now.Subtract(lastLog.Created).Minutes.ToString() + " minutes";
            }
            else if (DateTime.Now.Subtract(lastLog.Created).Seconds >= 1)
            {
                timeElapsed = DateTime.Now.Subtract(lastLog.Created).Seconds.ToString() + " seconds";
            }
            else
            {
                timeElapsed = "Just Now";
            }
            return timeElapsed;
        }
    }
}