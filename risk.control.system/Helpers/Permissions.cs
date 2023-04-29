using risk.control.system.Seeds;
using static risk.control.system.Helpers.Permissions;

namespace risk.control.system.Helpers
{
    public static class Permissions
    {
        public static List<string> GeneratePermissionsForModule(string module)
        {
            return new List<string>()
            {
                ModuleManager.GetModule(module, ApplicationOption.CREATE),
                ModuleManager.GetModule(module, ApplicationOption.VIEW),
                ModuleManager.GetModule(module, ApplicationOption.EDIT),
                ModuleManager.GetModule(module, ApplicationOption.DELETE),
            };
        }

        public static class Products
        {
            public static string View = ModuleManager.GetModule(nameof(Products), ApplicationOption.VIEW);
            public static string Create = ModuleManager.GetModule(nameof(Products), ApplicationOption.CREATE);
            public static string Edit = ModuleManager.GetModule(nameof(Products), ApplicationOption.EDIT);
            public static string Delete = ModuleManager.GetModule(nameof(Products), ApplicationOption.DELETE);
        }

        public static class Claims
        {
            public static string View = ModuleManager.GetModule(nameof(Claims),ApplicationOption.VIEW);
            public static string Create = ModuleManager.GetModule(nameof(Claims), ApplicationOption.CREATE);
            public static string Edit = ModuleManager.GetModule(nameof(Claims), ApplicationOption.EDIT);
            public static string Delete = ModuleManager.GetModule(nameof(Claims), ApplicationOption.DELETE);
        }

    }
    public static class ModuleManager
    {
        public static string GetModule(string module, string action)
        {
            return $"{ApplicationOption.PERMISSION}.{module}.{action}";
        }
    }
}
