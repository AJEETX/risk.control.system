using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Models
{
    public class Vendor : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long VendorId { get; set; }

        [Display(Name = "Agency name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Agency code")]
        public string? Code { get; set; } = string.Empty;

        [Display(Name = "Agency detail")]
        public string? Description { get; set; } = string.Empty;

        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string? Branch { get; set; } = string.Empty;
        public string Addressline { get; set; } = string.Empty;
        public List<VendorInvestigationServiceType>? VendorInvestigationServiceTypes { get; set; } = default!;

        [Display(Name = "State name")]
        public long? StateId { get; set; } = default!;

        public State? State { get; set; } = default!;

        [Display(Name = "Country name")]
        public long? CountryId { get; set; } = default!;

        public Country? Country { get; set; } = default!;

        [Display(Name = "Pincode")]
        public long? PinCodeId { get; set; } = default!;

        public PinCode? PinCode { get; set; } = default!;

        [Display(Name = "District")]
        public long? DistrictId { get; set; } = default!;

        [Display(Name = "District")]
        public District? District { get; set; } = default!;
        [NotMapped]
        public long SelectedPincodeId { get; set; }
        [NotMapped]
        public long SelectedDistrictId { get; set; }
        [NotMapped]
        public long SelectedStateId { get; set; }
        [NotMapped]
        public long SelectedCountryId { get; set; }

        [Display(Name = "Bank Name")]
        public string? BankName { get; set; } = default!;

        [Display(Name = "Bank Account Number")]
        public string? BankAccountNumber { get; set; } = default!;

        [Display(Name = "IFSC code")]
        public string? IFSCCode { get; set; } = default!;

        [Display(Name = "Agreement date")]
        [DataType(DataType.Date)]
        public DateTime? AgreementDate { get; set; } = DateTime.Now;

        [Display(Name = "Activated date")]
        [DataType(DataType.Date)]
        public DateTime? ActivatedDate { get; set; } = DateTime.Now;

        public VendorStatus? Status { get; set; } = VendorStatus.ACTIVE;
        public Domain? DomainName { get; set; } = Domain.com;

        [Display(Name = "Document")]
        public string? DocumentUrl { get; set; } = default!;

        [Display(Name = "Document")]
        [NotMapped]
        public IFormFile? Document { get; set; }

        [Display(Name = "Document url")]
        public byte[]? DocumentImage { get; set; } = default!;
        public string? AddressMapLocation { get; set; }
        public string? AddressLatitude { get; set; }
        public string? AddressLongitude { get; set; }

        [Display(Name = "Vendor Users")]
        public List<VendorApplicationUser>? VendorApplicationUser { get; set; }

        [Display(Name = "Insurer names")]
        public List<ClientCompany>? Clients { get; set; } = new List<ClientCompany>();

        [Display(Name = "Empanel")]
        [NotMapped]
        public bool SelectedByCompany { get; set; }

        public bool Deleted { get; set; } = false;

        [RegularExpression(@"^[A-Z]+[a-zA-Z''-'\s]*$")]
        [StringLength(5)]
        public string? Rating { get; set; }

        public int? RateCount
        {
            get { return ratings != null ? ratings.Count : 1; }
        }

        public int? RateTotal
        {
            get
            {
                return (ratings != null ? ratings.Sum(m => m.Rate) : 1);
            }
        }

        public virtual ICollection<AgencyRating>? ratings { get; set; }
        public string? MobileAppUrl { get; set; } = Applicationsettings.APP_URL;
        public bool CanChangePassword { get; set; } = false;
        public bool HasClaims { get; set; } = false;
        public bool EnableMailbox { get; set; } = false;

        public override string ToString()
        {
            return $"Investigation Agency Information:\n" +
           $"- Domain (web) name: {DomainName}\n" +
           $"- Name: {Name}\n" +
           $"- Code: {Code}\n" +
           $"- Description: {Description}\n" +
           $"- Phone Number: ${PhoneNumber}\n" +
           $"- Domain name: {Email}\n" +
           $"- Branch name: {Branch}\n" +
           $"- Address line: {Addressline}\n" +
           $"- City: {District}\n" +
           $"- State: {State}\n" +
           $"- Country: {Country}\n" +
           $"- Investigation Service Types: {VendorInvestigationServiceTypes}\n" +
           $"- Bank Name: {BankName}\n" +
           $"- Bank Account Number: {BankAccountNumber}\n" +
           $"- IFSC Code: {IFSCCode}\n" +
           $"- Agreement Date: {AgreementDate}\n" +
           $"- Activated Date: {ActivatedDate}\n" +
           $"- Status: {Status.GetEnumDisplayName()}";
        }
    }

    public enum VendorStatus
    {
        ACTIVE,
        INACTIVE,
        DELIST
    }
}