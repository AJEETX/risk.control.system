using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Api
{
    public interface ICompanyDashboardService
    {
        Task<DashboardData> GetCompanyUserCount(string userEmail, string role);
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

        public async Task<DashboardData> GetCompanyUserCount(string userEmail, string role)
        {
            DashboardData data = null!;
            if (role == COMPANY_ADMIN.DISPLAY_NAME)
            {
                data = await GetCompanyAdminCount(userEmail, role);
            }
            else if (role == CREATOR.DISPLAY_NAME)
            {
                data = await GetCreatorCount(userEmail, role);
            }
            else if (role == ASSESSOR.DISPLAY_NAME)
            {
                data = await GetAssessorCount(userEmail, role);
            }
            else if (role == MANAGER.DISPLAY_NAME)
            {
                data = await GetManagerCount(userEmail, role);
            }
            return data;
        }
        private async Task<DashboardData> GetCreatorCount(string userEmail, string role)
        {
            var creatorData = await creatorDashboardService.GetCreatorCount(userEmail);
            return creatorData;
        }

        private async Task<DashboardData> GetCompanyAdminCount(string userEmail, string role)
        {
            await using var _context = _contextFactory.CreateDbContext();

            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);

            var companyUsersCount = await _context.ApplicationUser.CountAsync(u => u.ClientCompanyId == companyUser!.ClientCompanyId && !u.Deleted && u.Email != userEmail);
            var data = new DashboardData
            {
                FirstBlockName = "All Users",
                FirstBlockCount = companyUsersCount,
                FirstBlockUrl = "/ManageCompanyUser/Users"
            };

            return data;
        }

        private async Task<DashboardData> GetAssessorCount(string userEmail, string role)
        {
            var assessorData = await assessorDashboardService.GetAssessorCount(userEmail, role);

            return assessorData;
        }

        private async Task<DashboardData> GetManagerCount(string userEmail, string role)
        {
            var managerData = await managerDashboardService.GetManagerCount(userEmail, role);
            return managerData;
        }

    }
}