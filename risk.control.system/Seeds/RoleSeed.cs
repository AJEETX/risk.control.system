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
            PORTAL_ADMIN.DISPLAY_NAME,
            COMPANY_ADMIN.DISPLAY_NAME,
            CREATOR.DISPLAY_NAME,
            ASSESSOR.DISPLAY_NAME,
            MANAGER.DISPLAY_NAME,
            AGENCY_ADMIN.DISPLAY_NAME,
            SUPERVISOR.DISPLAY_NAME,
            AGENT.DISPLAY_NAME,
                GUEST.DISPLAY_NAME
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
