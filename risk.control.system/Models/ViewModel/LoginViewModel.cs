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
        public bool OtpLogin { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

}