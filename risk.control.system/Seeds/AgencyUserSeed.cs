using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Seeds
{
    public static class AgencyUserSeed
    {
        public static async Task Seed(ApplicationDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager,
            Vendor vendor, ICustomApiClient customApiCLient, IFileStorageService fileStorageService)
        {
            var pinCode = await context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(p => p.PinCodeId == vendor.PinCodeId);
            await AgencyAdminSeed.Seed(context, env, userManager, vendor, pinCode!, fileStorageService);
            await SupervisorSeed.Seed(context, SUPERVISOR.CODE, env, userManager, vendor, pinCode!, SUPERVISOR.PROFILE_IMAGE, SUPERVISOR.FIRST_NAME, SUPERVISOR.LAST_NAME, fileStorageService);
            string agentEmailwithSuffix = AGENT.CODE + "@" + vendor.Email;
            await AgentSeed.Seed(context, agentEmailwithSuffix, env, customApiCLient, userManager, vendor, pinCode!, AGENT.PROFILE_IMAGE, AGENT.FIRST_NAME, AGENT.LAST_NAME, fileStorageService, "110 Mahoneys Road");

            //if (!System.Diagnostics.Debugger.IsAttached)
            //{
            //    string agent2EmailwithSuffix = AGENTX.CODE + "@" + vendor.Email;
            //    await AgentSeed.Seed(context, agent2EmailwithSuffix, webHostEnvironment, customApiCLient, userManager, vendor, vendor.PinCode.Code, AGENTX.PROFILE_IMAGE, AGENTX.FIRST_NAME, AGENTX.LAST_NAME, "44 Waverley Road");

            //    string agent3EmailwithSuffix = AGENTY.CODE + "@" + vendor.Email;
            //    await AgentSeed.Seed(context, agent3EmailwithSuffix, webHostEnvironment, customApiCLient, userManager, vendor, vendor.PinCode.Code, AGENTY.PROFILE_IMAGE, AGENTY.FIRST_NAME, AGENTX.LAST_NAME, "44 Waverley Road");

            //    string agent4EmailwithSuffix = AGENTZ.CODE + "@" + vendor.Email;
            //    await AgentSeed.Seed(context, agent4EmailwithSuffix, webHostEnvironment, customApiCLient, userManager, vendor, vendor.PinCode.Code, AGENTZ.PROFILE_IMAGE, AGENTZ.FIRST_NAME, AGENTZ.LAST_NAME, "44 Waverley Road");
            //}
        }
    }
}