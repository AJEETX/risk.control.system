using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class UsersViewModel
    {
        public string UserId { get; set; }
        public string ProfileImage { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public long? PinCodeId { get; set; }
        public string? PinCode { get; set; }
        public long? StateId { get; set; }
        public string? State { get; set; }
        public long? DistrictId { get; set; }
        public string? District { get; set; }
        public string? Country { get; set; }
        public long? CountryId { get; set; }
        public string Email { get; set; }
        public long VendorId { get; set; }
        public string VendorName { get; set; }
        public long CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string PhoneNumber { get; set; }
        public string Addressline { get; set; }
        public bool Active { get; set; }
        public IEnumerable<string> Roles { get; set; }

        [Display(Name = "Profile")]
        [NotMapped]
        public IFormFile? Profile { get; set; }

        [Display(Name = "Profile")]
        public byte[]? ProfileImageInByte { get; set; } = default!;
    }

    public class VendorUsersViewModel
    {
        [Display(Name = "Agency name")]
        public Vendor Vendor { get; set; }

        public List<UsersViewModel> Users { get; set; } = new();
    }

    public class CompanyUsersViewModel
    {
        [Display(Name = "Insurer name")]
        public ClientCompany Company { get; set; }

        public List<UsersViewModel> Users { get; set; } = new();
    }
}