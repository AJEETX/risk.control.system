using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ClientCompanyApplicationUser : ApplicationUser
    {
        [Display(Name = "Insurer name")]
        public string? ClientCompanyId { get; set; } = default!;
        [Display(Name = "Insurer name")]
        public ClientCompany? ClientCompany { get; set; } = default!;
        public string? Comments { get; set; } = default!;
    }
}
