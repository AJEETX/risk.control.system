using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers.Api.Claims
{
    public interface ICaseService
    {
        IQueryable<InvestigationTask> GetCasesWithDetail();
        Task<InvestigationTask> GetCaseById(long id);
        Task<InvestigationTask> GetCaseByIdForMedia(long id);
        Task<InvestigationTask> GetCaseByIdForQuestions(long id);
    }
    internal class CaseService : ICaseService
    {
        private readonly ApplicationDbContext context;

        public CaseService(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<InvestigationTask> GetCaseById(long id)
        {
            var _case = await context.Investigations
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationReport)
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CaseNotes)
                .FirstOrDefaultAsync(c => c.Id == id);
            return _case;
        }

        public async Task<InvestigationTask> GetCaseByIdForMedia(long id)
        {
            var claim = await context.Investigations
                 .Include(c => c.PolicyDetail)
                 .Include(c => c.CustomerDetail)
                 .Include(c => c.BeneficiaryDetail)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationReport)
                .FirstOrDefaultAsync(c => c.Id == id);
            return claim;
        }

        public async Task<InvestigationTask> GetCaseByIdForQuestions(long id)
        {
            var claim = await context.Investigations
                    .Include(c => c.InvestigationReport)
                    .ThenInclude(c => c.ReportTemplate)
                    .ThenInclude(c => c.LocationReport)
                    .FirstOrDefaultAsync(c => c.Id == id);
            return claim;
        }

        public IQueryable<InvestigationTask> GetCasesWithDetail()
        {
            IQueryable<InvestigationTask> applicationDbContext = context.Investigations
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CostCentre)
               .Include(c => c.ClientCompany)
               .ThenInclude(c => c.Country)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(c => c.BeneficiaryRelation)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.Country)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.State)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.District)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.Vendor)
               .Include(c => c.CaseNotes)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderByDescending(o => o.Created);
        }
    }
}