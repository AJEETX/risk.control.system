using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Api
{
    public interface ICompanyDashboardService
    {
        Task<DashboardData> GetCreatorCount(string userEmail, string role);

        Task<DashboardData> GetCompanyAdminCount(string userEmail, string role);

        Task<DashboardData> GetAssessorCount(string userEmail, string role);

        Task<DashboardData> GetManagerCount(string userEmail, string role);
    }

    internal class CompanyDashboardService : ICompanyDashboardService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ICreatorDashboardService creatorDashboardService;
        private readonly IAssessorDashboardService assessorDashboardService;
        private readonly IManagerDashboardService managerDashboardService;

        public CompanyDashboardService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ICreatorDashboardService creatorDashboardService,
            IAssessorDashboardService assessorDashboardService,
            IManagerDashboardService managerDashboardService)
        {
            _contextFactory = contextFactory;
            this.creatorDashboardService = creatorDashboardService;
            this.assessorDashboardService = assessorDashboardService;
            this.managerDashboardService = managerDashboardService;
        }

        public async Task<DashboardData> GetCreatorCount(string userEmail, string role)
        {
            var creatorData = await creatorDashboardService.GetCreatorCount(userEmail, role);
            return creatorData;
        }

        public async Task<DashboardData> GetCompanyAdminCount(string userEmail, string role)
        {
            await using var _context = _contextFactory.CreateDbContext();

            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);

            var companyUsersCount = await _context.ApplicationUser.CountAsync(u => u.ClientCompanyId == companyUser.ClientCompanyId && !u.Deleted && u.Email != userEmail);
            var data = new DashboardData();
            data.FirstBlockName = "All Users";
            data.FirstBlockCount = companyUsersCount;
            data.FirstBlockUrl = "/ManageCompanyUser/Users";

            return data;
        }

        public async Task<DashboardData> GetAssessorCount(string userEmail, string role)
        {
            var assessorData = await assessorDashboardService.GetAssessorCount(userEmail, role);

            return assessorData;
        }

        public async Task<DashboardData> GetManagerCount(string userEmail, string role)
        {
            var managerData = await managerDashboardService.GetManagerCount(userEmail, role);
            return managerData;
        }
    }
}