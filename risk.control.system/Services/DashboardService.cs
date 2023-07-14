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

        Dictionary<string, double> CalculateTimespan(string userEmail);
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
            if (companyUser != null)
            {
                var startDate = new DateTime(DateTime.Now.Year, 1, 1);
                var months = Enumerable.Range(0, 11)
                                       .Select(startDate.AddMonths)
                           .Select(m => m)
                           .ToList();
                var txn = _context.InvestigationTransaction;
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
            return dictMonthlySum;
        }

        public Dictionary<string, int> CalculateMonthlyCaseStatus(string userEmail)
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            Dictionary<string, int> dictWeeklyCases = new Dictionary<string, int>();
            if (companyUser != null)
            {
                var statuses = _context.InvestigationCaseStatus;
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
            return dictWeeklyCases;
        }

        public Dictionary<string, double> CalculateTimespan(string userEmail)
        {
            Dictionary<string, double> dictWeeklyCases = new Dictionary<string, double>();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            if (companyUser != null)
            {
                var statuses = _context.InvestigationCaseStatus;
                var tdetail = _context.InvestigationTransaction
                    .Include(i => i.ClaimsInvestigation).Where(d =>
                    d.ClaimsInvestigation.ClientCompanyId == companyUser.ClientCompanyId &&
                    (companyUser.IsClientAdmin ? true : d.UpdatedBy == userEmail) &&
                    d.Created > DateTime.Now.AddDays(-28));

                var userSubStatuses = tdetail.Select(s => s.InvestigationCaseSubStatusId).Distinct()?.ToList();
                var subStatuses = _context.InvestigationCaseSubStatus;
                var filteredCases = subStatuses.Where(c => userSubStatuses.Contains(c.InvestigationCaseSubStatusId));

                var cases = tdetail.GroupBy(g => g.ClaimsInvestigationId)?.ToList();

                var workDays = new List<int> { 1, 2, 3, 4, 5 };
                var casesWithSameStatus = new List<InvestigationTransaction> { };

                for (int i = 0; i < workDays.Count; i++)
                {
                    foreach (var caseWithStatuses in cases)
                    {
                        var caseCurrentStatusOrderedByTime = caseWithStatuses.OrderBy(o => o.Created);

                        var caseWithCurrentWorkDay = caseCurrentStatusOrderedByTime
                            .Where(c => c.Time2Update >= i &&
                            c.Time2Update < i + 1
                            );

                        if (caseWithCurrentWorkDay?.Count() > 0)
                        {
                            dictWeeklyCases.Add(i.ToString() + " Days", caseCurrentStatusOrderedByTime.Count());
                        }
                    }
                }
            }
            return dictWeeklyCases;
        }

        public Dictionary<string, int> CalculateWeeklyCaseStatus(string userEmail)
        {
            Dictionary<string, int> dictWeeklyCases = new Dictionary<string, int>();

            var tdetailDays = _context.InvestigationTransaction
             .Include(i => i.ClaimsInvestigation).Where(d =>
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
            return dictWeeklyCases;
        }
    }
}