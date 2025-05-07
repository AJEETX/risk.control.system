using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Vision.V1;
using risk.control.system.Helpers;

namespace risk.control.system.Models
{
    public class CustomerDetail : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CustomerDetailId { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public Gender? Gender { get; set; }

        [Required]
        [Display(Name = "Contact number")]
        [DataType(DataType.PhoneNumber)]
        public string ContactNumber { get; set; }

        [Required]
        [Display(Name = "Address line")]
        public string Addressline { get; set; }

        [Display(Name = "PinCode")]
        public long? PinCodeId { get; set; } = default!;

        [Display(Name = "PinCode")]
        public PinCode? PinCode { get; set; } = default!;

        [Display(Name = "State")]
        public long? StateId { get; set; } = default!;

        [Display(Name = "State")]
        public State? State { get; set; } = default!;

        [Display(Name = "Country")]
        public long? CountryId { get; set; } = default!;

        [Display(Name = "Country")]
        public Country? Country { get; set; } = default!;

        [Display(Name = "District")]
        public long? DistrictId { get; set; } = default!;

        [Display(Name = "District")]
        public District? District { get; set; } = default!;
        public long? InvestigationTaskId { get; set; }
        public InvestigationTask? InvestigationTask { get; set; }
        [NotMapped]
        public long SelectedPincodeId { get; set; }
        [NotMapped]
        public long SelectedDistrictId { get; set; }
        [NotMapped]
        public long SelectedStateId { get; set; }
        [NotMapped]
        public long SelectedCountryId { get; set; }

        [NotMapped]
        public CREATEDBY CREATEDBY { get; set; } = CREATEDBY.MANUAL;

        [Display(Name = "Customer type")]
        public CustomerType? CustomerType { get; set; }

        [Required]
        [Display(Name = "Annual Income")]
        public Income? Income { get; set; }

        [Required]
        [Display(Name = "Occupation")]
        public Occupation? Occupation { get; set; }

        [Required]
        [Display(Name = "Education")]
        public Education? Education { get; set; }

        [FileExtensions(Extensions = "jpg,jpeg,png")]
        public string? ProfilePictureUrl { get; set; }

        public byte[]? ProfilePicture { get; set; }
        public string? ProfilePictureExtension { get; set; }

        [Display(Name = "Image")]
        [NotMapped]
        public IFormFile? ProfileImage { get; set; }

        public string? Description { get; set; }
        public string? CustomerLocationMap { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }

        public string? AddressLocationInfo { get; set; }

        public override string ToString()
        {
            return $"Customer Information:\n" +
           $"- Name: {Name}\n" +
           $"- Date of birth: {DateOfBirth}\n" +
           $"- Gender: ${Gender}\n" +
           $"- Address line: {Addressline}\n" +
           $"- City: {District}\n" +
           $"- State: {State}\n" +
           $"- Country: {Country}\n" +
           $"- Contact Number: {ContactNumber}\n" +
           $"- Income: {Income}\n" +
           $"- Occupation: {Occupation.GetEnumDisplayName}\n" +
           $"- Education: {Education.GetEnumDisplayName}\n";
        }
    }
}