using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public partial class ApplicationUser : IdentityUser<Guid>
    {
        [FileExtensions(Extensions = "jpg,jpeg,png")]
        public string? ProfilePictureUrl { get; set; }
        public bool isSuperAdmin { get; set; } = false;
        public byte[]? ProfilePicture { get; set; }
        [Display(Name = "Image")]
        [NotMapped] 
        public IFormFile? ProfileImage { get; set; }
        [Required]
        public string FirstName { get; set; } = default!;
        [Required]
        public string LastName { get; set; } = default!;
        [Required]
        [Display(Name = "PinCode name")]
        public string PinCodeId { get; set; } = default!;
        [Display(Name = "PinCode name")]
        public PinCode PinCode { get; set; } = default!;
        [Required]
        [Display(Name = "State name")]
        public string StateId { get; set; } = default!;
        [Display(Name = "State name")]
        public State State { get; set; } = default!;
        [Required]
        [Display(Name = "Country name")]
        public string CountryId { get; set; } = default!;
        [Display(Name = "Country name")]
        public Country Country { get; set; } = default!;
        [Display(Name = "District")]
        public string? DistrictId { get; set; } = default!;
        [Display(Name = "District")]
        public District? District { get; set; } = default!;
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? Updated { get; set; }
        public string? UpdatedBy { get; set; } = default!;
        [Required]
        public string? Password { get; set; }
    }
    public class ApplicationRole : IdentityRole<Guid>
    {
        public ApplicationRole(string code, string name)
        {
            Name = name;
            Code = code;
        }
        public string Code { get; set; }
    }
}
