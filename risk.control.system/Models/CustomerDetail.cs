using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class CustomerDetail : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string CustomerDetailId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Display(Name = "Name")]
        public string CustomerName { get; set; }

        [Required]
        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        public DateTime CustomerDateOfBirth { get; set; }

        public Gender? Gender { get; set; }

        [Required]
        [Display(Name = "Contact number")]
        [DataType(DataType.PhoneNumber)]
        public long ContactNumber { get; set; }

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

        [Required]
        [Display(Name = "Customer type")]
        public CustomerType? CustomerType { get; set; }

        [Required]
        [Display(Name = "Income")]
        public Income? CustomerIncome { get; set; }

        [Required]
        [Display(Name = "Occupation")]
        public Occupation? CustomerOccupation { get; set; }

        [Required]
        [Display(Name = "Education")]
        public Education? CustomerEducation { get; set; }

        [FileExtensions(Extensions = "jpg,jpeg,png")]
        public string? ProfilePictureUrl { get; set; }

        public byte[]? ProfilePicture { get; set; }

        [Display(Name = "Image")]
        [NotMapped]
        public IFormFile? ProfileImage { get; set; }

        public string? Description { get; set; }
        public string? CustomerLocationMap { get; set; }
    }
}