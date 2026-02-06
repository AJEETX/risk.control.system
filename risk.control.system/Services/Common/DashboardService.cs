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
        private readonly ApplicationDbContext _context;
        private const string allocatedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;
        private const string assignedToAgentStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
        private const string submitted2SuperStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR;
        private const string enquiryStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;

        public DashboardService(ApplicationDbContext context)
        {
            this._context = context;
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
            var vendorCaseCount = new Dictionary<string, int>();

            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var existingVendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);

            var claimsCases = _context.Investigations
               .Include(c => c.Vendor)
               .Include(c => c.BeneficiaryDetail).Where(c => c.VendorId == vendorUser.VendorId &&
               !c.Deleted &&
               c.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
               c.SubStatus == assignedToAgentStatus);

            int countOfCases = 0;

            var agentCaseCount = new Dictionary<string, int>();

            var vendorUsers = _context.ApplicationUser.Where(u =>
            u.VendorId == existingVendor.VendorId && !u.IsVendorAdmin);

            foreach (var vendorNonAdminUser in vendorUsers)
            {
                vendorCaseCount.Add(vendorNonAdminUser.Email, 0);

                foreach (var claimsCase in claimsCases)
                {
                    if (claimsCase.TaskedAgentEmail?.Trim()?.ToLower() == vendorNonAdminUser.Email.Trim().ToLower())
                    {
                        if (!vendorCaseCount.TryGetValue(vendorNonAdminUser.Email, out countOfCases))
                        {
                            vendorCaseCount.Add(vendorNonAdminUser.Email, 1);
                        }
                        else
                        {
                            int currentCount = vendorCaseCount[vendorNonAdminUser.Email];
                            ++currentCount;
                            vendorCaseCount[vendorNonAdminUser.Email] = currentCount;
                        }
                    }
                }
            }

            return vendorCaseCount;
        }

        public async Task<Dictionary<string, (int count1, int count2)>> CalculateCaseChart(string userEmail)
        {
            Dictionary<string, (int count1, int count2)> dictMonthlySum = new Dictionary<string, (int count1, int count2)>();
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var startDate = new DateTime(DateTime.Now.Year, 1, 1);
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
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            Dictionary<string, (int count1, int count2)> dictWeeklyCases = new Dictionary<string, (int count1, int count2)>();
            if (companyUser != null)
            {
                var tdetail = _context.Investigations
                    .Include(i => i.PolicyDetail).Where(d =>
                        (companyUser.Role == AppRoles.ASSESSOR || companyUser.IsClientAdmin || d.UpdatedBy == userEmail) &&
                       d.ClientCompanyId == companyUser.ClientCompanyId &&
                       d.Created > DateTime.Now.AddMonths(-7) && !d.Deleted);
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
                       d.Created > DateTime.Now.AddMonths(-7) && !d.Deleted);
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
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            if (companyUser != null)
            {
                var tdetail = _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .Where(d =>
                    d.ClientCompanyId == companyUser.ClientCompanyId &&
                    (companyUser.Role == AppRoles.ASSESSOR || companyUser.IsClientAdmin || d.UpdatedBy == userEmail) &&
                    d.Created > DateTime.Now.AddDays(-28) && !d.Deleted);

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
                            var caseWithCurrentWorkDay = casesWithCurrentStatus.Where(c => DateTime.Now.Subtract(c.Updated.GetValueOrDefault()).TotalDays >= i && DateTime.Now.Subtract(c.Updated.GetValueOrDefault()).TotalDays < i + 1);

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
                    d.Created > DateTime.Now.AddDays(-28) && !d.Deleted);

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
                            var caseWithCurrentWorkDay = casesWithCurrentStatus.Where(c => DateTime.Now.Subtract(c.Updated.GetValueOrDefault()).TotalDays >= i && DateTime.Now.Subtract(c.Updated.GetValueOrDefault()).TotalDays < i + 1);

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

            var tdetailDays = _context.Investigations
                    .Include(i => i.PolicyDetail)
                     .Where(d => d.Created > DateTime.Now.AddDays(-28) && !d.Deleted);

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

            var tdetailDays = _context.Investigations
                    .Include(i => i.PolicyDetail)
                     .Where(d => d.Created > DateTime.Now.AddDays(-28) && !d.Deleted && d.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
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