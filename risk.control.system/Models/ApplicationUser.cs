using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;

namespace risk.control.system.Models
{
    public partial class ApplicationUser : IdentityUser<long>
    {
        public bool IsPasswordChangeRequired { get; set; } = true;
        [FileExtensions(Extensions = "jpg,jpeg,png")]
        public string? ProfilePictureUrl { get; set; }

        public bool IsSuperAdmin { get; set; } = false;
        public bool IsClientManager { get; set; } = false;
        public bool IsClientAdmin { get; set; } = false;
        public bool IsVendorAdmin { get; set; } = false;
        public byte[]? ProfilePicture { get; set; }
        public string? ProfilePictureExtension { get; set; }

        [Display(Name = "Image")]
        [NotMapped]
        public IFormFile? ProfileImage { get; set; }

        [Required]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = default!;

        [Required]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = default!;

        [Display(Name = "PinCode name")]
        public long? PinCodeId { get; set; } = default!;

        [Display(Name = "PinCode name")]
        public PinCode? PinCode { get; set; } = default!;

        [Display(Name = "State name")]
        public long? StateId { get; set; } = default!;

        [Display(Name = "State name")]
        public State? State { get; set; } = default!;

        [Display(Name = "Country name")]
        public long? CountryId { get; set; } = default!;

        [Display(Name = "Country name")]
        public Country? Country { get; set; } = default!;

        [Display(Name = "District")]
        public long? DistrictId { get; set; } = default!;

        [Display(Name = "District")]
        public District? District { get; set; } = default!;

        [Display(Name = "Address line")]
        public string? Addressline { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? Updated { get; set; }
        public string? UpdatedBy { get; set; } = "--";

        public string? Password { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = false;

        public bool Deleted { get; set; } = false;

        public string? SecretPin { get; set; }
        public string? MobileUId { get; set; }

        public AppRoles? Role { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public bool HasClaims { get; set; } = false;
        public string? AddressLatitude { get; set; }
        public string? AddressLongitude { get; set; }
        public string? AddressMapLocation { get; set; }
        public bool IsUpdated { get; set; } = true;

        [NotMapped]
        public long SelectedPincodeId { get; set; }
        [NotMapped]
        public long SelectedDistrictId { get; set; }
        [NotMapped]
        public long SelectedStateId { get; set; }
        [NotMapped]
        public long SelectedCountryId { get; set; }

        public override string ToString()
        {
            return $"User Information:\n" +
                $"- First Name: {FirstName}\n" +
                $"- Last Name: {LastName}\n" +
                $"- PinCode: {PinCode}\n" +
                $"- State: {State}\n" +
                $"- Country: {Country}\n" +
                $"- District: {District}\n" +
                $"- Address Line: {Addressline}\n" +
                $"- Created: {Created}\n" +
                $"- Updated: {Updated}\n" +
                $"- Updated By: {UpdatedBy}\n" +
                $"- Active: {Active}\n" +
                $"- Deleted: {Deleted}\n" +
                $"- Role: {Role}\n" +
                $"- Last Activity Date: {LastActivityDate}\n" +
                $"- Has Claims: {HasClaims}";
        }

    }

    public class ApplicationRole : IdentityRole<long>
    {
        public ApplicationRole()
        {
        }

        public ApplicationRole(string code, string name)
        {
            Name = name;
            Code = code;
        }

        [StringLength(20)]
        public string Code { get; set; }

        public long? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }
    }
}