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

        public bool SetPassword { get; set; }

        public string? LoginError { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [DisplayName("Email")]
        public string Email { get; set; }
        [Required]
        public string CountryId { get; set; }
        [Required]
        public string Mobile { get; set; }
        public string? ResetError { get; set; }
    }
    public class ForgotPasswordResult
    {
        public string CountryCode { get; set; }
        public byte[] ProfilePicture { get; set; }
    }
    public class OtpLoginModel
    {
        public string CountryIsd { get; set; }
        public string MobileNumber { get; set; }
        public string? UserEnteredOtp { get; set; }
        public string? LoginError { get; set; }
    }
}