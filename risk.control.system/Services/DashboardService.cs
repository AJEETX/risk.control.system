using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IDashboardService
    {
        Dictionary<string, int> CalculateWeeklyCaseStatus(string userEmail);

        Dictionary<string, int> CalculateMonthlyCaseStatus(string userEmail);

        Dictionary<string, int> CalculateCaseChart(string userEmail);

        TatResult CalculateTimespan(string userEmail);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public Dictionary<string, int> CalculateCaseChart(string userEmail)
        {
            Dictionary<string, int> dictMonthlySum = new Dictionary<string, int>();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var startDate = new DateTime(DateTime.Now.Year, 1, 1);
            var months = Enumerable.Range(0, 11)
                                   .Select(startDate.AddMonths)
                       .Select(m => m)
                       .ToList();
            var txn = _context.InvestigationTransaction;
            if (companyUser != null)
            {
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation).Where(d =>
                        (companyUser.IsClientAdmin ? true : d.UpdatedBy == userEmail) &&
                     d.ClaimsInvestigation.ClientCompanyId == companyUser.ClientCompanyId);

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var monthName in months)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };

                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();
                        if (userSubStatuses.Contains(caseCurrentStatus.InvestigationCaseSubStatusId) && caseCurrentStatus.Created > monthName.Date && caseCurrentStatus.Created <= monthName.AddMonths(1))
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictMonthlySum.Add(monthName.ToString("MMM"), casesWithSameStatus.Count);
                }
            }
            else if (vendorUser != null)
            {
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation)
                    .ThenInclude(i => i.CaseLocations)
                    .Where(d =>
                        (vendorUser.IsVendorAdmin ? true : d.UpdatedBy == userEmail) &&
                     d.ClaimsInvestigation.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var monthName in months)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };

                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();
                        if (userSubStatuses.Contains(caseCurrentStatus.InvestigationCaseSubStatusId) && caseCurrentStatus.Created > monthName.Date && caseCurrentStatus.Created <= monthName.AddMonths(1))
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictMonthlySum.Add(monthName.ToString("MMM"), casesWithSameStatus.Count);
                }
            }
            return dictMonthlySum;
        }

        public Dictionary<string, int> CalculateMonthlyCaseStatus(string userEmail)
        {
            var statuses = _context.InvestigationCaseStatus;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            Dictionary<string, int> dictWeeklyCases = new Dictionary<string, int>();
            if (companyUser != null)
            {
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation).Where(d =>
                        (companyUser.IsClientAdmin ? true : d.UpdatedBy == userEmail) &&
                       d.ClaimsInvestigation.ClientCompanyId == companyUser.ClientCompanyId &&
                       d.Created > DateTime.Now.AddMonths(-7));
                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var subStatus in filteredCases)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null && caseCurrentStatus.InvestigationCaseSubStatusId == subStatus.InvestigationCaseSubStatusId)
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictWeeklyCases.Add(subStatus.Name, casesWithSameStatus.Count);
                }
            }
            else if (vendorUser != null)
            {
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation)
                    .ThenInclude(i => i.CaseLocations)
                    .Where(d =>
                        (vendorUser.IsVendorAdmin ? true : d.UpdatedBy == userEmail) &&
                     d.ClaimsInvestigation.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId) &&
                       d.Created > DateTime.Now.AddMonths(-7));
                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var subStatus in filteredCases)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null && caseCurrentStatus.InvestigationCaseSubStatusId == subStatus.InvestigationCaseSubStatusId)
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }

                    dictWeeklyCases.Add(subStatus.Name, casesWithSameStatus.Count);
                }
            }
            return dictWeeklyCases;
        }

        public TatResult CalculateTimespan(string userEmail)
        {
            var dictWeeklyCases = new Dictionary<string, List<int>>();
            var result = new List<TatDetail>();
            int totalStatusChanged = 0;
            var statuses = _context.InvestigationCaseStatus;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            if (companyUser != null)
            {
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation).Where(d =>
                    d.ClaimsInvestigation.ClientCompanyId == companyUser.ClientCompanyId &&
                    (companyUser.IsClientAdmin ? true : d.UpdatedBy == userEmail) &&
                    d.Created > DateTime.Now.AddDays(-28));

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var userCaseStatuses = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var caseLogs = tdetail.GroupBy(g => g.InvestigationCaseSubStatusId)?.ToList();

                var workDays = new List<int> { 1, 2, 3, 4, 5 };
                var casesWithSameStatus = new List<InvestigationTransaction> { };
                foreach (var userCaseStatus in userCaseStatuses)
                {
                    List<int> caseListByStatus = new List<int>();

                    var caseLogsByStatus = caseLogs.Where(
                        c => c.Key == userCaseStatus.InvestigationCaseSubStatusId);

                    foreach (var caseWithSameStatus in caseLogsByStatus)
                    {
                        var casesWithCurrentStatus = caseWithSameStatus
                                                      .Where(c => c.InvestigationCaseSubStatusId == userCaseStatus.InvestigationCaseSubStatusId);
                        for (int i = 0; i < workDays.Count; i++)
                        {
                            var caseWithCurrentWorkDay = casesWithCurrentStatus.Where(c => c.Time2Update >= i && c.Time2Update < i + 1);

                            caseListByStatus.Add(caseWithCurrentWorkDay.Count());
                            if (caseWithCurrentWorkDay.Count() > 0)
                            {
                                totalStatusChanged++;
                            }
                        }
                    }

                    dictWeeklyCases.Add(userCaseStatus.Name, caseListByStatus);
                    result.Add(new TatDetail { Name = userCaseStatus.Name, Data = caseListByStatus });
                }
            }
            else if (vendorUser != null)
            {
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation)
                    .ThenInclude(i => i.CaseLocations)
                    .Where(d =>
                     d.ClaimsInvestigation.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId) &&
                    (vendorUser.IsVendorAdmin ? true : d.UpdatedBy == userEmail) &&
                    d.Created > DateTime.Now.AddDays(-28));

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var userCaseStatuses = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var caseLogs = tdetail.GroupBy(g => g.InvestigationCaseSubStatusId)?.ToList();

                var workDays = new List<int> { 1, 2, 3, 4, 5 };
                var casesWithSameStatus = new List<InvestigationTransaction> { };
                foreach (var userCaseStatus in userCaseStatuses)
                {
                    List<int> caseListByStatus = new List<int>();

                    var caseLogsByStatus = caseLogs.Where(
                        c => c.Key == userCaseStatus.InvestigationCaseSubStatusId);

                    foreach (var caseWithSameStatus in caseLogsByStatus)
                    {
                        var casesWithCurrentStatus = caseWithSameStatus
                                                      .Where(c => c.InvestigationCaseSubStatusId == userCaseStatus.InvestigationCaseSubStatusId);
                        for (int i = 0; i < workDays.Count; i++)
                        {
                            var caseWithCurrentWorkDay = casesWithCurrentStatus.Where(c => c.Time2Update >= i && c.Time2Update < i + 1);

                            caseListByStatus.Add(caseWithCurrentWorkDay.Count());
                            if (caseWithCurrentWorkDay.Count() > 0)
                            {
                                totalStatusChanged++;
                            }
                        }
                    }

                    dictWeeklyCases.Add(userCaseStatus.Name, caseListByStatus);
                    result.Add(new TatDetail { Name = userCaseStatus.Name, Data = caseListByStatus });
                }
            }

            return new TatResult { Count = totalStatusChanged, TatDetails = result };
        }

        public Dictionary<string, int> CalculateWeeklyCaseStatus(string userEmail)
        {
            Dictionary<string, int> dictWeeklyCases = new Dictionary<string, int>();

            var tdetailDays = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation)
             .ThenInclude(i => i.CaseLocations)
             .Where(d =>
             d.Created > DateTime.Now.AddDays(-28));

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            if (companyUser != null)
            {
                var statuses = _context.InvestigationCaseStatus;
                var tdetail = tdetailDays.Where(d =>
                    (companyUser.IsClientAdmin ? true : d.UpdatedBy == userEmail) &&
                    d.ClaimsInvestigation.ClientCompanyId == companyUser.ClientCompanyId);

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var subStatus in filteredCases)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null && caseCurrentStatus.InvestigationCaseSubStatusId == subStatus.InvestigationCaseSubStatusId)
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }
                    dictWeeklyCases.Add(subStatus.Name, casesWithSameStatus.Count);
                }
            }
            else if (vendorUser != null)
            {
                var statuses = _context.InvestigationCaseStatus;
                var tdetail = tdetailDays.Where(d =>
                    (vendorUser.IsVendorAdmin ? true : d.UpdatedBy == userEmail) &&
                    d.ClaimsInvestigation.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId);

                foreach (var subStatus in filteredCases)
                {
                    var casesWithSameStatus = new List<InvestigationTransaction> { };
                    foreach (var _case in cases)
                    {
                        var caseCurrentStatus = _case.OrderByDescending(o => o.Created).FirstOrDefault();

                        if (caseCurrentStatus != null && caseCurrentStatus.InvestigationCaseSubStatusId == subStatus.InvestigationCaseSubStatusId)
                        {
                            casesWithSameStatus.Add(caseCurrentStatus);
                        }
                    }
                    dictWeeklyCases.Add(subStatus.Name, casesWithSameStatus.Count);
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