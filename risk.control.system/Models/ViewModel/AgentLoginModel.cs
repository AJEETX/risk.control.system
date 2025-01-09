using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class AgentLoginModel
    {
        [Required]
        [EmailAddress]
        [DisplayName("Email")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [DisplayName("Password")]
        public string Password { get; set; }
        public string? Role { get; set; }

        [Display(Name = "123.123,234.234")]
        public string? Latlong { get; set; } = "-37.8397238,145.1653465";
    }
}