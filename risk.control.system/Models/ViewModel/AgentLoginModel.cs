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
    }
}