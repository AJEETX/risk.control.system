using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers.Api.PortalAdmin
{
    public interface IAdminDashBoardService
    {
        Task<DashboardData> GetSuperAdminCount(string userEmail, string role);
    }

    internal class AdminDashBoardService : IAdminDashBoardService
    {
        private readonly ApplicationDbContext _context;

        public AdminDashBoardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardData> GetSuperAdminCount(string userEmail, string role)
        {
            var allCompaniesCountTask = await _context.ClientCompany.CountAsync(c => !c.Deleted);
            var allAgenciesCountTask = await _context.Vendor.CountAsync(v => !v.Deleted);
            var AllUsersCountTask = await _context.ApplicationUser.CountAsync(u => !u.Deleted && u.Email != userEmail);

            //await Task.WhenAll(allCompaniesCountTask, allAgenciesCountTask, AllUsersCountTask);

            return new DashboardData
            {
                FirstBlockName = "Companies",
                FirstBlockCount = allCompaniesCountTask,
                FirstBlockUrl = "/ClientCompany/Companies",

                SecondBlockName = "Agencies",
                SecondBlockCount = allAgenciesCountTask,
                SecondBlockUrl = "/ClientCompany/Agencies",

                ThirdBlockName = "Users",
                ThirdBlockCount = AllUsersCountTask,
                ThirdBlockUrl = "/User"
            };
        }
    }
}