using risk.control.system.AppConstant;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ClientCompanyApplicationUser : ApplicationUser
    {
        [Display(Name = "Insurer name")]
        public long? ClientCompanyId { get; set; } = default!;

        [Display(Name = "Insurer name")]
        public ClientCompany? ClientCompany { get; set; } = default!;
        public CompanyRole? UserRole { get; set; }

        public string? Comments { get; set; } = default!;
    }
}