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
        public string BeneficiaryName { get; set; }

        [Display(Name = "BRelation")]
        public long BeneficiaryRelationId { get; set; }

        [Display(Name = "Relation")]
        public BeneficiaryRelation BeneficiaryRelation { get; set; }

        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Phone")]
        public long BeneficiaryContactNumber { get; set; }

        [Display(Name = "Income")]
        public Income? BeneficiaryIncome { get; set; }

        [FileExtensions(Extensions = "jpg,jpeg,png")]
        [Display(Name = "Beneficiary Photo")]
        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Photo")]
        public byte[]? ProfilePicture { get; set; }

        [Display(Name = "Photo")]
        [NotMapped]
        public IFormFile? ProfileImage { get; set; }

        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        public DateTime BeneficiaryDateOfBirth { get; set; }

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

        public string? BeneficiaryLocationMap { get; set; }
        public List<PreviousClaimReport> PreviousClaimReports { get; set; } = new List<PreviousClaimReport>();
        public string? ClaimsInvestigationId { get; set; }
        public ClaimsInvestigation? ClaimsInvestigation { get; set; }
    }
}