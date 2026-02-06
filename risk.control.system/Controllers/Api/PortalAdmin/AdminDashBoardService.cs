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
            var allCompaniesCountTask = _context.ClientCompany.CountAsync(c => !c.Deleted);
            var allAgenciesCountTask = _context.Vendor.CountAsync(v => !v.Deleted);
            var AllUsersCountTask = _context.ApplicationUser.CountAsync(u => !u.Deleted && u.Email != userEmail);

            await Task.WhenAll(allCompaniesCountTask, allAgenciesCountTask, AllUsersCountTask);

            var data = new DashboardData();
            data.FirstBlockName = "Companies";
            data.FirstBlockCount = await allCompaniesCountTask;
            data.FirstBlockUrl = "/ClientCompany/Companies";

            data.SecondBlockName = "Agencies";
            data.SecondBlockCount = await allAgenciesCountTask;
            data.SecondBlockUrl = "/ClientCompany/Agencies";

            data.ThirdBlockName = "Users";
            data.ThirdBlockCount = await AllUsersCountTask;
            data.ThirdBlockUrl = "/User";

            return data;
        }
    }
}