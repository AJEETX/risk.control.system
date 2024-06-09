using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [DisplayName("Email")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [DisplayName("Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        public bool ShowUserOnLogin { get; set; }

        public string? Error { get; set; }
        public string? Mobile { get; set; }
        public List<string>? Users { get; set; }
    }
}