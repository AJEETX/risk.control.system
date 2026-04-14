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

    internal class CompanyDashboardService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICreatorDashboardService creatorDashboardService,
        IAssessorDashboardService assessorDashboardService,
        IManagerDashboardService managerDashboardService) : ICompanyDashboardService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
        private readonly ICreatorDashboardService _creatorDashboardService = creatorDashboardService;
        private readonly IAssessorDashboardService _assessorDashboardService = assessorDashboardService;
        private readonly IManagerDashboardService _managerDashboardService = managerDashboardService;

        public async Task<DashboardData> GetCompanyUserCount(string userEmail, string role)
        {
            if (role == COMPANY_ADMIN.DISPLAY_NAME)
            {
                return await GetCompanyAdminCount(userEmail, role);
            }
            else if (role == CREATOR.DISPLAY_NAME)
            {
                return await GetCreatorCount(userEmail, role);
            }
            else if (role == ASSESSOR.DISPLAY_NAME)
            {
                return await GetAssessorCount(userEmail, role);
            }
            else if (role == MANAGER.DISPLAY_NAME)
            {
                return await GetManagerCount(userEmail, role);
            }
            return null!;
        }
        private async Task<DashboardData> GetCreatorCount(string userEmail, string role)
        {
            var creatorData = await _creatorDashboardService.GetCreatorCount(userEmail);
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
            var assessorData = await _assessorDashboardService.GetAssessorCount(userEmail, role);

            return assessorData;
        }

        private async Task<DashboardData> GetManagerCount(string userEmail, string role)
        {
            var managerData = await _managerDashboardService.GetManagerCount(userEmail, role);
            return managerData;
        }

    }
}