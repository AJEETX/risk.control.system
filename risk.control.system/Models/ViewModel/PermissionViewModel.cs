namespace risk.control.system.Models.ViewModel
{
    public class PermissionsViewModel
    {
        public string RoleName { get; set; }
        public string RoleId { get; set; }
        public IList<PermissionViewModel> PermissionViewModels { get; set; }

    }
    public class PermissionViewModel
    {
        public string RoleName { get; set; }
        public string RoleId { get; set; }
        public IList<RoleClaimsViewModel> RoleClaims { get; set; }
    }

    public class RoleClaimsViewModel
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public bool Selected { get; set; }
    }
}
