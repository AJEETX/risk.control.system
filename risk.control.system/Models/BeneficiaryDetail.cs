using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class BeneficiaryDetail : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BeneficiaryDetailId { get; set; }

        [Display(Name = "Beneficiary name")]
        public string Name { get; set; }

        [Display(Name = "Relation")]
        public long BeneficiaryRelationId { get; set; }

        [Display(Name = "Relation")]
        public BeneficiaryRelation? BeneficiaryRelation { get; set; }

        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Phone")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Annual Income")]
        public Income? Income { get; set; }

        public string? ImagePath { get; set; }

        public string? ProfilePictureExtension { get; set; }

        [Display(Name = "Photo")]
        [NotMapped]
        public IFormFile? ProfileImage { get; set; }

        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Country name")]
        public long? CountryId { get; set; } = default!;

        public Country? Country { get; set; } = default!;

        [Display(Name = "State")]
        public long? StateId { get; set; } = default!;

        public State? State { get; set; } = default!;

        [Display(Name = "District")]
        public long? DistrictId { get; set; } = default!;

        public District? District { get; set; } = default!;

        [Display(Name = "PinCode")]
        public long? PinCodeId { get; set; } = default!;

        [Display(Name = "PinCode")]
        public PinCode? PinCode { get; set; } = default!;

        [Display(Name = "Address")]
        public string Addressline { get; set; }


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
        public string? BeneficiaryLocationMap { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }

        public string? AddressLocationInfo { get; set; }

        public long? InvestigationTaskId { get; set; }
        public InvestigationTask? InvestigationTask { get; set; }

        public override string ToString()
        {
            return $"Beneficiary Information:\n" +
           $"- Name: {Name}\n" +
           $"- Date of birth: {DateOfBirth}\n" +
           $"- Relation: ${BeneficiaryRelation}\n" +
           $"- Address line: {Addressline}\n" +
           $"- City: {District}\n" +
           $"- State: {State}\n" +
           $"- Country: {Country}\n" +
           $"- Contact number: {PhoneNumber}\n" +
           $"- Income: {Income}";
        }
    }
}