using Microsoft.Identity.Client;
using risk.control.system.Seeds;

namespace risk.control.system.Helpers
{
    public static class Permissions
    {
        public static List<string> GeneratePermissionsForModule(string module)
        {
            return new List<string>()
            {
                ModuleManager.GetModule(module, Applicationsettings.CREATE),
                ModuleManager.GetModule(module, Applicationsettings.VIEW),
                ModuleManager.GetModule(module, Applicationsettings.EDIT),
                ModuleManager.GetModule(module, Applicationsettings.DELETE),
            };
        }

        public static class Products
        {
            public static string View = ModuleManager.GetModule(nameof(Products), Applicationsettings.VIEW);
            public static string Create = ModuleManager.GetModule(nameof(Products), Applicationsettings.CREATE);
            public static string Edit = ModuleManager.GetModule(nameof(Products), Applicationsettings.EDIT);
            public static string Delete = ModuleManager.GetModule(nameof(Products), Applicationsettings.DELETE);
        }
        public static class ModuleManager
        {
            public static string GetModule(string module, string action)
            {
                return $"{Applicationsettings.PERMISSION}.{module}.{action}";
            }
        }
    }
}
