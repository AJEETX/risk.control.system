using risk.control.system.AppConstant;

namespace risk.control.system.Models.ViewModel
{
    public class CompanyUserRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public long CompanyId { get; set; }
        public string Company { get; set; }
        public CompanyRole? UserRole { get; set; }
        public List<CompanyUserRoleViewModel> CompanyUserRoleViewModel { get; set; }
    }

    public class CompanyUserRoleViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool Selected { get; set; }
    }
}