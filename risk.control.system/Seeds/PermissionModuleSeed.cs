using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Helpers;

using static risk.control.system.Helpers.Permissions;

namespace risk.control.system.Seeds
{
    public static class PermissionModuleSeed
    {
        private static List<string> modules = new List<string> { "Mailbox", "Claims", "Setting", "General" };

        public static void SeedMailbox(ApplicationDbContext context)
        {
            var permissionTypes = Permissions.GetPermissionTypes();
            //ModuleName = "Mailbox"
            string Mailbox = "Mailbox";

            var mailboxPermissionTypes = permissionTypes;
            foreach (var permissionType in mailboxPermissionTypes)
            {
                var hydratedType = ModuleManager.GetModule(Mailbox, permissionType.Name);
                if (hydratedType != null)
                {
                    permissionType.Name = hydratedType;
                }
            }
            context.PermissionType.AddRange(mailboxPermissionTypes);

            var mailboxPermissionModule = new PermissionModule
            {
                ModuleName = Mailbox,
                PermissionTypes = mailboxPermissionTypes,
            };

            context.PermissionModule.Add(mailboxPermissionModule);
        }

        public static void SeedClaim(ApplicationDbContext context)
        {
            var claimsSubPermissionTypes = Permissions.GetPermissionTypes();

            string Claims = "Claims";

            var ClaimSubModuleName = "New";

            foreach (var permissionType in claimsSubPermissionTypes)
            {
                var hydratedType = ModuleManager.GetModule(ClaimSubModuleName, permissionType.Name);
                if (hydratedType != null)
                {
                    permissionType.Name = hydratedType;
                }
            }

            var claimSubModuleNew = new PermissionSubModule
            {
                SubModuleName = ClaimSubModuleName,
                PermissionTypes = claimsSubPermissionTypes
            };

            var claimsNewPermissionTypes = Permissions.GetPermissionTypes();

            foreach (var permissionType in claimsNewPermissionTypes)
            {
                var hydratedType = ModuleManager.GetModule(Claims, permissionType.Name);
                if (hydratedType != null)
                {
                    permissionType.Name = hydratedType;
                }
            }

            var claimsPermissionModule = new PermissionModule
            {
                ModuleName = Claims,
                PermissionTypes = claimsNewPermissionTypes,
                Sections = new List<PermissionSubModule> { claimSubModuleNew }
            };

            context.PermissionModule.Add(claimsPermissionModule);
        }
    }
}