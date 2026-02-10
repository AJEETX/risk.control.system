using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Services.Common
{
    public interface IDashboardService
    {
        Task<Dictionary<string, int>> CalculateAgencyClaimStatus(string userEmail);

        Task<Dictionary<string, int>> CalculateAgencyUnderwritingStatus(string userEmail);

        Task<Dictionary<string, int>> CalculateAgentCaseStatus(string userEmail);

        Task<Dictionary<string, (int count1, int count2)>> CalculateWeeklyCaseStatus(string userEmail);

        Task<Dictionary<string, (int count1, int count2)>> CalculateMonthlyCaseStatus(string userEmail);

        Task<Dictionary<string, (int count1, int count2)>> CalculateCaseChart(string userEmail);

        Task<Dictionary<string, int>> CalculateWeeklyCaseStatusPieClaims(string userEmail);

        Task<Dictionary<string, int>> CalculateWeeklyCaseStatusPieUnderwritings(string userEmail);

        Task<TatResult> CalculateTimespan(string userEmail);
    }

    internal class DashboardService : IDashboardService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private const string allocatedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;
        private const string assignedToAgentStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
        private const string submitted2SuperStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR;
        private const string enquiryStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;

        public DashboardService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Dictionary<string, int>> CalculateAgencyClaimStatus(string userEmail)
        {
            return await CalculateAgencyCaseStatus(userEmail, InsuranceType.CLAIM);
        }

        public async Task<Dictionary<string, int>> CalculateAgencyUnderwritingStatus(string userEmail)
        {
            return await CalculateAgencyCaseStatus(userEmail, InsuranceType.UNDERWRITING);
        }

        private async Task<Dictionary<string, int>> CalculateAgencyCaseStatus(string userEmail, InsuranceType insuranceType)
        {
            var vendorCaseCount = new Dictionary<string, int>();
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            List<Vendor> existingVendors = await _context.Vendor.ToListAsync();

            if (companyUser == null)
            {
                return vendorCaseCount;
            }

            var claimsCases = _context.Investigations
               .Include(c => c.PolicyDetail).Where(c => c.PolicyDetail.InsuranceType == insuranceType &&
               !c.Deleted && c.VendorId.HasValue &&
                                (c.SubStatus == allocatedStatus ||
                                c.SubStatus == assignedToAgentStatus ||
                                c.SubStatus == enquiryStatus ||
                                c.SubStatus == submitted2SuperStatus));

            int countOfCases = 0;
            foreach (var claimsCase in claimsCases)
            {
                if (!vendorCaseCount.TryGetValue(claimsCase.VendorId.Value.ToString(), out countOfCases))
                {
                    vendorCaseCount.Add(claimsCase.VendorId.Value.ToString(), 1);
                }
                else
                {
                    int currentCount = vendorCaseCount[claimsCase.VendorId.Value.ToString()];
                    ++currentCount;
                    vendorCaseCount[claimsCase.VendorId.Value.ToString()] = currentCount;
                }
            }

            Dictionary<string, int> vendorWithCaseCounts = new();

            foreach (var existingVendor in existingVendors)
            {
                foreach (var vendorCase in vendorCaseCount)
                {
                    if (vendorCase.Key == existingVendor.VendorId.ToString())
                    {
                        vendorWithCaseCounts.Add(existingVendor.Name, vendorCase.Value);
                    }
                }
            }
            return vendorWithCaseCounts;
        }

        public async Task<Dictionary<string, int>> CalculateAgentCaseStatus(string userEmail)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // 1. Get the VendorId for the current user
            var vendorId = await _context.ApplicationUser
                .Where(u => u.Email == userEmail)
                .Select(u => u.VendorId)
                .FirstOrDefaultAsync();

            if (vendorId == null) return new Dictionary<string, int>();

            // 2. Fetch all non-admin agent emails first (to ensure 0-count agents are included)
            var agentEmails = await _context.ApplicationUser
                .Where(u => u.VendorId == vendorId && !u.IsVendorAdmin)
                .Select(u => u.Email)
                .ToListAsync();

            // 3. Let the Database do the heavy lifting: Group by Email and Count
            var caseCountsByAgent = await _context.Investigations
                .Where(c => c.VendorId == vendorId &&
                            !c.Deleted &&
                            c.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                            c.SubStatus == assignedToAgentStatus &&
                            c.TaskedAgentEmail != null)
                .GroupBy(c => c.TaskedAgentEmail.Trim().ToLower())
                .Select(g => new { Email = g.Key, Count = g.Count() })
                .ToListAsync();

            // 4. Merge results into the final dictionary
            var result = agentEmails.ToDictionary(email => email, _ => 0);

            foreach (var item in caseCountsByAgent)
            {
                // Find the original casing email in our dictionary to update the count
                var match = agentEmails.FirstOrDefault(e => e.Trim().ToLower() == item.Email);
                if (match != null)
                {
                    result[match] = item.Count;
                }
            }

            return result;
        }

        public async Task<Dictionary<string, (int count1, int count2)>> CalculateCaseChart(string userEmail)
        {
            Dictionary<string, (int count1, int count2)> dictMonthlySum = new Dictionary<string, (int count1, int count2)>();
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var startDate = new DateTime(DateTime.UtcNow.Year, 1, 1);
            var months = Enumerable.Range(0, 11)
                                   .Select(startDate.AddMonths)
                       .Select(m => m)
                       .ToList();
            if (companyUser != null)
            {
                var tdetail = _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .Where(d =>
                        (companyUser.Role == AppRoles.ASSESSOR || companyUser.IsClientAdmin || d.UpdatedBy == userEmail) &&
                     d.ClientCompanyId == companyUser.ClientCompanyId && !d.Deleted);

                var userSubStatuses = tdetail.Select(s => s.SubStatus).Distinct()?.ToList();

                var cases = tdetail.GroupBy(g => g.Id);

                foreach (var monthName in months)
                {
                    var claimsWithSameStatus = new List<InvestigationTask> { };
                    var underwritingWithSameStatus = new List<InvestigationTask> { };

                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();
                        if (userSubStatuses.Contains(caseCurrentStatus.SubStatus) &&
                            caseCurrentStatus.Created > monthName.Date &&
                            caseCurrentStatus.Created <= monthName.AddMonths(1))
                        {
                            if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.CLAIM && !caseCurrentStatus.Deleted)
                            {
                                claimsWithSameStatus.Add(caseCurrentStatus);
                            }
                            else if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING && !caseCurrentStatus.Deleted)
                            {
                                underwritingWithSameStatus.Add(caseCurrentStatus);
                            }
                        }
                    }

                    dictMonthlySum.Add(monthName.ToString("MMM"), (claimsWithSameStatus.Count, underwritingWithSameStatus.Count));
                }
            }
            else if (vendorUser != null)
            {
                var tdetail = _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .Where(d =>
                        (vendorUser.IsVendorAdmin ? true : d.UpdatedBy == userEmail) &&
                     d.VendorId == vendorUser.VendorId && !d.Deleted);

                var userSubStatuses = tdetail.Select(s => s.SubStatus).Distinct()?.ToList();

                var cases = tdetail.GroupBy(g => g.Id);

                foreach (var monthName in months)
                {
                    var claimsWithSameStatus = new List<InvestigationTask> { };
                    var underwritingWithSameStatus = new List<InvestigationTask> { };

                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();
                        if (userSubStatuses.Contains(caseCurrentStatus.SubStatus) && caseCurrentStatus.Created > monthName.Date && caseCurrentStatus.Created <= monthName.AddMonths(1))
                        {
                            if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.CLAIM)
                            {
                                claimsWithSameStatus.Add(caseCurrentStatus);
                            }
                            else if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
                            {
                                underwritingWithSameStatus.Add(caseCurrentStatus);
                            }
                        }
                    }

                    dictMonthlySum.Add(monthName.ToString("MMM"), (claimsWithSameStatus.Count, underwritingWithSameStatus.Count));
                }
            }
            return dictMonthlySum;
        }

        public async Task<Dictionary<string, (int count1, int count2)>> CalculateMonthlyCaseStatus(string userEmail)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            Dictionary<string, (int count1, int count2)> dictWeeklyCases = new Dictionary<string, (int count1, int count2)>();
            if (companyUser != null)
            {
                var tdetail = _context.Investigations
                    .Include(i => i.PolicyDetail).Where(d =>
                        (companyUser.Role == AppRoles.ASSESSOR || companyUser.IsClientAdmin || d.UpdatedBy == userEmail) &&
                       d.ClientCompanyId == companyUser.ClientCompanyId &&
                       d.Created > DateTime.UtcNow.AddMonths(-7) && !d.Deleted);
                var userSubStatuses = tdetail.Select(s => s.SubStatus).Distinct()?.ToList();

                var cases = tdetail.GroupBy(g => g.Id);

                foreach (var subStatus in userSubStatuses)
                {
                    var claimsWithSameStatus = new List<InvestigationTask> { };
                    var underwritingWithSameStatus = new List<InvestigationTask> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.CLAIM && !caseCurrentStatus.Deleted)
                        {
                            claimsWithSameStatus.Add(caseCurrentStatus);
                        }
                        else if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING && !caseCurrentStatus.Deleted)
                        {
                            underwritingWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictWeeklyCases.Add(subStatus, (claimsWithSameStatus.Count, underwritingWithSameStatus.Count));
                }
            }
            else if (vendorUser != null)
            {
                var tdetail = _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .Where(d =>
                        (vendorUser.IsVendorAdmin ? true : d.UpdatedBy == userEmail) &&
                     d.VendorId == vendorUser.VendorId &&
                       d.Created > DateTime.UtcNow.AddMonths(-7) && !d.Deleted);
                var userSubStatuses = tdetail.Select(s => s.SubStatus).Distinct()?.ToList();

                var cases = tdetail.GroupBy(g => g.Id);

                foreach (var subStatus in userSubStatuses)
                {
                    var claimsWithSameStatus = new List<InvestigationTask> { };
                    var underwritingWithSameStatus = new List<InvestigationTask> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.CLAIM && !caseCurrentStatus.Deleted)
                        {
                            claimsWithSameStatus.Add(caseCurrentStatus);
                        }
                        else if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING && !caseCurrentStatus.Deleted)
                        {
                            underwritingWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictWeeklyCases.Add(subStatus, (claimsWithSameStatus.Count, underwritingWithSameStatus.Count));
                }
            }
            return dictWeeklyCases;
        }

        public async Task<TatResult> CalculateTimespan(string userEmail)
        {
            var dictWeeklyCases = new Dictionary<string, List<int>>();
            var result = new List<TatDetail>();
            int totalStatusChanged = 0;
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            if (companyUser != null)
            {
                var tdetail = _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .Where(d =>
                    d.ClientCompanyId == companyUser.ClientCompanyId &&
                    (companyUser.Role == AppRoles.ASSESSOR || companyUser.IsClientAdmin || d.UpdatedBy == userEmail) &&
                    d.Created > DateTime.UtcNow.AddDays(-28) && !d.Deleted);

                var userSubStatuses = tdetail.Select(s => s.SubStatus).Distinct()?.ToList();

                var caseLogs = tdetail.GroupBy(g => g.SubStatus)?.ToList();

                var workDays = new List<int> { 1, 2, 3, 4, 5 };
                var casesWithSameStatus = new List<InvestigationTask> { };
                foreach (var userCaseStatus in userSubStatuses)
                {
                    List<int> caseListByStatus = new List<int>();

                    foreach (var caseWithSameStatus in caseLogs)
                    {
                        var casesWithCurrentStatus = caseWithSameStatus.Where(c => c.SubStatus == userCaseStatus);
                        for (int i = 0; i < workDays.Count; i++)
                        {
                            var caseWithCurrentWorkDay = casesWithCurrentStatus.Where(c => DateTime.UtcNow.Subtract(c.Updated.GetValueOrDefault()).TotalDays >= i && DateTime.UtcNow.Subtract(c.Updated.GetValueOrDefault()).TotalDays < i + 1);

                            caseListByStatus.Add(caseWithCurrentWorkDay.Count());
                            if (caseWithCurrentWorkDay.Count() > 0)
                            {
                                totalStatusChanged++;
                            }
                        }
                    }

                    dictWeeklyCases.Add(userCaseStatus, caseListByStatus);
                    result.Add(new TatDetail { Name = userCaseStatus, Data = caseListByStatus });
                }
            }
            else if (vendorUser != null)
            {
                var tdetail = _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .Where(d =>
                     d.VendorId == vendorUser.VendorId &&
                    (vendorUser.IsVendorAdmin || d.UpdatedBy == userEmail) &&
                    d.Created > DateTime.UtcNow.AddDays(-28) && !d.Deleted);

                var userSubStatuses = tdetail.Select(s => s.SubStatus).Distinct()?.ToList();

                var caseLogs = tdetail.GroupBy(g => g.SubStatus)?.ToList();

                var workDays = new List<int> { 1, 2, 3, 4, 5 };
                var casesWithSameStatus = new List<InvestigationTask> { };
                foreach (var userCaseStatus in userSubStatuses)
                {
                    List<int> caseListByStatus = new List<int>();

                    foreach (var caseWithSameStatus in caseLogs)
                    {
                        var casesWithCurrentStatus = caseWithSameStatus
                                                      .Where(c => c.SubStatus == userCaseStatus);
                        for (int i = 0; i < workDays.Count; i++)
                        {
                            var caseWithCurrentWorkDay = casesWithCurrentStatus.Where(c => DateTime.UtcNow.Subtract(c.Updated.GetValueOrDefault()).TotalDays >= i && DateTime.UtcNow.Subtract(c.Updated.GetValueOrDefault()).TotalDays < i + 1);

                            caseListByStatus.Add(caseWithCurrentWorkDay.Count());
                            if (caseWithCurrentWorkDay.Count() > 0)
                            {
                                totalStatusChanged++;
                            }
                        }
                    }

                    dictWeeklyCases.Add(userCaseStatus, caseListByStatus);
                    result.Add(new TatDetail { Name = userCaseStatus, Data = caseListByStatus });
                }
            }

            return new TatResult { Count = totalStatusChanged, TatDetails = result };
        }

        public async Task<Dictionary<string, (int count1, int count2)>> CalculateWeeklyCaseStatus(string userEmail)
        {
            Dictionary<string, (int count1, int count2)> dictWeeklyCases = new Dictionary<string, (int, int)>();

            await using var _context = await _contextFactory.CreateDbContextAsync();
            var tdetailDays = _context.Investigations
                    .Include(i => i.PolicyDetail)
                     .Where(d => d.Created > DateTime.UtcNow.AddDays(-28) && !d.Deleted);

            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser != null)
            {
                var tdetail = tdetailDays.Where(d =>
                    (companyUser.Role == AppRoles.ASSESSOR || companyUser.IsClientAdmin || d.UpdatedBy == userEmail) &&
                    d.ClientCompanyId == companyUser.ClientCompanyId);

                var userSubStatuses = tdetail.Select(s => s.SubStatus).Distinct()?.ToList();

                var cases = tdetail.GroupBy(g => g.Id);

                foreach (var subStatus in userSubStatuses)
                {
                    var claimsWithSameStatus = new List<InvestigationTask> { };
                    var underwritingWithSameStatus = new List<InvestigationTask> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null && caseCurrentStatus.SubStatus == subStatus && !caseCurrentStatus.Deleted)
                        {
                            if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.CLAIM)
                            {
                                claimsWithSameStatus.Add(caseCurrentStatus);
                            }
                            else if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
                            {
                                underwritingWithSameStatus.Add(caseCurrentStatus);
                            }
                        }
                    }
                    dictWeeklyCases.Add(subStatus, (claimsWithSameStatus.Count, underwritingWithSameStatus.Count));
                }
            }
            else if (vendorUser != null)
            {
                var tdetail = tdetailDays.Where(d =>
                    (vendorUser.IsVendorAdmin || d.UpdatedBy == userEmail) &&
                    d.VendorId == vendorUser.VendorId && !d.Deleted);

                var userSubStatuses = tdetail.Select(s => s.SubStatus).Distinct()?.ToList();

                var cases = tdetail.GroupBy(g => g.Id);

                foreach (var subStatus in userSubStatuses)
                {
                    var claimsWithSameStatus = new List<InvestigationTask> { };
                    var underwritingWithSameStatus = new List<InvestigationTask> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.CLAIM && !caseCurrentStatus.Deleted)
                        {
                            claimsWithSameStatus.Add(caseCurrentStatus);
                        }
                        else if (caseCurrentStatus.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING && !caseCurrentStatus.Deleted)
                        {
                            underwritingWithSameStatus.Add(caseCurrentStatus);
                        }
                    }
                    dictWeeklyCases.Add(subStatus, (claimsWithSameStatus.Count, underwritingWithSameStatus.Count));
                }
            }
            return dictWeeklyCases;
        }

        public async Task<Dictionary<string, int>> CalculateWeeklyCaseStatusPieClaims(string userEmail)
        {
            return await CalculateWeeklyCaseStatusPie(userEmail, InsuranceType.CLAIM);
        }

        public async Task<Dictionary<string, int>> CalculateWeeklyCaseStatusPieUnderwritings(string userEmail)
        {
            return await CalculateWeeklyCaseStatusPie(userEmail, InsuranceType.UNDERWRITING);
        }

        private async Task<Dictionary<string, int>> CalculateWeeklyCaseStatusPie(string userEmail, InsuranceType insuranceType)
        {
            Dictionary<string, int> dictWeeklyCases = new Dictionary<string, int>();

            await using var _context = await _contextFactory.CreateDbContextAsync();
            var tdetailDays = _context.Investigations
                    .Include(i => i.PolicyDetail)
                     .Where(d => d.Created > DateTime.UtcNow.AddDays(-28) && !d.Deleted && d.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                     (d.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY && d.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY));

            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser != null)
            {
                var tdetail = tdetailDays.Where(d =>
                    (
                    companyUser.Role == AppRoles.ASSESSOR ||
                    companyUser.Role == AppRoles.MANAGER ||
                    companyUser.IsClientAdmin ||
                    d.UpdatedBy == userEmail ||
                    (companyUser.Role == AppRoles.CREATOR && d.CreatedUser == companyUser.Email)
                    ) &&
                    d.ClientCompanyId == companyUser.ClientCompanyId);

                var userSubStatuses = tdetail.Select(s => s.SubStatus).Distinct()?.ToList();

                var cases = tdetail.GroupBy(g => g.Id);

                foreach (var subStatus in userSubStatuses)
                {
                    var casesWithSameStatus = new List<InvestigationTask> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null &&
                            caseCurrentStatus.SubStatus == subStatus &&
                            !caseCurrentStatus.Deleted &&
                            caseCurrentStatus.PolicyDetail.InsuranceType == insuranceType)
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }
                    dictWeeklyCases.Add(subStatus, casesWithSameStatus.Count);
                }
            }
            else if (vendorUser != null)
            {
                var tdetail = tdetailDays.Where(d =>
                    (vendorUser.IsVendorAdmin || d.UpdatedBy == userEmail) &&
                    d.VendorId == vendorUser.VendorId);

                var userSubStatuses = tdetail.Select(s => s.SubStatus).Distinct()?.ToList();

                var cases = tdetail.GroupBy(g => g.Id);

                foreach (var subStatus in userSubStatuses)
                {
                    var casesWithSameStatus = new List<InvestigationTask> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null && caseCurrentStatus.SubStatus == subStatus)
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }
                    dictWeeklyCases.Add(subStatus, casesWithSameStatus.Count);
                }
            }
            return dictWeeklyCases;
        }
    }

    public class TatDetail
    {
        public string Name { get; set; }
        public List<int> Data { get; set; }
    }

    public class TatResult
    {
        public List<TatDetail> TatDetails { get; set; }
        public int Count { get; set; }
    }
}