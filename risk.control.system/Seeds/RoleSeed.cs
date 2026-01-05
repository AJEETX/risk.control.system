using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager)
        {
            string[] roles =
            {
            AppRoles.PORTAL_ADMIN.ToString(),
            AppRoles.COMPANY_ADMIN.ToString(),
            AppRoles.CREATOR.ToString(),
            AppRoles.ASSESSOR.ToString(),
            AppRoles.MANAGER.ToString(),
            AppRoles.AGENCY_ADMIN.ToString(),
            AppRoles.SUPERVISOR.ToString(),
            AppRoles.AGENT.ToString(),
            AppRoles.GUEST.ToString()
        };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = role,
                        Code = role.Substring(0, 2)
                    });
                }
            }
        }
    }

}
